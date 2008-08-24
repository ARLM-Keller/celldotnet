using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellDotNet.Cuda
{
	/// <summary>
	/// A buffer in shared memory.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class Shared1D<T>
	{
		public T this[int index]
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}
	}
}
