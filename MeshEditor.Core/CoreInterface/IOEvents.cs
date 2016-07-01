using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.CoreInterface
{
	/// <summary>
	/// delegat pro oznameni o provedeni IO operace
	/// </summary>
	public delegate void MeshIOEventHandler(object sender, MeshIOEventArgs e);

	/// <summary>
	/// argument oznamujici provedeni IO operace
	/// </summary>
	public class MeshIOEventArgs : EventArgs
	{
		public int PercentDone { get; set; }
		
		public string TaskName { get; }
		public string OperationName	{ get; }

		public MeshIOEventArgs(int percentDone)
		{
			PercentDone = percentDone;
		}
		public MeshIOEventArgs(int percentDone, string taskName, string operationName)
		{
			PercentDone = percentDone;
			OperationName = operationName;
			TaskName = taskName;
		}
	}

	/// <summary>
	/// delegat reprezentujici funkci vracejici pravdivostni hodnotu;
	/// pouzite pro urceni zda nedoslo ke zruseni operace nacitani nebo ukladani site
	/// </summary>
	public delegate bool YesNoQuestion();

}
