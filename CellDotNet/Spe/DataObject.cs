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
using System.Text;
using JetBrains.Annotations;

namespace CellDotNet.Spe
{
	/// <summary>
	/// Represents an <see cref="ObjectWithAddress"/> used for data in local storage.
	/// </summary>
	class DataObject : ObjectWithAddress
	{
		private int _size;

		public DataObject(int size, string name) : base(name)
		{
			Utilities.AssertArgument(size >= 0, "size >= 0");

			_size = size;
		}

		public DataObject(int size)
		{
			_size = size;
		}

		/// <summary>
		/// Constructs an instance with room for the specified number of quadwords.
		/// </summary>
		/// <param name="count"></param>
		/// <returns></returns>
		/// <param name="name"></param>
		static public DataObject FromQuadWords(int count, string name)
		{
			return new DataObject(count * 16, name);
		}

		public sealed override int Size
		{
			get { return _size; }
		}

		/// <summary>
		/// The value of the object. A value type array.
		/// </summary>
		[CanBeNull]
		public IList Value { get; private set; }

		public virtual void SetValue(int[] value)
		{
			Utilities.AssertArgument(value == null || value.Length <= (Size / 4), "value == null || value.Length <= (Size / 4)");
			if (Value != null)
				throw new InvalidOperationException("Re-assigning value - a bug?");

			Value = value;
		}

		public virtual void SetValue(long[] value)
		{
			Utilities.AssertArgument(value == null || value.Length <= (Size / 8), "value == null || value.Length <= (Size / 8)");
			if (Value != null)
				throw new InvalidOperationException("Re-assigning value - a bug?");

			Value = value;
		}

		public static DataObject FromQuadWords(int count, string name, int[] data)
		{
			var o = FromQuadWords(count, name);
			o.SetValue(data);
			return o;
		}

		public void Resize(int size)
		{
			_size = size;
		}
	}
}
