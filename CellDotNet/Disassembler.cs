using System;
using System.Collections.Generic;
using System.IO;

namespace CellDotNet
{
	class Disassembler
	{
		public static void DisassembleToConsole(CompileContext compileContext)
		{
			StringWriter sw = new StringWriter();

			try
			{
				new Disassembler().Disassemble(compileContext, sw);
			}
			finally
			{
				Console.Write(sw.GetStringBuilder());
			}
		}

		public void Disassemble(CompileContext compileContext, TextWriter writer)
		{
			IEnumerable<ObjectWithAddress> objects = compileContext.GetAllObjectsForDisassembly();
			Disassemble(objects, writer);
		}

		public void Disassemble(IEnumerable<ObjectWithAddress> objects, TextWriter writer)
		{
			List<ObjectWithAddress> olist = new List<ObjectWithAddress>(objects);

			// Lay out ordered by offset.
			olist.Sort(delegate(ObjectWithAddress x, ObjectWithAddress y)
				{ return x.Offset - y.Offset; });

			Dictionary<int, ObjectWithAddress> dict = new Dictionary<int, ObjectWithAddress>();

			// Build address map.
			List<ObjectWithAddress> nonRoutines = new List<ObjectWithAddress>();
			foreach (ObjectWithAddress o in olist)
			{
				dict.Add(o.Offset, o);
				if (!(o is SpuRoutine))
					nonRoutines.Add(o);
			}

			// Write address and size of non-routines.
			writer.WriteLine("# Data:");
			foreach (ObjectWithAddress o in nonRoutines)
			{
				writer.WriteLine("# object offset: {0:x6}, size: {1:x6}.", o.Offset, o.Size);
			}

			writer.WriteLine();
			writer.WriteLine();
			writer.WriteLine();

			// Disassemble routines.
			writer.WriteLine("# *****************************");
			writer.WriteLine("# Routines:");
			foreach (ObjectWithAddress o in olist)
			{
				SpuRoutine r = o as SpuRoutine;
				if (r == null)
					continue;

				writer.WriteLine();
				writer.WriteLine("# Routine offset: {0:x6}, size: {1:x6}.", r.Offset, r.Size);
				int newoffset = DisassembleInstructions(r.GetInstructions(), r.Offset, writer);

				if (newoffset != r.Offset + r.Size)
					throw new Exception(string.Format(
						"Offset after disassembly does not match with the object size. " + 
						"Expected new offset: {0:x6}; actual new offset: {1:x6}", 
						r.Offset + r.Size, newoffset));

			}
		}

		internal static int DisassembleInstructions(IEnumerable<SpuInstruction> instructions, int startOffset, TextWriter tw)
		{
			int offset = startOffset;

			foreach (SpuInstruction inst in instructions)
			{
				tw.Write("{0:x4}: ", offset);
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
					case SpuInstructionFormat.Custom:
						// Currently this only need to handle move.
						if (inst.OpCode == SpuOpCode.move)
						{
							tw.Write("{0} {1}, {2}", inst.OpCode.Name, inst.Rt, inst.Ra);
						}
						else
						{
							tw.WriteLine("{0} {1}, {2}", inst.OpCode.Name, inst.Rt, inst.Ra);
						}
						break;
					default:
						throw new Exception();
				}
				tw.WriteLine();

				offset += 4;
			}

			return offset;
		}
	}
}
