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
			SpuRoutine routine = new SpuRoutine();
			routine.Writer.BeginNewBasicBlock();
			routine.Writer.WriteBi(SpuAbiUtilities.LR);


			// Initialization.
			List<int> code = new List<int>();
			code.AddRange(new SpuInitializer(routine).GetCode());

			// A single return instruction.
			SpuInstructionWriter writer = new SpuInstructionWriter();
			writer.BeginNewBasicBlock();
			writer.WriteBi(SpuAbiUtilities.LR);

			code.AddRange(SpuInstruction.emit(writer.GetAsList()));

			// Run
			SpeContext ctx = new SpeContext();
			ctx.LoadProgram(code.ToArray());
			ctx.Run();
		}
	}
}
