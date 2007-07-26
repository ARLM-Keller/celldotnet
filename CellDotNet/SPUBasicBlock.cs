using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	class SpuBasicBlock : ObjectWithAddress
	{
		private SpuInstruction _head;
		public SpuInstruction Head
		{
			get { return _head; }
			set { _head = value; }
		}
	}
}
