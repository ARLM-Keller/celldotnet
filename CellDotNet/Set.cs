using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	class Set<T> : IEnumerable<T>
	{
		Dictionary<T, bool> dict = new Dictionary<T, bool>();

		public void Add(T item)
		{
			dict[item] = true;
		}

		public void Remove(T item)
		{
			dict.Remove(item);
		}

		public int Count
		{
			get { return dict.Count; }
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return dict.Keys.GetEnumerator();
		}

		public IEnumerator GetEnumerator()
		{
			return ((IEnumerable<T>) this).GetEnumerator();
		}
	}
}
