using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SpuAbiUtilitiesTest
	{
		private static VirtualRegister GetHardwareRegister(int regnum)
		{
			HardwareRegister reg = new HardwareRegister();
			reg.Register = regnum;
			VirtualRegister vr = new VirtualRegister();
			vr.Location = reg;
			return vr;
		}

		[Test]
		public void TestInitializationCode()
		{
			SpuAbiUtilities abiutil = new SpuAbiUtilities();

			// Initialization.
			List<int> code = new List<int>();
			code.AddRange(abiutil.GetInitializationCode());

			// A single return instruction.
			VirtualRegister LR = GetHardwareRegister(0);
			SpuInstructionWriter writer = new SpuInstructionWriter();
			writer.BeginNewBasicBlock();
			writer.WriteBi(LR);

			code.AddRange(SpuInstruction.emit(writer.GetAsList()));

			// Run
			SpeContext ctx = new SpeContext();
			ctx.LoadProgram(code.ToArray());
			ctx.Run();
		}
	}
}
