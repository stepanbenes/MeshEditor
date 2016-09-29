using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Common.Extensions;

namespace MeshEditor.LayerManager.Data
{
	public class LayerDiff
	{
		public LayerDiff(
			string dataDescription,
			int numberOfDataValues,
			double maxRelativeError,
			double averageRelativeError,
			double normalizedRootedMeanSquareDeviation)
		{
			DataDescription = dataDescription;
			NumberOfDataValues = numberOfDataValues;
			MaxRelativeError = maxRelativeError;
			AverageRelativeError = averageRelativeError;
			NormalizedRootedMeanSquareDeviation = normalizedRootedMeanSquareDeviation;
		}

		public string DataDescription { get; }
		public int NumberOfDataValues { get; }
		public double MaxRelativeError { get; }
		public double AverageRelativeError { get; }
		public double NormalizedRootedMeanSquareDeviation { get; }

		public static string GetTableHeader()
		{
			return $"{"".PadRight(20)} {"# DATA".PadLeft(11)} {"MAX REL ERR".PadLeft(22)} {"AVG REL ERR".PadLeft(22)} {"NRMSD".PadLeft(22)}";
		}

		public override string ToString()
		{
			return $"{DataDescription.TrimOrExtendToLength(20)} {NumberOfDataValues.ToString().PadLeft(11)} {MaxRelativeError.ToString().PadLeft(22)} {AverageRelativeError.ToString().PadLeft(22)} {NormalizedRootedMeanSquareDeviation.ToString().PadLeft(22)}";

			//StringBuilder text = new StringBuilder();
			//text.AppendLine($"╔ {DataDescription}");
			//text.AppendLine($"║ {nameof(NumberOfDataValues)}:                  {NumberOfDataValues}");
			//text.AppendLine($"║ {nameof(MaxRelativeError)}:                    {MaxRelativeError}");
			//text.AppendLine($"║ {nameof(AverageRelativeError)}:                {AverageRelativeError}");
			//text.Append    ($"╚ {nameof(NormalizedRootedMeanSquareDeviation)}: {NormalizedRootedMeanSquareDeviation}");
			//return text.ToString();
		}
	}
}
