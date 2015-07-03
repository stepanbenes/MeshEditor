using MeshEditor.CoreInterface;
using MeshEditor.Cuts;
using MeshEditor.Data;
using MeshEditor.DataVisualizer.Mathematics;
using MeshEditor.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Layers
{
	public class CrossSection : ILayer
	{

		#region Helper structs

		protected struct ElementCountPair
		{
			public Element Element;
			public int Count;

			public ElementCountPair(Element element, int count)
			{
				this.Element = element;
				this.Count = count;
			}
		}

		#endregion

		#region Fields, constructor

		protected VBO vbo;
		protected IndexBufferObject trianglesIBO, edgesIBO, linesIBO;

		protected List<ElementCountPair> crossedElements;
		protected Dictionary<EdgeIntersection, List<int>> intersectionsIndexMap;

		protected BackgroundWorker worker;

		private float offset, scaledOffset;

		private readonly float maxOffset;

		private bool showSectionThroughHiddenElements;

		/// <summary>
		/// Creates instance of CrossSection class
		/// </summary>
		/// <param name="pointOnPlane">Point that lies on the cross-section plane</param>
		/// <param name="planeNormal">Unit normal vector of the cross-section plane</param>
		public CrossSection(Vector3 pointOnPlane, Vector3 planeNormal)
		{
			float projection;
			Vector3.Dot(ref pointOnPlane, ref planeNormal, out projection);
			init(planeNormal, offset: projection);
		}

		/// <summary>
		/// Creates instance of CrossSection class
		/// </summary>
		/// <param name="planeNormal">Unit normal vector of the cross-section plane</param>
		/// <param name="offset">offset of the cross-section plane position</param>
		public CrossSection(Vector3 planeNormal, float offset, float maxOffset)
		{
			this.maxOffset = maxOffset;
			init(planeNormal, offset);
		}

		#endregion

		#region Properties

		[ReadOnly(true)]
		public Vector3 Normal { get; set; }
		
		[DisplayName("Relative Offset")]
		public float RelativeOffset
		{
			get { return offset; }
			set
			{
				if (offset != value)
				{
					offset = value;
					onUpdateOffset();
					GeometryChanged = true;
				}
			}
		}

		[Browsable(false)]
		public virtual bool IsCrossSectionPlane
		{
			get { return true; }
		}

		[Browsable(false)]
		public bool ShowSectionThroughHiddenElements
		{
			get { return showSectionThroughHiddenElements; }
			set
			{
				if (showSectionThroughHiddenElements != value)
				{
					showSectionThroughHiddenElements = value;
					GeometryChanged = true;
				}
			}
		}

		// --- ILayer members ------------------------
		public string Name { get; set; }
		public bool Visible { get; set;	}
		[Browsable(false)]
		public bool GeometryChanged { get; set; }
		[Browsable(false)]
		public bool ColorsChanged { get; set; }
		[Browsable(false)]
		public bool UpdateNeeded
		{
			get { return GeometryChanged || ColorsChanged; }
		}

		[Description("Describes rendering mode of this layer. If None is set, style is inherited from global display style setting.")]
		[DisplayName("Display Style")]
		public RenderMode DisplayStyle { get; set; }

		public event EventHandler RedrawNeeded;
		// -------------------------------------------

		#endregion

		#region Public methods

		public void Draw(IDataVisualizer dataVisualizer, RenderMode defaultDisplayStyle, bool elementPropertyColors)
		{
			if (vbo == null)
				return;

			RenderMode style = (DisplayStyle == RenderMode.None) ? defaultDisplayStyle : DisplayStyle;

			bool drawFaces = (style & RenderMode.Faces) != 0;
			bool drawLines = (style & RenderMode.AllLines) != 0;
			bool isPlanar = IsCrossSectionPlane;

			bool drawData = dataVisualizer != null && !elementPropertyColors;

			if (isPlanar)
			{
				GL.Normal3(Normal);
			}

			// FACES
			if (drawFaces && trianglesIBO != null)
			{
				GL.PolygonOffset(1f, 1f);

				GL.Enable(EnableCap.PolygonOffsetFill);

				if (Scene.FaceLighting)
					GL.Enable(EnableCap.Lighting);
				else
					GL.Disable(EnableCap.Lighting);

				if (drawData)
					dataVisualizer.BeginDraw(Scene.FaceLighting);

				vbo.Draw(trianglesIBO, bindColors: true, bindNormals: Scene.FaceLighting && !isPlanar);

				if (drawData)
					dataVisualizer.EndDraw();

				GL.Disable(EnableCap.PolygonOffsetFill);
			}

			// ORDINARY LINES
			if (drawLines)
			{
				if (Scene.EdgeLighting)
					GL.Enable(EnableCap.Lighting);
				else
					GL.Disable(EnableCap.Lighting);

				if (edgesIBO != null)
				{
					GL.LineWidth(Scene.OrdinaryEdgeWidth);
					GL.Color3(Scene.OrdinaryEdgeColor);
					
					if (drawFaces)
					{
						vbo.Draw(edgesIBO, bindColors: false, bindNormals: Scene.EdgeLighting && !isPlanar);
					}
					else
					{
						if (drawData)
							dataVisualizer.BeginDraw(Scene.EdgeLighting);

						vbo.Draw(edgesIBO, bindColors: true, bindNormals: Scene.EdgeLighting && !isPlanar);

						if (drawData)
							dataVisualizer.EndDraw();
					}
				}

				if (linesIBO != null)
				{
					GL.LineWidth(Scene.BorderEdgeWidth);
					GL.Color3(Scene.HardBorderColor);

					if ((defaultDisplayStyle & RenderMode.Faces) != 0)
					{
						vbo.Draw(linesIBO, bindColors: false, bindNormals: Scene.EdgeLighting && !isPlanar);
					}
					else
					{
						if (drawData)
							dataVisualizer.BeginDraw(Scene.EdgeLighting);

						vbo.Draw(linesIBO, bindColors: true, bindNormals: Scene.EdgeLighting && !isPlanar);

						if (drawData)
							dataVisualizer.EndDraw();
					}
				}
			}

			// POINTS
			if ((style & RenderMode.Points) != 0)
			{
				GL.Disable(EnableCap.Lighting);
				GL.PointSize(Scene.PointSize);
				GL.Color3(Scene.NodesColor);

				if (drawFaces || drawLines)
				{
					vbo.Draw(BeginMode.Points, bindColors: false, bindNormals: false);
				}
				else
				{
					if (drawData)
						dataVisualizer.BeginDraw(lightingEnabled: false);
					vbo.Draw(BeginMode.Points, bindColors: true, bindNormals: false);
					if (drawData)
						dataVisualizer.EndDraw();
				}
			}
		}

		public virtual void Update(Mesh mesh, IDataVisualizer dataVisualizer, bool elementPropertyColors)
		{
			Debug.Assert(UpdateNeeded);
			if (GeometryChanged)
			{
				updateGeometry(mesh, dataVisualizer, elementPropertyColors);
			}
			else if (ColorsChanged)
			{
				UpdateColors(dataVisualizer, elementPropertyColors);
			}
		}

		public void Dispose()
		{
			deleteBuffers();
			intersectionsIndexMap = null;
			crossedElements = null;
			RedrawNeeded = null;
		}

		public override string ToString()
		{
			return string.Format("{0} {1}:{2:G3}", Name, Normal, offset);
		}

		#endregion

		#region Protected methods

		protected void UpdateColors(IDataVisualizer dataVisualizer, bool elementPropertyColors)
		{
			if (vbo == null) // intersection of plane with mesh is empty (or the worker is not done yet)
			{
				// Do nothing
				ColorsChanged = false;
				return;
			}

			if (worker != null && worker.IsBusy) // worker is still working, return, it will be redrawn in next call
				return;

			if (elementPropertyColors)
			{
				vbo.ChangeColors(getPropertyColors());
			}
			else if (dataVisualizer != null && IsCrossSectionPlane)
			{
				vbo.ChangeColors(getIndexDataColorPairs(dataVisualizer));
			}
			else
			{
				int defaultColor = GetDefaultFaceColor(dataVisualizer);
				vbo.ChangeColors(defaultColor);
			}

			ColorsChanged = false;
		}

		protected virtual int GetDefaultFaceColor(IDataVisualizer dataVisualizer)
		{
			return Utilities.Functions.ColorToRgba32(Scene.FaceColor);
		}

		protected void deleteBuffers()
		{
			if (vbo != null)
			{
				vbo.Dispose();
				vbo = null;
			}
			if (trianglesIBO != null)
			{
				trianglesIBO.Dispose();
				trianglesIBO = null;
			}
			if (edgesIBO != null)
			{
				edgesIBO.Dispose();
				edgesIBO = null;
			}
			if (linesIBO != null)
			{
				linesIBO.Dispose();
				linesIBO = null;
			}
		}

		protected virtual void OnRedrawNeeded()
		{
			if (RedrawNeeded != null)
				RedrawNeeded(this, EventArgs.Empty);
		}

		#endregion

		#region Private methods

		private void updateGeometry(Mesh mesh, IDataVisualizer dataVisualizer, bool elementPropertyColors)
		{
			if (worker != null) // work in progress
				return;

			crossedElements = new List<ElementCountPair>();
			intersectionsIndexMap = new Dictionary<EdgeIntersection, List<int>>();

			List<Vector3> vertices = new List<Vector3>();
			List<int> triangleVertexIndices = new List<int>();
			List<int> edgeVertexIndices = new List<int>();
			List<int> lineVertexIndices = new List<int>();

			Debug.Assert(worker == null);
			worker = new BackgroundWorker();

			worker.DoWork += (s, e) =>
			{
				foreach (Element element in mesh.Elements)
				{
					if (!ShowSectionThroughHiddenElements && mesh.HiddenElements.Contains(element))
						continue;

					List<EdgeIntersection> intersectionInfoList;
					Vector3[] intersections;
					if (!getIntersectionsWithElement(element, dataVisualizer, out intersectionInfoList, out intersections))
						continue;

					int[] indices;

					if (intersections.Length == 2)
					{
						if (element is Element2D)
						{
							indices = new int[] { 0, 1 };
							
							lineVertexIndices.Add(vertices.Count);
							//vertices.Add(intersections[0]);
							addIntersectionVertex(vertices, intersections, intersectionInfoList, indices, 0);

							lineVertexIndices.Add(vertices.Count);
							//vertices.Add(intersections[1]);
							addIntersectionVertex(vertices, intersections, intersectionInfoList, indices, 1);
						}
						continue;
					}

					Vector3 pivot = Vector3.Zero;
					for (int i = 0; i < intersections.Length; i++)
					{
						pivot += intersections[i];
					}
					pivot /= (float)intersections.Length;

					//float[] intersectionAngles = new float[intersections.Count];
					Vector3 firstVector = intersections[0] - pivot;

					if (firstVector == Vector3.Zero) // all intersections are same, do not cut element - section plane incides only with one point in element
						continue; // TODO: vyresit nulovy vektor nebo blizky nule

					firstVector.Normalize();
					float[] intersectionAngles = new float[intersections.Length];
					intersectionAngles[0] = 0f;
					for (int i = 1; i < intersections.Length; i++)
					{
						Vector3 secondVector = intersections[i] - pivot;
						//System.Diagnostics.Debug.Assert(secondVector != Vector3.Zero);
						if (secondVector == Vector3.Zero) // TODO: vyresit nulovy vektor nebo blizky nule
							continue;
						secondVector.Normalize();
						float intersectionAngle = Utilities.Functions.GetAngleInDegreesBetweenUnitVectors_0_360(firstVector, secondVector, this.Normal);
						intersectionAngles[i] = intersectionAngle;
					}

					indices = new int[intersections.Length];
					for (int i = 0; i < indices.Length; i++)
					{
						indices[i] = i;
					}

					Comparison<int> compareAngles = (index1, index2) => intersectionAngles[index1].CompareTo(intersectionAngles[index2]);

					Array.Sort(indices, compareAngles);

					int firstIndex = vertices.Count;

					// add vertexes to vertex list and add indexes of edge vertexes to edgeVertexIndices list
					for (int i = 1; i < intersections.Length - 1; i++)
					{
						triangleVertexIndices.Add(vertices.Count);
						addIntersectionVertex(vertices, intersections, intersectionInfoList, indices, 0);

						edgeVertexIndices.Add(vertices.Count);
						triangleVertexIndices.Add(vertices.Count);
						addIntersectionVertex(vertices, intersections, intersectionInfoList, indices, i);
						edgeVertexIndices.Add(vertices.Count);
						triangleVertexIndices.Add(vertices.Count);
						addIntersectionVertex(vertices, intersections, intersectionInfoList, indices, i + 1);
					}

					int lastIndex = vertices.Count - 1;

					edgeVertexIndices.Add(lastIndex);
					edgeVertexIndices.Add(firstIndex);
					edgeVertexIndices.Add(firstIndex);
					edgeVertexIndices.Add(firstIndex + 1);

					crossedElements.Add(new ElementCountPair(element, lastIndex - firstIndex + 1));

				} // end of element loop

				// TODO: catch exceptions?
			};

			worker.RunWorkerCompleted += (s, e) =>
			{
				deleteBuffers();

				if (vertices.Count > 0)
				{
					int defaultColor = GetDefaultFaceColor(dataVisualizer);
					vbo = new VBO(BeginMode.Triangles, vertices.ToArray(), Enumerable.Repeat(defaultColor, vertices.Count).ToArray());
				}
				if (triangleVertexIndices.Count > 0)
				{
					trianglesIBO = new IndexBufferObject(BeginMode.Triangles, triangleVertexIndices.ToArray());
				}
				if (edgeVertexIndices.Count > 0)
				{
					edgesIBO = new IndexBufferObject(BeginMode.Lines, edgeVertexIndices.ToArray());
				}
				if (lineVertexIndices.Count > 0)
				{
					linesIBO = new IndexBufferObject(BeginMode.Lines, lineVertexIndices.ToArray());
				}

				UpdateColors(dataVisualizer, elementPropertyColors);

				// inform UI about the fact that work is done (redraw scene)
				OnRedrawNeeded();

				worker = null;
			};

			worker.RunWorkerAsync(); // do work

			GeometryChanged = false; // no need to call this method next frame
		}

		private void init(Vector3 planeNormal, float offset)
		{
			this.Normal = planeNormal;
			this.RelativeOffset = offset;
			onUpdateOffset();
			Name = "Cross-section";
			DisplayStyle = RenderMode.FacesLines;
			Visible = true;
			GeometryChanged = ColorsChanged = true;
		}

		private void onUpdateOffset()
		{
			// map from <0,1> interval to <-maxOffset, +maxOffset> interval
			scaledOffset = (offset - 0.5f) * 2f * maxOffset;
		}

		private IEnumerable<int> getPropertyColors()
		{
			Debug.Assert(crossedElements != null);

			int index = 0;
			for (int i = 0; i < crossedElements.Count; i++)
			{
				int toIndex = index + crossedElements[i].Count;
				for (; index < toIndex; index++)
				{
					yield return PropertyColorProvider.GetRGBA32(crossedElements[i].Element.Property);
				}
			}
		}

		private IEnumerable<KeyValuePair<int, int>> getIndexDataColorPairs(IDataVisualizer dataVisualizer)
		{
			Debug.Assert(dataVisualizer != null);
			Debug.Assert(intersectionsIndexMap != null);

			foreach (var pair in intersectionsIndexMap)
			{
				// TODO: Take into consideration value of dataVisualizer.DisplayColors.
				int color = Utilities.Functions.InterpolateTwoColors(dataVisualizer.GetDataColor(pair.Key.Node1, element: null), dataVisualizer.GetDataColor(pair.Key.Node2, element: null), pair.Key.T);
				foreach (int index in pair.Value)
				{
					yield return new KeyValuePair<int, int>(index, color);
				}
			}
		}

		private bool getIntersectionsWithElement(Element element, IDataVisualizer dataVisualizer, out List<EdgeIntersection> intersectionInfoList, out Vector3[] intersections)
		{
			float minDistance = float.MaxValue, maxDistance = float.MinValue;
			foreach (Node node in element.IterateThroughAllNodes())
			{
				float distance = Vector3.Dot(node.Position, this.Normal);
				minDistance = Math.Min(minDistance, distance);
				maxDistance = Math.Max(maxDistance, distance);
			}

			if (scaledOffset < minDistance || scaledOffset > maxDistance)
			{
				intersectionInfoList = null;
				intersections = null;
				return false;
			}

			intersectionInfoList = new List<EdgeIntersection>(element.GetAllIntersectionsOfEdgesWithPlane(this.Normal, scaledOffset + Common.EpsilonF /**/));

			if (intersectionInfoList.Count < 2)
			{
				intersections = null;
				return false;
			}

			HashSet<Vector3> hashTest = new HashSet<Vector3>();
			intersections = new Vector3[intersectionInfoList.Count];
			int uniqueCount = 0;
			for (int i = 0; i < intersectionInfoList.Count; i++)
			{
				Vector3 temp = intersectionInfoList[i].GetIntersection();
				if (hashTest.Add(temp)) // check if already exists
				{
					intersections[uniqueCount++] = temp;
				}
				else // if exists, remove duplicate
				{
					intersectionInfoList.RemoveAt(i--);
				}
			}

			if (uniqueCount != intersections.Length)
			{
				Array.Resize(ref intersections, uniqueCount); // trim excess
			}

			if (intersections.Length < 2)
			{
				return false;
			}

			return true;
		}

		private void addIntersectionVertex(List<Vector3> vertices, Vector3[] intersections, List<EdgeIntersection> intersectionInfoList, int[] indices, int i)
		{
			int globalIndex = vertices.Count;
			int localIndex = indices[i];
			List<int> indexList;
			if (!intersectionsIndexMap.TryGetValue(intersectionInfoList[localIndex], out indexList))
			{
				indexList = intersectionsIndexMap[intersectionInfoList[localIndex]] = new List<int>();
			}
			Debug.Assert(!indexList.Contains(globalIndex));
			indexList.Add(globalIndex);
			vertices.Add(intersections[localIndex]);
		}

		#endregion

	}
}
