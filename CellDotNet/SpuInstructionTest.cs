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
			AreEqual(Convert.ToInt32("001100110" + "0000000000110010" + "0000011", 2), inst.emit());
		}
	}
}
