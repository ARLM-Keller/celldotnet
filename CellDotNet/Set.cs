using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	class Set<T> : IEnumerable<T>, ICollection<T>
	{
		Dictionary<T, bool> dict = new Dictionary<T, bool>();

		public void Add(T item)
		{
			dict[item] = true;
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(T item)
		{
			return dict.ContainsKey(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			dict.Keys.CopyTo(array, arrayIndex);
		}

		bool ICollection<T>.Remove(T item)
		{
			return dict.Remove(item);
		}

		public void Remove(T item)
		{
			dict.Remove(item);
		}

		public int Count
		{
			get { return dict.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
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
