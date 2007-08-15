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
			const int magicnum = 0xfefefe;

			// The code to run just returns.
			SpuManualRoutine routine = new SpuManualRoutine();
			routine.Writer.BeginNewBasicBlock();
			routine.Writer.WriteLoadI4(HardwareRegister.GetHardwareRegister(3), magicnum);
			routine.Writer.WriteBi(HardwareRegister.LR);
			routine.Offset = 512;

			Console.WriteLine("init test");

			RegisterSizedObject returnLocation = new RegisterSizedObject();
			returnLocation.Offset = 1024;

			int[] code = new int[1000];
			{
				// Initialization.
				SpuInitializer initializer = new SpuInitializer(routine, returnLocation);
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

				int rc = ctx.Run();
				AreEqual(0, rc);

				int retval = ctx.DmaGetInt32((LocalStorageAddress) returnLocation.Offset);
				AreEqual(magicnum, retval);
			}
		}
	}
}
