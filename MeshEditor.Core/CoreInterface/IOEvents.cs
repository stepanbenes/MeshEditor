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
		private int percentDone; // <0;100>
		public int PercentDone
		{
			get { return percentDone; }
			set
			{
				if (value < 0)
					percentDone = 0;
				else if (value > 100)
					percentDone = 100;
				else
					percentDone = value;
			}
		}
		public MeshIOEventArgs(int percentDone)
		{
			PercentDone = percentDone;
		}
	}

	/// <summary>
	/// delegat reprezentujici funkci vracejici pravdivostni hodnotu;
	/// pouzite pro urceni zda nedoslo ke zruseni operace nacitani nebo ukladani site
	/// </summary>
	public delegate bool YesNoQuestion();

}
