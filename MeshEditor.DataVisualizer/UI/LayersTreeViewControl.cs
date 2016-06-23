using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MeshEditor.SolutionManager.IO;

namespace MeshEditor.DataVisualizer.UI
{
	public partial class LayersTreeViewControl : UserControl
	{
		public LayersTreeViewControl()
		{
			InitializeComponent();
		}

		public void SetLayerTree(IEnumerable<ILayerInfo> layers)
		{
			throw new NotImplementedException();
		}
	}
}
