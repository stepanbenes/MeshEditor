using MeshEditor.CoreInterface;
using MeshEditor.Data;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.DataVisualizer.Graphics;
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
		public bool DisplayColors
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public IVisualizerSettings Settings
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public void BeginDraw(bool lightingEnabled)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public void DrawItems(PropertyColorsMode propertyColorsMode)
		{
			throw new NotImplementedException();
		}

		public void EndDraw()
		{
			throw new NotImplementedException();
		}

		public void FinishUp()
		{
			throw new NotImplementedException();
		}

		public int GetColorForDataValue(double dataValue)
		{
			throw new NotImplementedException();
		}

		public int GetDataColor(Node node, Element element)
		{
			throw new NotImplementedException();
		}

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
			throw new NotImplementedException();
		}

		public void LoadData(string[] filenames, LongOpNotifier longOpNotifier)
		{
			throw new NotImplementedException();
		}
	}
}
