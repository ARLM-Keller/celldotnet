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

#if UNITTEST

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;




namespace CellDotNet.Spe
{
	[TestFixture]
	public class SpeContextTest : UnitTest
	{
		[Test]
		public void SpeSimpleDMATest()
		{
			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctxt = new SpeContext())
			{
				ctxt.LoadProgram(new int[] { 13, 14, 15, 16 });

				int[] lsa = ctxt.GetCopyOfLocalStorage16K();
				AreEqual(13, lsa[0], "DMA error.");
			}
		}

		private delegate void BasicTestDelegate();

		[Test, Ignore("Pointers are no longer supported.")]
		unsafe public void TestFirstCellProgram()
		{
			BasicTestDelegate del = delegate
										{
											int* i;
											i = (int*)0x40;
											*i = 34;
										};


			MethodBase method = del.Method;
			MethodCompiler mc = new MethodCompiler(method);
			mc.PerformProcessing(MethodCompileState.S2TreeConstructionDone);

			mc.PerformProcessing(MethodCompileState.S4InstructionSelectionDone);
			mc.GetBodyWriter().WriteStop();

			mc.PerformProcessing(MethodCompileState.S5RegisterAllocationDone);


			mc.PerformProcessing(MethodCompileState.S7RemoveRedundantMoves);


			int[] bincode = SpuInstruction.Emit(mc.GetBodyWriter().GetAsList());

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(bincode);

				ctx.Run();
				int[] ls = ctx.GetCopyOfLocalStorage16K();

				if (ls[0x40 / 4] != 34)
				{
					Console.WriteLine("Damn");
					Console.WriteLine("Value: {0}", ls[0x40/4]);
				}
			}
		}

		[Test]
		public void TestDma_PutGetInt32()
		{
			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.DmaPutValue((LocalStorageAddress) 32, 33000);
				ctx.DmaPutValue((LocalStorageAddress) 64, 34000);

				int readvalue = ctx.DmaGetValue<int>((LocalStorageAddress) 32);
				AreEqual(33000, readvalue);
			}
		}

		[Test]
		public void TestDma_PutGetFloat()
		{
			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.DmaPutValue((LocalStorageAddress)32, 33000f);
				ctx.DmaPutValue((LocalStorageAddress)64, 34000f);

				float readvalue = ctx.DmaGetValue<float>((LocalStorageAddress)32);
				AreEqual(33000f, readvalue);
			}
		}


		[Test]
		public void TestHasSpe()
		{
			// For this test we assume that a windows machine does not have spe hw,
			// and that anything else has spe hw.
			if (Path.DirectorySeparatorChar == '\\')
				IsFalse(SpeContext.HasSpeHardware);
			else
				IsTrue(SpeContext.HasSpeHardware);
		}

		private delegate int SimpleDelegateIntInt(int i);

#region Runtime checks tests

		[Test, ExpectedException(typeof(SpeOutOfMemoryException))]
		public void TestStopCode_OutOfMemoryException()
		{
			TestError_StopCodeException<SpeOutOfMemoryException>(SpuStopCode.OutOfMemory);
		}

		[Test, ExpectedException(typeof(SpeStackOverflowException))]
		public void TestStopCode_StackOverflowException()
		{
			TestError_StopCodeException<SpeStackOverflowException>(SpuStopCode.StackOverflow);
		}

		[Test, ExpectedException(typeof(PpeCallException))]
		public void TestPpeCallFailureTest()
		{
			BasicTestDelegate del = delegate { SpuRuntime.Stop(SpuStopCode.PpeCallFailureTest); };
			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				throw new PpeCallException();

			SpeContext.UnitTestRunProgram(cc);
		}

		private static void TestError_StopCodeException<T>(SpuStopCode stopcode) where T : Exception, new()
		{
			SpuInstructionWriter writer = new SpuInstructionWriter();
			writer.BeginNewBasicBlock();
			writer.WriteStop(stopcode);
			int[] code = SpuInstruction.Emit(writer.GetAsList());

			if (!SpeContext.HasSpeHardware)
				throw new T();

			using (SpeContext sc = new SpeContext())
			{
				sc.LoadProgram(code);
				sc.Run();
			}
		}

		[Test]
		public void TestTestStopCode_None()
		{
			SpuInstructionWriter writer = new SpuInstructionWriter();
			writer.BeginNewBasicBlock();
			writer.WriteStop();
			int[] code = SpuInstruction.Emit(writer.GetAsList());

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				sc.LoadProgram(code);
				sc.Run();
			}
		}

		private static int Recursion(int level)
		{
			if (level > 0)
				return Recursion(level - 1)+1;
			else
				return 1;
		}

		[Test, ExpectedException(typeof(SpeStackOverflowException))]
		public void TestRecursion_StackOverflow()
		{
			SimpleDelegateIntInt del = Recursion;

			CompileContext cc = new CompileContext(del.Method);

			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				throw new SpeStackOverflowException();

			using (SpeContext sc = new SpeContext())
			{
				sc.RunProgram(cc, 10000); // generates at least 320K stack.
			}
		}

		[Test]
		public void TestRecursion_WithoutStackOverflow()
		{
			SimpleDelegateIntInt del = Recursion;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				sc.RunProgram(cc, 100); // generates at least 6K stack which is ok.
			}
		}

		private static void OutOfMemory()
		{
			// The allocation of the arrays is the important thing here;
			// the other stuff is just to prevent c# from optimizing it all away.
			int[] sizes = new int[3];
			sizes[0] = 32*1024;
			sizes[1] = 16*1024;
			sizes[2] = 32*1024;

			int[] arraya = new int[sizes[0]];
			int[] arrayb = new int[sizes[1]];
			int[] arrayc = new int[sizes[2]];
			sizes[0] = arraya[0] + arrayb[0] + arrayc[0];
		}

		[Test, ExpectedException(typeof(SpeOutOfMemoryException))]
		public void TestOutOfMemory()
		{
			BasicTestDelegate del = OutOfMemory;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				throw new SpeOutOfMemoryException();

			using (SpeContext ctx = new SpeContext())
			{
				ctx.RunProgram(cc);
			}
		}

		private static void NotOutOfMemory()
		{
			int[] arraya = new int[32 * 1024];
			int[] arrayb = new int[16 * 1024];
			Utilities.PretendVariableIsUsed(arraya);
			Utilities.PretendVariableIsUsed(arrayb);
		}

		[Test]
		public void TestNotOutOfMemory()
		{
			BasicTestDelegate del = NotOutOfMemory;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.RunProgram(cc);
			}
		}

		#endregion

		[Test]
		public void TestGetLocalStorageSize()
		{
			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
				AreEqual(256*1024, sc.LocalStorageSize);
		}

		[Test]
		public void TestGetLocalStorageArea()
		{
			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				IntPtr ls = sc.LocalStorageMappedAddress;
				AreNotEqual((IntPtr)0, ls);
			}
		}


		[Test, Explicit]
		public void TestGetSpeControlArea()
		{
			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext spe = new SpeContext())
			{
				SpeControlArea area = spe.GetControlArea();
				AreEqual((uint)0, area.SPU_NPC);
			}
		}

		[Test]
		public void TestAllocation1()
		{
			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(8))
			{
				ArraySegment<int> segment = mem.ArraySegment;
				AreEqual(8, segment.Count);
				if (segment.Array.Length < 8 || segment.Array.Length > 16)
					Fail();
			}
		}

		[Test]
		public void TestAllocation2()
		{
			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(5))
			{
				ArraySegment<int> segment = mem.ArraySegment;
				AreEqual(5, segment.Count);
				if (segment.Array.Length < 5 || segment.Array.Length > 16)
					Fail();
			}
		}

		[Test]
		public void TestAllocation3()
		{
			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(64))
			{
				ArraySegment<int> segment = mem.ArraySegment;
				AreEqual(64, segment.Count);
				if (segment.Array.Length < 64 || segment.Array.Length > 192)
					Fail();
			}			
		}


#region Return value tests

		private delegate int IntReturnDelegate();
		private delegate float SingleReturnDelegate();

		[Test]
		public void TestRunProgram_ReturnInt32_Manual()
		{
			const int magicNumber = 40;
			IntReturnDelegate del = delegate { return magicNumber; };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			int[] code = cc.GetEmittedCode();

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code);

//				Utilities.DumpMemory(ctx, (LocalStorageAddress)0, 0x70, Console.Out);

				ctx.Run();

				int retval = ctx.DmaGetValue<int>(cc.ReturnValueAddress);
				AreEqual(magicNumber, retval);
			}
		}

		[Test]
		public void TestRunProgram_ReturnInt32()
		{
			const int magicNumber = 40;
			IntReturnDelegate del = delegate { return magicNumber; };

			object retval = SpeContext.UnitTestRunProgram(del);
			AreEqual(typeof(int), retval.GetType());
			AreEqual(magicNumber, (int)retval);
		}

		[Test]
		public void TestRunProgram_ReturnSingle()
		{
			const float magicNumber = float.NaN;
			SingleReturnDelegate del = delegate { return magicNumber; };

			if (!SpeContext.HasSpeHardware)
				return;

			object retval = SpeContext.UnitTestRunProgram(del);
			AreEqual(typeof(float), retval.GetType());
			AreEqual(magicNumber, (float)retval);
		}

		[Test]
		public void TestDelegateRun_ReturnInt()
		{
			IntReturnDelegate del = delegate { return 40; };
			IntReturnDelegate del2 = CreateSpeDelegate(del);

			int retval = del2();
			AreEqual(40, retval);
		}

		[Test]
		public void TestDelegateRun_ReturnSingle()
		{
			SingleReturnDelegate del = delegate { return 40; };
			SingleReturnDelegate del2 = CreateSpeDelegate(del);

			float retval = del2();
			AreEqual(40f, retval);
		}

		[Test, Ignore("Make this work.")]
		public void TestDelegateRun_WrappingWorks()
		{
			bool hasRun = false;

			IntReturnDelegate del = delegate { hasRun = true;
			                                 	return 34; };
			IntReturnDelegate del2 = CreateSpeDelegate(del);

			if (SpeContext.HasSpeHardware)
			{
				try
				{
					del2();
					Fail();
				}
				catch (Exception)
				{
					// Nothing.
				}
			}
			else
			{
				del2();
				IsTrue(hasRun);
			}
		}

		#endregion

		[Test]
		public void TestPpeInstanceCallMarshaling()
		{
			byte[] buf = new byte[3*16];

			int callArgumentValue = 0;
			const int magicnumber = 42;
			Action<int> methodToCall = delegate(int obj) { callArgumentValue = obj; };
			Marshaler marshaler = new Marshaler();

			// Method...
			Marshal.StructureToPtr(methodToCall.Method.MethodHandle, Marshal.UnsafeAddrOfPinnedArrayElement(buf, 0), false);
			// Arguments...
			byte[] argimg = marshaler.GetImage(new object[] {methodToCall.Target, magicnumber});
			Utilities.Assert(argimg.Length == 32, "argimg.Length == 32");
			Buffer.BlockCopy(argimg, 0, buf, 16, 32);

			SpeContext.PerformPpeCall(buf, marshaler);

			AreEqual(magicnumber, callArgumentValue);
		}

		[Test]
		public void TestPpeStaticCallMarshaling()
		{
			byte[] buf = new byte[2 * 16];

			const int magicnumber = 42;
			Action<int> methodToCall = delegate(int obj) { throw new DummyException(obj == magicnumber ? "ok" : "bad"); };
			Marshaler marshaler = new Marshaler();

			// Method...
			Marshal.StructureToPtr(methodToCall.Method.MethodHandle, Marshal.UnsafeAddrOfPinnedArrayElement(buf, 0), false);
			// Arguments...
			byte[] argimg = marshaler.GetImage(new object[] { magicnumber });
			Utilities.Assert(argimg.Length == 16, "argimg.Length == 16");
			Buffer.BlockCopy(argimg, 0, buf, 16, 16);

			try
			{
				SpeContext.PerformPpeCall(buf, marshaler);
				Fail("Should have gotten an exception.");
			}
			catch (TargetInvocationException e)
			{
				DummyException realexception = (DummyException) e.InnerException;
				AreEqual("ok", realexception.Message);
			}
		}

		private static T CreateSpeDelegate<T>(T delegateToWrap) where T : class
		{
			return SpeUnitTestDelegateRunner.CreateUnitTestSpeDelegate(delegateToWrap);
//			return SpeDelegateRunner<T>.CreateSpeDelegate(delegateToWrap);
		}


		/// <summary>
		/// This class is meant to be used in unit test: When SPE hardware is available
		/// it will behave as <see cref="SpeDelegateRunner"/>; and when not available,
		/// it will simply execute the original delegate.
		/// <para>
		/// This allows a unit test to execute totally indifferent whether it's running with
		/// or without SPE hardware.
		/// </para>
		/// </summary>
		private class SpeUnitTestDelegateRunner : SpeDelegateRunner
		{
			public static T CreateUnitTestSpeDelegate<T>(T delegateToWrap) where T : class
			{
				Delegate del = delegateToWrap as Delegate;
				SpeUnitTestDelegateRunner runner = new SpeUnitTestDelegateRunner(del);
				return runner.WrapperDelegate as T;
			}

			private SpeUnitTestDelegateRunner(Delegate delegateToWrap) : base(delegateToWrap)
			{
			}

			protected override object SpeDelegateWrapperExecute(object[] args)
			{
				object retval;

				if (SpeContext.HasSpeHardware)
					retval = base.SpeDelegateWrapperExecute(args);
				else
				{
					Delegate del = OriginalDelegate;
					retval = del.DynamicInvoke(args);
				}

				return retval;
			}
		}
	}
}
#endif