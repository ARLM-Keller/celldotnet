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
using System.IO;

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

		public int GetInstructionCount()
		{
			if (Head == null)
				return 0;

			int c = 0;
			foreach (SpuInstruction inst in Head.GetEnumerable())
			{
				c++;
				Utilities.PretendVariableIsUsed(inst);
			}

			return c;
		}

		[Obsolete("Only for debugging.")]
		public string Disassembly
		{
			get
			{
				if (Head == null)
					return "(empty)";
				StringWriter sw = new StringWriter();
				Disassembler.DisassembleInstructions(Head.GetEnumerable(), 0, sw);
				return sw.GetStringBuilder().ToString();
			}
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
