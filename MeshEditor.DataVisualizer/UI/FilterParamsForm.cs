using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MeshEditor.DataVisualizer.UI
{
	public class FilterParams
	{
		public FilterParams(IReadOnlyCollection<string> filterParameters, IReadOnlyCollection<double> keyTimeSteps, IReadOnlyCollection<string> compressionParameters, string layerName, string constraintFieldName)
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

	public class FilterParamsForm : Form
	{
		private FilterParams filterParams;

		public FilterParams FilterParams
		{
			get => filterParams ?? throw new InvalidOperationException("Filter params should be requested only if dialog result is OK");
			protected set => filterParams = value ?? throw new ArgumentNullException();
		}
	}
}
