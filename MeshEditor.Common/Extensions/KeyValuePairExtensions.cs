using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshEditor.Common.Extensions
{
	public static class KeyValuePairExtensions
	{
		public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> @this, out TKey key, out TValue value)
		{
			key = @this.Key;
			value = @this.Value;
		}
	}
}
