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
			string dataDescription,
			int numberOfDataValues,
			double maxRelativeError,
			double averageRelativeError,
			double meanSquareError)
		{
			DataDescription = dataDescription;
			NumberOfDataValues = numberOfDataValues;
			MaxRelativeError = maxRelativeError;
			AverageRelativeError = averageRelativeError;
			MeanSquareError = meanSquareError;
		}

		public string DataDescription { get; }
		public int NumberOfDataValues { get; }
		public double MaxRelativeError { get; }
		public double AverageRelativeError { get; }
		public double MeanSquareError { get; }

		public override string ToString()
		{
			StringBuilder text = new StringBuilder();

			text.AppendLine($"Diff of {DataDescription}");

			text.AppendLine($"{nameof(NumberOfDataValues)}: {NumberOfDataValues}");
			text.AppendLine($"{nameof(MaxRelativeError)}: {MaxRelativeError}");
			text.AppendLine($"{nameof(AverageRelativeError)}: {AverageRelativeError}");
			text.Append($"{nameof(MeanSquareError)}: {MeanSquareError}");

			return text.ToString();
		}
	}
}
