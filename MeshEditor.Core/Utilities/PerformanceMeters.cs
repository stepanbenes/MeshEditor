using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace MeshEditor.Utilities
{
	/// <summary>
	/// rozhrani specifikujici funkce, ktere by mel poskytovat univerzalni meric nejake veliciny
	/// </summary>
	public interface IMeter : IDisposable
	{
		void PrintValue();
		void Split();
		void Lap();
	}

	/// <summary>
	/// trida ulehcujici mereni doby trvani nejake operace a umoznujici snadny vypis namerenych hodnot
	/// </summary>
	public class Chronometer : IMeter
	{
		private Stopwatch stopwatch;

		public Chronometer()
		{
			stopwatch = new Stopwatch();
			stopwatch.Start();
		}

		public void PrintValue()
		{
			Console.WriteLine(stopwatch.ElapsedMilliseconds);
		}

		public void Split()
		{
			stopwatch.Stop();
			PrintValue();
			stopwatch.Start();
		}

		public void Lap()
		{
			stopwatch.Stop();
			PrintValue();
			stopwatch.Reset();
			stopwatch.Start();
		}
		
		#region IDisposable Members

		public void Dispose()
		{
			stopwatch.Stop();
			PrintValue();
			stopwatch = null;
		}

		#endregion
	}

	/// <summary>
	/// trida ulehcujici mereni obsazenosti pameti a umoznujici snadny vypis namerenych hodnot
	/// </summary>
	public class MemoryMeter : IMeter
	{
		public static long GetCurrentMemoryConsumption()
		{
			Thread.MemoryBarrier();
			return System.GC.GetTotalMemory(true);
		}

		private long memoryStart;
		private long memoryAfter;

		public MemoryMeter()
		{
			Thread.MemoryBarrier();
			memoryStart = memoryAfter = System.GC.GetTotalMemory(true);
		}
		
		public void PrintValue()
		{
			Console.WriteLine(memoryAfter - memoryStart);
		}

		public void Split()
		{
			Thread.MemoryBarrier();
			memoryAfter = System.GC.GetTotalMemory(true);
			PrintValue();
		}

		public void Lap()
		{
			Thread.MemoryBarrier();
			memoryAfter = System.GC.GetTotalMemory(true);
			PrintValue();
			memoryStart = memoryAfter;
		}
		
		#region IDisposable Members

		public void Dispose()
		{
			Thread.MemoryBarrier();
			memoryAfter = System.GC.GetTotalMemory(true);
			PrintValue();
		}

		#endregion
	}
}
