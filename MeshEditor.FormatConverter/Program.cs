using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MeshEditor.IO;
using System.Reflection;
using System.IO;
using MeshEditor.Construction;
using MeshEditor.Data;
using MeshEditor.CoreInterface;
using System.Threading;
using System.Globalization;
using OpenTK;
using Newtonsoft.Json;

namespace MeshEditor.FormatConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			if(args.Length < 2)
			{
				Console.WriteLine("Usage: {0} input output", Path.GetFileName(Assembly.GetExecutingAssembly().CodeBase));
				Console.ReadKey();
				return;
			}

			Mesh mesh = null;
			using (IMeshFileParser parser = MeshParserFactory.Create(args[0])) // choose parser
			{
				IMeshCreator meshCreator = new MeshConstructor();
				mesh = meshCreator.CreateMesh(parser, cancelled: null);
			}

			Layer surfaceLayer = createSurfaceLayer(mesh);
			writeLayerToFile(surfaceLayer, args[1]);

			Console.Clear();
			Console.Write("Done.");

			Console.ReadKey();
		}

		private static Layer createSurfaceLayer(Mesh mesh)
		{
			Layer layer = new Layer { Id = Guid.NewGuid(), Name = "Surface" };

			// find triangle faces
			List<Node> points = new List<Node>();
			Dictionary<Node, int> pointIndices = new Dictionary<Node, int>();
			List<int> triangleConnectivity = new List<int>();
			foreach (Element2D face in mesh.Faces)
			{
				foreach (Node node in face.IterateThroughAllNodes())
				{
					if (!pointIndices.ContainsKey(node))
					{
						points.Add(node);
						pointIndices[node] = points.Count;
					}
				}
				Triangle triangle = face as Triangle;
				if (triangle != null)
				{
					triangleConnectivity.Add(pointIndices[triangle.Node1]);
					triangleConnectivity.Add(pointIndices[triangle.Node2]);
					triangleConnectivity.Add(pointIndices[triangle.Node3]);
				}
				else
				{
					Quadrilateral quad = (Quadrilateral)face;
					// first half
					triangleConnectivity.Add(pointIndices[quad.Node1]);
					triangleConnectivity.Add(pointIndices[quad.Node2]);
					triangleConnectivity.Add(pointIndices[quad.Node3]);
					// second half
					triangleConnectivity.Add(pointIndices[quad.Node1]);
					triangleConnectivity.Add(pointIndices[quad.Node3]);
					triangleConnectivity.Add(pointIndices[quad.Node4]);
				}
			}
			List<int> edgeConnectivity = new List<int>();
			foreach (WingedEdge edge in mesh.Edges)
			{
				if (edge.FeatureAngle >= mesh.HardBorderLimit)
				{
					edgeConnectivity.Add(pointIndices[edge.BeginNode]);
					edgeConnectivity.Add(pointIndices[edge.EndNode]);
				}
			}

			List<double> pointCoordinates = new List<double>();
			foreach (Node point in points)
			{
				Vector3 transformedPosition = (point.Position / mesh.ResizeFactor) + mesh.PositionOffset;
				pointCoordinates.Add(transformedPosition.X);
				pointCoordinates.Add(transformedPosition.Y);
				pointCoordinates.Add(transformedPosition.Z);
			}
			layer.PointCoordinates = pointCoordinates.ToArray();
			layer.TriangleConnectivity = triangleConnectivity.ToArray();
			layer.EdgeConnectivity = edgeConnectivity.ToArray();

			return layer;
		}

		private static void writeLayerToFile(Layer layer, string outputFilename)
		{
			string json = JsonConvert.SerializeObject(layer, Formatting.Indented, new NotIndentedArrayJsonConverter());
			File.WriteAllText(outputFilename, json, Encoding.UTF8);
		}
	}
}
