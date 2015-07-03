using MeshEditor.CoreInterface;
using MeshEditor.Cuts;
using MeshEditor.Data;
using MeshEditor.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Layers
{
	public class IsoSurfaceSection : CrossSection
	{

		#region Fields, Properties, Constructor

		double sectionValue;

		[DisplayName("Section Value")]
		public double SectionValue
		{
			get { return sectionValue; }
			set
			{
				if (sectionValue != value)
				{
					sectionValue = value;
					GeometryChanged = true;
				}
			}
		}

		[Browsable(false)]
		public new float RelativeOffset { get { return 0f; } set { } }

		public IsoSurfaceSection(double sectionValue)
			: base(Vector3.Zero, 0f, 0f)
		{
			this.SectionValue = sectionValue;
			Name = "Iso-surface section";
		}

		#endregion

		#region Private methods

		private void updateGeometry(Mesh mesh, IDataVisualizer dataVisualizer, bool elementPropertyColors)
		{
			if (worker != null) // work in progress
				return;

			crossedElements = new List<ElementCountPair>();
			intersectionsIndexMap = null;

			List<Vector3> vertices = new List<Vector3>();
			List<int> triangleVertexIndices = new List<int>();
			List<int> edgeVertexIndices = new List<int>();
			List<int> lineVertexIndices = new List<int>();
			List<Vector3> normals = new List<Vector3>();

			Debug.Assert(worker == null);
			worker = new BackgroundWorker();

			worker.DoWork += (s, e) =>
			{
				foreach (Element element in mesh.Elements)
				{
					if (!ShowSectionThroughHiddenElements && mesh.HiddenElements.Contains(element))
						continue;

					Vector3[] intersections;
					Vector3 intersectionPlaneNormal;
					if (!getIntersectionsWithElement(element, dataVisualizer, out intersections, out intersectionPlaneNormal))
						continue;

					if (intersections.Length == 2)
					{
						Element2D element2D = element as Element2D;
						if (element2D != null)
						{
							lineVertexIndices.Add(vertices.Count);
							vertices.Add(intersections[0]);
							normals.Add(element2D.NormalVector);
							lineVertexIndices.Add(vertices.Count);
							vertices.Add(intersections[1]);
							normals.Add(element2D.NormalVector);
						}
						continue;
					}

					Vector3 pivot = Vector3.Zero; // pivot is center of intersection area
					for (int i = 0; i < intersections.Length; i++)
					{
						pivot += intersections[i];
					}
					pivot /= (float)intersections.Length;

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
						float intersectionAngle = Utilities.Functions.GetAngleInDegreesBetweenUnitVectors_0_360(firstVector, secondVector, intersectionPlaneNormal);
						intersectionAngles[i] = intersectionAngle;
					}

					int[] indices = new int[intersections.Length];
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
						vertices.Add(intersections[indices[0]]);

						edgeVertexIndices.Add(vertices.Count);
						triangleVertexIndices.Add(vertices.Count);
						vertices.Add(intersections[indices[i]]);
						edgeVertexIndices.Add(vertices.Count);
						triangleVertexIndices.Add(vertices.Count);
						vertices.Add(intersections[indices[i + 1]]);
					}

					edgeVertexIndices.Add(/*lastIndex = */vertices.Count - 1);
					edgeVertexIndices.Add(firstIndex);
					edgeVertexIndices.Add(firstIndex);
					edgeVertexIndices.Add(firstIndex + 1);

					int vertexesPerElement = vertices.Count - firstIndex;
					for (int i = 0; i < vertexesPerElement; i++)
						normals.Add(intersectionPlaneNormal);

					crossedElements.Add(new ElementCountPair(element, vertices.Count - firstIndex));

				} // end of element loop
			};

			worker.RunWorkerCompleted += (s, e) =>
			{
				deleteBuffers();

				if (vertices.Count > 0)
				{
					int defaultColor = GetDefaultFaceColor(dataVisualizer);
					vbo = new VBO(BeginMode.Triangles, vertices.ToArray(), Enumerable.Repeat(defaultColor, vertices.Count).ToArray(), normals.ToArray());
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

			worker.RunWorkerAsync(); // dow work

			GeometryChanged = false;
		}

		private bool getIntersectionsWithElement(Element element, IDataVisualizer dataVisualizer, out Vector3[] intersections, out Vector3 intersectionPlaneNormal)
		{
			Debug.Assert(dataVisualizer != null);

			double[] nodeValues = new double[element.NodeCount];
			int index = 0;
			double minValue = double.MaxValue, maxValue = double.MinValue;
			foreach (Node node in element.IterateThroughAllNodes())
			{
				double value = dataVisualizer.GetDataValue(node);
				nodeValues[index++] = value;
				minValue = Math.Min(minValue, value);
				maxValue = Math.Max(maxValue, value);
			}

			intersectionPlaneNormal = Vector3.Zero;
			intersections = null;

			if (!Utilities.Functions.ValueIsInInterval(SectionValue, minValue, maxValue))
			{
				return false;
			}

			List<EdgeIntersection> intersectionInfoList = new List<EdgeIntersection>(element.GetAllIntersectionsOfEdgesDataIsoSurface(SectionValue, nodeValues));

			if (intersectionInfoList.Count < 2)
			{
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
				//else // if exists, remove duplicate - no need, intersectionInfoList is not returned out of function like in CrossSection class
				//{
				//	intersectionInfoList.RemoveAt(i--);
				//}
			}

			if (uniqueCount != intersections.Length)
			{
				Array.Resize(ref intersections, uniqueCount); // trim excess
			}

			if (intersections.Length < 2)
			{
				return false;
			}

			if (intersections.Length > 2)
			{
				intersectionPlaneNormal = Vector3.Cross(intersections[2] - intersections[0], intersections[1] - intersections[0]);
				intersectionPlaneNormal.Normalize();
			}

			return true;
		}
		
		#endregion

		#region Overrides

		public override bool IsCrossSectionPlane
		{
			get { return false; }
		}

		protected override int GetDefaultFaceColor(IDataVisualizer dataVisualizer)
		{
			if (dataVisualizer == null)
				return base.GetDefaultFaceColor(dataVisualizer);
			return dataVisualizer.GetColorForDataValue(SectionValue);
		}

		public override void Update(Mesh mesh, IDataVisualizer dataVisualizer, bool elementPropertyColors)
		{
			Debug.Assert(UpdateNeeded);
			Debug.Assert(dataVisualizer != null);
			if (dataVisualizer == null)
				return;
			updateGeometry(mesh, dataVisualizer, elementPropertyColors);
		}

		public override string ToString()
		{
			return string.Format("{0} Value:{1:G3}", Name, sectionValue);
		}

		#endregion

	}
}
