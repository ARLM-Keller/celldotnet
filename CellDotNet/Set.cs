// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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

		public Set(IEnumerable<T> initialElements)
		{
			if (initialElements is ICollection<T>)
				dict = new Dictionary<T, bool>(((ICollection<T>)initialElements).Count);
			else
				dict = new Dictionary<T, bool>();

			AddAll(initialElements);
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

		public int Count
		{
			get { return dict.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		override public bool Equals(object obj)
		{
			if (!(obj is Set<T>))
				return false;

			Set<T> set = (Set<T>) obj;

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
