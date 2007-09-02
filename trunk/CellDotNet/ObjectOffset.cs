using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	class ObjectOffset : ObjectWithAddress
	{
		private ObjectWithAddress _parent;
		private int _offset;

		public ObjectOffset(ObjectWithAddress parent, int offset)
		{
			_parent = parent;
			_offset = offset;
		}

		public override int Offset
		{
			get
			{
				return _parent.Offset + _offset;
			}
			set { throw new InvalidOperationException("This is not an independant object."); }
		}

		public override int Size
		{
			get { throw new InvalidOperationException(); }
		}
	}
}
