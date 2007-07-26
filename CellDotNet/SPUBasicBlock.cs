using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	class SpuBasicBlock
	{
		private SpuInstruction _head;
		public SpuInstruction Head
		{
			get { return _head; }
			set { _head = value; }
		}

		private int _offset;
		/// <summary>
		/// Offset from the beginning of the method, in bytes.
		/// </summary>
		public int Offset
		{
			get { return _offset; }
			set { _offset = value; }
		}
	
	}
}
