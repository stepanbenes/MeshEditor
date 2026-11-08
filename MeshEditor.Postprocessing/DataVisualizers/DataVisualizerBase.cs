using MeshEditor.CoreInterface;
using MeshEditor.Data;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.DataVisualizer.Graphics;
using MeshEditor.DataVisualizer.Mathematics;
using MeshEditor.Graphics;
using OpenTK.Mathematics;
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
	internal abstract class DataVisualizerBase : IDataVisualizer, IDataVisualizerController
	{
		#region Fields, constructor

		IsoAreasShader isoAreasShader;
		
		protected DataVisualizerBase(IVisualizerSettings settings)
		{
			Settings = settings ?? new VisualizerSettings();
		}

		#endregion

		#region Properties

		public abstract bool DisplayData { get; }

		public abstract bool DisplayColors { get; }

		/// <summary>
		/// The displayed scalar component has no value at all (e.g. a result block without values): the mesh is drawn
		/// in the undefined value color and the legend says "no data" instead of showing the color scale.
		/// </summary>
		public bool HasNoScalarValues { get; protected set; }

		public const string NoDataLegendText = "no data";

		public IVisualizerSettings Settings { get; }

		public string StatusText { get; protected set; }
		protected string LegendText { get; set; }

		#endregion

		#region Public methods

		public void BeginDraw(bool lightingEnabled)
		{
			if (Settings.DrawIsoAreas && DisplayColors)
			{
				initIsoAreasShader();
				if (isoAreasShader.IsReady)
				{
					isoAreasShader.LightingEnabled = lightingEnabled;
					isoAreasShader.Use(Settings.IsoAreasSubIntervalNumber, Settings.ColorScale);
				}
			}
		}

		public virtual void DrawDecorations(PropertyColorsMode propertyColorsMode)
		{
			// DRAW COLOR SCALE LEGEND
			if (Settings.ShowColorScaleLegend && DisplayColors && (propertyColorsMode & (PropertyColorsMode.Elements | PropertyColorsMode.Faces)) == 0)
			{
				drawColorScaleLegend();
			}
		}

		public void EndDraw()
		{
			if (Settings.DrawIsoAreas && DisplayColors)
			{
				isoAreasShader.Unuse();
			}
		}

		public int GetDataColor(Node node, Element element)
		{
			return getColorForDataValue(GetDataValue(node, element));
		}

		public virtual void Initialize(Mesh mesh)
		{
			// Do nothing
		}

		public abstract double GetDataValue(Node node);

		public abstract double GetDataValue(Node node, Element element);

		public abstract int[] GetIDsOfNodesWithMaximumDataValue();

		public abstract int[] GetIDsOfNodesWithMinimumDataValue();

		public abstract double GetMaximumDataValue();

		public abstract double GetMinimumDataValue();

		#endregion

		#region Private methods

		private int getColorForDataValue(double dataValue)
		{
			if (double.IsNaN(dataValue))
				return ColorScale.UndefinedValueColor;
			return Settings.ColorScale.GetColorForValue(dataValue);
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

			string description = LegendText;

			// the color scale: a cell between each two control points, labeled by the values of the control points; a component
			// without any value has a single cell in the undefined value color (the color of the mesh) labeled "no data" instead
			bool noData = HasNoScalarValues;
			int[] boundaryColors = noData ? new[] { ColorScale.UndefinedValueColor, ColorScale.UndefinedValueColor } : controlPoints.Select(cp => cp.Color).ToArray();
			string[] labels = noData ? new[] { NoDataLegendText } : controlPoints.Select(getControlPointValueText).ToArray();
			int cellCount = boundaryColors.Length - 1;

			// -----------------------------------------------------------------------

			// sizes in device pixels (the text is measured in device pixels already)
			static int scale(int logicalPixels) => (int)MathF.Round(MeshEditor.Graphics.DeviceScaling.Scale(logicalPixels));

			Point startLocation;
			Size tableCellSize = new Size(scale(20), noData ? scale(20) : scale(40));
			SizeF captionSize = Utilities.Functions.MeasureTextLines(description);

			int captionHeight = (int)captionSize.Height + scale(20);

			int tableHeight = tableCellSize.Height * cellCount;
			var tableWidth = new Lazy<int>(() => calculateTableWidth());
			int margin = scale(10);

			int calculateTableWidth()
			{
				int maxLabelWidth = labels.Select(label => (int)Utilities.Functions.MeasureText(label).Width).DefaultIfEmpty().Max();
				return Math.Max(tableCellSize.Width + maxLabelWidth, (int)captionSize.Width);
			}

			switch (Scene.ColorScaleLegendPosition)
			{
				case ColorScaleLegendPosition.RightTop:
				default:
					startLocation = new Point(viewport[2] - tableWidth.Value - margin, margin);
					break;
				case ColorScaleLegendPosition.RightBottom:
					startLocation = new Point(viewport[2] - tableWidth.Value - margin, viewport[3] - tableHeight - captionHeight - margin);
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
			Utilities.Functions.DrawTextLines(description, textPosition, contrastColor);
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
					GL.Begin(PrimitiveType.Quads);
					{
						Point location = startLocation;
						byte r, g, b, a;
						Utilities.Functions.GetColorComponents(boundaryColors[0], out r, out g, out b, out a);
						for (int i = 0; i < cellCount; i++)
						{
							GL.Color4(r, g, b, a);
							GL.Vertex2(location.X, location.Y);
							GL.Vertex2(location.X + tableCellSize.Width, location.Y);

							Utilities.Functions.GetColorComponents(boundaryColors[i + 1], out r, out g, out b, out a);
							GL.Color4(r, g, b, a);
							GL.Vertex2(location.X + tableCellSize.Width, location.Y + tableCellSize.Height);
							GL.Vertex2(location.X, location.Y + tableCellSize.Height);

							location.Y += tableCellSize.Height;
						}
					}
					GL.End();

					EndDraw();

					// DRAW BOUNDARY LINES
					MeshEditor.Graphics.DeviceScaling.LineWidth(1f);
					GL.Color3(contrastColor);
					GL.Begin(PrimitiveType.Lines);
					{
						Point location = startLocation;
						for (int i = 0; i <= cellCount; i++)
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

			// DRAW NUMBERS (at the cell boundaries) or "no data" (in the middle of the cell)
			float valueTextHeight = Utilities.Functions.MeasureText("0").Height;
			textPosition = new Vector2(startLocation.X + tableCellSize.Width + scale(6), startLocation.Y - valueTextHeight / 2 + (noData ? tableCellSize.Height / 2 : 0));
			foreach (string label in labels)
			{
				Utilities.Functions.DrawText(label, textPosition, contrastColor);
				textPosition.Y += tableCellSize.Height;
			}

			static string getControlPointValueText(ColorScale.ControlPoint controlPoint) => controlPoint.Value.ToString("G4");
		}

		#endregion

		#region IDisposable pattern

		~DataVisualizerBase()
		{
			Dispose(false);
		}

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

				if (isoAreasShader != null)
				{
					isoAreasShader.Dispose();
					isoAreasShader = null;
				}
			}

			// unmanaged resources
		}

		#endregion
	}
}
