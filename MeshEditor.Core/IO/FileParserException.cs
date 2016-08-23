using System;

namespace MeshEditor.IO
{
	/// <summary>
	/// Indicates error that can happen while reading input file (either mesh or results).
	/// </summary>
	[Serializable]
	public class FileParserException : Exception
	{

		#region Fields

		private string fileName;
		private int lineNumber;
		private int linePosition;

		#endregion

		#region Properties

		public string FileName => fileName;

		public int LineNumber => lineNumber;

		public int LinePosition => linePosition;

		#endregion

		#region Constructors

		public FileParserException(string message, string fileName) : this(message, fileName, 0, 0, null) { }

		public FileParserException(string message, string fileName, int lineNumber) : this(message, fileName, lineNumber, 0, null) { }

		public FileParserException(string message, string fileName, int lineNumber, int linePosition) : this(message, fileName, lineNumber, linePosition, null) { }

		public FileParserException(string message, string fileName, int lineNumber, Exception inner) : this(message, fileName, lineNumber, 0, inner) { }

		public FileParserException(string message, string fileName, int lineNumber, int linePosition, Exception inner)
			: base(message, inner)
		{
			this.fileName = fileName;
			this.lineNumber = lineNumber;
			this.linePosition = linePosition;
		}

		#endregion

	}
}
