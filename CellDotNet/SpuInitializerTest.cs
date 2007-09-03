using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SpuInitializerTest : UnitTest
	{
		[Test]
		public void TestInitialization()
		{
			const int magicnum = 0x3654ff;

			SpecialSpeObjects _specialSpeObjects = new SpecialSpeObjects();
			_specialSpeObjects.SetMemorySettings(256*1024-0x20,8*1024-0x20,128*1024,118*1024);

			// The code to run just returns.
			SpuManualRoutine routine = new SpuManualRoutine(true);
			routine.Writer.BeginNewBasicBlock();
			routine.Writer.WriteLoadI4(HardwareRegister.GetHardwareRegister(3), magicnum);
			routine.Writer.WriteBi(HardwareRegister.LR);
			routine.WriteEpilog();
			routine.Offset = 512;

			RegisterSizedObject returnLocation = new RegisterSizedObject();
			returnLocation.Offset = 1024;

			int[] code = new int[1000];
			{
				// Initialization.
				SpuInitializer initializer = new SpuInitializer(routine, returnLocation, null, 0, _specialSpeObjects.StackPointerObject, _specialSpeObjects.NextAllocationStartObject, _specialSpeObjects.AllocatableByteCountObject);
				initializer.Offset = 0;
				initializer.PerformAddressPatching();
				int[] initCode = initializer.Emit();
				Buffer.BlockCopy(initCode, 0, code, initializer.Offset, initCode.Length * 4);
			}
			{
				routine.PerformAddressPatching();
				List<SpuInstruction> list = routine.Writer.GetAsList();
				int[] routineCode = SpuInstruction.emit(list);
				Buffer.BlockCopy(routineCode, 0, code, routine.Offset, routineCode.Length * 4);
			}

			if (!SpeContext.HasSpeHardware)
				return;

			// Run
			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code);

				ctx.Run();

				int retval1 = ctx.DmaGetValue<int>((LocalStorageAddress) returnLocation.Offset);

				int retval2 = ctx.DmaGetValue<int>((LocalStorageAddress)returnLocation.Offset);
				AreEqual(magicnum, retval1);
				AreEqual(magicnum, retval2);

			}
		}

		private delegate int IntDelegateTripleArg(int a, int b, int c);

		[Test]
		public void TestArguments_LoadArguments()
		{
			IntDelegateTripleArg del = delegate(int a, int b, int c) { return a + b + c; };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);
			int[] code = cc.GetEmittedCode();

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code);
				ctx.LoadArguments(cc, new object[]{1, 2, 3});
				ctx.Run();

				int returnValue = ctx.DmaGetValue<int>(cc.ReturnValueAddress);

				AreEqual(6, returnValue, "Function call returned a wrong value.");
			}
		}

		[Test]
		public void TestArguments_RunProgram()
		{
			IntDelegateTripleArg del = delegate(int a, int b, int c) { return a + b + c; };

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				ctx.RunProgram(cc, new object[] { 1, 2, 3 });
				int returnValue = ctx.DmaGetValue<int>(cc.ReturnValueAddress);
				AreEqual(6, returnValue, "Function call returned a wrong value.");
			}
		}

		private unsafe delegate int* IntPointerDelegate(int* ea);
		static unsafe int* PointerMethod(int* ea)
		{
			return ea + 1;
		}

		[Test]
		public unsafe void TestArguments_RunProgram2()
		{
			IntPointerDelegate del = PointerMethod;

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			if (!SpeContext.HasSpeHardware)
				return;

			using (SpeContext ctx = new SpeContext())
			{
				object rv = ctx.RunProgram(cc, new object[] { new IntPtr(16) });
				AreEqual(typeof(IntPtr), rv.GetType());
				AreEqual((IntPtr)32, (IntPtr)rv);
			}
		}
	}
}


