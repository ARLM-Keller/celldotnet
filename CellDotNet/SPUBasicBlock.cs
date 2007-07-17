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
		/// Byte offset of the basic block.
		/// </summary>
		public int Offset
		{
			get { return _offset; }
			set { _offset = value; }
		}

	}
}
