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

		public void TestRI10()
		{
			SpuInstruction inst = new SpuInstruction(SpuOpCode.stqd);
			inst.Constant = 0x2AA;
			inst.Rt = HardwareRegister.GetHardwareRegister(80);
			inst.Ra = HardwareRegister.GetHardwareRegister(81);
			AreEqual("00100100" + "1010101010" + "1010001" + "1010000", Convert.ToString(inst.Emit(), 2).PadLeft(32, '0'));
		}

		public void TestRI10_2()
		{
			SpuInstruction inst = new SpuInstruction(SpuOpCode.stqd);
			inst.Constant = 2;
			inst.Rt = HardwareRegister.GetHardwareRegister(80);
			inst.Ra = HardwareRegister.SP;
			int bin = inst.Emit();
			Console.WriteLine(bin.ToString("x8"));
			AreEqual("00100100" + "0000000010" + "0000001" + "1010000", Convert.ToString(bin, 2).PadLeft(32, '0'));
		}
	}
}
