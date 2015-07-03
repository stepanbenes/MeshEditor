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
using System.Threading;
using System.Globalization;

namespace MeshEditor.FormatConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

			if(args.Length < 2)
			{
				Console.WriteLine("Usage: {0} input output", Path.GetFileName(Assembly.GetExecutingAssembly().CodeBase));
				Console.ReadKey();
				return;
			}

			IMeshFileParser parser = MeshParserFactory.Create(args[0]); // choose parser

			IMeshSaver meshSaver;

			// choose saver
			switch (Path.GetExtension(args[1]))
			{
				case ".res":
					GiDResFileFormatGenerator generator = new GiDResFileFormatGenerator();
					generator.GenerateResultFile(parser, args[1]);
					meshSaver = null;
					break;
				default:
					meshSaver = MeshSaverFactory.Create(args[1]);
					break;
			}

			if (meshSaver != null)
			{
				meshSaver.Step += meshSaver_Step;
				meshSaver.SaveMesh(parser, args[1], /*cancelled: */ null);
			}
			
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
