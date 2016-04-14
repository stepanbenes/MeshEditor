using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Data
{
	public class LayerDiff
	{
		public LayerDiff(
			int numberOfDataComponents,
			int numberOfDataValues,
			double maxRelativeError,
			double averageRelativeError,
			double standardDeviation)
		{
			NumberOfDataComponents = numberOfDataComponents;
			NumberOfDataValues = numberOfDataValues;
			MaxRelativeError = maxRelativeError;
			AverageRelativeError = averageRelativeError;
			StandardDeviation = standardDeviation;
		}

		public int NumberOfDataComponents { get; }
		public int NumberOfDataValues { get; }
		public double MaxRelativeError { get; }
		public double AverageRelativeError { get; }
		public double StandardDeviation { get; }

		public override string ToString()
		{
			StringBuilder text = new StringBuilder();
			text.AppendLine($"{nameof(NumberOfDataComponents)}: {NumberOfDataComponents}");
			text.AppendLine($"{nameof(NumberOfDataValues)}: {NumberOfDataValues}");
			text.AppendLine($"{nameof(MaxRelativeError)}: {MaxRelativeError}");
			text.AppendLine($"{nameof(AverageRelativeError)}: {AverageRelativeError}");
			text.Append($"{nameof(StandardDeviation)}: {StandardDeviation}");
			return text.ToString();
		}
	}
}
