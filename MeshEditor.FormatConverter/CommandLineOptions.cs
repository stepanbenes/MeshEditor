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
		public abstract string ProjectName { get; set; }

		// Omitting long name, default --verbose
		[Option(HelpText = "Prints all messages to standard output.")]
		public bool Verbose { get; set; }

		[Option("directory", Required = false, HelpText = "Project location (if differs from current directory)")]
		public string Directory { get; set; }
	}
	
	[Verb("import", HelpText = "Convert supported mesh and result files to universal layer format")]
	class ImportOptions : Options
	{
		[Value(index: 0, MetaName = "Project name", Required = false, HelpText = "Name of new project")]
		public override string ProjectName { get; set; }

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
		public FilterType FilterType { get; set; }

		[Option("project", Required = false, HelpText = "Project name")]
		public override string ProjectName { get; set; }

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
		public CompressionMethod Method { get; set; }

		[Option("project", Required = false, HelpText = "Project name")]
		public override string ProjectName { get; set; }

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

		[Option("project", Required = false, HelpText = "Project name")]
		public override string ProjectName { get; set; }
	}
}
