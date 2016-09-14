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
		[Option(Required = false, HelpText = "Prints all messages to standard output.")]
		public bool Verbose { get; set; }

		[Option("solution", Required = false, HelpText = "Solution id (for remote solutions) or solution file path (for local solutions)")]
		public string Solution { get; set; }

		[Option("remote", Required = false, HelpText = "Use remote storage")]
		public bool ForceUseRemoteStorage { get; set; }

		[Option("pressanykey", Required = false, HelpText = "Require pressing any key before quitting the program")]
		public bool PressAnyKeyToQuit { get; set; }

		//[Option("config", HelpText = "Name of configuration file (relative or absolute path)")]
		//public string ConfigFile { get; set; }
	}

	abstract class LayerProducerOptions : Options
	{
		[Option('k', "keytimes", Required = false, HelpText = "Key time steps")]
		public IEnumerable<double> KeyTimeSteps { get; set; }

		[Option("field", Required = false, HelpText = "Name of field to compress")]
		public string FieldName { get; set; }

		[Option('c', "comparams", Required = false, HelpText = "Compression parameters. First should be compression method (Transparent, SVD, WT)")]
		public IEnumerable<string> CompressionParameters { get; set; }

		[Option('n', "name", Required = false, HelpText = "Name of new layer")]
		public string LayerName { get; set; }
	}

	[Verb("import", HelpText = "Convert supported mesh and result files to universal layer format")]
	class ImportOptions : LayerProducerOptions
	{
		[Option("gpextrapolation", Required = false, HelpText = "Gauss points extrapolation strategy (Default is Nearest)")]
		public string GaussPointsExtrapolationStrategyName { get; set; }
	}
	
	[Verb("filter", HelpText = "Add new filter layer based on parent layer")]
	class FilterOptions : LayerProducerOptions
	{
		[Value(index: 0, MetaName = "Parent layer", Required = true, HelpText = "Parent layer guid or name")]
		public string ParentLayer { get; set; }

		[Value(index: 1, MetaName = "Filter type", Required = true, HelpText = "Name of filter to be applied on the parent layer")]
		public string FilterType { get; set; }

		[Option('p', "params", Required = false, HelpText = "Filter parameters")]
		public IEnumerable<string> FilterParameters { get; set; }
	}

	[Verb("compress", HelpText = "Compress layer results")]
	class CompressOptions : LayerProducerOptions
	{
		[Value(index: 0, MetaName = "Layer to compress", Required = true, HelpText = "Layer's guid or name")]
		public string Layer { get; set; }
	}

	[Verb("list", HelpText = "Enumerate all solutions in base directory")]
	class ListOptions : Options
	{
	}

	[Verb("delete", HelpText = "Delete layer")]
	class DeleteOptions : Options
	{
		[Value(index: 0, MetaName = "Layer to delete", Required = false, HelpText = "Layer's guid or name")]
		public string Layer { get; set; }

		[Option("all", Required = false, HelpText = "Delete all layers in solution")]
		public bool DeleteAll { get; set; }
	}
}
