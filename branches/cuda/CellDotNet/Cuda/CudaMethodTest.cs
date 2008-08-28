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
			cm.PerformProcessing(CudaMethodCompileState.TreeConstructionDone);
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
			cm.PerformProcessing(CudaMethodCompileState.TreeConstructionDone);
		}

		[Test(Description = "This fails when with c# optimizations disabled, because then more blocks are generated.")]
		public void TestBuildListIR_Simple()
		{
			Func<int, int> del = i => i + 10;

			var cm = new CudaMethod(del.Method);
			cm.PerformProcessing(CudaMethodCompileState.ListContructionDone);

			AreEqual(1, cm.Blocks.Count);
			bool hasLdc;
			switch (cm.Blocks[0].Instructions.Count())
			{
				case 3:
					IsNull(cm.Blocks[0].Instructions.FirstOrDefault(inst => inst.IRCode == IRCode.Ldc_I4), 
						"ldc.i4 elimination seemingly performed, but found ldc.i4 instruction.");
					hasLdc = false;
					break;
				case 4:
					IsNotNull(cm.Blocks[0].Instructions.FirstOrDefault(inst => inst.IRCode == IRCode.Ldc_I4), 
						"ldc.i4 elimination seemingly NOT performed, but did not find ldc.i4 instruction.");
					hasLdc = true;
					break;
				default:
					Fail("Bad IR.");
					return;
			}
			AreEqual(IRCode.Ldarg, cm.Blocks[0].Instructions.ElementAt(0).IRCode);
			int checkOffset = hasLdc ? 1 : 0;
			if (hasLdc)
			{
				AreEqual(IRCode.Ldc_I4, cm.Blocks[0].Instructions.ElementAt(1).IRCode);
				AreEqual(10, cm.Blocks[0].Instructions.ElementAt(1).Operand);
			}
			AreEqual(IRCode.Add, cm.Blocks[0].Instructions.ElementAt(checkOffset + 1).IRCode);
			AreEqual(IRCode.Ret, cm.Blocks[0].Instructions.ElementAt(checkOffset + 2).IRCode);
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
			cm.PerformProcessing(CudaMethodCompileState.ListContructionDone);
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
			cm.PerformProcessing(CudaMethodCompileState.ListContructionDone);

//			AreEqual(3, cm.Blocks.Count);
			IsTrue(cm.Blocks.Count == 3 || cm.Blocks.Count == 4, "block count is " + cm.Blocks.Count);


			var branchinst = cm.Blocks[0].Instructions.Single(inst => inst.Operand is BasicBlock);
			IsTrue(ReferenceEquals(branchinst.Operand, cm.Blocks[1]) || ReferenceEquals(branchinst.Operand, cm.Blocks[2]), "Should have branched to one of the blocks.");
		}

		[Test]
		public void TestBuildListIR_Parameter()
		{
			Action<int> action = i => { i.ToString(); };
			var cm = new CudaMethod(action.Method);
			cm.PerformProcessing(CudaMethodCompileState.ListContructionDone);

			AreEqual(1, cm.Blocks.Count);
			List<ListInstruction> ilist = cm.Blocks[0].Instructions.Where(inst => inst.IRCode != IRCode.Nop).ToList();
//			IsTrue(ilist.Count == 4 || ilist.Count == 5, "Bad count:" + ilist.Count); // ldarga, call, pop, ret.
			AreEqual(4, ilist.Count);

			AreEqual(IRCode.Ldarga, ilist[0].IRCode);
			IsTrue(ilist[0].Operand is GlobalVReg, "Argument not vreg, but " + ilist[0].Operand.GetType());
		}
	}
}
