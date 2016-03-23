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

	[Verb("add", HelpText = "Add new layer based on parent layer")]
	class AddOptions : Options
	{
		[Value(index: 0, MetaName = "Layer name", Required = true, HelpText = "Name of new layer")]
		public string LayerName { get; set; }

		[Option('p', "parent", Required = true, HelpText = "Parent layer guid")]
		public Guid ParentLayerId { get; set; }

		[Option('f', "filters", Required = true, HelpText = "Filters to be applied on parent layer")]
		public IEnumerable<string> Filters { get; set; }
	}
}
