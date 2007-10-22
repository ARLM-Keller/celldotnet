using System;
using System.Collections.Generic;
using System.IO;

namespace CellDotNet
{
	class Disassembler
	{
		public static void DisassembleToConsole(CompileContext compileContext)
		{
			if (compileContext.State < CompileContextState.S6AddressPatchingDone)
				throw new InvalidOperationException("Address patching has not yet been performed.");

			DisassembleToConsole(compileContext.GetAllObjectsForDisassembly());
		}

		public static void DisassembleToConsole(IEnumerable<ObjectWithAddress> objects)
		{
			StringWriter sw = new StringWriter();

			try
			{
				new Disassembler().Disassemble(objects, sw);
			}
			finally
			{
				Console.Write(sw.GetStringBuilder());
			}
		}

		public void Disassemble(CompileContext compileContext, TextWriter writer)
		{
			if (compileContext.State < CompileContextState.S6AddressPatchingDone)
				throw new InvalidOperationException("Address patching has not yet been performed.");

			IEnumerable<ObjectWithAddress> objects = compileContext.GetAllObjectsForDisassembly();
			Disassemble(objects, writer);
		}

		public static void DisassembleUnconditionalToConsole(SpuDynamicRoutine routine)
		{
			StringWriter sw = new StringWriter();
			DisassembleUnconditional(routine, sw);
			Console.Write(sw.GetStringBuilder());			
		}

		public static void DisassembleUnconditionalToConsole(CompileContext compileContext)
		{
			StringWriter sw = new StringWriter();
			DisassembleUnconditional(compileContext, sw);
			Console.Write(sw.GetStringBuilder());
		}

		public static void DisassembleUnconditional(CompileContext compileContext, TextWriter writer)
		{
			IEnumerable<ObjectWithAddress> objects = compileContext.GetAllObjectsForDisassembly();
			DisassembleUnconditional(objects, writer);
		}

		public static void DisassembleUnconditional(SpuDynamicRoutine routine, TextWriter writer)
		{
			DisassembleUnconditional(new SpuDynamicRoutine[] { routine }, writer);
		}

		/// <summary>
		/// This one doesn't puke if address patching has not been performed.
		/// </summary>
		/// <param name="objects"></param>
		/// <param name="writer"></param>
		public static void DisassembleUnconditional(IEnumerable<ObjectWithAddress> objects, TextWriter writer)
		{
			foreach (ObjectWithAddress o in objects)
			{
				writer.WriteLine();

				SpuDynamicRoutine r = o as SpuDynamicRoutine;
				if (r == null)
					continue;

				writer.WriteLine();
				writer.WriteLine("# Name: {0}\r\n# Type: {1}.", 
					!string.IsNullOrEmpty(r.Name) ? r.Name : "(none)", r.GetType().Name);

				DisassembleInstructions(r.GetInstructions(), 0, writer);
			}
			writer.WriteLine();
		}

		public void Disassemble(IEnumerable<ObjectWithAddress> objects, TextWriter writer)
		{
			List<ObjectWithAddress> olist = new List<ObjectWithAddress>(objects);
			List<string> layoutErrorMsg = new List<string>();

			// Lay out ordered by offset.
			olist.Sort(delegate(ObjectWithAddress x, ObjectWithAddress y)
				{ return x.Offset - y.Offset; });

			Dictionary<int, ObjectWithAddress> addressConflictDetector = new Dictionary<int, ObjectWithAddress>();

			// Detected address conflicts and identify data objects.
			List<ObjectWithAddress> nonRoutines = new List<ObjectWithAddress>();
			foreach (ObjectWithAddress o in olist)
			{
				if (!(o is SpuDynamicRoutine))
					nonRoutines.Add(o);

				if (o.Size == 0)
					continue;

				try
				{
					addressConflictDetector.Add(o.Offset, o);
				}
				catch (ArgumentException)
				{
					layoutErrorMsg.Add(string.Format("Multiple objects are assigned to the same address. Address: {0:x6}, object 1: {1}, object 2: {2}.", 
					                               o.Offset, o.Name, addressConflictDetector[o.Offset].Name));
				}
			}

			// Write address and size of non-routines.
			writer.WriteLine("# *****************************");
			writer.WriteLine("# Data:");
			bool isFirst = true;
			foreach (ObjectWithAddress o in nonRoutines)
			{
				if (!isFirst)
					writer.WriteLine();

				writer.WriteLine("# Name: {3}\r\n# Offset: {0:x6}, size: {1:x6}, type: {2}.",
					o.Offset, o.Size, o.GetType().Name, !string.IsNullOrEmpty(o.Name) ? o.Name : "(none)");

				isFirst = false;
			}

			writer.WriteLine();
			writer.WriteLine();
			writer.WriteLine();

			// Disassemble routines.
			writer.WriteLine("# *****************************");
			writer.WriteLine("# Code:");
			foreach (ObjectWithAddress o in olist)
			{
				SpuDynamicRoutine r = o as SpuDynamicRoutine;
				if (r == null)
					continue;

				writer.WriteLine();
				writer.WriteLine("# Name: {3}\r\n# Offset: {0:x6}, size: {1:x6}, type: {2}.",
					r.Offset, r.Size, r.GetType().Name, !string.IsNullOrEmpty(o.Name) ? o.Name : "(none)");
				int newoffset = DisassembleInstructions(r.GetFinalInstructions(), r.Offset, writer);

				if (newoffset != r.Offset + r.Size)
					throw new BadCodeLayoutException(string.Format(
						"Offset after disassembly does not match with the object size. " + 
						"Expected new offset: {0:x6}; actual new offset: {1:x6}", 
						r.Offset + r.Size, newoffset));

			}

			if (layoutErrorMsg.Count != 0)
				throw new BadCodeLayoutException("One or more layout errors were detected:\r\n" + string.Join("\r\n", layoutErrorMsg.ToArray()));
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
						throw new BadSpuInstructionException();
					case SpuInstructionFormat.RR:
						tw.Write("{0} {1}, {2}, {3}", inst.OpCode.Name, inst.Rt, inst.Ra, inst.Rb);
						break;
					case SpuInstructionFormat.RR2:
						tw.Write("{0} {1}, {2}", inst.OpCode.Name, inst.Rt, inst.Ra);
						break;
					case SpuInstructionFormat.RR1:
						tw.Write("{0} {1}", inst.OpCode.Name, inst.Ra);
						break;
					case SpuInstructionFormat.Rrr:
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
						tw.Write("{0} {1}, 0x{2:x}", inst.OpCode.Name, inst.Rt, inst.Constant);
						break;
					case SpuInstructionFormat.RI14:
					case SpuInstructionFormat.RI16NoRegs:
						tw.Write("{0} 0x{1:x}", inst.OpCode.Name, inst.Constant);
						break;
					case SpuInstructionFormat.RI18:
						tw.Write("{0} {1}, {2}", inst.OpCode.Name, inst.Rt, inst.Constant);
						break;
					case SpuInstructionFormat.Weird:
						if (inst.OpCode == SpuOpCode.stop)
						{
							tw.Write("{0} 0x{1:x} ", inst.OpCode.Name, inst.Constant);
							break;
						}
						else if (inst.OpCode == SpuOpCode.nop)
						{
							tw.Write("nop");
							break;
						}

						throw new NotImplementedException();
					case SpuInstructionFormat.Custom:
						// Currently this only need to handle move.
						if (inst.OpCode == SpuOpCode.move)
						{
							tw.Write("{0} {1}, {2}   # no. {3}", inst.OpCode.Name, inst.Rt, inst.Ra, inst.SpuInstructionNumber);
						}
						else
						{
							tw.Write("{0} {1}, {2}", inst.OpCode.Name, inst.Rt, inst.Ra);
						}
						break;
					case SpuInstructionFormat.Channel:
						string chan;
						if (Enum.IsDefined(typeof (SpuWriteChannel), inst.Constant))
							chan = ((SpuWriteChannel) inst.Constant).ToString();
						else if (Enum.IsDefined(typeof (SpuReadChannel), inst.Constant))
							chan = ((SpuReadChannel) inst.Constant).ToString();
						else
							chan = "??";
						if (inst.OpCode == SpuOpCode.rchcnt)
						{
							tw.Write("{0} {1}", inst.OpCode.Name, chan);
						}
						else
							tw.Write("{0} ${1}, {2}", inst.OpCode.Name, chan, inst.Rt);

						break;
					default:
						throw new BadSpuInstructionException();
				}
				tw.WriteLine();

				offset += 4;
			}

			return offset;
		}
	}
}
