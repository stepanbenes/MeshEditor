using MeshEditor.CoreInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class DataPickerForm : Form
	{
		public DataPickerForm(IDataVisualizerController dataVisualizer, LongOpNotifier longOpNotifier)
		{
			InitializeComponent();
			Debug.Assert(dataVisualizer != null && longOpNotifier != null);
			this.dataPickerControl.DataVisualizer = dataVisualizer;
			this.dataPickerControl.LongOpNotifier = longOpNotifier;

			this.dataPickerControl.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
		}
	}
}
