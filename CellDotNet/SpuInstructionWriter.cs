using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	partial class SpuInstructionWriter
	{
		private List<SpuInstruction> _instructions;

		private void WriteRR(SpuOpCode opcode, VirtualRegister rt, VirtualRegister ra, VirtualRegister rb)
		{
			new SpuInstruction();
		}

		private void WriteRR2(SpuOpCode opcode, VirtualRegister rt, VirtualRegister ra)
		{
			
		}

		private void WriteRR1(SpuOpCode opcode, VirtualRegister rt)
		{
			
		}

		private void WriteRR1DE(SpuOpCode opcode, VirtualRegister ra)
		{
			
		}

		private void WriteRR2DE(SpuOpCode opcode, VirtualRegister rt, VirtualRegister ra)
		{
			
		}

		private void WriteRRR(SpuOpCode opcode, VirtualRegister rt, VirtualRegister ra, VirtualRegister rb, VirtualRegister rc)
		{
			
		}

		private void WriteRI7(SpuOpCode opcode, VirtualRegister rt, VirtualRegister ra, int value)
		{
			
		}

		private void WriteRI8(SpuOpCode opcode, VirtualRegister rt, VirtualRegister ra, int scale)
		{
			
		}

		private void WriteRI10(SpuOpCode opcode, VirtualRegister rt, VirtualRegister ra, int scale)
		{
			
		}

		private void WriteRI16(SpuOpCode opcode, VirtualRegister rt, int symbol)
		{
			
		}

		private void WriteRI16x(SpuOpCode opcode, int symbol)
		{
			
		}

		private void WriteRI18(SpuOpCode opcode, VirtualRegister rt, int symbol)
		{
			
		}
	}

}
