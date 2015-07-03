using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom.Compiler;
using System.Reflection;
using Microsoft.CSharp;
using MeshEditor.Data;

namespace MeshEditor.Cuts
{
	/// <summary>
	/// delegat pro metodu, ktera vezme souradnice bodu a rekne, zda je bod v oblasti rezu ci nikoli
	/// </summary>
	public delegate bool CutTest(float x, float y, float z);

	/// <summary>
	/// staticka trida, ktera obsahuje metodu vracejici anonymni testovaci metodu pro rez site
	/// </summary>
	public static class CutTestProvider
	{

		#region Public methods

		public static CutTest ProvideTestFunction(string test)
		{
			test = prepareTestStringForBuilding(test);
			string source = buildSourceText(test);
			Assembly asm = compile(source);
			Type myNewType = asm.GetType("RuntimeGenerated.TempClass");

			CutTest cutTest = (CutTest)myNewType.InvokeMember("GetCutTest", BindingFlags.InvokeMethod, null, null, new object[] { });

			//CutTest cutTest = delegate(float x, float y, float z)
			//{
			//    return z > 470f && y > 10f; // je to ve skutecnych jednotkach site
			//};

			return cutTest;
		}

		#endregion

		#region Private methods

		private static string prepareTestStringForBuilding(string test)
		{
			StringBuilder text = new StringBuilder(test.ToLower());

			text.Replace("and", "&&");
			text.Replace("or", "||");
			text.Replace("asin", "Math.Asin");
			text.Replace("acos", "Math.Acos");
			text.Replace("atan", "Math.Atan");
			text.Replace("sinh", "Math.Sinh");
			text.Replace("cosh", "Math.Cosh");
			text.Replace("tanh", "Math.Tanh");
			text.Replace("sin", "Math.Sin");
			text.Replace("cos", "Math.Cos");
			text.Replace("tan", "Math.Tan");
			text.Replace("pow", "Math.Pow");
			text.Replace("log", "Math.Log");
			text.Replace("min", "Math.Min");
			text.Replace("max", "Math.Max");
			text.Replace("abs", "Math.Abs");
			text.Replace("sqrt", "Math.Sqrt");
			
			// Available variables:
			// x y z
			// Available relational operators:
			// == != > < >= <=
			// Available operations or functions:
			// + - * / and or
			// pow(base,exp) log(base,arg) min(a,b) max(a,b) 
			// sin() cos() tan() abs() sqrt() 
			// asin() acos() atan() sinh() cosh() tanh() 


			return text.ToString();
		}

		private static string buildSourceText(string test)
		{
			string head = "using MeshEditor.Cuts; using System; namespace RuntimeGenerated{";
			string tail = "}";
			string content = @"public static class TempClass { public static CutTest GetCutTest(){ return delegate(float x, float y, float z){return (" + test + ");}; }}";

			return head + content + tail;
		}

		private static Assembly compile(string source)
		{
			CSharpCodeProvider compiler = new CSharpCodeProvider();
			CompilerParameters compilerParametres = new CompilerParameters();

			/*************/
			List<string> assemblyLocations = new List<string>();

			//foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			//    if (asm.FullName.Contains("Core"))
			//        assemblyLocations.Add(asm.Location);

			// pridat reference na assembly
			assemblyLocations.Add(typeof(CutTestProvider).Assembly.Location);
			assemblyLocations.Add("System.dll");

			foreach (string location in assemblyLocations)
				compilerParametres.ReferencedAssemblies.Add(location);
			/**************/

			compilerParametres.GenerateExecutable = false;
			compilerParametres.GenerateInMemory = false;
			compilerParametres.IncludeDebugInformation = false;

			// kompilace
			CompilerResults compilerResults = compiler.CompileAssemblyFromSource(compilerParametres, source);
			
			// pri kompilaci doslo k chybam - vyhodit vujjimku
			if (compilerResults.Errors.Count > 0)
				throw new RuntimeCompilationException("Runtime compliling error", compilerResults);

			return compilerResults.CompiledAssembly;
		}

		#endregion

	}

	/// <summary>
	/// vyjimka vznikla pri dynamicke kompilaci algebraickeho vyrazu specifikujiciho oblast rezu
	/// </summary>
	[global::System.Serializable]
	public class RuntimeCompilationException : Exception
	{
		private CompilerResults results = null;
		
		public CompilerResults Results
		{
			get { return results; }
		}
	
		public RuntimeCompilationException() { }
		public RuntimeCompilationException(string message) : base(message) { }
		public RuntimeCompilationException(string message, Exception inner) : base(message, inner) { }
		protected RuntimeCompilationException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }

		public RuntimeCompilationException(string message, CompilerResults results)
			: base(message)
		{
			this.results = results;
		}

		public string GetErrorMessages()
		{
			if (results == null)
				return string.Empty;
			StringBuilder text = new StringBuilder();
			foreach (CompilerError error in results.Errors)
				text.AppendLine(error.ErrorText);
			return text.ToString();
		}
	}
}
