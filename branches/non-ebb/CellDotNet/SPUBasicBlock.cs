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
