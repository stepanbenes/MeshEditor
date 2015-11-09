using MeshEditor.CoreInterface;
using MeshEditor.Data;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.DataVisualizer.Graphics;
using MeshEditor.DataVisualizer.IO;
using MeshEditor.DataVisualizer.Mathematics;
using MeshEditor.DataVisualizer.UI;
using MeshEditor.Graphics;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text; 

namespace MeshEditor.DataVisualizer
{
	public abstract class DataVisualizerBase : IDataVisualizer, IDataVisualizerController
	{

		#region Fields, constructor

		Mesh mesh;
		Dictionary<DataInfo, int> dataIndexMap;

		protected int dataIndexCounter;

		protected HashSet<string> loadedFiles;

		IVisualizerSettings settings;

		IsoAreasShader isoAreasShader;

		Dictionary<Node, Vector3> originalNodePositions; // used for deformed model drawing

		VectorField vectorField; // used for drawing of vector arrows

		protected Dictionary<int, Node> nodeIndexMap;

		public DataVisualizerBase()
		{
			loadedFiles = new HashSet<string>();
			settings = new VisualizerSettings();
			settings.PropertyChanged += OnSettingsPropertyChanged;
		}

		public IEnumerable<string> LoadedFiles
		{
			get { return loadedFiles; }
		}
		
		#endregion

		#region IDataVisualizerController

		public IVisualizerSettings Settings
		{
			get { return settings; }
		}

		public Dictionary<DataInfo, int> DataIndexMap
		{
			get { return dataIndexMap; }
		}

		public void SetDeformationDataIndex(DataIndex dataIndex)
		{
			double maxXAbsValue = GetDataValueRange(dataIndex.Index).GetMaxAbsValue();
			double maxYAbsValue = GetDataValueRange(dataIndex.Index + 1).GetMaxAbsValue();
			double maxZAbsValue = GetDataValueRange(dataIndex.Index + 2).GetMaxAbsValue();
			double maxAbsValue = Math.Max(Math.Max(maxXAbsValue, maxYAbsValue), maxZAbsValue);
			settings.DeformationScale.SetDeformationDataIndex(dataIndex, maxAbsValue);
		}

		public virtual bool IsContinuousInTime(out IntervalD timeRange)
		{
			timeRange = IntervalD.Zero;
			return false;
		}

		#endregion

		#region Abstract methods

		public abstract void LoadData(IApproximationParameters approximationParameters, string[] filenames, LongOpNotifier longOpNotifier);

		public abstract void FinishUp();

		public abstract int GetDataColor(Node node, Element element);

		public abstract double GetDataValue(Node node, DataIndex dataIndex);

		public abstract double GetDataValue(Node node, DataIndex dataIndex, out float maxError);

		public abstract ApproximationQuality GetApproximationQuality(LongOpNotifier longOpNotifier);

		public abstract int[] GetEntitiesWithMaximumDataValue();

		public abstract int[] GetEntitiesWithMinimumDataValue();

		public abstract double GetMaximumDataValue();

		public abstract double GetMinimumDataValue();

		protected abstract IntervalD GetDataValueRange(int dataIndex);

		#endregion

		#region Virtual methods

		public virtual void Initialize(Mesh mesh)
		{
			Debug.Assert(mesh != null);

			this.mesh = mesh;
			dataIndexCounter = 0;
			if (loadedFiles == null)
				loadedFiles = new HashSet<string>();
			else
				loadedFiles.Clear();
			dataIndexMap = new Dictionary<DataInfo, int>();

			//settings.CurrentDataIndex = 0;
		}

		public virtual void BeginDraw(bool lightingEnabled)
		{
			if (settings.DrawIsoAreas && DisplayColors)
			{
				initIsoAreasShader();
				if (isoAreasShader.IsReady)
				{
					isoAreasShader.LightingEnabled = lightingEnabled;
					isoAreasShader.Use(settings.IsoAreasSubIntervalNumber, settings.ColorScale);
				}
			}
		}

		public virtual void EndDraw()
		{
			if (settings.DrawIsoAreas)
			{
				isoAreasShader.Unuse();
			}
		}

		public virtual void DrawItems(PropertyColorsMode propertyColorsMode)
		{
			// DRAW VECTORS AS ARROWS
			if (vectorField != null)
			{
				vectorField.Draw();
			}

			// DRAW COLOR SCALE LEGEND
			if (settings.ShowColorScaleLegend && DisplayColors && (propertyColorsMode & (PropertyColorsMode.Elements | PropertyColorsMode.Faces)) == 0)
			{
				drawColorScaleLegend();
			}
		}

		protected virtual void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			string[] propertyNames = e.PropertyName.Split(';');
			foreach (string propertyName in propertyNames)
			{
				switch (propertyName)
				{
				case "DeformationScale":
					if (originalNodePositions == null) // save original node positions
					{
						setupDeformedMesh(DeformAction.Save);
					}
					else if (!settings.DeformationScale.DrawDeformed) // restore original node positions
					{
						setupDeformedMesh(DeformAction.Restore);
					}
					else // update deformed nodes positions
					{
						setupDeformedMesh(DeformAction.Update);
					}
					break;
				case "ShowVectors":
				case "VectorDataIndex":
				case "VectorLengthFactor":
				case "MoveEndOfArrowsToNodes":
					setupVectorField();
					break;
				//case "IsoAreasSubIntervalNumber": /**/
				//	testCrossSection.SectionPlane.Offset = settings.IsoAreasSubIntervalNumber * 0.1f;
				//	testCrossSection.Create(mesh.Elements, this);
				//	break;
				}
			}
		}

		#endregion

		#region Public methods

		public bool DisplayColors
		{
			get { return settings.ShowScalars; }
		}

		public double GetDataValue(Node node)
		{
			return GetDataValue(node, settings.ScalarDataIndex);
		}

		public double GetDataValue(Node node, out float maxError)
		{
			return GetDataValue(node, settings.ScalarDataIndex, out maxError);
		}

		public int GetColorForDataValue(double dataValue)
		{
			if (double.IsNaN(dataValue))
				return ColorScale.UndefinedValueColor;
			return Settings.ColorScale.GetColorForValue(dataValue);
		}

		#endregion

		#region Protected methods

		protected void GetOriginalNodePosition(Node node, out Vector3 position)
		{
			if (originalNodePositions == null || !originalNodePositions.TryGetValue(node, out position))
				position = node.Position;
		}

		protected void createNodeIndexMap(bool loadInternalEntities)
		{
			this.nodeIndexMap = new Dictionary<int, Node>();
			if (loadInternalEntities)
			{
				foreach (Element element in mesh.Elements)
				{
					foreach (Node node in element.IterateThroughAllNodesIncludingEdgeMiddleNodes())
						nodeIndexMap[node.ID] = node;
				}
			}
			else
			{
				foreach (Node node in mesh.GetNodes(includeMiddleNodes: true))
					nodeIndexMap[node.ID] = node;
			}
		}

		protected Dictionary<int, Element> createElementMap()
		{
			Dictionary<int, Element> elementMap = new Dictionary<int, Element>();
			foreach (Element element in mesh.Elements)
			{
				elementMap[element.ID] = element;
			}
			return elementMap;
		}

		#endregion

		#region Private methods

		private enum DeformAction
		{
			Save, Restore, Update
		}

		private void setupDeformedMesh(DeformAction action)
		{
			//// nastavit stred rotace site pro nastroj Orbit a radius viditelne site
			//mesh.CenterOfRotation = (nodesEdgesIncidence.Count > 0) ? Utilities.Functions.GetCenterOfLineSegment(ref lowerBound, ref upperBound) : /*Vector3.Zero*/ mesh.PositionOffset * -mesh.ResizeFactor;
			//mesh.Radius = (nodesEdgesIncidence.Count > 0) ? (lowerBound - upperBound).Length * 0.5f : 1f;

			//mesh.LowerBound = lowerBound;
			//mesh.UpperBound = upperBound;
			//// --------------------------------------------------------------------

			Vector3 lowerBound = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			Vector3 upperBound = new Vector3(float.MinValue, float.MinValue, float.MinValue);

			switch (action)
			{
				case DeformAction.Save:
					originalNodePositions = new Dictionary<Node, Vector3>();
					foreach (Node node in nodeIndexMap.Values)
					{
						originalNodePositions[node] = node.Position;
						node.Position += getDisplacementVector(node);
						updateBounds(node.Position, ref lowerBound, ref upperBound);
					}
					updateFaceNormals();
					break;
				case DeformAction.Restore:
					Debug.Assert(originalNodePositions != null);
					foreach (Node node in nodeIndexMap.Values)
					{
						node.Position = originalNodePositions[node];
						updateBounds(node.Position, ref lowerBound, ref upperBound);
					}
					updateFaceNormals();
					originalNodePositions = null;
					break;
				case DeformAction.Update:
					Debug.Assert(originalNodePositions != null);
					foreach (Node node in nodeIndexMap.Values)
					{
						node.Position = originalNodePositions[node];
						node.Position += getDisplacementVector(node);
						updateBounds(node.Position, ref lowerBound, ref upperBound);
					}
					updateFaceNormals();
					break;
				default:
					throw new NotSupportedException();
			}

			// update mesh bounds
			mesh.CenterOfRotation = Utilities.Functions.GetCenterOfLineSegment(ref lowerBound, ref upperBound);
			mesh.Radius = (lowerBound - upperBound).Length * 0.5f;

			mesh.LowerBound = lowerBound;
			mesh.UpperBound = upperBound;
		}

		private void updateFaceNormals()
		{
			foreach (Element2D face in mesh.Faces)
				face.UpdateNormalVector();
		}

		private Vector3 getDisplacementVector(Node node)
		{
			DataIndex deformationDataIndex = settings.DeformationScale.DeformationDataIndex;

			if (deformationDataIndex.Index < 0)
				return Vector3.Zero;

			double displX = GetDataValue(node, deformationDataIndex);
			if (double.IsNaN(displX))
				displX = 0.0;
			double displY = GetDataValue(node, deformationDataIndex.WithIndex(deformationDataIndex.Index + 1));
			if (double.IsNaN(displY))
				displY = 0.0;
			double displZ = GetDataValue(node, deformationDataIndex.WithIndex(deformationDataIndex.Index + 2));
			if (double.IsNaN(displZ))
				displZ = 0.0;

			return new Vector3((float)displX, (float)displY, (float)displZ) * settings.DeformationScale.Multiplier;
		}

		private static void updateBounds(Vector3 point, ref Vector3 lowerBound, ref Vector3 upperBound)
		{
			if (point.X < lowerBound.X) // X
				lowerBound.X = point.X;
			if (point.X > upperBound.X)
				upperBound.X = point.X;
			if (point.Y < lowerBound.Y) // Y
				lowerBound.Y = point.Y;
			if (point.Y > upperBound.Y)
				upperBound.Y = point.Y;
			if (point.Z < lowerBound.Z) // Z
				lowerBound.Z = point.Z;
			if (point.Z > upperBound.Z)
				upperBound.Z = point.Z;
		}

		private void initIsoAreasShader()
		{
			// lazy initialization of shader
			if (isoAreasShader == null)
			{
				isoAreasShader = new IsoAreasShader(Scene.FaceLighting);
				Debug.Assert(isoAreasShader.IsReady);
			}
		}

		private void setupVectorField()
		{
			if (vectorField != null)
			{
				vectorField.Dispose();
				vectorField = null;
			}

			if (!settings.ShowVectors)
				return;

			Debug.Assert(nodeIndexMap != null);

			int vectorDataIndex = Settings.VectorDataIndex.Index;

			IntervalD xRange = GetDataValueRange(vectorDataIndex);
			IntervalD yRange = GetDataValueRange(vectorDataIndex + 1);
			IntervalD zRange = GetDataValueRange(vectorDataIndex + 2);

			double maxAbsValue = Math.Max(Math.Max(xRange.GetMaxAbsValue(), yRange.GetMaxAbsValue()), zRange.GetMaxAbsValue());

			float resizeFactor = 0f;
			if (maxAbsValue > Common.Epsilon)
			{
				resizeFactor = (float)(Settings.VectorLengthFactor / maxAbsValue);
			}

			if (resizeFactor.IsAlmostZero())
				return;

			Vector3[] positions = new Vector3[nodeIndexMap.Count];
			Vector3[] vectors = new Vector3[nodeIndexMap.Count];
			int index = 0;

			DataIndex xDataIndex = Settings.VectorDataIndex;
			DataIndex yDataIndex = Settings.VectorDataIndex.WithIndex(xDataIndex.Index + 1);
			DataIndex zDataIndex = Settings.VectorDataIndex.WithIndex(xDataIndex.Index + 2);

			foreach (Node node in nodeIndexMap.Values)
			{
				double x = GetDataValue(node, xDataIndex);
				double y = GetDataValue(node, yDataIndex);
				double z = GetDataValue(node, zDataIndex);
				
				positions[index] = node.Position;
				vectors[index] = new Vector3((float)x, (float)y, (float)z);
				++index;
			}

			vectorField = new VectorField(positions, vectors, resizeFactor, Settings.MoveEndOfArrowsToNodes);
		}

		private void drawColorScaleLegend()
		{
			int[] viewport = new int[4];
			GL.GetInteger(GetPName.Viewport, viewport);

			// -------------------------------------------------------------------------
			float[] backgroundColorComponents = new float[4];
			GL.GetFloat(GetPName.ColorClearValue, backgroundColorComponents); // what about performance of GL.GetFloat ??
			Color backgroundColor = Color.FromArgb((int)(backgroundColorComponents[0] * 255f), (int)(backgroundColorComponents[1] * 255f), (int)(backgroundColorComponents[2] * 255f), (int)(backgroundColorComponents[3] * 255f));
			//Color backgroundColor = Scene.ActiveBackColor;
			Color contrastColor = Utilities.Functions.GetContrastColor(backgroundColor);
			// -------------------------------------------------------------------------

			Debug.Assert(Settings.ColorScale.ControlPoints.Length >= 2);
			ColorScale.ControlPoint[] controlPoints = Settings.ColorScale.ControlPoints.Reverse().ToArray();

			// -----------------------------------------------------------------------

			int captionHeight = 0;
			string description = settings.ScalarDataDescription;
			if (settings.DisplayMethod == ScalarDataDisplayMethod.ApproximationError)
			{
				description += Environment.NewLine + "[Approximation Error]";
				captionHeight += 80;
			}
			else
			{
				captionHeight += 60;
			}

			// -----------------------------------------------------------------------

			Point startLocation;
			Size tableCellSize = new Size(20, 40);
			SizeF captionSize = Utilities.Functions.MeasureText(description, new Vector2());

			int tableHeight = tableCellSize.Height * (controlPoints.Length - 1);
			int tableWidth = Math.Max(tableCellSize.Width + 60, (int)captionSize.Width);
			const int margin = 10;

			switch (Scene.ColorScaleLegendPosition)
			{
				case ColorScaleLegendPosition.RightTop:
				default:
					startLocation = new Point(viewport[2] - tableWidth - margin, margin);
					break;
				case ColorScaleLegendPosition.RightBottom:
					startLocation = new Point(viewport[2] - tableWidth - margin, viewport[3] - tableHeight - captionHeight - margin);
					break;
				case ColorScaleLegendPosition.LeftBottom:
					startLocation = new Point(margin, viewport[3] - tableHeight - captionHeight - margin);
					break;
				case ColorScaleLegendPosition.LeftTop:
					startLocation = new Point(margin, margin);
					break;
			}

			// DRAW DATA VALUE DESCRIPTION ---------------------------------------------
			Vector2 textPosition = new Vector2(startLocation.X, startLocation.Y);
			Utilities.Functions.DrawText(description, textPosition, contrastColor);
			// -------------------------------------------------------------------------
			startLocation.Y += captionHeight;
			
			GL.MatrixMode(MatrixMode.Projection);
			GL.PushMatrix();
			{
				GL.LoadIdentity();
				GL.Ortho(0, viewport[2], viewport[3], 0, 0, 1);

				GL.MatrixMode(MatrixMode.Modelview);
				GL.PushMatrix();
				{
					GL.LoadIdentity();

					GL.Disable(EnableCap.Lighting);

					BeginDraw(lightingEnabled: false);

					// DRAW COLOR RECTANGLES
					GL.Begin(BeginMode.Quads);
					{
						Point location = startLocation;
						byte r, g, b, a;
						Utilities.Functions.GetColorComponents(controlPoints[0].Color, out r, out g, out b, out a);
						for (int i = 0; i < controlPoints.Length - 1; i++)
						{
							GL.Color4(r, g, b, a);
							GL.Vertex2(location.X, location.Y);
							GL.Vertex2(location.X + tableCellSize.Width, location.Y);

							Utilities.Functions.GetColorComponents(controlPoints[i + 1].Color, out r, out g, out b, out a);
							GL.Color4(r, g, b, a);
							GL.Vertex2(location.X + tableCellSize.Width, location.Y + tableCellSize.Height);
							GL.Vertex2(location.X, location.Y + tableCellSize.Height);

							location.Y += tableCellSize.Height;
						}
					}
					GL.End();

					EndDraw();

					// DRAW BOUNDARY LINES
					GL.LineWidth(1f);
					GL.Color3(contrastColor);
					GL.Begin(BeginMode.Lines);
					{
						Point location = startLocation;
						for (int i = 0; i < controlPoints.Length; i++)
						{
							GL.Vertex2(location.X, location.Y);
							GL.Vertex2(location.X + tableCellSize.Width, location.Y);

							location.Y += tableCellSize.Height;
						}

						GL.Vertex2(startLocation.X, startLocation.Y);
						GL.Vertex2(startLocation.X, startLocation.Y + tableHeight);

						GL.Vertex2(startLocation.X + tableCellSize.Width, startLocation.Y);
						GL.Vertex2(startLocation.X + tableCellSize.Width, startLocation.Y + tableHeight);
					}
					GL.End();


					//GL.Enable(EnableCap.Lighting);
				}
				GL.PopMatrix();
			}
			GL.MatrixMode(MatrixMode.Projection);
			GL.PopMatrix();

			GL.MatrixMode(MatrixMode.Modelview);

			// DRAW NUMBERS
			textPosition = new Vector2(startLocation.X + tableCellSize.Width + 4, startLocation.Y - 9);
			for (int i = 0; i < controlPoints.Length; i++)
			{
				Utilities.Functions.DrawText(controlPoints[i].Value.ToString("G4"), textPosition, contrastColor);
				textPosition.Y += tableCellSize.Height;
			}
		}

		#endregion

		#region IDisposable pattern

		//~DataVisualizerBase()
		//{
		//	Dispose(false);
		//}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// managed resources
			}

			if (originalNodePositions != null && settings.DeformationScale.DrawDeformed) // restore original node positions
			{
				setupDeformedMesh(DeformAction.Restore);
			}

			// unmanaged resources
			if (isoAreasShader != null)
			{
				isoAreasShader.Dispose();
				isoAreasShader = null;
			}

			if (vectorField != null)
			{
				vectorField.Dispose();
				vectorField = null;
			}
		}

		#endregion

	}
}
