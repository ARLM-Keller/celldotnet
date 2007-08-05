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
			routine.Writer.WriteBi(HardwareRegister.LR);

			// Initialization.
			List<int> code = new List<int>();
			code.AddRange(new SpuInitializer(routine, null).Emit());

			// A single return instruction.
			SpuInstructionWriter writer = new SpuInstructionWriter();
			writer.BeginNewBasicBlock();
			writer.WriteBi(HardwareRegister.LR);

			code.AddRange(SpuInstruction.emit(writer.GetAsList()));

			// Run
			using (SpeContext ctx = new SpeContext())
			{
				ctx.LoadProgram(code.ToArray());
				ctx.Run();
			}
		}
	}
}
