using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SpuInstructionTest : UnitTest
	{
		public void TestRI16()
		{
			SpuInstruction inst = new SpuInstruction(SpuOpCode.brsl);
			inst.Constant = 50;
			inst.Rt = HardwareRegister.GetHardwareRegister(3);
			AreEqual("001100110" + "0000000000110010" + "0000011", Convert.ToString(inst.Emit(), 2).PadLeft(32, '0'));
		}
	}
}
