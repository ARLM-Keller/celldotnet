// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// Used to get ABI-related SPU code such as initialization code and code to 
	/// handle caller-saves registers.
	/// </summary>
	static class SpuAbiUtilities
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
