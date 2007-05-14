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

		VirtualRegister GetNextVirtualRegister()
		{
			_lastRegisterNumber++;
			return new VirtualRegister(_lastRegisterNumber);
		}

		public void Convert(BasicBlock bb, List<SpuInstruction> output)
		{
			throw new NotImplementedException();
		}
	}
}
