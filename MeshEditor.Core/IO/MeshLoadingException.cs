using System;

namespace MeshEditor.IO
{
	/// <summary>
	/// vyjimka, ktera nastane pri cteni vstupniho souboru se siti
	/// </summary>
	public class MeshLoadingException : Exception
	{
		private int lineNumber;
		public int LineNumber
		{
			get { return lineNumber; }
		}

		public MeshLoadingException() : this(string.Empty, -1, null) { }
		public MeshLoadingException(string message) : this(message, -1, null) { }
		public MeshLoadingException(string message, int lineNumber) : this(message, lineNumber, null) { }
		
		public MeshLoadingException(string message, int lineNumber, Exception inner)
			: base(message, inner)
		{
			this.lineNumber = lineNumber;
		}
		
		protected MeshLoadingException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context)
		{
			this.lineNumber = -1;
		}
	}
}
