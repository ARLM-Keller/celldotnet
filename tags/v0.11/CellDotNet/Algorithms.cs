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
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// Miscellaneous algorithmic stuff.
	/// </summary>
	static class Algorithms
	{
		static public List<T> FindAll<T>(IEnumerable<T> list, Predicate<T> predicate)
		{
			List<T> l2 = new List<T>();
			foreach (T t in list)
			{
				if (predicate(t))
					l2.Add(t);
			}

			return l2;
		}

		public static bool AreEqualSets<T>(IEnumerable<T> s1, IEnumerable<T> s2)
		{
			return AreEqualSets(s1, s2, Comparer<T>.Default);
		}

		public static bool AreEqualSets<T>(IEnumerable<T> s1, IEnumerable<T> s2, IComparer<T> comparer)
		{
			ICollection<T> c1 = s1 as ICollection<T>;
			ICollection<T> c2 = s2 as ICollection<T>;

			if (c1 != null && c2 != null && c1.Count != c2.Count)
				return false;

			List<T> l1 = new List<T>(s1);
			l1.Sort(comparer);

			List<T> l2 = new List<T>(s2);
			l2.Sort(comparer);

			return AreEqualSortedSets(l1, l2, comparer);
		}

		public static bool AreEqualSortedSets<T>(ICollection<T> s1, ICollection<T> s2, IComparer<T> comparer)
		{
			if (s1.Count != s2.Count)
				return false;

			IEnumerator<T> e1 = s1.GetEnumerator();
			IEnumerator<T> e2 = s2.GetEnumerator();

			bool ok1 = e1.MoveNext();
			bool ok2 = e2.MoveNext();

			while (ok1 && ok2)
			{
				if (comparer.Compare(e1.Current, e2.Current) != 0)
					return false;

				ok1 = e1.MoveNext();
				ok2 = e2.MoveNext();
			}

			return !(ok1 ^ ok2);
		}
	}
}
