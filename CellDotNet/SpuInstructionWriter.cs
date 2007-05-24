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

		protected void AddInstruction(SpuInstruction inst)
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
			inst.Ra = ra;
			inst.Rb = rb;
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
 */
		}

        private void WriteRR(SpuOpCode opcode, VirtualRegister ra, VirtualRegister rb, VirtualRegister rt)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Rb = rb;
			inst.Rt = rt;
			AddInstruction(inst);
		}

		private VirtualRegister WriteRR2(SpuOpCode opcode, VirtualRegister ra)
		{
		    return WriteRR(opcode, ra, NextRegister());
/*
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
            inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
 */
		}

		private VirtualRegister WriteRR1(SpuOpCode opcode)
		{
		    return WriteRR(opcode, NextRegister(), NextRegister());
/*
			SpuInstruction inst = new SpuInstruction(opcode);
            inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
 */
		}

        // TODO brugen af denne funktion bør astattes af den neden for.
		private void WriteRR1DE(SpuOpCode opcode, VirtualRegister ra)
		{
		    WriteRR2DE(opcode, ra);
/*
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			AddInstruction(inst);
 */
		}

        private void WriteRR1DE(SpuOpCode opcode, VirtualRegister ra, bool D, bool E)
        {
            WriteRR2DE(opcode, ra, D, E);
/*
            SpuInstruction inst = new SpuInstruction(opcode);
            inst.Ra = ra;
            inst.Constant  = (D) ? 0x20 : 0x00; // 0x20 = 0100000b
            inst.Constant |= (E) ? 0x10 : 0x00; // 0x10 = 0010000b
            AddInstruction(inst);
 */
        }

        // TODO brugen af denne funktion bør astattes af den neden for.
        private VirtualRegister WriteRR2DE(SpuOpCode opcode, VirtualRegister ra)
		{
		    return WriteRR2DE(opcode, ra, false, false);
/*
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
 */
		}

        private VirtualRegister WriteRR2DE(SpuOpCode opcode, VirtualRegister ra, bool D, bool E)
        {
            SpuInstruction inst = new SpuInstruction(opcode);
            inst.Ra = ra;
            inst.Rt = NextRegister();
            inst.Constant  = (D) ? 0x20 : 0x00; // 0x20 = 0100000b
            inst.Constant |= (E) ? 0x10 : 0x00; // 0x10 = 0010000b
            AddInstruction(inst);
            return inst.Rt;
        }

		private VirtualRegister WriteRRR(SpuOpCode opcode, VirtualRegister ra, VirtualRegister rb, VirtualRegister rc)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Rb = rb;
			inst.Rc = rc;
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
		}

        private VirtualRegister WriteRI7(SpuOpCode opcode, VirtualRegister ra, int value)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Constant = value & 0x0000007F; //NOTE muligivs undersøge om value passer i 7 bit.
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
		}

		private VirtualRegister WriteRI8(SpuOpCode opcode, VirtualRegister ra, int scale)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Constant = scale & 0x000000ff;
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
		}

        private VirtualRegister WriteRI10(SpuOpCode opcode, VirtualRegister ra, int scale)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
            inst.Constant = scale & 0x000003ff; //NOTE muligivs undersøge om value passer i 10 bit.
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
		}

		private void WriteRI10Sourced(SpuOpCode opcode, VirtualRegister ra, VirtualRegister rt, int scale)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Rb = rt;
            inst.Constant = scale & 0x000003ff; //NOTE muligivs undersøge om value passer i 10 bit.
			AddInstruction(inst);
		}

        private VirtualRegister WriteRI16(SpuOpCode opcode, int symbol)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Constant = symbol;
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
		}

		private void WriteRI16Sourced(SpuOpCode opcode, VirtualRegister rt, int symbol)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = rt;
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
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
		}

		// custom instructions ===============================================

//		public void WriteStop()
//		{
//			SpuInstruction inst = new SpuInstruction(SpuOpCode.stop);
//			inst.Constant = 0;
//			AddInstruction(inst);
//		}

		/// <summary>
		/// Pseudo instruction.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dest"></param>
		public void WriteMove(VirtualRegister src, VirtualRegister dest)
		{
			SpuInstruction iload = new SpuInstruction(SpuOpCode.ilh);
			iload.Constant = 0;
			iload.Rt = NextRegister();
			AddInstruction(iload);

			SpuInstruction ior = new SpuInstruction(SpuOpCode.or);
			ior.Ra = iload.Rt;
			ior.Rb = src;
			ior.Rt = dest;
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
						tw.Write("{0} {1}, {2}, {3}", inst.OpCode.Name, inst.Rt, inst.Ra, inst.Rb);
						break;
					case SpuInstructionFormat.RR2:
					case SpuInstructionFormat.RR2DE:
						tw.Write("{0} {1}, {2}", inst.OpCode.Name, inst.Rt, inst.Ra);
						break;
					case SpuInstructionFormat.RR1:
					case SpuInstructionFormat.RR1DE:
						tw.Write("{0} {1}", inst.OpCode.Name, inst.Ra);
						break;
					case SpuInstructionFormat.RRR:
						tw.Write("{0} {1}, {2}, {3}, {4}", inst.OpCode.Name, inst.Rt, inst.Ra, inst.Rb, inst.Rc);
						break;
					case SpuInstructionFormat.RI7:
					case SpuInstructionFormat.RI8:
						tw.Write("{0} {1}, {2}, {3}", inst.OpCode.Name, inst.Rt, inst.Ra, inst.Constant);
						break;
					case SpuInstructionFormat.RI10:
						tw.Write("{0} {1}, {3}({2})", inst.OpCode.Name, inst.Rt, inst.Ra, inst.Constant);
						break;
					case SpuInstructionFormat.RI16:
						tw.Write("{0} {1}, {2}", inst.OpCode.Name, inst.Rt, inst.Constant);
						break;
					case SpuInstructionFormat.RI16NoRegs:
						tw.Write("{0} {1}", inst.OpCode.Name, inst.Constant);
						break;
					case SpuInstructionFormat.RI18:
						tw.Write("{0} {1}, {2}", inst.OpCode.Name, inst.Rt, inst.Constant);
						break;
					default:
						throw new Exception();
				}
				tw.WriteLine();
			}
		}
	}

}
