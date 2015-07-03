using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;
using MeshEditor.Data;
using OpenTK;
using Wintellect.PowerCollections;
using MeshEditor.Utilities;
using System.Drawing;
using System.Diagnostics;

namespace MeshEditor.Graphics
{
	/// <summary>
	/// trida pro reprezentaci Vertex Buffer Object pro zobrazeni povrchu site.
	/// obsahuje buffer pro vertexy, normaly, barvy uzlu, barvy hran a barvy ploch
	/// </summary>
	public class VertexBufferObject : IDisposable
    {

        #region Static Fields, Constructor

		private static bool isSupportedField;
		private static bool isSupportedWasSet;

		public static bool IsSupported
		{
			get
			{
				if (!isSupportedWasSet)
					determineSupportForVBO();
				return isSupportedField;
			}
		}

		private static void determineSupportForVBO()
		{
			// get OpenGL version first
			int major, minor;
			Utilities.Functions.GetOpenGLVersion(out major, out minor);

			if (major >= 2)
				isSupportedField = true;
			else if (major == 1 && minor >= 4)
				isSupportedField = true;
			else
				isSupportedField = false;

			isSupportedWasSet = true;

			// ==================================================
			//string version = GL.GetString(StringName.Version);
			//isSupportedField = (version >= "1.4");
			//isSupportedWasSet = true;
			// ==================================================
			

            //IsSupported = false;
			//IsSupported = true;

            //IsSupported = GL.SupportsExtension("VERSION_1_5");
            //IsSupported = GL.SupportsFunction("glDrawElements");
		}

        static VertexBufferObject()
        {
			isSupportedWasSet = false;
        }

        #endregion

        #region Instance Fields, Constructor

        private int vertexCount, normalCount;

		// IDs of buffers
		private int vertexBufferID, normalBufferID;			
		private int faceColorBufferID, edgeColorBufferID, nodeColorBufferID;

		public VertexBufferObject()
		{
			this.vertexCount = this.normalCount = 0;
			this.vertexBufferID = this.normalBufferID = 0;
			this.faceColorBufferID = this.edgeColorBufferID = this.nodeColorBufferID = 0;
		}

		public int FaceColorBufferID
		{
			get { return faceColorBufferID; }
		}

		public int EdgeColorBufferID
		{
			get { return edgeColorBufferID; }
		}

		public int NodeColorBufferID
		{
			get { return nodeColorBufferID; }
		}

		#endregion

		#region Public methods
		
		public void CreateFrom(List<Element2D> faces, IEnumerable<Node> allSurfaceNodes, PropertyColorsMode colorMode, float softBorderLimit, float hardBorderLimit, out Dictionary<Element2D, int[]> faceIndexMap, out Dictionary<WingedEdge, int> edgeIndexMap, out Dictionary<Node, int> nodeIndexMap)
		{
			int facesCount = faces.Count;

			bool elementPropertyColors = (colorMode & PropertyColorsMode.Elements) != 0;
			bool facePropertyColors = (colorMode & PropertyColorsMode.Faces) != 0;
			bool edgePropertyColors = (colorMode & PropertyColorsMode.Edges) != 0;
			bool nodePropertyColors = (colorMode & PropertyColorsMode.Nodes) != 0;
			bool smooth = (Scene.MeshShadingModel == ShadingModel.Smooth);

			edgeIndexMap = new Dictionary<WingedEdge, int>();

			faceIndexMap = new Dictionary<Element2D, int[]>(facesCount);
			nodeIndexMap = new Dictionary<Node, int>();

			Dictionary<Node, List<int>> vertexMap = new Dictionary<Node, List<int>>();
			Dictionary<Node, List<Element2D>> neighborMap = (smooth) ? new Dictionary<Node, List<Element2D>>() : null;
			
			List<Vector3> vertices = new List<Vector3>(facesCount * 3);
			List<Vector3> normals = new List<Vector3>(facesCount * 3);
			List<int> colorsOfFaces = new List<int>(facesCount * 3);

			int ordinaryFaceColor = Utilities.Functions.ColorToRgba32(Scene.FaceColor);
			int faceColor;
			int vertexIndex = 0;
			foreach (Element2D face in faces)
			{
				List<Node> nodesOfFace = new List<Node>(face.IterateThroughAllNodes());
				// ------------------------------------------------------
				int edgeIndex = 0;
				foreach (WingedEdge e in face.IterateThroughAllEdges())
				{
					edgeIndexMap[e] = EncodeEdgeIndex(edgeIndex, e, nodesOfFace[edgeIndex], vertices.Count);
					edgeIndex++;
				}
				// ------------------------------------------------------
				faceColor = getColorOfFace(face, facePropertyColors, elementPropertyColors, ordinaryFaceColor);
				// ------------------------------------------------------
				foreach (Node n in nodesOfFace)
				{
					nodeIndexMap[n] = vertices.Count;

					vertices.Add(n.Position);
					colorsOfFaces.Add(faceColor);

					bool vertexProcessed = vertexMap.ContainsKey(n);
					if (!vertexProcessed)
						vertexMap[n] = new List<int>();
					vertexMap[n].Add(vertices.Count - 1);
					if (smooth)
					{
						normals.Add(Vector3.Zero);
						if (!vertexProcessed)
							neighborMap[n] = new List<Element2D>();
						neighborMap[n].Add(face);
					}
					else
						normals.Add(face.NormalVector);
				}

				if (face.NodeCount == 3)
				{
					faceIndexMap[face] = new int[] { vertexIndex, vertexIndex + 1, vertexIndex + 2 };
					vertexIndex += 3;
				}
				else if (face.NodeCount == 4)
				{
					int n1 = vertexIndex;
					int n2 = vertexIndex + 1;
					int n3 = vertexIndex + 2;
					int n4 = vertexIndex + 3;

					// teselace
					faceIndexMap[face] = new int[] { n1, n2, n3, n1, n3, n4 };
					vertexIndex += 4;
				}
				else
					throw new NotSupportedException("This type of face is not supported (" + face.GetType().Name + ")");
			}


			if (smooth)
			{
				/**/ // !! pocitani normal pro vertexy by slo vynechat u rezu - pred rezem (vymazanim bufferu) bych si je zapamatoval, a pak bych je tady jen pouzil
				foreach (Node n in vertexMap.Keys)
				{
					Vector3 normal;
					List<Element2D> neighbors = neighborMap[n];
					List<int> indices = vertexMap[n];
					if (nodeIsOnBorderEdge(n, neighbors[0], hardBorderLimit))
					{
						for (int i = 0; i < neighbors.Count; i++)
						{
							normal = interpolateNormalFor(n, neighbors[i], hardBorderLimit); /**/
							normals[indices[i]] = normal;
						}
					}
					else
					{
						normal = Vector3.Zero;
						foreach (Element2D f in neighborMap[n])
							normal += f.NormalVector;
						normal.Normalize();

						for (int i = 0; i < indices.Count; i++)
							normals[indices[i]] = normal;
					}
				}
			}
			

			this.normalCount = normals.Count;

			// ----------------------------
			includeAdditionalSurfaceNodes(vertices, vertexMap, nodeIndexMap, allSurfaceNodes);
			this.vertexCount = vertices.Count;
			// ----------------------------

			// generovani vertex bufferu
			GL.GenBuffers(1, out vertexBufferID);
			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferID);
			int vertexBufferSize = vertices.Count * Vector3.SizeInBytes;
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)vertexBufferSize, vertices.ToArray(), BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			// generovani normal bufferu
			GL.GenBuffers(1, out normalBufferID);
			GL.BindBuffer(BufferTarget.ArrayBuffer, normalBufferID);
			int normalBufferSize = normals.Count * Vector3.SizeInBytes;
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)normalBufferSize, normals.ToArray(), BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			// generovani face-color bufferu
			GL.GenBuffers(1, out faceColorBufferID);
			GL.BindBuffer(BufferTarget.ArrayBuffer, faceColorBufferID);
			int faceColorBufferSize = colorsOfFaces.Count * sizeof(int);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)faceColorBufferSize, colorsOfFaces.ToArray(), BufferUsageHint.StaticDraw/**/);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			// generovani edge-color bufferu
			createEdgeColorBuffer(edgeIndexMap, edgePropertyColors, softBorderLimit, hardBorderLimit);

			// generovani node-color bufferu
			createNodeColorBuffer(nodeIndexMap, nodePropertyColors);
		}

		private int getColorOfFace(Element2D face, bool facePropertyColors, bool elementPropertyColors, int ordinaryFaceColor)
		{
			IFaceOfElement3D faceOfElement = face as IFaceOfElement3D;
			bool faceIs2DElement = (faceOfElement == null || faceOfElement.ParentElement == null);
			
			if (elementPropertyColors && !faceIs2DElement)
				return PropertyColorProvider.GetRGBA32(faceOfElement.ParentElement.Property);
			else if (facePropertyColors || (elementPropertyColors && faceOfElement == null))
				return PropertyColorProvider.GetRGBA32(face.Property);
			return ordinaryFaceColor;
		}

		public static int EncodeEdgeIndex(int edgeIndex, WingedEdge e, Node nodeOfEdge, int verticesCount)
		{
			int index;
			if (nodeOfEdge == e.EndNode)
				index = verticesCount + edgeIndex;
			else if (nodeOfEdge == e.BeginNode)
				index = -(verticesCount + edgeIndex + 1); // prictu jednicku, aby to fungovalo i pro nulu (pak ji musim odecist)
			else
				throw new Exception("Color buffer creating error: Edge does not contain this node."); /**/
			return index;
		}

		public static int DecodeEdgeIndex(int encodedEdgeIndex, out bool reversed)
		{
			if (encodedEdgeIndex < 0)
			{
				reversed = true;
				return (-encodedEdgeIndex) - 1;
			}
			reversed = false;
			return encodedEdgeIndex;
		}

		public static int DecodeEdgeIndex(int encodedEdgeIndex)
		{
			if (encodedEdgeIndex < 0)
				return (-encodedEdgeIndex) - 1;
			return encodedEdgeIndex;
		}

		public void DrawMinimum(IndexBufferObject ibo)
		{
			if (ibo == null) // neni co kreslit, koncim :)
				return;

			GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);
			//----------------------------------------------------
			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferID);
			GL.VertexPointer(3, VertexPointerType.Float, 0, IntPtr.Zero);
			GL.EnableClientState(EnableCap.VertexArray);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo.BufferID);
			//GL.IndexPointer(IndexPointerType.Int, 0, IntPtr.Zero);
			//GL.EnableClientState(EnableCap.IndexArray);

			GL.DrawElements(ibo.Mode, ibo.ElementCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
			
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			//----------------------------------------------------
			GL.PopClientAttrib();
		}

		public void DrawNodes(IndexBufferObject ibo)
		{
			if (ibo == null) // neni co kreslit, koncim :)
				return;

			GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);
			//----------------------------------------------------
			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferID);
			GL.VertexPointer(3, VertexPointerType.Float, 0, IntPtr.Zero);
			GL.EnableClientState(EnableCap.VertexArray);

			GL.BindBuffer(BufferTarget.ArrayBuffer, nodeColorBufferID);
			GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, IntPtr.Zero);
			GL.EnableClientState(EnableCap.ColorArray);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo.BufferID);
			//GL.IndexPointer(IndexPointerType.Int, 0, IntPtr.Zero);
			//GL.EnableClientState(EnableCap.IndexArray);

			GL.DrawElements(ibo.Mode, ibo.ElementCount, DrawElementsType.UnsignedInt, IntPtr.Zero);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			//----------------------------------------------------
			GL.PopClientAttrib();
		}

		public void DrawFaces(IndexBufferObject ibo)
		{
			if (ibo == null) // neni co kreslit, koncim :)
				return;

			GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);
			//----------------------------------------------------
			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferID);
			GL.VertexPointer(3, VertexPointerType.Float, 0, IntPtr.Zero);
			GL.EnableClientState(EnableCap.VertexArray);

			GL.BindBuffer(BufferTarget.ArrayBuffer, normalBufferID);
			GL.NormalPointer(NormalPointerType.Float, 0, IntPtr.Zero);
			GL.EnableClientState(EnableCap.NormalArray);

			GL.BindBuffer(BufferTarget.ArrayBuffer, faceColorBufferID);
			GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, IntPtr.Zero);
			GL.EnableClientState(EnableCap.ColorArray);
			
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo.BufferID);
			//GL.IndexPointer(IndexPointerType.Int, 0, IntPtr.Zero);
			//GL.EnableClientState(EnableCap.IndexArray);
			
			GL.DrawElements(ibo.Mode, ibo.ElementCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
			
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			//----------------------------------------------------
			GL.PopClientAttrib();
		}

		public void DrawEdges(IndexBufferObject ibo, bool bindNormalBuffer)
		{
			if (ibo == null) // neni co kreslit, koncim :)
				return;
			
			GL.PushClientAttrib(ClientAttribMask.ClientVertexArrayBit);
			//----------------------------------------------------
			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferID);
			GL.VertexPointer(3, VertexPointerType.Float, 0, IntPtr.Zero);
			GL.EnableClientState(EnableCap.VertexArray);

			if (bindNormalBuffer)
			{
				GL.BindBuffer(BufferTarget.ArrayBuffer, normalBufferID);
				GL.NormalPointer(NormalPointerType.Float, 0, IntPtr.Zero);
				GL.EnableClientState(EnableCap.NormalArray);
			}

			GL.BindBuffer(BufferTarget.ArrayBuffer, edgeColorBufferID);
			GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, IntPtr.Zero);
			GL.EnableClientState(EnableCap.ColorArray);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo.BufferID);
			//GL.IndexPointer(IndexPointerType.Int, 0, IntPtr.Zero);
			//GL.EnableClientState(EnableCap.IndexArray);

			GL.DrawElements(ibo.Mode, ibo.ElementCount, DrawElementsType.UnsignedInt, IntPtr.Zero);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			//----------------------------------------------------
			GL.PopClientAttrib();
		
		}

		public void InvertAllNormals()
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, this.normalBufferID);
			IntPtr videoMemory = GL.MapBuffer(BufferTarget.ArrayBuffer, BufferAccess.ReadWrite);
			// -----------------
			unsafe
			{
				Vector3* items = (Vector3*)videoMemory.ToPointer();
				for (int i = 0; i < this.normalCount; i++)
					items[i] = -items[i];
			}
			// -----------------
			if (!GL.UnmapBuffer(BufferTarget.ArrayBuffer))
			{
#if DEBUG
				throw new Exception("Error while unmapping buffer.");
#else
				Console.WriteLine("Error while unmapping buffer.");
#endif
			}
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		#endregion

		#region Private methods

		private void includeAdditionalSurfaceNodes(List<Vector3> vertices, Dictionary<Node, List<int>> vertexMap, Dictionary<Node, int> nodeIndexMap, IEnumerable<Node> allSurfaceNodes)
		{
			foreach (Node n in allSurfaceNodes)
			{
				if (!vertexMap.ContainsKey(n))
				{
					List<int> list = new List<int>(1); // capacity = 1
					list.Add(vertices.Count);
					vertexMap[n] = list;
					nodeIndexMap[n] = vertices.Count;
					vertices.Add(n.Position);
				}
			}
		}

		private void createNodeColorBuffer(Dictionary<Node, int> nodeIndexMap, bool nodePropertyColors)
		{
			int[] colors = new int[this.vertexCount];
			int ordinaryColor = Utilities.Functions.ColorToRgba32(Scene.NodesColor);
			foreach (KeyValuePair<Node, int> pair in nodeIndexMap)
			{
				int color = (nodePropertyColors) ? PropertyColorProvider.GetRGBA32(pair.Key.Property) : ordinaryColor;
				colors[pair.Value] = color;
			}

			// generovani node-color bufferu
			GL.GenBuffers(1, out nodeColorBufferID);
			GL.BindBuffer(BufferTarget.ArrayBuffer, nodeColorBufferID);
			int nodeColorBufferSize = colors.Length * sizeof(int);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)nodeColorBufferSize, colors, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		private void createEdgeColorBuffer(Dictionary<WingedEdge, int> edgeIndexMap, bool edgePropertyColors, float softBorderLimit, float hardBorderLimit)
		{
			int[] colors = new int[this.vertexCount];
			int edgeColor = 0;
			int ordinaryColor = Utilities.Functions.ColorToRgba32(Scene.OrdinaryEdgeColor);
			int softColor = Utilities.Functions.ColorToRgba32(Scene.SoftBorderColor);
			int hardColor = Utilities.Functions.ColorToRgba32(Scene.HardBorderColor);
			
			//int hardCount = 0;

			foreach (WingedEdge edge in edgeIndexMap.Keys)
			{
				if (edgePropertyColors)
					edgeColor = PropertyColorProvider.GetRGBA32(edge.Property);
				else if (edge.FeatureAngle < softBorderLimit)
					edgeColor = ordinaryColor;
				else if (edge.FeatureAngle < hardBorderLimit)
					edgeColor = softColor;
				else
				{
					edgeColor = hardColor;
					//hardCount++;
				}
				int idx = DecodeEdgeIndex(edgeIndexMap[edge]);
				colors[idx] = edgeColor;
			}

			//Console.WriteLine("pocet tlustych hran: " + hardCount);
			//Console.WriteLine("pocet barev: " + colors.Length);
			//Console.WriteLine("pocet hran: " + edgeIndexMap.Count);

			// generovani edge-color bufferu
			GL.GenBuffers(1, out edgeColorBufferID);
			GL.BindBuffer(BufferTarget.ArrayBuffer, edgeColorBufferID);
			int edgeColorBufferSize = colors.Length * sizeof(int);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)edgeColorBufferSize, colors, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		private static Vector3 interpolateNormalFor(Node node, Element2D face, float hardBorderLimit)
		{
			Set<Element2D> result = new Set<Element2D>();
			result.Add(face);
			Stack<Element2D> good = new Stack<Element2D>();
			good.Push(face);
			while (good.Count > 0)
			{
				Element2D f = good.Pop();
				foreach (Element2D neighbor in f.GetNeighbors(hardBorderLimit))
				{
					if (!result.Contains(neighbor) && neighbor.ContainsNode(node))
					{
						result.Add(neighbor);
						good.Push(neighbor);
					}
				}
			}

			Vector3 normal = Vector3.Zero;
			foreach (Element2D f in result)
				normal += f.NormalVector;
			return normal / result.Count;
		}

		private static bool nodeIsOnBorderEdge(Node n, Element2D face, float hardBorderLimit)
		{
			foreach (WingedEdge edge in face.IterateThroughAllEdges())
			{
				//if (edge == null)
				//	continue;
				List<WingedEdge> neighbors = null;
				if (edge.BeginNode == n)
					neighbors = edge.BeginNeighbors;
				else if (edge.EndNode == n)
					neighbors = edge.EndNeighbors;

				if (neighbors == null)
					continue;

				foreach (WingedEdge neighbor in neighbors)
					if (neighbor.FeatureAngle >= hardBorderLimit)
						return true;
			}
			return false;
		}

		#endregion

		#region Disposing

		public void Dispose()
        {
            GL.DeleteBuffers(1, ref vertexBufferID);
			GL.DeleteBuffers(1, ref normalBufferID);
			GL.DeleteBuffers(1, ref faceColorBufferID);
			GL.DeleteBuffers(1, ref edgeColorBufferID);
			GL.DeleteBuffers(1, ref nodeColorBufferID);
			vertexCount = normalCount = 0;
        }
    
        #endregion
	
	}
}
