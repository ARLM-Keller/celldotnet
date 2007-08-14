using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace CellDotNet
{
	[DebuggerDisplay("Count = {Count}")]
	public class Set<T> : IEnumerable<T>, ICollection<T>
	{
		Dictionary<T, bool> dict = new Dictionary<T, bool>();

		public Set()
		{
			
		}

		public Set(int capacity)
		{
			dict = new Dictionary<T, bool>(capacity);
		}

		public void Add(T item)
		{
			dict[item] = true;
		}

		public void AddAll(Set<T> set)
		{
			if (set != null)
				foreach (T item in set)
				{
					Add(item);
			}
		}

		public void AddAll(IEnumerable<T> values)
		{
			if (values != null)
				foreach (T item in values)
				{
					Add(item);
				}
		}

		public void RemoveAll(Set<T> set)
		{
			if (set != null)
				foreach (T item in set)
					Remove(item);
		}

		public void Clear()
		{
			dict.Clear();
		}

		public bool Contains(T item)
		{
			return dict.ContainsKey(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			dict.Keys.CopyTo(array, arrayIndex);
		}

		public bool Remove(T item)
		{
			return dict.Remove(item);
		}

		public void RemoveAll(IEnumerable<T> values)
		{
			if(values != null)
				foreach (T t in values)
					Remove(t);
		}

		// Returns a "Random" item from the set.
		public T getItem()
		{
			IEnumerator<T> e = ((IEnumerable<T>)this).GetEnumerator();
			e.MoveNext();
			return e.Current;
		}

		public int Count
		{
			get { return dict.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		override public bool Equals(Object o)
		{
			if (!(o is Set<T>))
				return false;

			Set<T> set = (Set<T>) o;

			if (Count != set.Count)
				return false;

			foreach (T e in set)
				if (!Contains(e))
					return false;
			return true;
		}

		public override int GetHashCode()
		{
			int hash = 0;
			foreach (T t in this)
				hash += t.GetHashCode();

			return hash;
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return dict.Keys.GetEnumerator();
		}

		public IEnumerator GetEnumerator()
		{
			return ((IEnumerable<T>) this).GetEnumerator();
		}

		public override string ToString()
		{
			return "Count = " + Count;
		}

	}
}
