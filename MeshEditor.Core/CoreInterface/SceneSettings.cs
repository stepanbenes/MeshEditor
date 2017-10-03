using System;
using System.Collections.Generic;
using System.Text;
using MeshEditor.Data;
using System.Drawing;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using OpenTK.Graphics.OpenGL;
using MeshEditor.Graphics;
using MeshEditor.Common;

namespace MeshEditor.CoreInterface
{
	/// <summary>
	/// trida zapouzdrujici hlavni volby a nastaveni programu
	/// </summary>
	[Serializable]
	[DefaultProperty("LineSmooth")]
	public class SceneSettings
	{

		#region Static members

		private static SceneSettings instance;
		private static MemoryStream mem;

		/// <summary>
		/// Static constructor
		/// </summary>
		static SceneSettings()
		{
			instance = null;
			mem = null;
		}

		public static SceneSettings Instance
		{
			get
			{
				if (instance == null)
					instance = new SceneSettings();
				return instance;
			}
		}

		public static void SaveState()
		{
			if (mem != null)
				mem.Close();
			// serializace do pameti
			mem = new MemoryStream();
			BinaryFormatter formatter = new BinaryFormatter();
			try
			{
				formatter.Serialize(mem, Instance);
				mem.Position = 0;
			}
			catch (Exception)
			{
				mem = null;
			}
		}

		public static void RestoreState()
		{
			if (mem == null)
				return;
			// deserializace z pameti
			BinaryFormatter formatter = new BinaryFormatter();
			try
			{
				instance = (SceneSettings)formatter.Deserialize(mem);
				instance.update();
			}
#if !DEBUG
			catch (Exception)
			{

			}
#endif
			finally
			{
				mem.Close();
			}
		}

		public static void SaveToConfigurationFile()
		{
			ConfigurationManager.SetConfigurationObject("SceneSettings", Instance);

//			// serializace do souboru
//			FileStream stream = null;
//			try
//			{
//				stream = new FileStream(filename, FileMode.Create);
//				BinaryFormatter serializer = new BinaryFormatter();
//				serializer.Serialize(stream, Instance);
//			}
//#if !DEBUG
//			catch (Exception)
//			{
//			}
//#endif
//			finally
//			{
//				if (stream != null)
//					stream.Close();
//			}
		}

		public static void LoadFromConfigurationFile()
		{
			instance = ConfigurationManager.GetConfigurationObject<SceneSettings>("SceneSettings") ?? new SceneSettings();
			instance.update();

//			if (!File.Exists(filename))
//				return;

//			// deserializace ze souboru
//			FileStream stream = null;
//			try
//			{
//				stream = new FileStream(filename, FileMode.Open);
//				BinaryFormatter serializer = new BinaryFormatter();
//				instance = (AppSettings)serializer.Deserialize(stream);
//				instance.update();
//			}
//#if !DEBUG
//			catch (Exception)
//			{
//				instance = new AppSettings();
//			}
//#endif
//			finally
//			{
//				if (stream != null)
//					stream.Close();
//			}
		}

		public static void Reset()
		{
			Scene.SetDefaultParametres(true);
			instance = new SceneSettings();
			instance.update();
		}

		#endregion

		#region Private fields & constructor

		private bool lineSmooth, pointSmooth, edgeLighting, faceLighting;
		private float pointSize, ordinaryEdgeWidth, borderEdgeWidth, beamWidth;
		private Color activeBackColor, nonActiveBackColor, faceColor, ordinaryEdgeColor, firstBorderColor, secondBorderColor, selectedElementColor, selectedFaceColor, selectedEdgeColor, selectedNodeColor, selectedFaceAndElementColor, beamColor, selectedBeamColor, nodesColor, nodeNumbersColor, elementNumbersColor, selectedElementNumbersColor;
		private ShadingModel shadingModel;
		private RenderMode defaultRenderMode;
		private string sifelFileFormatExtension;
		private bool showOpenGLLowVersionMessage;
		private float defaultFirstBorderAngleLimit, defaultSecondBorderAngleLimit;
		private ColorScaleLegendPosition legendPosition;
		private Color vectorArrowsColor;

		private void update()
		{
			LineSmooth = lineSmooth;
			PointSmooth = pointSmooth;
			EdgeLighting = edgeLighting;
			FaceLighting = faceLighting;
			
			ShadingModel = shadingModel;
			PointSize = pointSize;
			OrdinaryEdgeWidth = ordinaryEdgeWidth;
			BorderEdgeWidth = borderEdgeWidth;
			BeamWidth = beamWidth;
			DefaultRenderMode = defaultRenderMode;
			SifelFileformatExtension = sifelFileFormatExtension;

			ActiveBackColor = activeBackColor;
			NonActiveBackColor = nonActiveBackColor;
			FaceColor = faceColor;
			NodesColor = nodesColor;
			NodeNumbersColor = nodeNumbersColor;
			ElementNumbersColor = elementNumbersColor;
			BeamColor = beamColor;
			OrdinaryEdgeColor = ordinaryEdgeColor;
			FirstBorderColor = firstBorderColor;
			SecondBorderColor = secondBorderColor;
			SelectedBeamColor = selectedBeamColor;
			SelectedEdgeColor = selectedEdgeColor;
			SelectedElementColor = selectedElementColor;
			SelectedFaceAndElementColor = selectedFaceAndElementColor;
			SelectedFaceColor = selectedFaceColor;
			SelectedNodeColor = selectedNodeColor;
			SelectedElementNumbersColor = selectedElementNumbersColor;

			DefaultFirstBorderAngleLimit = defaultFirstBorderAngleLimit;
			DefaultSecondBorderAngleLimit = defaultSecondBorderAngleLimit;

			LegendPosition = legendPosition;
			VectorArrowsColor = vectorArrowsColor;
		}

		/// <summary>
		/// Private constructor
		/// </summary>
		private SceneSettings()
		{
			lineSmooth = LineSmooth;
			pointSmooth = PointSmooth;
			edgeLighting = EdgeLighting;
			faceLighting = FaceLighting;
			shadingModel = ShadingModel;
			pointSize = PointSize;
			ordinaryEdgeWidth = OrdinaryEdgeWidth;
			borderEdgeWidth = BorderEdgeWidth;
			beamWidth = BeamWidth;
			defaultRenderMode = DefaultRenderMode;
			sifelFileFormatExtension = SifelFileformatExtension;

			activeBackColor = ActiveBackColor;
			nonActiveBackColor = NonActiveBackColor;
			faceColor = FaceColor;
			nodesColor = NodesColor;
			nodeNumbersColor = NodeNumbersColor;
			elementNumbersColor = ElementNumbersColor;
			beamColor = BeamColor;
			ordinaryEdgeColor = OrdinaryEdgeColor;
			firstBorderColor = FirstBorderColor;
			secondBorderColor = SecondBorderColor;
			selectedBeamColor = SelectedBeamColor;
			selectedEdgeColor = SelectedEdgeColor;
			selectedElementColor = SelectedElementColor;
			selectedFaceAndElementColor = SelectedFaceAndElementColor;
			selectedFaceColor = SelectedFaceColor;
			selectedNodeColor = SelectedNodeColor;
			selectedElementNumbersColor = SelectedElementNumbersColor;

			showOpenGLLowVersionMessage = true;

			defaultFirstBorderAngleLimit = DefaultFirstBorderAngleLimit;
			defaultSecondBorderAngleLimit = DefaultSecondBorderAngleLimit;

			legendPosition = LegendPosition;
			vectorArrowsColor = VectorArrowsColor;
		}

		#endregion

		#region Setting items

		[Browsable(false)]
		public bool ShowOpenGLLowVersionMessage
		{
			get { return showOpenGLLowVersionMessage; }
			set { showOpenGLLowVersionMessage = value; }
		}

		[Category("Entity appearance"), DisplayName("Smooth lines"), Description("Line antialiazing")]
		public bool LineSmooth
		{
			get { return Scene.LineSmooth; }
			set { lineSmooth = Scene.LineSmooth = value; }
		}

		[Category("Entity appearance"), DisplayName("Smooth points"), Description("Point antialiazing")]
		public bool PointSmooth
		{
			get { return Scene.PointSmooth; }
			set { pointSmooth = Scene.PointSmooth = value; }
		}

		[Category("Entity appearance"), DisplayName("Face lighting")]
		public bool FaceLighting
		{
			get { return Scene.FaceLighting; }
			set { faceLighting = Scene.FaceLighting = value; }
		}
		
		[Category("Entity appearance"), DisplayName("Edge lighting")]
		public bool EdgeLighting
		{
			get { return Scene.EdgeLighting; }
			set { edgeLighting = Scene.EdgeLighting = value; }
		}

		[Category("Entity appearance"), DisplayName("Node size"), Description("Node size in pixels")]
		public float PointSize
		{
			get { return Scene.PointSize; }
			set
			{
				if (value >= 0f)
					pointSize = Scene.PointSize = value;
			}
		}

		[Category("Entity appearance"), DisplayName("Ordinary edge width"), Description("Ordinary edge (not sharp-border edges) width in pixels")]
		public float OrdinaryEdgeWidth
		{
			get { return Scene.OrdinaryEdgeWidth; }
			set
			{
				if (value >= 0f)
					ordinaryEdgeWidth = Scene.OrdinaryEdgeWidth = value;
			}
		}

		[Category("Entity appearance"), DisplayName("Border edge width"), Description("Sharp border edge width in pixels")]
		public float BorderEdgeWidth
		{
			get { return Scene.BorderEdgeWidth; }
			set
			{
				if (value >= 0f)
					borderEdgeWidth = Scene.BorderEdgeWidth = value;
			}
		}

		[Category("Entity appearance"), DisplayName("Beam width"), Description("Beam width in pixels")]
		public float BeamWidth
		{
			get { return Scene.BeamWidth; }
			set 
			{
				if (value >= 0f)
					beamWidth = Scene.BeamWidth = value;
			}
		}

		[Category("Mesh appearance"), DisplayName("Mesh shading model"), Description("Shading model of faces (smooth or flat)")]
		[RecreateBuffers]
		public ShadingModel ShadingModel
		{
			get { return Scene.MeshShadingModel; }
			set { shadingModel = Scene.MeshShadingModel = value; }
		}

		[Category("Mesh appearance"), DisplayName("Default render mode")]
		public RenderMode DefaultRenderMode
		{
			get { return Scene.DefaultRenderMode; }
			set { defaultRenderMode = Scene.DefaultRenderMode = value; }
		}

		[Category("Mesh appearance"), DisplayName("Default first border angle"), Description("Default value of soft border angle limit in degrees. Available values in range <0; 180>")]
		[DontRefresh]
		public float DefaultFirstBorderAngleLimit
		{
			get { return Scene.DefaultFirstBorderAngleLimit; }
			set
			{
				if (value >= 0f && value <= 180f)
					defaultFirstBorderAngleLimit = Scene.DefaultFirstBorderAngleLimit = value;
			}
		}

		[Category("Mesh appearance"), DisplayName("Default second border angle"), Description("Default value of sharp boundary angle limit in degrees. Available values in range <0; 180>")]
		[DontRefresh]
		public float DefaultSecondBorderAngleLimit
		{
			get { return Scene.DefaultSecondBorderAngleLimit; }
			set
			{
				if (value >= 0f && value <= 180f)
					defaultSecondBorderAngleLimit = Scene.DefaultSecondBorderAngleLimit = value;
			}
		}

		// -----------------------------------------------------------------------------

		[DisplayName("SIFEL file format extension"), Description("Extension of default input/output file format used in SIFEL software")]
		public string SifelFileformatExtension
		{
			get { return Scene.SifelFileFormatExtension; }
			set
			{
				string ext = value?.Trim();
				if (ext != null)
				{
					if (!ext.StartsWith("."))
						ext = "." + ext;
					Scene.SifelFileFormatExtension = ext;
				}
				sifelFileFormatExtension = Scene.SifelFileFormatExtension;
			}
		}

		[ReadOnly(true)]
		[DisplayName("OpenGL version support"), Description("Supported OpenGL version by your graphics card (version 2.0+ is recommended)")]
		public string OpenGLVersion
		{
			get { return Utilities.Functions.GetOpenGLVersionString(); }
		}
		
		// ---------------------------------------------------------------------

		[Category("Entity colors"), DisplayName("Face color")]
		[UpdateColorBuffers]
		public Color FaceColor
		{
			get { return Scene.FaceColor; }
			set { faceColor = Scene.FaceColor = value; }
		}

		[Category("Entity colors"), DisplayName("Node color")]
		[UpdateColorBuffers]
		public Color NodesColor
		{
			get { return Scene.NodesColor; }
			set { nodesColor = Scene.NodesColor = value; }
		}

		[Category("Entity colors"), DisplayName("Ordinary edge color")]
		[UpdateColorBuffers]
		public Color OrdinaryEdgeColor
		{
			get { return Scene.OrdinaryEdgeColor; }
			set { ordinaryEdgeColor = Scene.OrdinaryEdgeColor = value; }
		}

		[Category("Entity colors"), DisplayName("First border edge color")]
		[UpdateColorBuffers]
		public Color FirstBorderColor
		{
			get { return Scene.SoftBorderColor; }
			set { firstBorderColor = Scene.SoftBorderColor = value; }
		}

		[Category("Entity colors"), DisplayName("Second border edge color")]
		[UpdateColorBuffers]
		public Color SecondBorderColor
		{
			get { return Scene.HardBorderColor; }
			set { secondBorderColor = Scene.HardBorderColor = value; }
		}

		[Category("Entity colors"), DisplayName("Beam color")]
		[UpdateColorBuffers]
		public Color BeamColor
		{
			get { return Scene.BeamColor; }
			set { beamColor = Scene.BeamColor = value; }
		}

		// -----------------------------------------------------------------------------

		[Category("Selection colors"), DisplayName("Selected element color")]
		[UpdateColorBuffers]
		public Color SelectedElementColor
		{
			get { return Scene.SelectedElementColor; }
			set { selectedElementColor = Scene.SelectedElementColor = value; }
		}

		[Category("Selection colors"), DisplayName("Selected face color")]
		[UpdateColorBuffers]
		public Color SelectedFaceColor
		{
			get { return Scene.SelectedFaceColor; }
			set { selectedFaceColor = Scene.SelectedFaceColor = value; }
		}

		[Category("Selection colors"), DisplayName("Selected face and element"), Description("Color displayed when both - element and its face are selected")]
		[UpdateColorBuffers]
		public Color SelectedFaceAndElementColor
		{
			get { return Scene.SelectedFaceAndElementColor; }
			set { selectedFaceAndElementColor = Scene.SelectedFaceAndElementColor = value; }
		}

		[Category("Selection colors"), DisplayName("Selected edge color")]
		[UpdateColorBuffers]
		public Color SelectedEdgeColor
		{
			get { return Scene.SelectedEdgeColor; }
			set { selectedEdgeColor = Scene.SelectedEdgeColor = value; }
		}

		[Category("Selection colors"), DisplayName("Selected node color")]
		[UpdateColorBuffers]
		public Color SelectedNodeColor
		{
			get { return Scene.SelectedNodeColor; }
			set { selectedNodeColor = Scene.SelectedNodeColor = value; }
		}

		[Category("Selection colors"), DisplayName("Selected beam color")]
		[UpdateColorBuffers]
		public Color SelectedBeamColor
		{
			get { return Scene.SelectedBeamColor; }
			set { selectedBeamColor = Scene.SelectedBeamColor = value; }
		}

		[Category("Selection colors"), DisplayName("Selected element numbers color")]
		public Color SelectedElementNumbersColor
		{
			get { return Scene.SelectedElementNumbersColor; }
			set { selectedElementNumbersColor = Scene.SelectedElementNumbersColor = value; }
		}

		// -----------------------------------------------------------------------------

		[Category("Other colors"), DisplayName("Background (active window)")]
		public Color ActiveBackColor
		{
			get { return Scene.ActiveBackColor; }
			set
			{
				activeBackColor = Scene.ActiveBackColor = value;
				Scene.LabelColor = Utilities.Functions.GetContrastColor(ActiveBackColor);
			}
		}

		[Category("Other colors"), DisplayName("Background (non-active window)")]
		public Color NonActiveBackColor
		{
			get { return Scene.NonActiveBackColor; }
			set { nonActiveBackColor = Scene.NonActiveBackColor = value; }
		}

		[Category("Other colors"), DisplayName("Node numbers color")]
		public Color NodeNumbersColor
		{
			get { return Scene.NodeNumbersColor; }
			set { nodeNumbersColor = Scene.NodeNumbersColor = value; }
		}

		[Category("Other colors"), DisplayName("Element numbers color")]
		public Color ElementNumbersColor
		{
			get { return Scene.ElementNumbersColor; }
			set { elementNumbersColor = Scene.ElementNumbersColor = value; }
		}

		// -----------------------------------------------------------------------------
		// POSTPROCESSING

		[Category("Postprocessing"), DisplayName("Color scale legend position")]
		public ColorScaleLegendPosition LegendPosition
		{
			get { return Scene.ColorScaleLegendPosition; }
			set { legendPosition = Scene.ColorScaleLegendPosition = value; }
		}

		[Category("Postprocessing"), DisplayName("Vector arrows color")]
		public Color VectorArrowsColor
		{
			get { return Scene.VectorArrowsColor; }
			set { vectorArrowsColor = Scene.VectorArrowsColor = value; }
		}

		#endregion

	}

	/// <summary>
	/// atribut, ktery rika, ze sit nemusi byt prekreslena po zmene dane polozky
	/// </summary>
	[global::System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	public sealed class DontRefreshAttribute : Attribute
	{ }

	/// <summary>
	/// atribut, ktery rika, ze po zmene dane polozky maji byt obnoveny color buffery
	/// </summary>
	[global::System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	public sealed class UpdateColorBuffersAttribute : Attribute
	{ }

	/// <summary>
	/// atribut, ktery rika, ze maji byt kompletne obnoveny vsechny buffery po zmene dane polozky
	/// </summary>
	[global::System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
	public sealed class RecreateBuffersAttribute : Attribute
	{ }
	
}
