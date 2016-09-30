using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeshEditor.Common.Extensions;

namespace MeshEditor.LayerManager.Data
{
	public class LayerDiff
	{
		public static LayerDiff CreateFrom(IReadOnlyCollection<ComponentDiff> componentDiffs)
		{
			var relevantCds = componentDiffs.Where(cd => cd.NumberOfDataValues > 0).ToArray();
			double? minNormMaxErr  = relevantCds.Select(cd => cd.NormalizedMaxError). Min(ignore: double.NaN);
			double? minNormMeanErr = relevantCds.Select(cd => cd.NormalizedMeanError).Min(ignore: double.NaN);

			double? maxNormMaxErr  = relevantCds.Select(cd => cd.NormalizedMaxError). Max(ignore: double.NaN);
			double? maxNormMeanErr = relevantCds.Select(cd => cd.NormalizedMeanError).Max(ignore: double.NaN);

			return new LayerDiff(
				totalComponents: componentDiffs.Count,
				lowestMaxError: relevantCds.Where(cd => cd.NormalizedMaxError == minNormMaxErr),
				lowestMeanError: relevantCds.Where(cd => cd.NormalizedMeanError == maxNormMeanErr),
				highestMaxError: relevantCds.Where(cd => cd.NormalizedMaxError == maxNormMaxErr),
				highestMeanError: relevantCds.Where(cd => cd.NormalizedMeanError == maxNormMeanErr));
		}

		readonly int totalComponents;
		readonly IEnumerable<ComponentDiff> lowestMaxError, lowestMeanError, highestMaxError, highestMeanError;

		private LayerDiff(int totalComponents, IEnumerable<ComponentDiff> lowestMaxError, IEnumerable<ComponentDiff> lowestMeanError, IEnumerable<ComponentDiff> highestMaxError, IEnumerable<ComponentDiff> highestMeanError)
		{
			this.totalComponents = totalComponents;
			this.lowestMaxError = lowestMaxError;
			this.lowestMeanError = lowestMeanError;
			this.highestMaxError = highestMaxError;
			this.highestMeanError = highestMeanError;
		}

		public override string ToString()
		{
			StringBuilder text = new StringBuilder();
			{
				text.AppendLine("╔═ LOWEST MAX ERROR");
				foreach (var diff in lowestMaxError)
					text.AppendLine("║ " + diff.ToString());
				text.AppendLine("╠═ LOWEST MEAN ERROR");
				foreach (var diff in lowestMeanError)
					text.AppendLine("║ " + diff.ToString());
				text.AppendLine("╠═ HIGHEST MAX ERROR");
				foreach (var diff in highestMaxError)
					text.AppendLine("║ " + diff.ToString());
				text.AppendLine("╠═ HIGHEST MEAN ERROR");
				foreach (var diff in highestMeanError)
					text.AppendLine("║ " + diff.ToString());
				text.Append($"╚═ of {totalComponents} total components");
			}
			return text.ToString();
		}
	}

	public class ComponentDiff
	{
		public static ComponentDiff CreateFrom(IEnumerable<Tuple<ComponentDataDescription, ComponentDataDescription>> timeStepSequence)
		{
			int numberOfDataValues = 0;
			double? minValue = double.MaxValue;
			double? maxValue = double.MinValue;
			double? maxError = double.MinValue;
			double errorSum = 0.0;
			double squareErrorSum = 0.0;
			string fieldAndComponentName = null;

			foreach (var timeStepPair in timeStepSequence)
			{
				if (fieldAndComponentName == null)
				{
					fieldAndComponentName = $"{timeStepPair.Item1.FieldName}/{timeStepPair.Item1.ComponentName}";
				}

				var aValues = timeStepPair.Item1.Values;
				var bValues = timeStepPair.Item2.Values;

				Debug.Assert(aValues.Length == bValues.Length);

				for (int i = 0; i < aValues.Length; i++)
				{
					if (double.IsNaN(aValues[i]) || double.IsNaN(bValues[i]))
						continue;
					minValue = Math.Min(minValue ?? double.MaxValue, Math.Min(aValues[i], bValues[i]));
					maxValue = Math.Max(maxValue ?? double.MinValue, Math.Max(aValues[i], bValues[i]));
					double error = Math.Abs(aValues[i] - bValues[i]);
					maxError = Math.Max(maxError ?? double.MinValue, error);
					errorSum += error;
					squareErrorSum += error * error;
					numberOfDataValues += 1;
				}
			}

			double dataRange = (minValue.HasValue) ? maxValue.Value - minValue.Value : double.NaN;

			return new ComponentDiff(
				fieldAndComponentName,
				numberOfDataValues,
				dataRange,
				maxError ?? double.NaN,
				meanError: errorSum / numberOfDataValues,
				meanSquareError: squareErrorSum / numberOfDataValues);
		}

		private ComponentDiff(
			string dataDescription,
			int numberOfDataValues,
			double dataRange,
			double maxError,
			double meanError,
			double meanSquareError)
		{
			DataDescription = dataDescription;
			NumberOfDataValues = numberOfDataValues;
			DataRange = dataRange;
			MaxError = maxError;
			MeanError = meanError;
			MSE = meanSquareError;
		}

		public string DataDescription { get; }
		public int NumberOfDataValues { get; }
		public double DataRange { get; }
		public double MaxError { get; }
		public double MeanError { get; }

		/// <summary>
		/// Mean Square Error
		/// </summary>
		public double MSE { get; }

		/// <summary>
		/// Normalized Max Error
		/// </summary>
		public double NormalizedMaxError => MaxError / DataRange;

		/// <summary>
		/// Normalized Mean Error
		/// </summary>
		public double NormalizedMeanError => MeanError / DataRange;

		/// <summary>
		/// Rooted Mean Square Deviation
		/// </summary>
		public double RMSD => Math.Sqrt(MSE);

		/// <summary>
		/// Normalized Rooted Mean Square Deviation
		/// </summary>
		public double NRMSD => RMSD / DataRange;

		/// <summary>
		/// Peek Signal to Noise Ratio. PSNR=10\log_{10}\frac{(X_{max}-X_{min})^{2}}{MSE}
		/// </summary>
		public double PSNR => -20.0 * Math.Log10(NRMSD);


		public static string GetTableHeader()
		{
			return $"  {"".PadRight(20)} {"# DATA".PadLeft(11)} {"NORM MAX ERR".PadLeft(22)} {"NORM MEAN ERR".PadLeft(22)} {"NRMSD".PadLeft(22)} {"PSNR [db]".PadLeft(22)}";
		}

		public override string ToString()
		{
			return $"{DataDescription.TrimOrExtendToLength(20)} {NumberOfDataValues.ToString().PadLeft(11)} {NormalizedMaxError.ToString().PadLeft(22)} {NormalizedMeanError.ToString().PadLeft(22)} {NRMSD.ToString().PadLeft(22)} {PSNR.ToString().PadLeft(22)}";

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
