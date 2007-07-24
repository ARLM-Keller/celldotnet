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
		private VirtualRegister SP;
		private VirtualRegister LR;

		public SpuAbiUtilities()
		{
			LR = GetHardwareRegister(0);
			SP = GetHardwareRegister(1);
		}

		private static VirtualRegister GetHardwareRegister(int regnum)
		{
			HardwareRegister reg = new HardwareRegister();
			reg.Register = regnum;
			VirtualRegister vr = new VirtualRegister();
			vr.Location = reg;
			return vr;
		}

		public int[] GetInitializationCode()
		{
			SpuInstructionWriter w = new SpuInstructionWriter();

			w.BeginNewBasicBlock();

			// Initialize stack pointer to two qwords below ls top.
			w.WriteLoadI4(SP, 0x4ffff - 0x20);

			// Store zero to Back Chain.
			VirtualRegister zeroreg = GetHardwareRegister(75);
			w.WriteLoadI4(zeroreg, 0);
			w.WriteStqd(zeroreg, SP, 0);
			
			// Branch to method and set LR.
			// The methode is assumed to be immediately after this code.
			w.WriteBrsl(LR, 1);

			w.WriteStop();

			return SpuInstruction.emit(w.GetAsList());
		}
	}
}
