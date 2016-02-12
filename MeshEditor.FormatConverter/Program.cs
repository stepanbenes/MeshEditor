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
using System.Text.RegularExpressions;
using MeshEditor.DataVisualizer.IO;
using MeshEditor.DataVisualizer.Data;

namespace MeshEditor.FormatConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			if (args.Length < 1)
			{
				Console.WriteLine("Usage: {0} mesh-file [result-files]", Path.GetFileName(Assembly.GetExecutingAssembly().CodeBase));
				Console.ReadKey();
				return;
			}

			Layer surfaceLayer = createSurfaceLayer(args[0], args.Skip(1));
			writeLayer(surfaceLayer, Path.GetDirectoryName(args[0]));

			Console.Clear();
			Console.Write("Done.");

			Console.ReadKey();
		}

		private static Layer createSurfaceLayer(string meshFilename, IEnumerable<string> resultFilenames)
		{
			Mesh mesh = null;
			using (IMeshFileParser parser = MeshParserFactory.Create(meshFilename)) // choose parser
			{
				IMeshCreator meshCreator = new MeshConstructor();
				mesh = meshCreator.CreateMesh(parser, cancelled: null);
			}

			Layer layer = new Layer { Id = Guid.NewGuid(), Name = "Surface" };

			Dictionary<int, int> nodeIdMap;

			MeshFile meshFile = createMeshFile(mesh, out nodeIdMap);
			meshFile.LayerId = layer.Id;
			layer.MeshFile = meshFile;

			//ResultSummaryFile resultSummaryFile = createResultSummaryFile(
			//surfaceLayer.res

			List<ResultDescriptor> resultDescriptors = new List<ResultDescriptor>();
			HashSet<double> timeSteps = new HashSet<double>();

			foreach (string resultFilename in resultFilenames)
			{
				using (IDataFileParser dataParser = DataParserFactory.Create(resultFilename))
				{
					DataInfo dataInfo;
					while ((dataInfo = dataParser.ReadNextResult()) != null)
					{
						if (dataInfo.Location != DataLocation.Nodes)
						{
							throw new NotSupportedException($"{dataInfo.Location} location is not supported.");
						}

						double[][] values = new double[dataInfo.DataType.ComponentCount][];
						for (int i = 0; i < dataInfo.DataType.ComponentCount; i++) // init array
							values[i] = new double[nodeIdMap.Count];
						foreach (NodeValue nodeValue in dataParser.ReadResultBlock())
						{
							int idInLayer;
							if (nodeIdMap.TryGetValue(nodeValue.EntityNumber, out idInLayer))
							{
								for (int i = 0; i < dataInfo.DataType.ComponentCount; i++)
								{
									values[i][idInLayer - 1] = nodeValue.ValueComponents[i];
								}
							}
						}

						for (int i = 0; i < dataInfo.DataType.ComponentCount; i++)
						{
							ResultFile resultFile = new ResultFile
							{
								LayerId = layer.Id,
								ResultName = dataInfo.DataType.Name,
								ComponentName = dataInfo.DataType.Components[i].Name,
								TimeStep = dataInfo.Time,
								CompressionLevel = 0, // no compression
								Data = convertData(values[i]),
							};

							string resultJsonFilePrefix = $"{layer.Name}-{resultFile.ResultName}-{resultFile.ComponentName}-{resultFile.TimeStep}";
							string resultJsonFilename = writeJsonFile(Path.GetDirectoryName(meshFilename), resultJsonFilePrefix, resultFile.LayerId, "result", resultFile);

							ResultDescriptor resultDescriptor = new ResultDescriptor
							{
								ResultName = dataInfo.DataType.Name,
								ComponentName = dataInfo.DataType.Components[i].Name,
								TimeStep = dataInfo.Time,
								FileName = resultJsonFilename,
							};

							resultDescriptors.Add(resultDescriptor);
						}

						timeSteps.Add(dataInfo.Time);
					}
				}
			}

			ResultSummaryFile resultSummaryFile = new ResultSummaryFile
			{
				LayerId = layer.Id,
				TimeSteps = timeSteps.OrderBy(t => t).ToArray(),
				ResultDescriptors = resultDescriptors.ToArray(),
			};

			layer.ResultSummaryFile = resultSummaryFile;

			return layer;
		}

		private static string convertData(double[] values)
		{
			//byte[] bytes = values.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
			byte[] bytes = new byte[values.Length * sizeof(double)];
			Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
			return Convert.ToBase64String(bytes);
		}

		private static MeshFile createMeshFile(Mesh mesh, out Dictionary<int, int> nodeIdMap)
		{
			MeshFile meshFile = new MeshFile();

			// find triangle faces
			List<Node> points = new List<Node>();
			nodeIdMap = new Dictionary<int, int>();
			List<int> triangleConnectivity = new List<int>();
			foreach (Element2D face in mesh.Faces)
			{
				foreach (Node node in face.IterateThroughAllNodes())
				{
					if (!nodeIdMap.ContainsKey(node.ID))
					{
						points.Add(node);
						nodeIdMap[node.ID] = points.Count;
					}
				}
				Triangle triangle = face as Triangle;
				if (triangle != null)
				{
					triangleConnectivity.Add(nodeIdMap[triangle.Node1.ID]);
					triangleConnectivity.Add(nodeIdMap[triangle.Node2.ID]);
					triangleConnectivity.Add(nodeIdMap[triangle.Node3.ID]);
				}
				else
				{
					Quadrilateral quad = (Quadrilateral)face;
					// first half
					triangleConnectivity.Add(nodeIdMap[quad.Node1.ID]);
					triangleConnectivity.Add(nodeIdMap[quad.Node2.ID]);
					triangleConnectivity.Add(nodeIdMap[quad.Node3.ID]);
					// second half
					triangleConnectivity.Add(nodeIdMap[quad.Node1.ID]);
					triangleConnectivity.Add(nodeIdMap[quad.Node3.ID]);
					triangleConnectivity.Add(nodeIdMap[quad.Node4.ID]);
				}
			}
			List<int> edgeConnectivity = new List<int>();
			foreach (WingedEdge edge in mesh.Edges)
			{
				if (edge.FeatureAngle >= mesh.HardBorderLimit)
				{
					edgeConnectivity.Add(nodeIdMap[edge.BeginNode.ID]);
					edgeConnectivity.Add(nodeIdMap[edge.EndNode.ID]);
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

			return meshFile;
		}

		private static void writeLayer(Layer layer, string path)
		{
			// MeshFile
			writeJsonFile(path, layer.Name, layer.Id, "mesh", layer.MeshFile);
			// ResultSummaryFile
			writeJsonFile(path, layer.Name, layer.Id, "summary", layer.ResultSummaryFile);
		}

		private static string writeJsonFile(string path, string filePrefix, Guid layerId, string fileType, object objectToSerialize)
		{
			string filename = Path.Combine(path, createUniqueFileName(filePrefix, layerId, fileType, "json"));
			string json = JsonConvert.SerializeObject(objectToSerialize, Formatting.Indented, new NotIndentedArrayJsonConverter());
			File.WriteAllText(filename, json, Encoding.UTF8);
			return filename;
		}

		private static string createUniqueFileName(string prefix, Guid guid, string suffix, string extension)
		{
			Regex regex = new Regex("[^a-zA-Z0-9-]");
			string prefixNormalized = regex.Replace(prefix, "");
			string suffixNormalized = regex.Replace(suffix, "");

			return $"{prefixNormalized}_{guid.ToString()}.{suffix}.{extension}";
		}
	}
}
