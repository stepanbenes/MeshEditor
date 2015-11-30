using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Linq;

using OpenTK;
using OpenTK.Graphics.OpenGL;

// Compatibility assembly (for TextPrinter)
using OpenTK.Graphics;

// alias
using Utils = MeshEditor.Utilities.Functions;
using MeshEditor.Graphics;
using MeshEditor.CoreInterface;


namespace MeshEditor.Data
{
	/// <summary>
	/// trida zapouzdrujici celou vnitni reprezentaci site. 
	/// obsahuje strukturu typu okridlena hrana, seznamy uzlu, prvku, ploch, hran a beamu. a vertex buffer objekty
	/// </summary>
	public class Content
	{

		#region Static members

		private static MeshEditor.OpenTKCompatibility.TextPrinter textPrinter;
		private static Font textFont;

		static Content()
		{
			textPrinter = new OpenTKCompatibility.TextPrinter();
			textFont = new Font(FontFamily.GenericSansSerif, 8f, FontStyle.Regular);
		}

		#endregion

		#region Fields

		// ---------------------------------------------------------------
		private readonly Mesh parentMesh;

		private List<Element> elements;
		private List<Beam> beams;
		private List<Element2D> faces;
		private List<WingedEdge> edges;
		private Dictionary<Node, List<WingedEdge>> nodesEdgesIncidence;
		private List<Node> edgeMiddleNodes;
		private HashSet<Node> beamNodesNotInFaces; // uzly od beamu, ktere nejsou obsazeny v siti
		private Dictionary<Node, int> nodeIndexMap; // pro VBO - pro kazdy uzel - jeho index v bufferu

		private Dictionary<Element2D, Vector2> faceCentersPositions;
		private Dictionary<Beam, Vector2> beamCentersPositions;
		private HashSet<Node> stickyNodes;

		// ---------------------------------------------------------------

		// buffers

		private RichVBO vbo; // main vertex buffer
		private BeamVBO beamVBO; // vertex buffer for beams only
		
		private IndexBufferObject facesIBO;
		private IndexBufferObject ordinaryEdgesIBO;
		private IndexBufferObject softEdgesIBO;
		private IndexBufferObject hardEdgesIBO;
		private IndexBufferObject nodesIBO;
		private IndexBufferObject middleNodesIBO;

		private IndexBufferObject visibleNodesIBO;
		
		// ===============================

		private HashSet<Node> visibleNodes;

		// -------------------------------

		// Data visualizer
		private IDataVisualizer dataVisualizer;
		// -------------------------------

		#endregion

		#region Constructor, initialization

		public Content(Mesh parentMesh)
		{
			this.parentMesh = parentMesh;

			this.elements = new List<Element>();
			this.beams = new List<Beam>();
			this.faces = new List<Element2D>();
			this.edges = new List<WingedEdge>();
			this.nodesEdgesIncidence = new Dictionary<Node, List<WingedEdge>>();
			this.edgeMiddleNodes = new List<Node>();
			this.beamNodesNotInFaces = new HashSet<Node>();
			this.nodeIndexMap = null;
			this.faceCentersPositions = null;
			this.beamCentersPositions = null;
			this.stickyNodes = new HashSet<Node>();

			vbo = null;
			beamVBO = null;
			facesIBO = ordinaryEdgesIBO = softEdgesIBO = hardEdgesIBO = nodesIBO = middleNodesIBO = null;

			this.visibleNodes = null;
			this.visibleNodesIBO = null;
		}

		public void TrimExcessMemory()
		{
			faces.TrimExcess();
			edges.TrimExcess();
			// nodesEdgesIncidence asi tezko zmensim
			beams.TrimExcess();
			edgeMiddleNodes.TrimExcess();
			elements.TrimExcess();
		}

		#endregion

		#region Properties

		public List<Element> Elements
		{
			get { return elements; }
		}
		public List<Beam> Beams
		{
			get { return beams; }
		}
		public List<Element2D> Faces
		{
			get { return faces; }
		}
		public List<WingedEdge> Edges
		{
			get { return edges; }
		}
		public Dictionary<Node, List<WingedEdge>> NodesEdgesIncidence
		{
			get { return nodesEdgesIncidence; }
		}
		public List<Node> EdgeMiddleNodes
		{
			get { return edgeMiddleNodes; }
		}
		public HashSet<Node> BeamNodesNotInFaces
		{
			get { return beamNodesNotInFaces; }
		}
		
		public bool VisibleNodesReady
		{
			get { return (this.visibleNodesIBO != null || this.visibleNodes != null); }
		}

		public HashSet<Node> VisibleNodes
		{
			get { return visibleNodes; }
			set { visibleNodes = value; }
		}

		public int ExternalNodesCount
		{
			get { return nodesEdgesIncidence.Count + edgeMiddleNodes.Count + beamNodesNotInFaces.Count; }
		}

		public Dictionary<Element2D, Vector2> FaceCentersPositions
		{
			get { return faceCentersPositions; }
			set { faceCentersPositions = value; }
		}

		public Dictionary<Beam, Vector2> BeamCentersPositions
		{
			get { return beamCentersPositions; }
			set { beamCentersPositions = value; }
		}

		public HashSet<Node> StickyNodes
		{
			get { return stickyNodes; }
			set { stickyNodes = value; }
		}

		//public bool VBOisReady
		//{
		//    get { return vbo != null; }
		//}

		public IDataVisualizer DataVisualizer
		{
			get { return dataVisualizer; }
			set { dataVisualizer = value; }
		}

		#endregion

		#region Buffer creating, deleting

		/// <summary>
		/// Creates vertex buffer object for whole mesh and index buffer objects for each surface object.
		/// </summary>
		public bool CreateBuffers(PropertyColorsMode colorMode, float softBorderLimit, float hardBorderLimit)
		{
            bool ready = false;
			if (RichVBO.IsSupported)
			{
#if !DEBUG
				try
				{
#endif
				
				DeleteBuffers(); // first delete old buffers if exist

				
				Dictionary<Element2D, int[]> faceIndexMap;
				//Dictionary<Node, int> nodeIndexMap;
				Dictionary<WingedEdge, int> edgeIndexMap;

				// create global Vertex buffer object
				this.vbo = new RichVBO();
				vbo.CreateFrom(faces, GetAllExternalNodes(), colorMode, softBorderLimit, hardBorderLimit, out faceIndexMap, out edgeIndexMap, out this.nodeIndexMap);

                // create vertex buffer object for beams
				if (this.beams.Count > 0)
					createBeamVBO((colorMode & PropertyColorsMode.Beams) != 0);

                // update colors
                UpdateAllColors(new HashSet<ISelectable>()/**/, colorMode, softBorderLimit, hardBorderLimit);

				// create index buffers
				createIndexBuffers(faceIndexMap, edgeIndexMap, this.nodeIndexMap, softBorderLimit, hardBorderLimit);
				// ----------------------------------------

				ready = true;
#if !DEBUG
				}
				catch (Exception)
				{
					this.vbo = null;
					this.nodeIndexMap = null;
					Scene.MeshShadingModel = ShadingModel.Flat;
                    ready = false;
				}
#endif

			}
			else
			{
				this.vbo = null;
				this.beamVBO = null;
				Scene.MeshShadingModel = ShadingModel.Flat;
                ready = false;
			}
            return ready;
		}

		private void createIndexBuffers(Dictionary<Element2D, int[]> faceIndexMap, Dictionary<WingedEdge, int> edgeIndexMap, Dictionary<Node, int> nodeIndexMap, float softBorderLimit, float hardBorderLimit)
		{
			List<int> indices = new List<int>(this.faces.Count * 3 /*approximate initial capacity*/);

			// create Index buffer object for faces
			foreach (Element2D face in faces)
				indices.AddRange(faceIndexMap[face]);

			if (indices.Count > 0)
				this.facesIBO = new IndexBufferObject(BeginMode.Triangles, indices.ToArray());
			// ------------------------------------
			// create Index buffer object for nodes
			indices = new List<int>(nodesEdgesIncidence.Count);
			foreach (Node n in GetSimpleExternalNodes())
				indices.Add(nodeIndexMap[n]);
			
			if (indices.Count > 0)
				this.nodesIBO = new IndexBufferObject(BeginMode.Points, indices.ToArray());
			// ------------------------------------
			// create Index buffer object for middle nodes in center of edges
			indices = new List<int>();
			foreach (Node n in this.edgeMiddleNodes)
				indices.Add(nodeIndexMap[n]);

			if (indices.Count > 0)
				this.middleNodesIBO = new IndexBufferObject(BeginMode.Points, indices.ToArray());
			// ------------------------------------
			// create Index buffer object for edges
			List<int> ordinaryEdgesIndices = new List<int>();
			List<int> softEdgesIndices = new List<int>();
			List<int> hardEdgesIndices = new List<int>();

			foreach (WingedEdge e in edges)
			{
				bool reversed;
				int index = RichVBO.DecodeEdgeIndex(edgeIndexMap[e], out reversed);
				
				int first = nodeIndexMap[(reversed) ? e.EndNode : e.BeginNode];
				int second = index;

				if (e.FeatureAngle >= hardBorderLimit)
				{
					hardEdgesIndices.Add(first);
					hardEdgesIndices.Add(second);
				}
				else if (e.FeatureAngle >= softBorderLimit)
				{
					softEdgesIndices.Add(first);
					softEdgesIndices.Add(second);
				}
				else
				{
					ordinaryEdgesIndices.Add(first);
					ordinaryEdgesIndices.Add(second);
				}
			}

			// ----------------------------------------
			if (ordinaryEdgesIndices.Count > 0)
				this.ordinaryEdgesIBO = new IndexBufferObject(BeginMode.Lines, ordinaryEdgesIndices.ToArray());
			if (softEdgesIndices.Count > 0)
				this.softEdgesIBO = new IndexBufferObject(BeginMode.Lines, softEdgesIndices.ToArray());
			if (hardEdgesIndices.Count > 0)
				this.hardEdgesIBO = new IndexBufferObject(BeginMode.Lines, hardEdgesIndices.ToArray());

		}

		private void createBeamVBO(bool beamPropertyColors)
		{
			this.beamVBO = new BeamVBO(beams.Count, beams, beamPropertyColors); 
		}

		public void DeleteBuffers()
		{
			if (this.vbo != null)
			{
				this.vbo.Dispose();
				this.vbo = null;
			}
			if (this.beamVBO != null)
			{
				this.beamVBO.Dispose();
				this.beamVBO = null;
			}
			if (this.facesIBO != null)
			{
				this.facesIBO.Dispose();
				this.facesIBO = null;
			}
			if (this.ordinaryEdgesIBO != null)
			{
				this.ordinaryEdgesIBO.Dispose();
				this.ordinaryEdgesIBO = null;
			}
			if (this.softEdgesIBO != null)
			{
				this.softEdgesIBO.Dispose();
				this.softEdgesIBO = null;
			}
			if (this.hardEdgesIBO != null)
			{
				this.hardEdgesIBO.Dispose();
				this.hardEdgesIBO = null;
			}
			if (this.nodesIBO != null)
			{
				this.nodesIBO.Dispose();
				this.nodesIBO = null;
			}
			if (this.middleNodesIBO != null)
			{
				this.middleNodesIBO.Dispose();
				this.middleNodesIBO = null;
			}
			if (this.visibleNodesIBO != null)
			{
				this.visibleNodesIBO.Dispose();
				this.visibleNodesIBO = null;
			}
		}

		public void CreateVisibleNodesBuffer(Rectangle window)
		{
			List<int> indices = new List<int>();

			foreach (KeyValuePair<Node, int> pair in this.nodeIndexMap)
			{
				if (visibleNodes.Contains(pair.Key))
				{
					indices.Add(pair.Value);
				}
			}

			if (this.visibleNodesIBO != null)
			{
				this.visibleNodesIBO.Dispose();
				this.visibleNodesIBO = null;
			}

			if (visibleNodes.Count > 0)
				this.visibleNodesIBO = new IndexBufferObject(BeginMode.Points, indices.ToArray());

		}

		#endregion

		#region Buffer updating

		public void UpdateNodeCoordinates()
		{
			IntPtr videoMemory;
			if (this.vbo != null && this.vbo.MapBuffer(BufferTarget.ArrayBuffer, this.vbo.VertexBufferID, BufferAccess.WriteOnly, out videoMemory))
			{
				unsafe
				{
					Vector3* items = (Vector3*)videoMemory.ToPointer();
					//int selectedColor = Utils.ColorToRgba32(Scene.SelectedNodeColor);
					//int nodeColor = 0;
					int index = 0;
					foreach (Element2D face in faces)
					{
						foreach (Node node in face.IterateThroughAllNodes())
						{
							items[index++] = node.Position;
						}
					}
					foreach (Node node in GetBeamAndMiddleNodes())
					{
						index = nodeIndexMap[node];
						items[index] = node.Position;
					}
				}

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

			if (this.beamVBO != null && this.beamVBO.MapBuffer(BufferTarget.ArrayBuffer, this.beamVBO.VertexBufferID, BufferAccess.WriteOnly, out videoMemory))
			{
				unsafe
				{
					Vector3* items = (Vector3*)videoMemory.ToPointer();
					//int selectedColor = Utils.ColorToRgba32(Scene.SelectedNodeColor);
					//int nodeColor = 0;
					int index = 0;
					foreach (Beam beam in beams)
					{
						items[index++] = beam.BeginNode.Position;
						items[index++] = beam.EndNode.Position;
					}
				}

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
		}

        public void UpdateAllColors(HashSet<ISelectable> selected, PropertyColorsMode colorMode, float edgeSoftBorderLimit, float edgeHardBorderLimit)
        {
            UpdateFaceColors(selected, colorMode);
            UpdateNodeColors(selected, colorMode);
            UpdateEdgeColors(selected, colorMode, edgeSoftBorderLimit, edgeHardBorderLimit);
            UpdateBeamColors(selected, colorMode);
        }

		public void UpdateNodeColors(HashSet<ISelectable> selected, PropertyColorsMode colorMode)
		{
			if (this.vbo == null)
				return;

			bool nodePropertyColors = (colorMode & PropertyColorsMode.Nodes) != 0;

			IntPtr videoMemory;

			if (!this.vbo.MapBuffer(BufferTarget.ArrayBuffer, this.vbo.NodeColorBufferID, BufferAccess.WriteOnly, out videoMemory))
				return;

			unsafe
			{
				int* items = (int*)videoMemory.ToPointer();
				int selectedColor = Utils.ColorToRgba32(Scene.SelectedNodeColor);
				int nodeColor = 0;

				foreach (Node node in GetAllExternalNodes())
				{
					int vertexIndex = this.nodeIndexMap[node];
					if (selected.Contains(node))
						items[vertexIndex] = selectedColor;
					else
					{
						if (nodePropertyColors)
							nodeColor = PropertyColorProvider.GetRGBA32(node.Property);
						else
							nodeColor = Utils.ColorToRgba32(Scene.NodesColor);
						items[vertexIndex] = nodeColor;
					}
				}
			}

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

		public void UpdateEdgeColors(HashSet<ISelectable> selected, PropertyColorsMode colorMode, float softBorderLimit, float hardBorderLimit)
		{
			if (this.vbo == null)
				return;

			bool edgePropertyColors = (colorMode & PropertyColorsMode.Edges) != 0;

			IntPtr videoMemory;

			if (!this.vbo.MapBuffer(BufferTarget.ArrayBuffer, this.vbo.EdgeColorBufferID, BufferAccess.WriteOnly, out videoMemory))
				return;

			unsafe
			{
				int* items = (int*)videoMemory.ToPointer();
				int selectedColor = Utils.ColorToRgba32(Scene.SelectedEdgeColor);
				int edgeColor = 0;

				int vertexIndex = 0;
				foreach (Element2D face in faces)
				{
					foreach (WingedEdge edge in face.IterateThroughAllEdges())
					{
						IFaceOfElement3D face1OfElement3D = edge.Face1 as IFaceOfElement3D;
						IFaceOfElement3D face2OfElement3D = edge.Face2 as IFaceOfElement3D;
						bool parentElementIsSelected = (face1OfElement3D != null && selected.Contains(face1OfElement3D.ParentElement)) || (face2OfElement3D != null && selected.Contains(face2OfElement3D.ParentElement)) || (face1OfElement3D == null && selected.Contains(edge.Face1)) || (face2OfElement3D == null && selected.Contains(edge.Face2));

						if (selected.Contains(edge) || parentElementIsSelected)
						{
							items[vertexIndex] = selectedColor;
						}
						else
						{
							if (edgePropertyColors)
								edgeColor = PropertyColorProvider.GetRGBA32(edge.Property);
							else if (edge.FeatureAngle >= hardBorderLimit)
								edgeColor = Utils.ColorToRgba32(Scene.HardBorderColor);
							else if (edge.FeatureAngle >= softBorderLimit)
								edgeColor = Utils.ColorToRgba32(Scene.SoftBorderColor);
							else
								edgeColor = Utils.ColorToRgba32(Scene.OrdinaryEdgeColor);
							items[vertexIndex] = edgeColor;
						}
						vertexIndex++;
					}
				}
			}

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

		public void UpdateFaceColors(HashSet<ISelectable> selected, PropertyColorsMode colorMode)
		{
			if (this.vbo == null)
				return;

			bool facePropertyColors = (colorMode & PropertyColorsMode.Faces) != 0;
			bool elementPropertyColors = (colorMode & PropertyColorsMode.Elements) != 0;

			int selectedFaceColor = Utils.ColorToRgba32(Scene.SelectedFaceColor);
			int selectedElementColor = Utils.ColorToRgba32(Scene.SelectedElementColor);
			int selectedFaceAndElementColor = Utils.ColorToRgba32(Scene.SelectedFaceAndElementColor);
			int ordinaryFaceColor = Utils.ColorToRgba32(Scene.FaceColor);
			int faceColor = ordinaryFaceColor;

			IntPtr videoMemory;

			if (!this.vbo.MapBuffer(BufferTarget.ArrayBuffer, this.vbo.FaceColorBufferID, BufferAccess.WriteOnly, out videoMemory))
				return;

			unsafe
			{
				int index = 0;
				int* items = (int*)videoMemory.ToPointer();

				foreach (Element2D face in faces)
				{
					// -------------------------------------------
					// nastaveni barvy
					IFaceOfElement3D faceOfElement = face as IFaceOfElement3D;
					bool containsFace = selected.Contains(face);
					bool containsParentElement = false;
					bool faceIs2DElement = (faceOfElement == null || faceOfElement.ParentElement == null);
					if (!faceIs2DElement)
						containsParentElement = selected.Contains(faceOfElement.ParentElement);

					if (containsParentElement & containsFace)
						faceColor = Utils.ColorToRgba32(Scene.SelectedFaceAndElementColor);
					else if (containsParentElement)
						faceColor = Utils.ColorToRgba32(Scene.SelectedElementColor);
					else if (containsFace)
					{
						if (faceIs2DElement)
							faceColor = Utils.ColorToRgba32(Scene.SelectedElementColor);
						else
							faceColor = Utils.ColorToRgba32(Scene.SelectedFaceColor);
					}
					else if (elementPropertyColors & (faceOfElement != null))
						faceColor = getRGBA32ColorForFace(face, faceOfElement.ParentElement.Property, hatchTwinElements: true);
					else if (facePropertyColors | (elementPropertyColors & (faceOfElement == null)))
						faceColor = getRGBA32ColorForFace(face, face.Property, hatchTwinElements: (faceOfElement == null)); // hatch only if it is 2D element, not face
					else
						faceColor = ordinaryFaceColor;
					// ------------------------------------------

					bool drawData = (dataVisualizer != null && dataVisualizer.DisplayColors) & !elementPropertyColors & !facePropertyColors;

					if (drawData)
					{
						Element element = faceIs2DElement ? (Element)face : (Element)faceOfElement.ParentElement;
						foreach (Node node in face.IterateThroughAllNodes())
						{
							int dataColor = dataVisualizer.GetDataColor(node, element);
							if (containsFace | containsParentElement) // if face is selected, invert color
								dataColor = Utils.InvertColor(dataColor) & 0x00FFFFFF; // zero alpha byte to mark color to be handled special in iso-areas shader
							items[index++] = dataColor;
						}
					}
					else
					{
						// ------------------------------------
						int count = face.NodeCount;
						for (int i = 0; i < count; i++)
							items[index + i] = faceColor;
						// ------------------------------------
						index += count;
					}
				}
			}

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

		public void UpdateBeamColors(HashSet<ISelectable> selected, PropertyColorsMode colorMode)
		{
			if (this.beamVBO == null)
				return;

			bool beamPropertyColors = (colorMode & PropertyColorsMode.Beams) != 0;

			IntPtr videoMemory;

			if (!this.beamVBO.MapBuffer(BufferTarget.ArrayBuffer, this.beamVBO.ColorBufferID, BufferAccess.WriteOnly, out videoMemory))
				return;

			unsafe
			{
				int* items = (int*)videoMemory.ToPointer();
				int selectedColor = Utils.ColorToRgba32(Scene.SelectedBeamColor);
				int beamColor = 0;
				int vertexIndex = 0;
				foreach (Beam beam in beams)
				{
					if (dataVisualizer != null && dataVisualizer.DisplayColors)
					{
						items[vertexIndex] = dataVisualizer.GetDataColor(beam.BeginNode, beam);
						items[vertexIndex + 1] = dataVisualizer.GetDataColor(beam.EndNode, beam);
						if (selected.Contains(beam))
						{
							items[vertexIndex] = Utils.InvertColor(items[vertexIndex], alpha: 252); // zero alpha byte to mark color to be handled special in iso-areas shader,
							items[vertexIndex + 1] = Utils.InvertColor(items[vertexIndex + 1], alpha: 252); // but blending is on, so I can't set alpha to zero, alpha is set almost to ze 1.0, but not entirely to help shader to distinguish selected entity
						}
					}
					else
					{
						if (selected.Contains(beam))
						{
							items[vertexIndex] = items[vertexIndex + 1] = selectedColor;
						}
						else
						{
							if (beamPropertyColors)
								beamColor = PropertyColorProvider.GetRGBA32(beam.Property);
							else
								beamColor = Utils.ColorToRgba32(Scene.BeamColor);
							items[vertexIndex] = items[vertexIndex + 1] = beamColor;
						}
					}
					vertexIndex += 2;
				}
			}

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

		#region Drawing

		/// <summary>
		/// Draws faces in mesh only
		/// </summary>
		public void DrawFacesOnly()
		{
			if (this.vbo != null)
			{
				vbo.DrawMinimum(this.facesIBO);
			}
			else
			{
				GL.Begin(BeginMode.Triangles);
				foreach (Element2D face in faces)
					face.Draw();
				GL.End();
			}
		}

		public void DrawFaces(HashSet<ISelectable> selectedItems, bool facePropertyColors, bool elementPropertyColors, Camera camera)
		{
			if (vbo != null)
			{
				vbo.DrawFaces(this.facesIBO);
			}
			else
			{
				GL.Begin(BeginMode.Triangles);
				foreach (Element2D face in faces)
				{
					// -------------------------------------------
					// nastaveni barvy
					IFaceOfElement3D faceOfElement = face as IFaceOfElement3D;
                    bool containsFace = selectedItems.Contains(face);
                    bool containsParentElement = false;
                    bool faceIs2DElement = (faceOfElement == null || faceOfElement.ParentElement == null);
                    if (!faceIs2DElement)
                        containsParentElement = selectedItems.Contains(faceOfElement.ParentElement);

                    if (containsParentElement && containsFace)
                        GL.Color3(Scene.SelectedFaceAndElementColor);
                    else if (containsParentElement)
                        GL.Color3(Scene.SelectedElementColor);
                    else if (containsFace)
                    {
                        if (faceIs2DElement)
                            GL.Color3(Scene.SelectedElementColor);
                        else
                            GL.Color3(Scene.SelectedFaceColor);
                    }
					else if (elementPropertyColors && faceOfElement != null)
					{
						GL.Color3(PropertyColorProvider.Get(faceOfElement.ParentElement.Property));
					}
					else if (facePropertyColors)
					{
						GL.Color3(PropertyColorProvider.Get(face.Property));
					}
					else
						GL.Color3(Scene.FaceColor);
					// ------------------------------------------

                   
					GL.Normal3(face.NormalVector);
					face.Draw();
				}
				GL.End();
			}
		}

		public void DrawOrdinaryAndSoftEdges(HashSet<ISelectable> selectedItems, float softBorderLimit, float hardBorderLimit, bool edgePropertyColors)
		{
			if (vbo != null)
			{
				vbo.DrawEdges(this.ordinaryEdgesIBO, Scene.EdgeLighting);
				vbo.DrawEdges(this.softEdgesIBO, Scene.EdgeLighting);
			}
			else
			{
				GL.Begin(BeginMode.Lines);
				foreach (WingedEdge edge in edges)
				{
					if (edge.FeatureAngle >= hardBorderLimit)
						continue;
					if (selectedItems.Contains(edge))
						GL.Color3(Scene.SelectedEdgeColor);
					else if (edgePropertyColors)
						GL.Color3(PropertyColorProvider.Get(edge.Property));
					else
					{
						if (edge.FeatureAngle >= softBorderLimit)
							GL.Color3(Scene.SoftBorderColor);
						else
							GL.Color3(Scene.OrdinaryEdgeColor);
					}
					GL.Vertex3(edge.BeginNode.Position);
					GL.Vertex3(edge.EndNode.Position);
				}
				GL.End();
			}
		}

		public void DrawHardBorderEdges(HashSet<ISelectable> selectedItems, float hardBorderLimit, bool edgePropertyColors)
		{
			if (vbo != null)
			{
				vbo.DrawEdges(this.hardEdgesIBO, Scene.EdgeLighting);
			}
			else
			{
				GL.Begin(BeginMode.Lines);
				foreach (WingedEdge edge in edges)
				{
					if (edge.FeatureAngle < hardBorderLimit)
						continue;
					if (selectedItems.Contains(edge))
						GL.Color3(Scene.SelectedEdgeColor);
					else if (edgePropertyColors)
						GL.Color3(PropertyColorProvider.Get(edge.Property));
					else
						GL.Color3(Scene.HardBorderColor);
					GL.Vertex3(edge.BeginNode.Position);
					GL.Vertex3(edge.EndNode.Position);
				}
				GL.End();
			}
		}

		public void DrawNodes(HashSet<ISelectable> selectedItems, bool nodePropertyColors, bool includeMiddleNodes)
		{
			if (vbo != null)
			{
				vbo.DrawNodes(this.nodesIBO);
				if (includeMiddleNodes)
					vbo.DrawNodes(this.middleNodesIBO);
			}
			else
			{
				GL.Begin(BeginMode.Points);
				foreach (Node n in (includeMiddleNodes) ? GetAllExternalNodes() : GetSimpleExternalNodes())
					DrawSingleNode(n, selectedItems.Contains(n), nodePropertyColors);
				GL.End();
			}
		}

		public static void DrawSingleNode(Node n, bool isSelected, bool nodePropertyColors)
		{
			if (isSelected)
				GL.Color3(Scene.SelectedNodeColor);
			else if (nodePropertyColors)
				GL.Color3(PropertyColorProvider.Get(n.Property));
			else
				GL.Color3(Scene.NodesColor);
			GL.Vertex3(n.Position);
		}

        public void DrawBeams(HashSet<ISelectable> selectedItems, bool beamPropertyColors)
		{
			GL.LineWidth(Scene.BeamWidth);
			if (Scene.LineSmooth)
			{
				GL.Enable(EnableCap.LineSmooth);
				GL.Enable(EnableCap.Blend);
			}
			//GL.Color3(Scene.BeamColor);
			// ----------------------------------------------
			if (this.beamVBO != null) // buffer mode
			{
				this.beamVBO.Draw();
			}
			else // Immediate mode
			{
				GL.Begin(BeginMode.Lines);
				foreach (Beam beam in beams)
				{
                    if (selectedItems.Contains(beam))
                        GL.Color3(Scene.SelectedBeamColor);
                    else if (beamPropertyColors)
						GL.Color3(PropertyColorProvider.Get(beam.Property));
                    else
                        GL.Color3(Scene.BeamColor);
					GL.Vertex3(beam.BeginNode.Position);
					GL.Vertex3(beam.EndNode.Position);
				}
				GL.End();
			}
			// ----------------------------------------------
			if (Scene.LineSmooth)
			{
				GL.Disable(EnableCap.LineSmooth);
				GL.Disable(EnableCap.Blend);
			}
		}

		public void DrawVisibleNodes(HashSet<ISelectable> selectedItems, bool nodePropertyColors, bool drawNodeNumbers)
		{
			if (this.vbo != null && this.visibleNodesIBO != null)
				this.vbo.DrawNodes(this.visibleNodesIBO);
			else // vykreslit pomoci immediate mode
			{
				GL.Begin(BeginMode.Points);
				foreach (Node n in visibleNodes)
					DrawSingleNode(n, selectedItems.Contains(n), nodePropertyColors);
				GL.End();
			}
			if (drawNodeNumbers)
				drawVisibleNodeNumbers(selectedItems); /**/ // !!!
		}

		public void DrawVisibleElementNumbers(HashSet<ISelectable> selectedItems)
		{
			if (faceCentersPositions == null)
				return;

			int[] viewport;
			Scene.ExtractViewport(out viewport);

			//double[] modelview;
			//double[] projection;
			//Scene.ExtractMatrices(out viewport, out modelview, out projection);

			//Vector3 cameraDir = camera.GetDirection();
			RectangleF area = new RectangleF(0f, 0f, 0f, 0f);
			textPrinter.Begin(); // sets orthografic projection
			foreach (KeyValuePair<Element2D, Vector2> pair in faceCentersPositions)
			{
				Element2D face = pair.Key;
				Vector2 winPos = pair.Value;
				// --------------------------------------------------------------------------

				IFaceOfElement3D faceOfElement = face as IFaceOfElement3D;
				int id;
				bool selected;
				if (faceOfElement == null)
				{
					id = face.ID;
					selected = selectedItems.Contains(face);
				}
				else if (faceOfElement.ParentElement == null)
				{
					continue;
				}
				else
				{
					id = faceOfElement.ParentElement.ID;
					selected = selectedItems.Contains(faceOfElement.ParentElement);
				}
				// --------------------------------------------------------------------------
				area.X = winPos.X - 10;
				area.Y = viewport[3] - winPos.Y - 8;
				textPrinter.Print(id.ToString(), textFont, selected ? Scene.SelectedElementNumbersColor : Scene.ElementNumbersColor, area);
			}
			textPrinter.End(); // restores projection matrix
		}

		private void drawVisibleNodeNumbers(HashSet<ISelectable> selectedItems)
		{
			int[] viewport;
			double[] modelview;
			double[] projection;
			Scene.ExtractMatrices(out viewport, out modelview, out projection);

			Vector3 winPos;

			RectangleF area = new RectangleF(0f, 0f, 0f, 0f);
			textPrinter.Begin(); // sets orthografic projection
			foreach (Node n in this.visibleNodes)
			{
				if (stickyNodes.Contains(n))
					continue;
				Utils.GluProject(n.Position, modelview, projection, viewport, out winPos);
				area.X = winPos.X + 1;
				area.Y = viewport[3] - winPos.Y + 1;
				textPrinter.Print(n.ID.ToString(), textFont, selectedItems.Contains(n) ? Scene.SelectedNodeColor : Scene.NodeNumbersColor, area);
			}
			textPrinter.End(); // restores projection matrix
		}

		public void DrawVisibleBeamNumbers(HashSet<ISelectable> selectedItems)
		{
			if (beams.Count == 0)
				return;
			int[] viewport;
			double[] modelview;
			double[] projection;
			Scene.ExtractMatrices(out viewport, out modelview, out projection);

			Vector3 winPos;
			RectangleF area = new RectangleF(0f, 0f, 0f, 0f);
			textPrinter.Begin(); // sets orthografic projection
			//TextPrinterOptions options = TextPrinterOptions.NoCache; /* !!! */
			foreach (Beam beam in beams)
			{
				if (!visibleNodes.Contains(beam.BeginNode) && !visibleNodes.Contains(beam.EndNode))
					continue;
				if (stickyNodes.Contains(beam.BeginNode) && stickyNodes.Contains(beam.EndNode))
					continue;
				Utils.GluProject(beam.GetCenter(), modelview, projection, viewport, out winPos);
				//if (winPos.Z >= 0f && winPos.Z <= 1f)
				//{
				bool selected = selectedItems.Contains(beam);
				area.X = winPos.X - 10;
				area.Y = viewport[3] - winPos.Y - 8;
				textPrinter.Print(beam.ID.ToString(), textFont, selected ? Scene.SelectedElementNumbersColor : Scene.ElementNumbersColor, area);
				//}
			}
			textPrinter.End(); // restores projection matrix
		}

		private int getRGBA32ColorForFace(Element2D face, Property baseProperty, bool hatchTwinElements)
		{
            if (hatchTwinElements && face.HasTwinElements)
			{
				//if (baseProperty.IsZero)
				//{
				//	return PropertyColorProvider.ZeroColor_RGBA32 & 0x00FFFFFF; // return zero color, set alpha to zero
				//}
				// value 0 and 255 have special meaning. 0 means no color in this slot, 255 in alpha slot means no twin elements, other number is 1-based index to color palette
				int colorPaletteIndex = parentMesh.Statistics.GetIndexOfPropertyInElementPropertyColorsPalette(baseProperty);
				int shift = 24;
				int color = (colorPaletteIndex + 1) << shift; // indexes are 1-based
				foreach (Element2D twin in face.GetTwinElements())
				{
					colorPaletteIndex = parentMesh.Statistics.GetIndexOfPropertyInElementPropertyColorsPalette(twin.Property);
					if (!twin.Property.IsZero) // ignore zero property of twin elements (zero base property is not ignored)
					{
						shift -= 8;
						if (shift < 0) // exceeded limit of 4 representable colors
						{
							return PropertyColorProvider.GetRGBA32(baseProperty) & 0x00FFFFFF; // return original color, set alpha to zero
						}
						color |= (colorPaletteIndex + 1) << shift; // indexes are 1-based
					}
				}
				return color;
			}
			else
			{
				return PropertyColorProvider.GetRGBA32(baseProperty) | unchecked((int)0xFF000000); // return original color with alpha set to 255
			}
		}

		public static void DrawTextLabels(KeyValuePair<string, Vector2>[] textPositions, float windowHeight)
		{
			RectangleF area = new RectangleF(0f, 0f, 0f, 0f);
			textPrinter.Begin(); // sets orthografic projection
			foreach (var textPosition in textPositions)
			{
				Vector2 winPos = textPosition.Value;
                area.X = winPos.X + 1;
				area.Y = windowHeight - winPos.Y + 1;
				textPrinter.Print(textPosition.Key, textFont, Scene.LabelColor, area);
			}
			textPrinter.End(); // restores projection matrix
		}

		#endregion

		#region Other public methods

		public IEnumerable<Node> GetSimpleExternalNodes()
		{
			foreach (Node n in nodesEdgesIncidence.Keys)
				yield return n;
			foreach (Node n in beamNodesNotInFaces)
				yield return n;
		}

		public IEnumerable<Node> GetAllExternalNodes()
		{
			foreach (Node n in nodesEdgesIncidence.Keys)
				yield return n;
			foreach (Node n in this.edgeMiddleNodes)
				yield return n;
			foreach (Node n in beamNodesNotInFaces)
				yield return n;
		}

		public IEnumerable<Node> GetBeamAndMiddleNodes()
		{
			foreach (Node n in this.edgeMiddleNodes)
				yield return n;
			foreach (Node n in beamNodesNotInFaces)
				yield return n;
		}

		public IEnumerable<Node> GetBeamNodesNotInFacesIncludeMiddleNodes()
		{
			foreach (Node node in beamNodesNotInFaces)
				yield return node;
			foreach (Beam beam in beams)
			{
				QuadraticBeam q = beam as QuadraticBeam;
				if (q != null && !nodesEdgesIncidence.ContainsKey(q.MiddleNode))
					yield return q.MiddleNode;
			}
		}

		public void ClearSurface()
		{
			this.faces = new List<Element2D>();
			this.edges = new List<WingedEdge>();
			this.nodesEdgesIncidence = new Dictionary<Node, List<WingedEdge>>();
			this.edgeMiddleNodes = new List<Node>();
			this.nodeIndexMap = null;
			this.visibleNodes = null;
		}

		#endregion

	}
}
