using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MeshEditor.DataVisualizer.UI
{

	public class FilterParamsForm : Form
	{
		public class Output
		{
			public Output(IReadOnlyCollection<string> filterParameters, IReadOnlyCollection<double> keyTimeSteps, IReadOnlyCollection<string> compressionParameters, string layerName, string constraintFieldName)
			{
				FilterParameters = filterParameters;
				KeyTimeSteps = keyTimeSteps;
				CompressionParameters = compressionParameters;
				LayerName = layerName;
				ConstraintFieldName = constraintFieldName;
			}

			public IReadOnlyCollection<string> FilterParameters { get; }
			public IReadOnlyCollection<double> KeyTimeSteps { get; }
			public IReadOnlyCollection<string> CompressionParameters { get; }
			public string LayerName { get; }
			public string ConstraintFieldName { get; }
		}

		public virtual Output GetOutput() => throw new InvalidOperationException("Method needs to be overriden in derived class.");
	}
}
