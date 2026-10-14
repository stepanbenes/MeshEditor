using System;

namespace MeshEditor.SolutionManager
{
	/// <summary>
	/// User signed in to the remote solution storage with a Microsoft account. Also the owner of a remote solution.
	/// </summary>
	public class RemoteUser
	{
		/// <summary>
		/// Object id of the Microsoft account (stable and unique). Remote solutions of the user are stored under this id.
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// User name (e-mail address) for display.
		/// </summary>
		public string Name { get; set; }

		public override string ToString() => Name ?? Id;
	}

	/// <summary>
	/// The remote storage needs an interactive sign-in (nobody is signed in, or the sign-in expired or was revoked).
	/// </summary>
	public class RemoteSignInRequiredException : Exception
	{
		public RemoteSignInRequiredException(string message, Exception innerException = null)
			: base(message, innerException)
		{ }
	}
}
