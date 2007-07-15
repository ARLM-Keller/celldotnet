using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace CellDotNet
{
	partial class SpuInstructionWriter
	{
		private List<SpuBasicBlock> _basicBlocks = new List<SpuBasicBlock>();
		/// <summary>
		/// The instructions that have been written.
		/// </summary>
		public ReadOnlyCollection<SpuBasicBlock> BasicBlocks
		{
			get { return _basicBlocks.AsReadOnly(); }
		}

		/// <summary>
		/// Creates a list from the instructions in the basic blocks. You probably don't want to use
		/// this method for anything but experimenting.
		/// </summary>
		/// <returns></returns>
		public List<SpuInstruction> GetAsList()
		{
			List<SpuInstruction> list = new List<SpuInstruction>();
			foreach (SpuBasicBlock bb in _basicBlocks)
			{
				SpuInstruction inst = bb.Head;

				while (inst != null)
				{
					list.Add(inst);
					inst = inst.Next;
				}
			}

			return list;
		}

		private int _regnum = 1;

		public VirtualRegister NextRegister()
		{
			return new VirtualRegister(_regnum++);
		}

		public void StartNewBasicBlock()
		{
			_basicBlocks.Add(new SpuBasicBlock());
			_prevInstruction = null;
		}

		private SpuInstruction _prevInstruction;
		private void AddInstruction(SpuInstruction inst)
		{
			if (_prevInstruction != null)
			{
				_prevInstruction.Next = inst;
				_prevInstruction = inst;
			}
			else
			{
				// New bb.
				Utilities.Assert(_basicBlocks.Count != 0, "StartNewBasicBlock() has not been called.");
				_basicBlocks[_basicBlocks.Count - 1].Head = inst;
				_prevInstruction = inst;
			}
		}

		private VirtualRegister WriteRR(SpuOpCode opcode, VirtualRegister ra, VirtualRegister rb)
		{
			VirtualRegister rt = NextRegister();
			WriteRR(opcode, ra, rb, rt);
			return rt;
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
		}

		private VirtualRegister WriteRR1(SpuOpCode opcode)
		{
			return WriteRR(opcode, NextRegister(), NextRegister());
		}

		private VirtualRegister WriteRR2DE(SpuOpCode opcode, VirtualRegister ra, bool D, bool E)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Rt = NextRegister();
			inst.Constant = (D) ? 0x20 : 0x00; // 0x20 = 0100000b
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
			inst.Constant = value & 0x0000007F; //NOTE muligivs unders�ge om value passer i 7 bit.
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
			inst.Constant = scale & 0x000003ff; //NOTE muligivs unders�ge om value passer i 10 bit.
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
		}

		private void WriteRI10Sourced(SpuOpCode opcode, VirtualRegister ra, VirtualRegister rt, int scale)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Rb = rt;
			inst.Constant = scale & 0x000003ff; //NOTE muligivs unders�ge om value passer i 10 bit.
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

		/// <summary>
		/// Pseudo instruction.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dest"></param>
		public void WriteMove(VirtualRegister src, VirtualRegister dest)
		{
			SpuInstruction inst = new SpuInstruction(SpuOpCode.move);
			inst.Ra = src;
			inst.Rt = dest;

			AddInstruction(inst);
//
//			SpuInstruction iload = new SpuInstruction(SpuOpCode.il);
//			iload.Constant = 0;
//			iload.Rt = NextRegister();
//			AddInstruction(iload);
//
//			SpuInstruction ior = new SpuInstruction(SpuOpCode.or);
//			ior.Ra = iload.Rt;
//			ior.Rb = src;
//			ior.Rt = dest;
//			AddInstruction(ior);
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
			foreach (SpuBasicBlock bb in _basicBlocks)
			{
				SpuInstruction inst = bb.Head;

				Utilities.AssertNotNull(inst, "inst");

				do
				{
					switch (inst.OpCode.Format)
					{
						case SpuInstructionFormat.None:
							throw new Exception();
						case SpuInstructionFormat.RR:
							tw.Write("{0} {1}, {2}, {3}", inst.OpCode.Name, inst.Rt, inst.Ra, inst.Rb);
							break;
						case SpuInstructionFormat.RR2:
							tw.Write("{0} {1}, {2}", inst.OpCode.Name, inst.Rt, inst.Ra);
							break;
						case SpuInstructionFormat.RR1:
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
						case SpuInstructionFormat.WEIRD:
							if (inst.OpCode == SpuOpCode.stop)
							{
								tw.Write(inst.OpCode.Name);
								break;
							}

							throw new NotImplementedException();
						default:
							throw new Exception();
					}
					tw.WriteLine();

					inst = inst.Next;
				} while (inst != null);
			}
		}
	}
}