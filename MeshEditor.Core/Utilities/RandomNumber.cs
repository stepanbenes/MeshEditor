using System;
using System.Collections.Generic;
using System.Text;

namespace MeshEditor.Utilities
{
	/// <summary>
	/// staticka trida obsahujici funkce pro ziskani nahodnych cisel
	/// </summary>
	public static class RandomNumber
	{
		private static Random rand;

		static RandomNumber()
		{
			rand = new Random();
		}

		/// <summary>
		/// vrati nahodne cele cislo z intervalu [min, max)
		/// </summary>
		/// <param name="min">dolni mez, patri do intervalu hodnot</param>
		/// <param name="max">horni mez, nepatri do intervalu hodnot</param>
		public static int Get(int min, int max)
		{
			return rand.Next(min, max);
		}

		public static double GetDoubleBetweenZeroAndOne()
		{
			return rand.NextDouble();
		}

		public static byte GetRandomByte()
		{
			return (byte)(rand.Next(0, 256));
		}
	}
}
