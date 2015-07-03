using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.IO
{
	public class DataLoadingException : Exception
	{
		private int lineNumber;
		public int LineNumber
		{
			get { return lineNumber; }
		}

		private string filename;
		public string Filename
		{
			get { return filename; }
		}

		public DataLoadingException() : this(string.Empty, null, -1, null) { }
		public DataLoadingException(string message) : this(message, null, -1, null) { }
		public DataLoadingException(string message, string filename) : this(message, filename, -1, null) { }
		public DataLoadingException(string message, string filename, int lineNumber) : this(message, filename, lineNumber, null) { }

		public DataLoadingException(string message, string filename, int lineNumber, Exception inner)
			: base(message, inner)
		{
			this.lineNumber = lineNumber;
			this.filename = filename;
		}

		protected DataLoadingException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context)
		{
			this.lineNumber = -1;
		}
	}
}
