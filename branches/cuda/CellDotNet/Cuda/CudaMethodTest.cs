using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CellDotNet.Intermediate;
using NUnit.Framework;

namespace CellDotNet.Cuda
{
	[TestFixture]
	public class CudaMethodTest : UnitTest
	{
		[Test]
		public void TestBuildTreeIR_Simple()
		{
			Action action = delegate
			                	{
			                		int i = 234;
			                		i.ToString();
			                	};
			var cm = new CudaMethod(action.Method);
			cm.PerformProcessing(CudaMethod.CompileState.TreeConstructionDone);
		}

		[Test]
		public void TestBuildTreeIR()
		{
			Action action = delegate
			                	{
			                		int sum = 0;
			                		for (int j = 0; j < 10; j++)
			                		{
			                			sum += j;
			                		}
			                		sum.ToString();
			                	};
			var cm = new CudaMethod(action.Method);
			cm.PerformProcessing(CudaMethod.CompileState.TreeConstructionDone);
		}

		[Test]
		public void TestBuildListIR_Simple()
		{
			Func<int, int> del = i => i + 10;

			var cm = new CudaMethod(del.Method);
			cm.PerformProcessing(CudaMethod.CompileState.ListContructionDone);

			AreEqual(1, cm.Blocks.Count);
			AreEqual(4, cm.Blocks[0].Instructions.Count());
			AreEqual(IRCode.Ldarg, cm.Blocks[0].Instructions.ElementAt(0).IRCode);
			AreEqual(IRCode.Ldc_I4, cm.Blocks[0].Instructions.ElementAt(1).IRCode);
			AreEqual(10, cm.Blocks[0].Instructions.ElementAt(1).Operand);
			AreEqual(IRCode.Add, cm.Blocks[0].Instructions.ElementAt(2).IRCode);
			AreEqual(IRCode.Ret, cm.Blocks[0].Instructions.ElementAt(3).IRCode);
		}

		[Test]
		public void TestBuildListIR()
		{
			Action action = delegate
			{
				int sum = 0;
				for (int j = 0; j < 10; j++)
				{
					sum += j;
				}
				sum.ToString();
			};
			var cm = new CudaMethod(action.Method);
			cm.PerformProcessing(CudaMethod.CompileState.ListContructionDone);
		}

		[Test]
		public void TestBuildListIR_Branch()
		{
			Func<int, int> del = delegate(int i)
			                     	{
										if (i > 10)
											return 10;
			                     		return i;
			                     	};

			var cm = new CudaMethod(del.Method);
			cm.PerformProcessing(CudaMethod.CompileState.ListContructionDone);

			AreEqual(3, cm.Blocks.Count);

			var branchinst = cm.Blocks[0].Instructions.Single(inst => inst.Operand is BasicBlock);
			IsTrue(ReferenceEquals(branchinst.Operand, cm.Blocks[1]) || ReferenceEquals(branchinst.Operand, cm.Blocks[2]), "Should have branched to one of the blocks.");
		}

		[Test]
		public void TestBuildListIR_Parameter()
		{
			Action<int> action = i => { i.ToString(); };
			var cm = new CudaMethod(action.Method);
			cm.PerformProcessing(CudaMethod.CompileState.ListContructionDone);

			AreEqual(1, cm.Blocks.Count);
			List<ListInstruction> ilist = cm.Blocks[0].Instructions.ToList();
			AreEqual(4, ilist.Count); // ldarga, call, pop, ret.

			AreEqual(IRCode.Ldarga, ilist[0].IRCode);
			IsTrue(ilist[0].Operand is GlobalVReg, "Argument not vreg, but " + ilist[0].Operand.GetType());
		}

		[Test]
		public void TestConditionalBranchPredication_Int()
		{
			Action<int> action = i =>
			                     	{
										if (i > 5)
											i.ToString();
			                     	};
			var cm = new CudaMethod(action.Method);
			cm.PerformProcessing(CudaMethod.CompileState.ConditionalBranchHandlingDone);

			List<ListInstruction> instlist = cm.Blocks.SelectMany(b => b.Instructions).ToList();
			ListInstruction br = instlist.Single(inst => inst.IRCode == IRCode.Br);
			IsNotNull(br.Predicate);
			IsTrue(typeof (PredicateValue).IsAssignableFrom(br.Predicate.ReflectionType), "The branch needs to be properly predicated.");
			
		}
	}
}
