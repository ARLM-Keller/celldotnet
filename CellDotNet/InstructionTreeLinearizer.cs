using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// Converts an instruction tree to a list of instructions that uses registers.
	/// </summary>
	class InstructionTreeLinearizer
	{
		private int _lastRegisterNumber;

		Register GetNextVirtualRegister()
		{
			_lastRegisterNumber++;
			return new Register(_lastRegisterNumber);
		}

		public void Convert(BasicBlock bb, List<ListInstruction> output)
		{
			throw new NotImplementedException();
		}
	}
}
