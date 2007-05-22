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

		public VirtualRegister NextRegister()
		{
			return new VirtualRegister(_regnum++);
		}

		private void AddInstruction(SpuInstruction inst)
		{
			_instructions.Add(inst);
		}

		private VirtualRegister WriteRR(SpuOpCode opcode, VirtualRegister ra, VirtualRegister rb)
		{
		    VirtualRegister rt = NextRegister();
		    WriteRR(opcode, ra, rb, rt);
		    return rt;
/*
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Source2 = rb;
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
 */
		}

        private void WriteRR(SpuOpCode opcode, VirtualRegister ra, VirtualRegister rb, VirtualRegister rt)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Source2 = rb;
			inst.Destination = rt;
			AddInstruction(inst);
		}

		private VirtualRegister WriteRR2(SpuOpCode opcode, VirtualRegister ra)
		{
		    return WriteRR(opcode, ra, NextRegister());
/*
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
            inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
 */
		}

		private VirtualRegister WriteRR1(SpuOpCode opcode)
		{
		    return WriteRR(opcode, NextRegister(), NextRegister());
/*
			SpuInstruction inst = new SpuInstruction(opcode);
            inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
 */
		}

        // TODO brugen af denne funktion b�r astattes af den neden for.
		private void WriteRR1DE(SpuOpCode opcode, VirtualRegister ra)
		{
		    WriteRR2DE(opcode, ra);
/*
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			AddInstruction(inst);
 */
		}

        private void WriteRR1DE(SpuOpCode opcode, VirtualRegister ra, bool D, bool E)
        {
            WriteRR2DE(opcode, ra, D, E);
/*
            SpuInstruction inst = new SpuInstruction(opcode);
            inst.Source1 = ra;
            inst.Constant  = (D) ? 0x20 : 0x00; // 0x20 = 0100000b
            inst.Constant |= (E) ? 0x10 : 0x00; // 0x10 = 0010000b
            AddInstruction(inst);
 */
        }

        // TODO brugen af denne funktion b�r astattes af den neden for.
        private VirtualRegister WriteRR2DE(SpuOpCode opcode, VirtualRegister ra)
		{
		    return WriteRR2DE(opcode, ra, false, false);
/*
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
 */
		}

        private VirtualRegister WriteRR2DE(SpuOpCode opcode, VirtualRegister ra, bool D, bool E)
        {
            SpuInstruction inst = new SpuInstruction(opcode);
            inst.Source1 = ra;
            inst.Destination = NextRegister();
            inst.Constant  = (D) ? 0x20 : 0x00; // 0x20 = 0100000b
            inst.Constant |= (E) ? 0x10 : 0x00; // 0x10 = 0010000b
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
			inst.Constant = value & 0x0000007F; //NOTE muligivs unders�ge om value passer i 7 bit.
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
		}

		private VirtualRegister WriteRI8(SpuOpCode opcode, VirtualRegister ra, int scale)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Constant = scale & 0x000000ff;
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
		}

        private VirtualRegister WriteRI10(SpuOpCode opcode, VirtualRegister ra, int scale)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
            inst.Constant = scale & 0x000003ff; //NOTE muligivs unders�ge om value passer i 10 bit.
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
		}

		private void WriteRI10Sourced(SpuOpCode opcode, VirtualRegister ra, VirtualRegister rt, int scale)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = ra;
			inst.Source2 = rt;
            inst.Constant = scale & 0x000003ff; //NOTE muligivs unders�ge om value passer i 10 bit.
			AddInstruction(inst);
		}

        private VirtualRegister WriteRI16(SpuOpCode opcode, int symbol)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Constant = symbol;
			inst.Destination = NextRegister();
			AddInstruction(inst);
			return inst.Destination;
		}

		private void WriteRI16Sourced(SpuOpCode opcode, VirtualRegister rt, int symbol)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Source1 = rt;
			inst.Constant = symbol & 0x0000ffff;
			AddInstruction(inst);
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

		// custom instructions ===============================================

		public void WriteStop()
		{
			SpuInstruction inst = new SpuInstruction(SpuOpCode.stop);
			inst.Constant = 0;
			AddInstruction(inst);
		}

		/// <summary>
		/// Pseudo instruction.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dest"></param>
		public void WriteMove(VirtualRegister src, VirtualRegister dest)
		{
			SpuInstruction iload = new SpuInstruction(SpuOpCode.ilh);
			iload.Constant = 0;
			iload.Destination = NextRegister();
			AddInstruction(iload);

			SpuInstruction ior = new SpuInstruction(SpuOpCode.or);
			ior.Source1 = iload.Destination;
			ior.Source2 = src;
			ior.Destination = dest;
			AddInstruction(ior);
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
						tw.Write("{0} {1}, {2}, {3}", inst.OpCode.Name, inst.Destination, inst.Source1, inst.Source2);
						break;
					case SpuInstructionFormat.RR2:
						tw.Write("{0} {1}, {2}", inst.OpCode.Name, inst.Destination, inst.Source1);
						break;
					case SpuInstructionFormat.RR1:
						tw.Write("{0} {1}", inst.OpCode.Name, inst.Destination);
						break;
					case SpuInstructionFormat.RR1DE:
						tw.Write("{0} {1}", inst.OpCode.Name, inst.Source1);
						break;
					case SpuInstructionFormat.RR2DE:
						tw.Write("{0} {1}, {2}", inst.OpCode.Name, inst.Destination, inst.Source1);
						break;
					case SpuInstructionFormat.RRR:
						tw.Write("{0} {1}, {2}, {3}, {4}", inst.OpCode.Name, inst.Destination, inst.Source1, inst.Source2, inst.Source3);
						break;
					case SpuInstructionFormat.RI7:
						tw.Write("{0} {1}, {2}, {3}", inst.OpCode.Name, inst.Destination, inst.Source1, inst.Constant);
						break;
					case SpuInstructionFormat.RI8:
						tw.Write("{0} {1}, {2}, {3}", inst.OpCode.Name, inst.Destination, inst.Source1, inst.Constant);
						break;
					case SpuInstructionFormat.RI10:
						if (inst.OpCode.NoRegisterWrite)
							tw.Write("{0} {1}, {3}({2})", inst.OpCode.Name, inst.Source2, inst.Source1, inst.Constant);
						else
							tw.Write("{0} {1}, {3}({2})", inst.OpCode.Name, inst.Destination, inst.Source1, inst.Constant);
						break;
					case SpuInstructionFormat.RI16:
						tw.Write("{0} {1}, {2}", inst.OpCode.Name, inst.Destination, inst.Constant);
						break;
					case SpuInstructionFormat.RI16x:
						tw.Write("{0} {1}", inst.OpCode.Name, inst.Constant);
						break;
					case SpuInstructionFormat.RI18:
						tw.Write("{0} {1}, {2}", inst.OpCode.Name, inst.Destination, inst.Constant);
						break;
					default:
						throw new Exception();
				}
				tw.WriteLine();
			}
		}
	}

}
