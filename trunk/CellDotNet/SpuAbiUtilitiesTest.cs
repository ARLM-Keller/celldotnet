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
	}
}
