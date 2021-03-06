// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CellDotNet.Spe
{
	public class Disassembler
	{
		public static void DisassembleToConsole(CompileContext compileContext)
		{
			if (compileContext.State < CompileContextState.S6AddressPatchingDone)
				throw new InvalidOperationException("Address patching has not yet been performed.");

			DisassembleToConsole(compileContext.GetAllObjectsForDisassembly());
		}

		public static void DisassembleToFile(string filename, CompileContext compileContext)
		{
			if (compileContext.State < CompileContextState.S6AddressPatchingDone)
				throw new InvalidOperationException("Address patching has not yet been performed.");

			DisassembleToFile(filename, compileContext.GetAllObjectsForDisassembly());
		}

		static void DisassembleToConsole(IEnumerable<ObjectWithAddress> objects)
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

		internal static void DisassembleToFile(string filename, IEnumerable<ObjectWithAddress> objects)
		{
			using (StreamWriter writer = new StreamWriter(filename, false, Encoding.ASCII))
			{
				new Disassembler().Disassemble(objects, writer);
			}
		}

		public void Disassemble(CompileContext compileContext, TextWriter writer)
		{
			if (compileContext.State < CompileContextState.S6AddressPatchingDone)
				throw new InvalidOperationException("Address patching has not yet been performed.");

			IEnumerable<ObjectWithAddress> objects = compileContext.GetAllObjectsForDisassembly();
			Disassemble(objects, writer);
		}

		static void DisassembleUnconditionalToConsole(SpuRoutine routine)
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

		static void DisassembleUnconditional(SpuRoutine routine, TextWriter writer)
		{
			DisassembleUnconditional(new SpuRoutine[] { routine }, writer);
		}

		/// <summary>
		/// This one doesn't puke if address patching has not been performed.
		/// </summary>
		/// <param name="objects"></param>
		/// <param name="writer"></param>
		static void DisassembleUnconditional(IEnumerable<ObjectWithAddress> objects, TextWriter writer)
		{
			foreach (ObjectWithAddress o in objects)
			{
				writer.WriteLine();

				SpuRoutine r = o as SpuRoutine;
				if (r == null)
					continue;

				writer.WriteLine();
				writer.WriteLine("# Name: {0}\r\n# Type: {1}.", 
					!string.IsNullOrEmpty(r.Name) ? r.Name : "(none)", r.GetType().Name);

				DisassembleInstructions(r.GetInstructions(), 0, writer);
			}
			writer.WriteLine();
		}

		void Disassemble(IEnumerable<ObjectWithAddress> objects, TextWriter writer)
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
				if (!(o is SpuRoutine))
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
				SpuRoutine r = o as SpuRoutine;
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

		static string GetRegisterName(VirtualRegister reg)
		{
			if (reg == null)
				return "$";
			if (reg.IsRegisterSet)
				return "$" + (int) reg.Register;
			return "$$" + reg.Number;
		}

		internal static int DisassembleInstructions(IEnumerable<SpuInstruction> instructions, int startOffset, TextWriter tw)
		{
			int offset = startOffset;

			foreach (SpuInstruction inst in instructions)
			{
//				tw.Write("{0:x4}: ", offset);
				string rt = GetRegisterName(inst.Rt);
				string ra = GetRegisterName(inst.Ra);
				string rb = GetRegisterName(inst.Rb);
				string rc = GetRegisterName(inst.Rc);
				switch (inst.OpCode.Format)
				{
					case SpuInstructionFormat.None:
						throw new BadSpuInstructionException();
					case SpuInstructionFormat.RR:
						tw.Write("{0} {1}, {2}, {3}", inst.OpCode.Name, rt, ra, rb);
						break;
					case SpuInstructionFormat.RR2:
						tw.Write("{0} {1}, {2}", inst.OpCode.Name, rt, ra);
						break;
					case SpuInstructionFormat.RR1:
						tw.Write("{0} {1}", inst.OpCode.Name, ra);
						break;
					case SpuInstructionFormat.Rrr:
						tw.Write("{0} {1}, {2}, {3}, {4}", inst.OpCode.Name, rt, ra, rb, rc);
						break;
					case SpuInstructionFormat.RI7:
					case SpuInstructionFormat.RI8:
						tw.Write("{0} {1}, {2}, {3}", inst.OpCode.Name, rt, ra, inst.Constant);
						break;
					case SpuInstructionFormat.RI10:
						{
							switch(inst.OpCode.Name)
							{
								case ("stqd"):
								case ("lqd"):
								case ("cwd"):
								case ("chd"):
								case ("cdd"):
								case ("cbd"):
									tw.Write("{0} {1}, {3}({2})", inst.OpCode.Name, rt, ra, inst.Constant);
									break;
								default:
									tw.Write("{0} {1}, {2}, 0x{3:x}", inst.OpCode.Name, rt, ra, inst.Constant);
									break;
							}
							break;
						}
					case SpuInstructionFormat.RI16:
						tw.Write("{0} {1}, 0x{2:x}", inst.OpCode.Name, rt, inst.Constant);
						break;
					case SpuInstructionFormat.RI14:
					case SpuInstructionFormat.RI16NoRegs:
						tw.Write("{0} 0x{1:x}", inst.OpCode.Name, inst.Constant);
						break;
					case SpuInstructionFormat.RI18:
						tw.Write("{0} {1}, {2}", inst.OpCode.Name, rt, inst.Constant);
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
							tw.Write("{0} {1}, {2}", inst.OpCode.Name, rt, ra, inst.SpuInstructionNumber);
						}
						else
						{
							tw.Write("{0} {1}, {2}", inst.OpCode.Name, rt, ra);
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
							tw.Write("{0} ${1}, {2}", inst.OpCode.Name, chan, rt);

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
