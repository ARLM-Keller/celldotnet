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
using System.Reflection;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class PartialEvaluatorTest : UnitTest
	{
		public static bool BoolProperty
		{
			get
			{
				SpuRuntime.Stop();
				return false;
			}
		}

		const int MagicNumber = 50;

		[Test]
		public void TestBranch_True()
		{
			Converter<int, int> del = 
				delegate
					{
						if (BoolProperty)
							return MagicNumber;
						else
						{
							SpuRuntime.Stop();
							return -1;
						}
					};

			// Fix BoolProperty to true.
			Dictionary<MethodInfo, int> fixedMethods = new Dictionary<MethodInfo, int>();
			PropertyInfo pi = GetType().GetProperty("BoolProperty");
			fixedMethods.Add(pi.GetGetMethod(), 1);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S2TreeConstructionDone);
			MethodCompiler mc = cc.EntryPointAsMetodCompiler;
			IRTreeBuilder.RemoveNops(mc.Blocks);

			new PartialEvaluator(fixedMethods).Evaluate(mc);

			// This should have eliminated all method calls.
			mc.ForeachTreeInstruction(
				delegate(TreeInstruction obj)
					{
						if (obj.Opcode == IROpCodes.Call)
							Fail("Method calls were not removed.");
					});
			int retCount = 0;
			mc.ForeachTreeInstruction(
				delegate(TreeInstruction obj)
				{
					if (obj.Opcode == IROpCodes.Ret)
					{
						retCount++;
						if (retCount > 1)
							Fail("> 1 return instructions.");
					}
				});

//			new TreeDrawer().DrawMethod(mc);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc, 0);
				AreEqual(MagicNumber, (int) rv);
			}
		}


		[Test]
		public void TestBranch_False()
		{
			Converter<int, int> del =
				delegate
				{
					if (BoolProperty)
					{
						SpuRuntime.Stop();
						return -1;
					}
					else
						return MagicNumber;
				};

			// Fix BoolProperty to true.
			Dictionary<MethodInfo, int> fixedMethods = new Dictionary<MethodInfo, int>();
			PropertyInfo pi = GetType().GetProperty("BoolProperty");
			fixedMethods.Add(pi.GetGetMethod(), 0);

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S2TreeConstructionDone);
			MethodCompiler mc = cc.EntryPointAsMetodCompiler;
			IRTreeBuilder.RemoveNops(mc.Blocks);

			new PartialEvaluator(fixedMethods).Evaluate(mc);

			// This should have eliminated all method calls.
			mc.ForeachTreeInstruction(
				delegate(TreeInstruction obj)
				{
					if (obj.Opcode == IROpCodes.Call)
						Fail("Method calls were not removed.");
				});
			int retCount = 0;
			mc.ForeachTreeInstruction(
				delegate(TreeInstruction obj)
				{
					if (obj.Opcode == IROpCodes.Ret)
					{
						retCount++;
						if (retCount > 1)
							Fail("> 1 return instructions.");
					}
				});

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				object rv = sc.RunProgram(cc, 0);
				AreEqual(MagicNumber, (int)rv);
			}
		}

		static public int IntProperty
		{
			get { throw new InvalidOperationException(); }
		}

		static void DummyIntMethod(int i)
		{
			Utilities.PretendVariableIsUsed(i);
			// Nothing.
		}

		[Test]
		public void TestMethodArgument()
		{
			Action<int> del =
				delegate
				{
					DummyIntMethod(IntProperty);
				};

			Dictionary<MethodInfo, int> fixedMethods = new Dictionary<MethodInfo, int>();
			PropertyInfo pi = GetType().GetProperty("IntProperty");
			fixedMethods.Add(pi.GetGetMethod(), MagicNumber);

			MethodCompiler mc = new MethodCompiler(del.Method);
			IRTreeBuilder.RemoveNops(mc.Blocks);
			new PartialEvaluator(fixedMethods).Evaluate(mc);

			AreEqual(1, mc.Blocks.Count);
			AreEqual(2, mc.Blocks[0].Roots.Count);

			TreeInstruction callinst = mc.Blocks[0].Roots[0];
			AreEqual(IROpCodes.Call, callinst.Opcode);

			TreeInstruction arginst = new List<TreeInstruction>(callinst.GetChildInstructions())[0];
			AreEqual(IROpCodes.Ldc_I4, arginst.Opcode);
			AreEqual(MagicNumber, arginst.OperandAsInt32);
		}
	}
}
