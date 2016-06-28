using MeshEditor.CoreInterface;
using MeshEditor.Data;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.DataVisualizer.Graphics;
using MeshEditor.DataVisualizer.Mathematics;
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
	internal abstract class DataVisualizerBase : IDataVisualizer, IDataVisualizerController
	{
		#region Fields, constructor

		private IsoAreasShader isoAreasShader;

		public DataVisualizerBase()
		{
			Settings = new VisualizerSettings();
		}

		#endregion

		#region Properties

		public virtual bool DisplayColors => Settings.ShowScalars;

		public IVisualizerSettings Settings { get; }

		public string ScalarDataDescription { get; protected set; }

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

		public void DrawItems(PropertyColorsMode propertyColorsMode)
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

		public double GetDataValue(Node node)
		{
			double error;
			return GetDataValue(node, out error);
		}

		public void Initialize(Mesh mesh)
		{
			// Do nothing
		}

		public abstract double GetDataValue(Node node, Element element);

		public abstract double GetDataValue(Node node, out double error);

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

			int captionHeight = 0;
			string description = ScalarDataDescription;
			captionHeight += 60;
			//if (settings.DisplayMethod == ScalarDataDisplayMethod.ApproximationError)
			//{
			//	description += Environment.NewLine + "[Approximation Error]";
			//	captionHeight += 20;
			//}

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
			}

			// unmanaged resources
			if (isoAreasShader != null)
			{
				isoAreasShader.Dispose();
				isoAreasShader = null;
			}
		}

		#endregion
	}
}
