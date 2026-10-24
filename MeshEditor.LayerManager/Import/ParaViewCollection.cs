using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MeshEditor.LayerManager.Import
{
	/// <summary>
	/// ParaView data collection (.pvd): the .vtu files of a time series with their time steps, e.g.
	/// &lt;VTKFile type="Collection"&gt;&lt;Collection&gt;&lt;DataSet timestep="0.5" file="result_1.vtu"/&gt;...
	/// Each file becomes an analysis result with its own mesh (the mesh can change in time).
	/// </summary>
	public static class ParaViewCollection
	{
		public const string FileExtension = ".pvd";

		public static bool IsCollection(string fileName) => string.Equals(Path.GetExtension(fileName), FileExtension, StringComparison.OrdinalIgnoreCase);

		/// <summary>
		/// Analysis results of the data sets of the collection, ordered by time step. The paths of the .vtu files are relative to the collection.
		/// </summary>
		public static IReadOnlyList<AnalysisResult> ReadAnalysisResults(string fileName)
		{
			XElement root = XDocument.Load(fileName).Root;
			if (root?.Name.LocalName != "VTKFile" || !string.Equals((string)root.Attribute("type"), "Collection", StringComparison.OrdinalIgnoreCase))
				throw new FormatException($"'{fileName}' is not a ParaView data collection (VTKFile of type Collection).");

			string directory = Path.GetDirectoryName(Path.GetFullPath(fileName));
			var results = new List<AnalysisResult>();
			foreach (XElement dataSet in root.Elements("Collection").Elements("DataSet"))
			{
				string file = (string)dataSet.Attribute("file") ?? throw new FormatException($"A data set in '{fileName}' has no file.");
				if (!string.Equals(Path.GetExtension(file), ".vtu", StringComparison.OrdinalIgnoreCase))
					throw new NotSupportedException($"'{file}' in '{fileName}': only .vtu files (VTK XML unstructured grid) are supported.");
				string timeStepText = (string)dataSet.Attribute("timestep") ?? "0";
				if (!decimal.TryParse(timeStepText, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal timeStep))
					throw new FormatException($"The time step '{timeStepText}' of '{file}' in '{fileName}' is not a number.");
				if (results.Any(result => result.TimeStep == timeStep))
					throw new NotSupportedException($"'{fileName}' has more than one data set at time step {timeStepText} (parts of a data set are not supported).");

				string path = Path.GetFullPath(Path.Combine(directory, file));
				results.Add(new AnalysisResult { TimeStep = timeStep, MeshRecordNames = new[] { path }, DataRecordNames = new[] { path } });
			}
			if (results.Count == 0)
				throw new FormatException($"'{fileName}' has no data sets.");
			return results.OrderBy(result => result.TimeStep).ToList();
		}
	}
}
