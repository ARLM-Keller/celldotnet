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
		public void TestBuildTreeIRSimple()
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
		public void TestBuildListIRSimple()
		{
			Func<int, int> del = i => i + 10;

			var cm = new CudaMethod(del.Method);
			cm.PerformProcessing(CudaMethod.CompileState.ListContructionDone);

			AreEqual(1, cm.Blocks.Count);
			AreEqual(4, cm.Blocks[0].Instructions.Count());
			AreEqual(IROpCodes.Ldarg, cm.Blocks[0].Instructions.ElementAt(0).OpCode);
			AreEqual(IROpCodes.Ldc_I4, cm.Blocks[0].Instructions.ElementAt(1).OpCode);
			AreEqual(10, cm.Blocks[0].Instructions.ElementAt(1).Operand);
			AreEqual(IROpCodes.Add, cm.Blocks[0].Instructions.ElementAt(2).OpCode);
			AreEqual(IROpCodes.Ret, cm.Blocks[0].Instructions.ElementAt(3).OpCode);
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
	}
}
