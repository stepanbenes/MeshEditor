using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace MeshEditor.FormatConverter
{
	class Options
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
	}

	[Verb("filter", HelpText = "Add new filter layer based on parent layer")]
	class FilterOptions : Options
	{
		[Value(index: 0, MetaName = "Filter name", Required = true, HelpText = "Filter to be applied on parent layer")]
		public string Filter { get; set; }

		[Value(index: 1, MetaName = "Parent layer", Required = true, HelpText = "Parent layer guid")]
		public Guid ParentLayerId { get; set; }

		[Option('p', "params", Required = false, HelpText = "Filter parameters")]
		public IEnumerable<string> FilterParameters { get; set; }

		[Option('n', "name", Required = false, HelpText = "Name of new layer")]
		public string LayerName { get; set; }
	}
}
