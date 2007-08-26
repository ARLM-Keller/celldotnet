using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace CellDotNet
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
				ctxt.LoadProgram(new int[] { 13 });

				int[] lsa = ctxt.GetCopyOffLocalStorage();

				if (lsa[0] != 13)
					Assert.Fail("DMA error.");
			}
		}

		private delegate void BasicTestDelegate();

		[Test]
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

			new TreeDrawer().DrawMethod(mc);


			mc.PerformProcessing(MethodCompileState.S4InstructionSelectionDone);
			mc.GetBodyWriter().WriteStop();

			Console.WriteLine();
			Console.WriteLine("Disassembly: ");
			Console.WriteLine(mc.GetBodyWriter().Disassemble());

			mc.PerformProcessing(MethodCompileState.S5RegisterAllocationDone);

			Console.WriteLine();
			Console.WriteLine("Disassembly after regalloc: ");
			Console.WriteLine(mc.GetBodyWriter().Disassemble());

			mc.PerformProcessing(MethodCompileState.S7RemoveRedundantMoves);

			Console.WriteLine();
			Console.WriteLine("Disassembly after remove of redundant moves: ");
			Console.WriteLine(mc.GetBodyWriter().Disassemble());

			int[] bincode = SpuInstruction.emit(mc.GetBodyWriter().GetAsList());

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(bincode);

				ctx.Run();
				int[] ls = ctx.GetCopyOffLocalStorage();

				if (ls[0x40 / 4] != 34)
				{
					Console.WriteLine("øv");
					Console.WriteLine("Value: {0}", ls[0x40/4]);
				}
			}
		}

		[Test]
		public void TestPutGetInt32()
		{
			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
//				ctx.DmaPutValue((LocalStorageAddress) 32, 33000);
				ctx.DmaPutValue((LocalStorageAddress) 64, 34000);

//				int readvalue = ctx.DmaGetValue<int>((LocalStorageAddress) 32);
//				AreEqual(33000, readvalue);
			}
		}

		[Test]
		public void TestPutGetFloat()
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

		private static void TestError_StopCodeException<T>(SpuStopCode stopcode) where T : Exception, new()
		{
			SpuInstructionWriter writer = new SpuInstructionWriter();
			writer.BeginNewBasicBlock();
			writer.WriteStop(stopcode);
			int[] code = SpuInstruction.emit(writer.GetAsList());

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
			int[] code = SpuInstruction.emit(writer.GetAsList());

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext sc = new SpeContext())
			{
				sc.LoadProgram(code);
				sc.Run();
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
		public void TestRunProgram_ReturnInt32_Manual()
		{
			const int magicNumber = 40;
			IntReturnDelegate del = delegate { return magicNumber; };
			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);
			Disassembler.DisassembleToConsole(cc);

			int[] code = cc.GetEmittedCode();

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code);

				Utilities.DumpMemory(ctx, (LocalStorageAddress)0, 0x70, Console.Out);

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

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				object retval = ctx.RunProgram(del);
				AreEqual(typeof(int), retval.GetType());
				AreEqual(magicNumber, (int)retval);
			}
		}

		[Test]
		public void TestRunProgram_ReturnSingle()
		{
			const float magicNumber = float.NaN;
			SingleReturnDelegate del = delegate { return magicNumber; };

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				object retval = ctx.RunProgram(del);
				AreEqual(typeof(float), retval.GetType());
				AreEqual(magicNumber, (float)retval);
			}
		}

		private delegate int IntReturnDelegate();
		private delegate float SingleReturnDelegate();

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

		private static T CreateSpeDelegate<T>(T delegateToWrap) where T : class
		{
			return SpeUnitTestDelegateRunner<T>.CreateUnitTestSpeDelegate(delegateToWrap);
//			return SpeDelegateRunner<T>.CreateSpeDelegate(delegateToWrap);
		}


		/// <summary>
		/// This class is meant to be used in unit test: When SPE hardware is available
		/// it will behave as <see cref="SpeDelegateRunner{T}"/>; and when not available,
		/// it will simply execute the original delegate.
		/// <para>
		/// This allows a unit test to execute totally indifferent whether it's running with
		/// or without SPE hardware.
		/// </para>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		private class SpeUnitTestDelegateRunner<T> : SpeDelegateRunner<T> where T : class
		{
			public static T CreateUnitTestSpeDelegate(T delegateToWrap)
			{
				SpeUnitTestDelegateRunner<T> runner = new SpeUnitTestDelegateRunner<T>(delegateToWrap);
				return runner.TypedWrapperDelegate;
			}

			private SpeUnitTestDelegateRunner(T delegateToWrap) : base(delegateToWrap)
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
