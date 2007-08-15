using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SpuInitializerTest
	{
		[Test]
		public void TestInitialization()
		{
			// The code to run just returns.
			SpuManualRoutine routine = new SpuManualRoutine();
			routine.Writer.BeginNewBasicBlock();
			routine.Writer.WriteLoadI4(HardwareRegister.GetHardwareRegister(45), 235);
//			routine.Writer.WriteStop();
			routine.Writer.WriteBi(HardwareRegister.LR);
			routine.Offset = 512;

			Console.WriteLine("init test");

			int[] code = new int[1000];
			{
				// Initialization.
				SpuInitializer initializer = new SpuInitializer(routine, null);
				initializer.Offset = 0;
				initializer.PerformAddressPatching();
//				List<SpuInstruction> list = initializer._writer.GetAsList();
				int[] initCode = initializer.Emit();
				Buffer.BlockCopy(initCode, 0, code, initializer.Offset, initCode.Length * 4);
			}
			{
				routine.PerformAddressPatching();
				List<SpuInstruction> list = routine.Writer.GetAsList();
				int[] routineCode = SpuInstruction.emit(list);
				Buffer.BlockCopy(routineCode, 0, code, routine.Offset, routineCode.Length * 4);
			}

//			code[0] = new SpuInstruction(SpuOpCode.stop).emit();

			if (!SpeContext.HasSpeHardware)
				return;

			// Run
			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code);
				ctx.Run();
			}
		}
	}
}
