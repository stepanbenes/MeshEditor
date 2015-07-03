using MeshEditor.Data;
using MeshEditor.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenTK;

namespace MeshEditor.FormatConverter
{
	public class GiDResFileFormatGenerator
	{
		public void GenerateResultFile(IMeshFileParser parser, string destination)
		{
			List<Node> nodes = new List<Node>();
			double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
			foreach (Node node in parser.ReadNodes())
			{
				minX = Math.Min(minX, node.Position.X);
				minY = Math.Min(minY, node.Position.Y);
				minZ = Math.Min(minZ, node.Position.Z);
				maxX = Math.Max(maxX, node.Position.X);
				maxY = Math.Max(maxY, node.Position.Y);
				maxZ = Math.Max(maxZ, node.Position.Z);

				nodes.Add(node);
			}

			using (TextWriter writer = new StreamWriter(destination))
			{
				writer.WriteLine("GiD Post Results File 1.0");
				writer.WriteLine("# Generated with MeshEditor.GiDResFileFormatGenerator v1.1");

				writer.WriteLine("Result \"Displacement\" \"GeneratedResults\" 1 Vector OnNodes");
				writer.WriteLine("ComponentNames \"X\" \"Y\" \"Z\"");

				writer.WriteLine("Values");

				foreach (Node node in nodes)
				{
					double x = (node.Position.X - minX) / (maxX - minX);
					double y = (node.Position.Y - minY) / (maxY - minY);
					double z = (node.Position.Z - minZ) / (maxZ - minZ);

					writer.WriteLine(string.Format("{0} {1} {2} {3}", node.ID, x, y, z));
				}

				writer.WriteLine("End Values");
			}
		}
	}
}
