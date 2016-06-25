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
		public DataVisualizerBase()
		{
			Settings = new VisualizerSettings();
		}

		public bool DisplayColors => Settings.ShowScalars;

		public IVisualizerSettings Settings { get; }

		public void BeginDraw(bool lightingEnabled)
		{
			// TODO: implement this method
		}

		public void Dispose()
		{
			// TODO: implement this method
		}

		public void DrawItems(PropertyColorsMode propertyColorsMode)
		{
			// TODO: implement this method
		}

		public void EndDraw()
		{
			// TODO: implement this method
		}

		public void FinishUp()
		{
			throw new NotImplementedException();
		}

		public int GetColorForDataValue(double dataValue)
		{
			if (double.IsNaN(dataValue))
				return ColorScale.UndefinedValueColor;
			return Settings.ColorScale.GetColorForValue(dataValue);
		}

		public abstract int GetDataColor(Node node, Element element);

		public double GetDataValue(Node node)
		{
			throw new NotImplementedException();
		}

		public double GetDataValue(Node node, out float maxError)
		{
			throw new NotImplementedException();
		}

		public int[] GetEntitiesWithMaximumDataValue()
		{
			throw new NotImplementedException();
		}

		public int[] GetEntitiesWithMinimumDataValue()
		{
			throw new NotImplementedException();
		}

		public double GetMaximumDataValue()
		{
			throw new NotImplementedException();
		}

		public double GetMinimumDataValue()
		{
			throw new NotImplementedException();
		}

		public void Initialize(Mesh mesh)
		{
			// TODO: implement this method
		}

		public void LoadData(string[] filenames, LongOpNotifier longOpNotifier)
		{
			throw new NotImplementedException();
		}
	}
}
