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
		/// Writes inner epilog.
		/// </summary>
		/// <param name="epilog"></param>
		public static void WriteEpilog(SpuInstructionWriter epilog)
		{
			// Assume that the code that wants to return has placed the return value in the correct
			// registers (R3+).

			// Restore old SP.
			epilog.WriteLqd(HardwareRegister.SP, HardwareRegister.SP, 0);

			// TODO: Restore caller-saves.

			// Restore old LR from callers frame.
			epilog.WriteLqd(HardwareRegister.LR, HardwareRegister.SP, 1);

			// Return.
			epilog.WriteBi(HardwareRegister.LR);
		}

		public static void WriteProlog(int frameSlots, SpuInstructionWriter prolog)
		{
			// Save LR in caller's frame.
			prolog.WriteStqd(HardwareRegister.LR, HardwareRegister.SP, 1);

			// Establish new SP.
			prolog.WriteAi(HardwareRegister.SP, HardwareRegister.SP, -frameSlots*16);

			// Store SP at new frame's Back Chain.
			prolog.WriteStqd(HardwareRegister.SP, HardwareRegister.SP, 0);
		}
	}
}
