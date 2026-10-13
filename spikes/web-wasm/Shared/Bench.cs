using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MeshEditor.LayerManager;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Filters;
using MeshEditor.LayerManager.Import;
using MeshEditor.LayerManager.Storage;

public class MemoryStorage : IStorageService
{
	public readonly Dictionary<string, byte[]> Records = new();
	static string Norm(string r) => r.Replace('\\', '/').TrimStart('/');
	public Stream Load(string record) => Records.TryGetValue(Norm(record), out var b) ? new MemoryStream(b, false) : throw new FileNotFoundException(record);
	public Stream Save(string record) => new CommitStream(this, Norm(record));
	public void Delete(string record) => Records.Remove(Norm(record));
	public void DeleteDirectory(string name) { foreach (var k in Records.Keys.Where(k => k.StartsWith(Norm(name) + "/")).ToList()) Records.Remove(k); }
	public long TotalBytes => Records.Values.Sum(v => (long)v.Length);
	sealed class CommitStream : MemoryStream
	{
		readonly MemoryStorage s; readonly string r;
		public CommitStream(MemoryStorage s, string r) { this.s = s; this.r = r; }
		protected override void Dispose(bool disposing) { if (disposing) s.Records[r] = ToArray(); base.Dispose(disposing); }
	}
}

public static class Bench
{
	public static void Run(MemoryStorage import, string mesh, string res, Action<string> log)
	{
		var layers = new MemoryStorage();
		var none = Enumerable.Empty<decimal>();
		var sw = Stopwatch.StartNew();
		void step(string name, Func<string> a)
		{
			sw.Restart();
			try { var info = a(); log($"{name}: {sw.ElapsedMilliseconds} ms  {info}"); }
			catch (Exception ex) { log($"{name}: FAILED {ex.GetType().Name}: {ex.Message}"); }
		}
		var result = new AnalysisResult { MeshRecordNames = new[] { mesh }, DataRecordNames = new[] { res } };
		var gen = new LayerGenerator(layers, layers, compressionService: CompressionServiceFactory.Create(Array.Empty<string>(), null));
		Guid master = Guid.Empty;
		step("Import master layer", () =>
		{
			var s = gen.GenerateMasterLayer("master", new[] { AnalysisResultImportServiceFactory.Create(import, result, "Nearest") }, none);
			master = s.Id;
			return $"fields={string.Join(",", s.Fields.Select(f => f.Key + "[" + string.Join("|", f.Value.Components.Select(c => c.Key + ":" + string.Join("/", c.Value.TimeSteps.Keys.Take(3)))) + "]"))} store={layers.TotalBytes / 1024} KiB";
		});
		Guid surface = Guid.Empty;
		step("Surface filter", () => { var s = gen.GenerateFilterLayer(master, FilterFactory.Create(FilterType.Surface, new[] { "45" }), "surface", none); surface = s.Id; var g = gen.LoadGeometry(s.Id, s.MeshFallbackLayerId, s.Meshes[0].Index); return $"points={g.NumberOfPoints} cells={g.NumberOfCells}"; });
		step("Slice filter", () => { var s = gen.GenerateFilterLayer(master, FilterFactory.Create(FilterType.Slice, new[] { "0", "0", "1", "1" }), "slice", none); var g = gen.LoadGeometry(s.Id, s.MeshFallbackLayerId, s.Meshes[0].Index); return $"points={g.NumberOfPoints} cells={g.NumberOfCells}"; });
		step("Iso-surface filter", () => { var s = gen.GenerateFilterLayer(master, FilterFactory.Create(FilterType.IsoSurface, new[] { "Temperature", "value", "40" }), "iso", none); var g = gen.LoadGeometry(s.Id, s.MeshFallbackLayerId, s.Meshes[0].Index); return $"points={g.NumberOfPoints} cells={g.NumberOfCells}"; });
		step("Deformation filter on surface", () => { var s = gen.GenerateFilterLayer(surface, FilterFactory.Create(FilterType.Deformation, new[] { "Displacement", "1" }), "deformed", none); return "ok"; });
		step("SVD compression of surface", () =>
		{
			var g2 = new LayerGenerator(layers, layers, compressionService: CompressionServiceFactory.Create(new[] { "SVD" }, null));
			var s = g2.CompressLayer(surface, none, "svd"); return $"store={layers.TotalBytes / 1024} KiB";
		});
		log($"Managed memory: {GC.GetTotalMemory(false) / (1024 * 1024)} MiB");
	}
}
