using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// Represents an <see cref="ObjectWithAddress"/> used for data in local storage.
	/// </summary>
	class DataObject : ObjectWithAddress
	{
		private DataObject(int size)
		{
			Utilities.AssertArgument(size >= 0, "size >= 0");

			_size = size;
		}

		/// <summary>
		/// Constructs an instance with room for the specified number of quadwords.
		/// </summary>
		/// <param name="count"></param>
		/// <returns></returns>
		static public DataObject FromQuadWords(int count)
		{
			return new DataObject(count * 16);
		}

		private int _size;
		public override int Size
		{
			get { return _size; }
		}
	}
}
