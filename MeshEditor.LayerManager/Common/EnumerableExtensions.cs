using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.LayerManager.Common
{
	public static class EnumerableExtensions
	{
		/// <summary>Adds a single element to the end of an IEnumerable.</summary>
		/// <typeparam name="T">Type of enumerable to return.</typeparam>
		/// <returns>IEnumerable containing all the input elements, followed by the specified additional element.</returns>
		public static IEnumerable<T> Append<T>(this IEnumerable<T> source, T element)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			return concatIterator(element, source, false);
		}

		/// <summary>Adds a single element to the start of an IEnumerable.</summary>
		/// <typeparam name="T">Type of enumerable to return.</typeparam>
		/// <returns>IEnumerable containing the specified additional element, followed by all the input elements.</returns>
		public static IEnumerable<T> Prepend<T>(this IEnumerable<T> tail, T head)
		{
			if (tail == null)
				throw new ArgumentNullException(nameof(tail));
			return concatIterator(head, tail, true);
		}

		private static IEnumerable<T> concatIterator<T>(T extraElement, IEnumerable<T> source, bool insertAtStart)
		{
			if (insertAtStart)
				yield return extraElement;
			foreach (var e in source)
				yield return e;
			if (!insertAtStart)
				yield return extraElement;
		}

		/// <summary>
		/// Returns empty enumerable if source sequence is null.
		/// </summary>
		public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)
		{
			return source ?? Enumerable.Empty<T>();
		}

		/// <summary>
		/// Split an IEnumerable<T> into fixed-sized chunks.
		/// see: http://stackoverflow.com/questions/13709626/split-an-ienumerablet-into-fixed-sized-chunks-return-an-ienumerableienumerab
		/// </summary>
		public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> items, int partitionSize)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));
			if (partitionSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(partitionSize));
			return new PartitionHelper<T>(items, partitionSize);
		}

		private sealed class PartitionHelper<T> : IEnumerable<IEnumerable<T>>
		{
			readonly IEnumerable<T> items;
			readonly int partitionSize;
			bool hasMoreItems;

			internal PartitionHelper(IEnumerable<T> i, int ps)
			{
				items = i;
				partitionSize = ps;
			}

			public IEnumerator<IEnumerable<T>> GetEnumerator()
			{
				using (var enumerator = items.GetEnumerator())
				{
					hasMoreItems = enumerator.MoveNext();
					while (hasMoreItems)
						yield return GetNextBatch(enumerator).ToList();
				}
			}

			IEnumerable<T> GetNextBatch(IEnumerator<T> enumerator)
			{
				for (int i = 0; i < partitionSize; ++i)
				{
					yield return enumerator.Current;
					hasMoreItems = enumerator.MoveNext();
					if (!hasMoreItems)
						yield break;
				}
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		public static bool IsOrdered<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			var comparer = Comparer<TKey>.Default;
			using (var iterator = source.GetEnumerator())
			{
				if (!iterator.MoveNext())
					return true;

				TKey current = keySelector(iterator.Current);

				while (iterator.MoveNext())
				{
					TKey next = keySelector(iterator.Current);
					if (comparer.Compare(current, next) > 0)
						return false;

					current = next;
				}
			}

			return true;
		}

		public static bool IsOrderedDescending<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keySelector)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			var comparer = Comparer<TKey>.Default;
			using (var iterator = source.GetEnumerator())
			{
				if (!iterator.MoveNext())
					return true;

				TKey current = keySelector(iterator.Current);

				while (iterator.MoveNext())
				{
					TKey next = keySelector(iterator.Current);
					if (comparer.Compare(current, next) < 0)
						return false;

					current = next;
				}
			}

			return true;
		}
	}
}
