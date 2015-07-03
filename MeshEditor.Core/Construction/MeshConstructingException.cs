using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.Construction
{
	/// <summary>
	/// vyjimka, ktera muze nastat pri konstrukci site
	/// </summary>
	public class MeshConstructingException : Exception
	{
		public MeshConstructingException() { }
		public MeshConstructingException(string message) : base(message) { }
		public MeshConstructingException(string message, Exception inner) : base(message, inner) { }
		protected MeshConstructingException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}
}
