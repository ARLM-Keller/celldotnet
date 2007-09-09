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
			get { throw new InvalidOperationException(); }
		}

		const int MagicNumber = 50;

		private static int BranchMethod(int arg)
		{
			if (BoolProperty)
				return MagicNumber;
			else
				return -MagicNumber;
		}

		[Test]
		public void TestBranch_True()
		{
			Converter<int, int> del = BranchMethod;

			// Fix BoolProperty to true.
			Dictionary<MethodInfo, int> fixedMethods = new Dictionary<MethodInfo, int>();
			PropertyInfo pi = GetType().GetProperty("BoolProperty");
			fixedMethods.Add(pi.GetGetMethod(), 1);

			MethodCompiler mc = new MethodCompiler(del.Method);
			IRTreeBuilder.RemoveNops(mc.Blocks);

			Console.WriteLine();
			Console.WriteLine("Initial:");
			new TreeDrawer().DrawMethod(mc);

			new PartialEvaluator().Evaluate(mc, fixedMethods);

			Console.WriteLine();
			Console.WriteLine("After eval:");
			new TreeDrawer().DrawMethod(mc);

			AreEqual(1, mc.Blocks.Count);
			AreEqual(1, mc.Blocks[0].Roots.Count);
			AreEqual(IROpCodes.Ret, mc.Blocks[0].Roots[0].Opcode);
			AreEqual(IROpCodes.Ldc_I4, mc.Blocks[0].Roots[0].Left.Opcode);
			AreEqual(MagicNumber, mc.Blocks[0].Roots[0].Left.OperandAsInt32);
		}


		[Test]
		public void TestBranch_False()
		{
			Converter<int, int> del = BranchMethod;

			// Fix BoolProperty to true.
			Dictionary<MethodInfo, int> fixedMethods = new Dictionary<MethodInfo, int>();
			PropertyInfo pi = GetType().GetProperty("BoolProperty");
			fixedMethods.Add(pi.GetGetMethod(), 0);

			MethodCompiler mc = new MethodCompiler(del.Method);
			IRTreeBuilder.RemoveNops(mc.Blocks);

			new PartialEvaluator().Evaluate(mc, fixedMethods);

			AreEqual(1, mc.Blocks.Count);
			AreEqual(1, mc.Blocks[0].Roots.Count);
			AreEqual(IROpCodes.Ret, mc.Blocks[0].Roots[0].Opcode);
			AreEqual(IROpCodes.Ldc_I4, mc.Blocks[0].Roots[0].Left.Opcode);
			AreEqual(-MagicNumber, mc.Blocks[0].Roots[0].Left.OperandAsInt32);
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
			new TreeDrawer().DrawMethod(mc);
			new PartialEvaluator().Evaluate(mc, fixedMethods);

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
