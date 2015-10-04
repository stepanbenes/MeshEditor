using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MeshEditor.Data;

namespace MeshEditor.IO
{
	/// <summary>
	/// VTK XML mesh definition parser.
	/// Only serial UnstructuredGrid (.vtu) is supported.
	/// </summary>
	class VTKXMLMeshParser : IMeshFileParser
	{

		#region Fields, constructor

		private string filename;
		private bool fileProcessed;
		private int currentLineNumber;

		private List<Node> nodes;
		private List<ElementDraft> elements;

		public VTKXMLMeshParser(string filename)
		{
			this.filename = filename;
			currentLineNumber = -1;

			nodes = new List<Node>();
			elements = new List<ElementDraft>();
		}

		#endregion

		#region IMeshFileParser

		public string Filename => filename;

		public int CurrentLineNumber => currentLineNumber;

		public int NodeCount
		{
			get
			{
				if (!fileProcessed)
					processFile();
				return nodes.Count;
			}
		}

		public IEnumerable<Node> ReadNodes()
		{
			if (!fileProcessed)
				processFile();
			return nodes;
		}

		public int ElementCount
		{
			get
			{
				if (!fileProcessed)
					processFile();
				return elements.Count;
			}
		}

		public IEnumerable<ElementDraft> ReadElements()
		{
			if (!fileProcessed)
				processFile();
			return elements;
		}

		#endregion

		#region IDisposable Support

		public void Dispose()
		{ }

		#endregion

		#region Private methods

		private void processFile()
		{
			Debug.Assert(!fileProcessed);

			throw new NotImplementedException();

			fileProcessed = true;
		}

		#endregion

	}
}
