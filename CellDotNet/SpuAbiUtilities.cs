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

		public static void WriteProlog(int frameSlots, SpuInstructionWriter prolog, ObjectWithAddress stackOverflow)
		{
			// Save LR in caller's frame.
			prolog.WriteStqd(HardwareRegister.LR, HardwareRegister.SP, 1);

			prolog.WriteMove(HardwareRegister.SP, HardwareRegister.GetHardwareRegister((CellRegister)75));

			// Establish new SP.
			prolog.WriteAi(HardwareRegister.SP, HardwareRegister.SP, -frameSlots*16);

			if (stackOverflow != null)
			{
				VirtualRegister isNotOverflow = HardwareRegister.GetHardwareRegister((CellRegister) 76);

				prolog.WriteCgti(isNotOverflow, HardwareRegister.SP, 0);

				prolog.WriteGb(isNotOverflow, isNotOverflow);

				prolog.WriteAndi(isNotOverflow, isNotOverflow, 2);

				prolog.WriteConditionalBranch(SpuOpCode.brz, isNotOverflow, stackOverflow);
			}

			// Store SP at new frame's Back Chain.
			prolog.WriteStqd(HardwareRegister.GetHardwareRegister((CellRegister)75), HardwareRegister.SP, 0);
		}
	}
}
