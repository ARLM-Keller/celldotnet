using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// Used to get ABI-related SPU code such as initialization code and code to 
	/// handle caller-saves registers.
	/// </summary>
	class SpuAbiUtilities
	{
		/// <summary>
		/// The Link Register.
		/// </summary>
		public static VirtualRegister LR = GetHardwareRegister(0);
		/// <summary>
		/// The Stack Pointer register.
		/// </summary>
		public static VirtualRegister SP = GetHardwareRegister(1);

		public static VirtualRegister GetHardwareRegister(int regnum)
		{
			HardwareRegister reg = new HardwareRegister();
			reg.Register = regnum;
			VirtualRegister vr = new VirtualRegister();
			vr.Location = reg;
			return vr;
		}
	}
}
