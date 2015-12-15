using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.CoreInterface;
using MeshEditor.Data;
using MeshEditor.DataVisualizer.Data;
using MeshEditor.DataVisualizer.IO;
using OpenTK;

namespace MeshEditor.DataVisualizer
{
	public class WaveletDataVisualizer : ExactDataVisualizer
	{

		#region Fields



		#endregion

		#region Overrides

		public override void LoadData(IApproximationParameters approximationParameters, string[] filenames, LongOpNotifier longOpNotifier)
		{
			base.LoadData(approximationParameters, filenames, longOpNotifier);

			// TODO: apply wavelet transform to time dimension
			//foreach(
		}

		public override double GetDataValue(Node node, DataIndex dataIndex)
		{
			throw new NotImplementedException();
		}

		public override ApproximationQuality GetApproximationQuality(LongOpNotifier longOpNotifier)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Private methods



		#endregion

	}
}
