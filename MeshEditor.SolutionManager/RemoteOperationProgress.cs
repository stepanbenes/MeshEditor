namespace MeshEditor.SolutionManager
{
	/// <summary>
	/// Progress of an upload to or a deletion from the remote solution storage.
	/// </summary>
	public readonly struct RemoteOperationProgress
	{
		public RemoteOperationProgress(string message, int percentDone)
		{
			Message = message;
			PercentDone = percentDone;
		}

		/// <summary>
		/// Current step, e.g. the blob being uploaded. The same step is reported repeatedly while its percentage grows.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// 0 to 100, or -1 when the amount of work is not known yet.
		/// </summary>
		public int PercentDone { get; }

		public override string ToString() => PercentDone >= 0 ? $"{Message} ({PercentDone}%)" : Message;
	}
}
