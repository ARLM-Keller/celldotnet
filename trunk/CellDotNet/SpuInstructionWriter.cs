using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	partial class SpuInstructionWriter
	{
		private List<SpuInstruction> _instructions = new List<SpuInstruction>();

		private void AddInstruction(SpuInstruction inst)
		{
			_instructions.Add(inst);
		}

		private void WriteRR(SpuOpCode opcode, VirtualRegister rt, VirtualRegister ra, VirtualRegister rb)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Source2 = rb;
			inst.Destination = rt;
			AddInstruction(inst);
		}

		private void WriteRR2(SpuOpCode opcode, VirtualRegister rt, VirtualRegister ra)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Destination = rt;
			AddInstruction(inst);			
		}

		private void WriteRR1(SpuOpCode opcode, VirtualRegister rt)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Destination = rt;
			AddInstruction(inst);			
		}

		private void WriteRR1DE(SpuOpCode opcode, VirtualRegister ra)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			AddInstruction(inst);
		}

		private void WriteRR2DE(SpuOpCode opcode, VirtualRegister rt, VirtualRegister ra)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Destination = rt;
			AddInstruction(inst);
		}

		private void WriteRRR(SpuOpCode opcode, VirtualRegister rt, VirtualRegister ra, VirtualRegister rb, VirtualRegister rc)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Source2 = rb;
			inst.Source3 = rc;
			inst.Destination = rt;
			AddInstruction(inst);
		}

		private void WriteRI7(SpuOpCode opcode, VirtualRegister rt, VirtualRegister ra, int value)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Constant = value;
			inst.Destination = rt;
			AddInstruction(inst);
		}

		private void WriteRI8(SpuOpCode opcode, VirtualRegister rt, VirtualRegister ra, int scale)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Constant = scale;
			inst.Destination = rt;
			AddInstruction(inst);
		}

		private void WriteRI10(SpuOpCode opcode, VirtualRegister rt, VirtualRegister ra, int scale)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Constant = scale;
			inst.Destination = rt;
			AddInstruction(inst);
		}

		private void WriteRI16(SpuOpCode opcode, VirtualRegister rt, int symbol)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Constant = symbol;
			inst.Destination = rt;
			AddInstruction(inst);
		}

		private void WriteRI16x(SpuOpCode opcode, int symbol)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Constant = symbol;
			AddInstruction(inst);
		}

		private void WriteRI18(SpuOpCode opcode, VirtualRegister rt, int symbol)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Constant = symbol;
			inst.Destination = rt;
			AddInstruction(inst);
		}
	}

}
