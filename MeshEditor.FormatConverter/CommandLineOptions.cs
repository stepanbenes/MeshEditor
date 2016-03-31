using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using MeshEditor.LayerManager.Compression;
using MeshEditor.LayerManager.Filters;

namespace MeshEditor.FormatConverter
{
	abstract class Options
	{
		// Omitting long name, default --verbose
		[Option(HelpText = "Prints all messages to standard output.")]
		public bool Verbose { get; set; }
	}
	
	[Verb("import", HelpText = "Convert supported mesh and result files to universal layer format")]
	class ImportOptions : Options
	{
		[Value(index: 0, MetaName = "Project name", Required = true, HelpText = "Name of new project")]
		public string ProjectName { get; set; }

		[Option('m', "mesh", Required = true, HelpText = "Mesh file to be processed.")]
		public string MeshFile { get; set; }

		[Option('r', "result", Required = false, HelpText = "Result files to be processed.")]
		public IEnumerable<string> ResultFiles { get; set; }

		[Option("compression", Required = false, HelpText = "Compression method")]
		public CompressionMethod CompressionMethod { get; set; }
	}

	[Verb("filter", HelpText = "Add new filter layer based on parent layer")]
	class FilterOptions : Options
	{
		[Value(index: 0, MetaName = "Parent layer", Required = true, HelpText = "Parent layer guid or name")]
		public string ParentLayer { get; set; }

		[Value(index: 1, MetaName = "Filter type", Required = true, HelpText = "Name of filter to be applied on the parent layer")]
		public FilterType FilterType { get; set; }

		[Option('p', "params", Required = false, HelpText = "Filter parameters")]
		public IEnumerable<string> FilterParameters { get; set; }

		[Option('n', "name", Required = false, HelpText = "Name of new layer")]
		public string LayerName { get; set; }
	}
}
