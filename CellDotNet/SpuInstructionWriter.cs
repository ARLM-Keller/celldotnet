using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace CellDotNet
{
	partial class SpuInstructionWriter
	{
		private List<SpuInstruction> _instructions = new List<SpuInstruction>();
		/// <summary>
		/// The instructions that have been written.
		/// </summary>
		public ReadOnlyCollection<SpuInstruction> Instructions
		{
			get { return _instructions.AsReadOnly(); }
		}

		private int _regnum = 1;

		private VirtualRegister NextRegister()
		{
			return new VirtualRegister(_regnum++);
		}

		private void AddInstruction(SpuInstruction inst)
		{
			_instructions.Add(inst);
		}

		private VirtualRegister WriteRR(SpuOpCode opcode, VirtualRegister ra, VirtualRegister rb)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Source2 = rb;
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
		}

		private VirtualRegister WriteRR2(SpuOpCode opcode, VirtualRegister ra)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
		}

		private VirtualRegister WriteRR1(SpuOpCode opcode)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
		}

		private void WriteRR1DE(SpuOpCode opcode, VirtualRegister ra)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			AddInstruction(inst);
		}

		private VirtualRegister WriteRR2DE(SpuOpCode opcode, VirtualRegister ra)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
		}

		private VirtualRegister WriteRRR(SpuOpCode opcode, VirtualRegister ra, VirtualRegister rb, VirtualRegister rc)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Source2 = rb;
			inst.Source3 = rc;
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
		}

		private VirtualRegister WriteRI7(SpuOpCode opcode, VirtualRegister ra, int value)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Constant = value;
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
		}

		private VirtualRegister WriteRI8(SpuOpCode opcode, VirtualRegister ra, int scale)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Constant = scale;
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
		}

		private VirtualRegister WriteRI10(SpuOpCode opcode, VirtualRegister ra, int scale)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Constant = scale;
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
		}

		private VirtualRegister WriteRI16(SpuOpCode opcode, int symbol)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Constant = symbol;
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
		}

		private void WriteRI16x(SpuOpCode opcode, int symbol)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Constant = symbol;
			AddInstruction(inst);
		}

		private VirtualRegister WriteRI18(SpuOpCode opcode, int symbol)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Constant = symbol;
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
		}


		/// <summary>
		/// Returns the instructions that are currently in the writer as assembly code.
		/// </summary>
		/// <returns></returns>
		public string Disassemble()
		{
			StringWriter tw = new StringWriter();

			Disassemble(tw);

			return tw.GetStringBuilder().ToString();
		}

		private void Disassemble(TextWriter tw)
		{
			foreach (SpuInstruction inst in _instructions)
			{
				switch (inst.OpCode.Format)
				{
					case SpuInstructionFormat.None:
						throw new Exception();
					case SpuInstructionFormat.RR:
						tw.Write("{0} ${1}, ${2}, ${3}", inst.OpCode.Name, inst.Destination, inst.Source1, inst.Source2);
						break;
					case SpuInstructionFormat.RR2:
						tw.Write("{0} ${1}, ${2}", inst.OpCode.Name, inst.Destination, inst.Source1);
						break;
					case SpuInstructionFormat.RR1:
						tw.Write("{0} ${1}", inst.OpCode.Name, inst.Destination);
						break;
					case SpuInstructionFormat.RR1DE:
						tw.Write("{0} ${1}", inst.OpCode.Name, inst.Source1);
						break;
					case SpuInstructionFormat.RR2DE:
						tw.Write("{0} ${1}, ${2}", inst.OpCode.Name, inst.Destination, inst.Source1);
						break;
					case SpuInstructionFormat.RRR:
						tw.Write("{0} ${1}, ${2}, ${3}", inst.OpCode.Name, inst.Destination, inst.Source1, inst.Source2, inst.Source3);
						break;
					case SpuInstructionFormat.RI7:
						tw.Write("{0} ${1}, ${2}, ${3}", inst.OpCode.Name, inst.Destination, inst.Source1, inst.Constant);
						break;
					case SpuInstructionFormat.RI8:
						tw.Write("{0} ${1}, ${2}, ${3}", inst.OpCode.Name, inst.Destination, inst.Source1, inst.Constant);
						break;
					case SpuInstructionFormat.RI10:
						tw.Write("{0} ${1}, ${2}, ${3}", inst.OpCode.Name, inst.Destination, inst.Source1, inst.Constant);
						break;
					case SpuInstructionFormat.RI16:
						tw.Write("{0} ${1}, ${2}", inst.OpCode.Name, inst.Destination, inst.Constant);
						break;
					case SpuInstructionFormat.RI16x:
						tw.Write("{0} ${1}", inst.OpCode.Name, inst.Constant);
						break;
					case SpuInstructionFormat.RI18:
						tw.Write("{0} ${1}, ${2}", inst.OpCode.Name, inst.Destination, inst.Constant);
						break;
					default:
						throw new Exception();
				}
				tw.WriteLine();
			}
		}
	}

}
