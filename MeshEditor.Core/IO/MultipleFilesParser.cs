using MeshEditor.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MeshEditor.IO
{
	public class MultipleFilesParser : IMeshFileParser
	{
		IMeshFileParser[] parsers;
		int currentParser;
		int nodeCount, elementCount;
		string batchName;

		public MultipleFilesParser(params string[] filenames)
		{
			Debug.Assert(filenames != null && filenames.Length > 1);
			parsers = new IMeshFileParser[filenames.Length];
			for (int i = 0; i < filenames.Length; i++)
			{
				parsers[i] = MeshParserFactory.Create(filenames[i]);
			}

			batchName = Utilities.Functions.GetFileBatchDescription(filenames);
			nodeCount = elementCount = -1;
		}

		public string Filename
		{
			get { return batchName; }
		}

		public int NodeCount
		{
			get
			{
				if (nodeCount == -1)
					nodeCount = parsers.Sum(p => p.NodeCount);
				return nodeCount;
			}
		}

		public IEnumerable<Node> ReadNodes()
		{
			currentParser = -1;
			foreach (IMeshFileParser parser in parsers)
			{
				++currentParser;
				foreach (Node node in parser.ReadNodes())
					yield return node;
			}
		}

		public int ElementCount
		{
			get
			{
				if (elementCount == -1)
					elementCount = parsers.Sum(p => p.ElementCount);
				return elementCount;
			}
		}

		public IEnumerable<ElementDraft> ReadElements()
		{
			currentParser = -1;
			foreach (IMeshFileParser parser in parsers)
			{
				++currentParser;
				foreach (ElementDraft ed in parser.ReadElements())
					yield return ed;
			}
		}

		public int CurrentLineNumber
		{
			get { return parsers[currentParser].CurrentLineNumber; }
		}

		public void Dispose()
		{
			currentParser = -1;
			foreach (IMeshFileParser parser in parsers)
			{
				++currentParser;
				parser.Dispose();
			}
			parsers = null;
		}
	}
}
