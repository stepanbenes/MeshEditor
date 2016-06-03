using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;

namespace MeshEditor.FormatConverter
{
	abstract class Options
	{
		// Omitting long name, default --verbose
		[Option(HelpText = "Prints all messages to standard output.")]
		public bool Verbose { get; set; }

		[Option("solution", Required = false, HelpText = "Solution id (same as Simulation id in db)")]
		public int? SolutionId { get; set; }
		
		[Option("remote", Required = false, HelpText = "Use remote storage")]
		public bool ForceUseRemoteStorage { get; set; }

		//[Option("config", HelpText = "Name of configuration file (relative or absolute path)")]
		//public string ConfigFile { get; set; }
	}

	abstract class AnalysisResultOptions : Options
	{
		[Option('l', "lengths", Required = true, HelpText = "Lenghts of analysis result groups")]
		public IEnumerable<int> AnalysisResultGroupLengths { get; set; }

		[Option('r', "results", Required = true, HelpText = "Mesh and result files to be processed (first file in each group is expected to be mesh, others data)")]
		public IEnumerable<string> AnalysisResultRecordNames { get; set; }
	}

	[Verb("create", HelpText = "Create new solution")]
	class CreateOptions : AnalysisResultOptions
	{
		[Option('p', "project", Required = false, HelpText = "Project name")]
		public string ProjectName { get; set; }
	}

	[Verb("import", HelpText = "Convert supported mesh and result files to universal layer format")]
	class ImportOptions : AnalysisResultOptions
	{
		
	}
	
	[Verb("filter", HelpText = "Add new filter layer based on parent layer")]
	class FilterOptions : Options
	{
		[Value(index: 0, MetaName = "Parent layer", Required = true, HelpText = "Parent layer guid or name")]
		public string ParentLayer { get; set; }

		[Value(index: 1, MetaName = "Filter type", Required = true, HelpText = "Name of filter to be applied on the parent layer")]
		public string FilterType { get; set; }

		[Option('p', "params", Required = false, HelpText = "Filter parameters")]
		public IEnumerable<string> FilterParameters { get; set; }

		[Option('n', "name", Required = false, HelpText = "Name of new layer")]
		public string LayerName { get; set; }
	}

	[Verb("compress", HelpText = "Compress layer results")]
	class CompressOptions : Options
	{
		[Value(index: 0, MetaName = "Layer to compress", Required = true, HelpText = "Layer's guid or name")]
		public string Layer { get; set; }

		[Value(index: 1, MetaName = "Compression method", Required = true, HelpText = "Compression method (Transparent, SVD, WT)")]
		public string Method { get; set; }

		[Option('k', "keytimes", Required = false, HelpText = "Key time steps")]
		public IEnumerable<double> KeyTimeSteps { get; set; }

		[Option("field", Required = false, HelpText = "Name of field to compress")]
		public string FieldName { get; set; }

		[Option("component", Required = false, HelpText = "Name of Component of field to compress")]
		public string ComponentName { get; set; }

		[Option('p', "params", Required = false, HelpText = "Compression parameters")]
		public IEnumerable<string> CompressionParameters { get; set; }
	}

	[Verb("list", HelpText = "Enumerate all solutions in base directory")]
	class ListOptions : Options
	{
	}
}
