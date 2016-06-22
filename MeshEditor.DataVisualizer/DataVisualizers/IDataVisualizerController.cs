using MeshEditor.DataVisualizer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer
{
	public interface IDataVisualizerController
	{
		IVisualizerSettings Settings { get; }
	}
}
