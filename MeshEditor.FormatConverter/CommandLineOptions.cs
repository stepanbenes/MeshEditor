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

		[Option("config", HelpText = "Name of configuration file (relative or absolute path)")]
		public string ConfigFile { get; set; }
	}
	
	[Verb("import", HelpText = "Convert supported mesh and result files to universal layer format")]
	class ImportOptions : Options
	{
		[Value(index: 0, MetaName = "Solution id", Required = true, HelpText = "Solution id (same as Simulation id in db)")]
		public int SolutionId { get; set; }

		[Value(index: 1, MetaName = "Project name", Required = false, HelpText = "Name of new project")]
		public string ProjectName { get; set; }

		[Option('m', "mesh", Required = true, HelpText = "Mesh file to be processed.")]
		public string MeshFile { get; set; }

		[Option('r', "result", Required = false, HelpText = "Result files to be processed.")]
		public IEnumerable<string> ResultFiles { get; set; }
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

		[Value(index: 1, Required = true, HelpText = "Compression method")]
		public string Method { get; set; }

		[Option("field", Required = false, HelpText = "Name of field to compress")]
		public string FieldName { get; set; }

		[Option("component", Required = false, HelpText = "Name of Component of field to compress")]
		public string ComponentName { get; set; }
	}

	[Verb("diff", HelpText = "Compare two layers")]
	class DiffOptions : Options
	{
		[Value(index: 0, MetaName = "Layer to compare with its parent", Required = true, HelpText = "layer's guid or name")]
		public string Layer { get; set; }
	}

	[Verb("list", HelpText = "Enumerate all solutions in base directory")]
	class ListOptions : Options
	{
		//[Value(index: 0, MetaName = "Solution id", Required = false, HelpText = "Id of solution whose layers should be displayed")]
		//public int? SolutionId { get; set; }
	}
}
