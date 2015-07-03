using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MeshEditor.IO;
using System.Reflection;
using System.IO;
using MeshEditor.Construction;
using MeshEditor.Data;
using MeshEditor.CoreInterface;

namespace MeshEditor.FormatConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			if(args.Length < 2)
			{
				Console.WriteLine("Usage: {0} input output", Path.GetFileName(Assembly.GetExecutingAssembly().CodeBase));
				Console.ReadKey();
				return;
			}

			IMeshFileParser parser;

			// choose parser
			switch (Path.GetExtension(args[0]))
			{
				case ".top":
					parser = new DefaultFileFormatParser(args[0]);
					break;
				case ".ply":
					parser = new PLYFileFormatParser(args[0]);
					break;
				case ".obj":
					parser = new OBJFileFormatParser(args[0]);
					break;
				case ".msh":
					parser = new GiDMshFileFormatParser(args[0]);
					break;
				default:
					throw new NotSupportedException();
			}

			IMeshSaver meshSaver;

			// choose saver
			switch (Path.GetExtension(args[1]))
			{
				case ".msh":
					meshSaver = new GiDMshFileFormatSaver();
					break;
				case ".vtk":
					meshSaver = new VTKFileFormatSaver(VTKFileFormatSaver.VTKFileFormats.SimpleASCII);
					break;
				default:
					throw new NotSupportedException();
			}
			meshSaver.Step += meshSaver_Step;

			meshSaver.SaveMesh(parser, args[1], /*cancelled: */ null);

			//MeshConstructor constructor = new MeshConstructor();
			//Mesh mesh = constructor.CreateMesh(parser, null);			
			//GiDMshFileFormatSaver meshSaver = new GiDMshFileFormatSaver();
			//meshSaver.SaveMesh(mesh, args[1], /*saveWithoutCuttedElements: */ false, null);

			Console.Clear();
			Console.Write("Done.");

			Console.ReadKey();
		}

		private static void meshSaver_Step(object sender, MeshIOEventArgs e)
		{
			Console.Clear();
			Console.Write("Done " + e.PercentDone + "%");
		}
	}
}
