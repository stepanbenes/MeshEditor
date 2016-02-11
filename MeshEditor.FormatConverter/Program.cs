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

			if(args.Length < 1)
			{
				Console.WriteLine("Usage: {0} mesh-file [result-files]", Path.GetFileName(Assembly.GetExecutingAssembly().CodeBase));
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
			writeMeshFile(surfaceLayer.MeshFile, args[1]);

			Console.Clear();
			Console.Write("Done.");

			Console.ReadKey();
		}

		private static Layer createSurfaceLayer(Mesh mesh)
		{
			Layer surfaceLayer = new Layer { Id = Guid.NewGuid(), Name = "Surface" };

			MeshFile meshFile = new MeshFile { LayerId = surfaceLayer.Id };

			// find triangle faces
			List<Node> points = new List<Node>();
			Dictionary<int, int> pointIndices = new Dictionary<int, int>();
			List<int> triangleConnectivity = new List<int>();
			foreach (Element2D face in mesh.Faces)
			{
				foreach (Node node in face.IterateThroughAllNodes())
				{
					if (!pointIndices.ContainsKey(node.ID))
					{
						points.Add(node);
						pointIndices[node.ID] = points.Count;
					}
				}
				Triangle triangle = face as Triangle;
				if (triangle != null)
				{
					triangleConnectivity.Add(pointIndices[triangle.Node1.ID]);
					triangleConnectivity.Add(pointIndices[triangle.Node2.ID]);
					triangleConnectivity.Add(pointIndices[triangle.Node3.ID]);
				}
				else
				{
					Quadrilateral quad = (Quadrilateral)face;
					// first half
					triangleConnectivity.Add(pointIndices[quad.Node1.ID]);
					triangleConnectivity.Add(pointIndices[quad.Node2.ID]);
					triangleConnectivity.Add(pointIndices[quad.Node3.ID]);
					// second half
					triangleConnectivity.Add(pointIndices[quad.Node1.ID]);
					triangleConnectivity.Add(pointIndices[quad.Node3.ID]);
					triangleConnectivity.Add(pointIndices[quad.Node4.ID]);
				}
			}
			List<int> edgeConnectivity = new List<int>();
			foreach (WingedEdge edge in mesh.Edges)
			{
				if (edge.FeatureAngle >= mesh.HardBorderLimit)
				{
					edgeConnectivity.Add(pointIndices[edge.BeginNode.ID]);
					edgeConnectivity.Add(pointIndices[edge.EndNode.ID]);
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
			meshFile.PointCoordinates = pointCoordinates.ToArray();
			meshFile.TriangleConnectivity = triangleConnectivity.ToArray();
			meshFile.EdgeConnectivity = edgeConnectivity.ToArray();

			surfaceLayer.MeshFile = meshFile;

			return surfaceLayer;
		}

		private static void writeMeshFile(MeshFile meshFile, string outputFilename)
		{
			string json = JsonConvert.SerializeObject(meshFile, Formatting.Indented, new NotIndentedArrayJsonConverter());
			File.WriteAllText(outputFilename, json, Encoding.UTF8);
		}
	}
}
