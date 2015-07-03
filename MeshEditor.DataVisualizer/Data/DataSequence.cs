using MeshEditor.DataVisualizer.Mathematics;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MeshEditor.DataVisualizer.Data
{
	public class DataSequence : DataAbstract
	{

		#region Fields, Constructor

		SortedList<double, DataAbstract> timeStamps;
		public static readonly double MaxRelativeError = 0.001; // !!
		
		public DataSequence()
		{
			timeStamps = new SortedList<double, DataAbstract>();
		}

		#endregion

		#region Public methods

		public void AddTimeStamp(double time, DataAbstract data)
		{
			DataSequence dataSequence = data as DataSequence;
			if (dataSequence != null)
			{
				foreach (var pair in dataSequence.timeStamps)
					timeStamps.Add(pair.Key, pair.Value);
			}
			else
			{
				timeStamps.Add(time, data);
			}
		}

		//public static bool LinearDependencyTest(Polynomial func1, Polynomial func2, float tolerance)
		//{
		//	// TODO: remove QuadrilinearForm and Polynomial.ComputeValue(x,y,z,t) from Polynomial.cs

		//	throw new NotImplementedException();
		//}

		public void SetupDataDescription()
		{
			foreach (DataAbstract data in timeStamps.Values)
			{
				MaxError = Math.Max(MaxError, data.MaxError);
				ItemCount += data.ItemCount;
				//Approximation = null;

				if (data.MinValue < MinValue) // compare to minimum
				{
					MinValue = data.MinValue;
					MinValueEntityNumber = data.MinValueEntityNumber;
				}
				if (data.MaxValue > MaxValue) // compare to maximum
				{
					MaxValue = data.MaxValue;
					MaxValueEntityNumber = data.MaxValueEntityNumber;
				}
			}

			if (timeStamps.Count > 0)
			{
				AverageError = timeStamps.Values.Average(d => d.AverageError); // /**/ is it right? is it optimal?
			}
		}

		/// <summary>
		/// Removes time stamps in data sequence that can be interpolated from other time stamps without big loss of precision.
		/// </summary>
		/// <param name="domainPoints">Sequence of test points in area that descibes approximated data sequence object.</param>
		/// <param name="fixedTimes">Sorted array of times that must be preserved.</param>
		public void CompressTimeInDomain(IEnumerable<Vector3> domainPoints, double[] fixedTimes)
		{
			Debug.Assert(ItemCount > 0); // SetupDataDescription() must be called before this method

			// compress time stamps (remove interleaved stamps that can be removed without loss of precission)
			if (timeStamps.Count > 2)
			{
				Debug.Assert(fixedTimes != null);
				Debug.Assert(/*isIncreasing = */ !fixedTimes.SkipWhile((x, i) => i == 0 || fixedTimes[i - 1] < x).Any());

				List<int> fixedTimesIndexes = new List<int>(fixedTimes.Length + 2);

				// create fixed times index list --------------------
				fixedTimesIndexes.Add(0);
				for (int i = 0; i < fixedTimes.Length; i++)
				{
					int index = timeStamps.IndexOfKey(fixedTimes[i]); // TODO: find nearest timeStamp to fixed time if not found exact same
					if (index > 0 && index < timeStamps.Count - 1)
						fixedTimesIndexes.Add(index);
				}
				fixedTimesIndexes.Add(timeStamps.Count - 1);
				// --------------------------------------------------
				
				List<double> timesToRemove = new List<double>();

				// compress fixed time intervals --------------------
				for (int i = 1; i < fixedTimesIndexes.Count; i++)
				{
					compressTimeStamps(fixedTimesIndexes[i - 1], fixedTimesIndexes[i], timesToRemove, domainPoints);
				}
				// --------------------------------------------------

				// remove redundant time stamps ---------------------
				foreach (double time in timesToRemove)
				{
					timeStamps.Remove(time);
				}
				// --------------------------------------------------
			}
		}

		#endregion

		#region Overrides

		public override bool ContainsTime(double time)
		{
			Debug.Assert(timeStamps.Count > 0);
			return time >= timeStamps.Keys[0] && time <= timeStamps.Keys[timeStamps.Count - 1];
			//return timeStamps.ContainsKey(time);
		}

		public override double ComputeValueAt(ref Vector4 spacetime)
		{
			// Assume keys are doubles, may need to convert to doubles if required here.
			List<double> times = timeStamps.Keys.ToList();

			int ipos = times.BinarySearch(spacetime.W);

			if (ipos >= 0)
			{
				// exact target found at position "ipos"
				return timeStamps.Values[ipos].ComputeValueAt(ref spacetime);
			}
			else
			{
				// Exact key not found: BinarySearch returns negative when the 
				// exact target is not found, which is the bitwise complement 
				// of the next index in the list larger than the target.
				ipos = ~ipos;
				if (ipos >= 0 && ipos < times.Count)
				{
					if (ipos > 0)
					{
						// target is between positions "ipos-1" and "ipos"
						return interpolateTwoTimeStamps(ipos - 1, ipos, ref spacetime);
					}
					else
					{
						// target is below position "ipos"
						return double.NaN; /**/
					}
				}
				else
				{
					// target is above position "ipos"
					return double.NaN; /**/
				}
			}
		}

		public override long GetSizeInBytes()
		{
			return timeStamps.Values.Sum(d => d.GetSizeInBytes()) + sizeof(double) * timeStamps.Count + IntPtr.Size;
		}

		#endregion

		#region Private methods

		private double interpolateTwoTimeStamps(int index1, int index2, ref Vector4 spacetime)
		{
			double value1 = timeStamps.Values[index1].ComputeValueAt(ref spacetime);
			double value2 = timeStamps.Values[index2].ComputeValueAt(ref spacetime);

			double t = ((double)spacetime.W - timeStamps.Keys[index1]) / (timeStamps.Keys[index2] - timeStamps.Keys[index1]);
			Debug.Assert(t >= 0.0 && t <= 1.0);

			return (value2 - value1) * t + value1;
		}

		private void compressTimeStamps(int fromIndex, int toIndex, List<double> timesToRemove, IEnumerable<Vector3> domainPoints)
		{
			Debug.Assert(timesToRemove != null);
			if (toIndex - 1 <= fromIndex)
				return;

			double fromTime = timeStamps.Keys[fromIndex];
			double toTime = timeStamps.Keys[toIndex];

			Polynomial from = timeStamps.Values[fromIndex].Approximation;
			Polynomial to = timeStamps.Values[toIndex].Approximation;

			List<double> toRemove = new List<double>();

			for (int index = fromIndex + 1; index < toIndex; index++)
			{
				double time = timeStamps.Keys[index];
				double timeFactor = (time - fromTime) / (toTime - fromTime);
				
				Polynomial testFunction = timeStamps.Values[index].Approximation;
				Polynomial interpolation = Polynomial.Interpolate(from, to, (float)timeFactor);

				double sumDiffSqr = 0.0;
				double sumFuncSqr = 0.0;

				foreach (Vector3 testPoint in domainPoints)
				{
					double testValue = testFunction.ComputeValue(testPoint.X, testPoint.Y, testPoint.Z);
					double interpolatedValue = interpolation.ComputeValue(testPoint.X, testPoint.Y, testPoint.Z);
					double diff = testValue - interpolatedValue;
					
					sumDiffSqr += diff * diff;
					sumFuncSqr += testValue * testValue;
				}

				double absoluteError = Math.Sqrt(sumDiffSqr);
				double absoluteValue = Math.Sqrt(sumFuncSqr);

				double relativeError;
				if (absoluteError <= Common.Epsilon && absoluteValue <= Common.Epsilon)
					relativeError = 0.0;
				else if (absoluteValue <= Common.Epsilon)
					relativeError = 1.0;
				else
					relativeError = (absoluteError / absoluteValue);
				
				if (relativeError <= MaxRelativeError)
				{
					toRemove.Add(time);
				}
				else
				{
					toRemove = null;
					int half = (toIndex + fromIndex) >> 1; // divide by two
					compressTimeStamps(fromIndex, half, timesToRemove, domainPoints);
					compressTimeStamps(half, toIndex, timesToRemove, domainPoints);
					return;
				}
			}

			timesToRemove.AddRange(toRemove); // finally, mark all interleaved time stamps to be removed and quit current branch of recursion

		}

		#endregion

	}
}
