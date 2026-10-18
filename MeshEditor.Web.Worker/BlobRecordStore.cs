using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MeshEditor.LayerManager.Storage;

namespace MeshEditor.Web.Worker
{
	/// <summary>
	/// Records of one folder of a blob container, held in memory. The layer generator reads and writes records synchronously,
	/// which a web browser can't do over the network: the records are downloaded before (<see cref="PrefetchAsync"/>) and
	/// the written ones are uploaded after (<see cref="FlushAsync"/>). Record "a/b.json" is blob "{folder}/a/b.json".
	/// </summary>
	sealed class BlobRecordStore : IStorageService
	{
		private const int ParallelRequests = 8;

		private readonly BlobContainerClient container;
		private readonly string folder;
		private readonly Dictionary<string, byte[]> records = new Dictionary<string, byte[]>();
		private readonly Dictionary<string, byte[]> written = new Dictionary<string, byte[]>();
		private readonly HashSet<string> prefetchedPrefixes = new HashSet<string>();

		public BlobRecordStore(BlobContainerClient container, string folder)
		{
			this.container = container ?? throw new ArgumentNullException(nameof(container));
			this.folder = string.IsNullOrWhiteSpace(folder) ? throw new ArgumentException("Folder is not specified", nameof(folder)) : folder.TrimEnd('/');
		}

		public long CachedBytes => records.Values.Sum(r => (long)r.Length);

		public IReadOnlyCollection<string> WrittenRecords => written.Keys;

		/// <summary>
		/// Downloads the record unless it is in memory. Returns false when the blob does not exist.
		/// </summary>
		public async Task<bool> PrefetchAsync(string record, CancellationToken cancellationToken)
		{
			record = normalize(record);
			if (records.ContainsKey(record))
				return true;
			try
			{
				var response = await container.GetBlobClient(toBlobName(record)).DownloadContentAsync(cancellationToken).ConfigureAwait(false);
				records[record] = response.Value.Content.ToArray();
				return true;
			}
			catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
			{
				return false;
			}
		}

		/// <summary>
		/// Downloads all records whose names start with the prefix (e.g. all records of a layer: "{layerId}/").
		/// The progress is reported by downloaded records.
		/// </summary>
		public async Task<int> PrefetchFolderAsync(string recordPrefix, Action<int, int> progress, CancellationToken cancellationToken)
		{
			recordPrefix = normalize(recordPrefix);
			if (prefetchedPrefixes.Contains(recordPrefix))
				return 0;

			var names = new List<string>();
			await foreach (var blob in container.GetBlobsAsync(BlobTraits.None, BlobStates.None, toBlobName(recordPrefix), cancellationToken).ConfigureAwait(false))
			{
				string record = blob.Name.Substring(folder.Length + 1);
				if (!records.ContainsKey(record))
					names.Add(record);
			}

			int done = 0;
			progress?.Invoke(0, names.Count);
			using (var throttle = new SemaphoreSlim(ParallelRequests))
			{
				await Task.WhenAll(names.Select(async record =>
				{
					await throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
					try
					{
						var response = await container.GetBlobClient(toBlobName(record)).DownloadContentAsync(cancellationToken).ConfigureAwait(false);
						records[record] = response.Value.Content.ToArray();
						progress?.Invoke(++done, names.Count);
					}
					finally
					{
						throttle.Release();
					}
				})).ConfigureAwait(false);
			}
			prefetchedPrefixes.Add(recordPrefix);
			return names.Count;
		}

		/// <summary>
		/// Uploads the records written since the last flush. The progress is reported by uploaded bytes (done, total).
		/// Returns the blob names, so that a failed operation can delete them.
		/// </summary>
		public async Task<IReadOnlyList<string>> FlushAsync(Action<long, long> progress, CancellationToken cancellationToken)
		{
			var toUpload = written.ToList();
			long total = toUpload.Sum(r => (long)r.Value.Length), done = 0;
			var uploaded = new List<string>();
			progress?.Invoke(0, total);
			using (var throttle = new SemaphoreSlim(ParallelRequests))
			{
				await Task.WhenAll(toUpload.Select(async record =>
				{
					await throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
					try
					{
						string blobName = toBlobName(record.Key);
						var options = new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" } }; // overwrites
						await container.GetBlobClient(blobName).UploadAsync(BinaryData.FromBytes(record.Value), options, cancellationToken).ConfigureAwait(false);
						uploaded.Add(blobName);
						done += record.Value.Length;
						progress?.Invoke(done, total);
					}
					finally
					{
						throttle.Release();
					}
				})).ConfigureAwait(false);
			}
			written.Clear();
			return uploaded;
		}

		/// <summary>
		/// Deletes blobs of a failed operation. Not cancellable, failures are ignored.
		/// </summary>
		public async Task DeleteBlobsAsync(IEnumerable<string> blobNames)
		{
			foreach (string blobName in blobNames)
			{
				try
				{
					await container.GetBlobClient(blobName).DeleteIfExistsAsync().ConfigureAwait(false);
				}
				catch (Exception)
				{ }
			}
		}

		/// <summary>
		/// Blob names of the written records, before they are flushed.
		/// </summary>
		public IEnumerable<string> GetBlobNamesOfWrittenRecords() => written.Keys.Select(toBlobName);

		/// <summary>
		/// Adds a record that is already in memory (e.g. a file read by the browser).
		/// </summary>
		public void Put(string record, byte[] content) => records[normalize(record)] = content;

		/// <summary>
		/// Forgets the records of a folder (e.g. a deleted layer).
		/// </summary>
		public void Forget(string recordPrefix)
		{
			recordPrefix = normalize(recordPrefix);
			foreach (string record in records.Keys.Where(r => r.StartsWith(recordPrefix, StringComparison.Ordinal)).ToList())
				records.Remove(record);
			prefetchedPrefixes.RemoveWhere(p => p.StartsWith(recordPrefix, StringComparison.Ordinal));
		}

		#region IStorageService

		public Stream Load(string record)
		{
			record = normalize(record);
			if (records.TryGetValue(record, out byte[] content))
				return new MemoryStream(content, writable: false);
			throw new FileNotFoundException($"Record '{record}' was not downloaded.", record);
		}

		public Stream Save(string record) => new RecordStream(this, normalize(record));

		public void Delete(string record)
		{
			record = normalize(record);
			records.Remove(record);
			written.Remove(record);
		}

		public void DeleteDirectory(string name)
		{
			string prefix = normalize(name).TrimEnd('/') + "/";
			foreach (string record in records.Keys.Concat(written.Keys).Where(r => r.StartsWith(prefix, StringComparison.Ordinal)).ToList())
				Delete(record);
		}

		#endregion

		private string toBlobName(string record) => folder + "/" + record;

		private static string normalize(string record) => record.Replace('\\', '/').TrimStart('/');

		/// <summary>
		/// Written record: stored when the layer generator closes the stream.
		/// </summary>
		private sealed class RecordStream : MemoryStream
		{
			private readonly BlobRecordStore store;
			private readonly string record;

			public RecordStream(BlobRecordStore store, string record)
			{
				this.store = store;
				this.record = record;
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					byte[] content = ToArray();
					store.records[record] = content;
					store.written[record] = content;
				}
				base.Dispose(disposing);
			}
		}
	}
}
