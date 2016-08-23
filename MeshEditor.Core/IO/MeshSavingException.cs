using System;

namespace MeshEditor.IO
{
	/// <summary>
	/// vyjimka, ktera nastane pri ukladani site do souboru
	/// </summary>
	[Serializable]
	public class MeshSavingException : Exception
	{
		private int lineNumber;

		public int LineNumber
		{
			get { return lineNumber; }
		}

		public MeshSavingException()
			: this(string.Empty, -1, null)
		{ }
		public MeshSavingException(string message)
			: this(message, -1, null)
		{ }
		public MeshSavingException(string message, int lineNumber)
			: this(message, lineNumber, null)
		{ }
		public MeshSavingException(string message, Exception inner)
			: this(message, -1, inner)
		{ }
		public MeshSavingException(string message, int lineNumber, Exception inner) 
			: base(message, inner)
		{
			this.lineNumber = lineNumber;
		}

		protected MeshSavingException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context)
		{
			this.lineNumber = -1;
		}
	}
}
