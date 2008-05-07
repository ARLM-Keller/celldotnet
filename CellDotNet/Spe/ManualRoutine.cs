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
using System.ComponentModel;

namespace CellDotNet.Spe
{
	/// <summary>
	/// Represents code which is not the result of a method compilation, but
	/// rather generated "manually".
	/// </summary>
	class ManualRoutine : SpuRoutine
	{
		private bool _omitEpilog = false;
		/// <summary>
		/// Is set to true when the routine should no longer be written to.
		/// </summary>
		private bool _isPatchingDone;

		public ManualRoutine(bool omitEpilog) : this(omitEpilog, null)
		{
		}

		public ManualRoutine(bool omitEpilog, string name) : base(name)
		{
			_writer = new SpuInstructionWriter();
			_omitEpilog = omitEpilog;
		}

		private SpuInstructionWriter _writer;
		public SpuInstructionWriter Writer
		{
			get
			{
//				if (_isPatchingDone)
//					throw new InvalidOperationException("This routine has been patched.");
				return _writer;
		}
		}

		public override int Size
		{
			get { return Writer.GetInstructionCount() * 4; }
		}

		public override int[] Emit()
		{
			int[] bodybin = SpuInstruction.Emit(Writer.GetAsList());
			return bodybin;
		}

		public override IEnumerable<SpuInstruction> GetFinalInstructions()
		{
			if (!_isPatchingDone)
				throw new InvalidOperationException();

			return Writer.GetAsList();
		}

		public override IEnumerable<SpuInstruction> GetInstructions()
		{
			return Writer.GetAsList();
		}

		public void WriteProlog(int frameslots, ManualRoutine stackOverflow)
		{
			_writer.BeginNewBasicBlock();

			SpuAbiUtilities.WriteProlog(frameslots, _writer, stackOverflow);
		}

		public void WriteEpilog()
		{
			_writer.BeginNewBasicBlock();

			SpuAbiUtilities.WriteEpilog(_writer);
		}

		public override void PerformAddressPatching()
		{
			if (!_isPatchingDone)
				PerformAddressPatching(Writer.BasicBlocks, Writer.CurrentBlock);
			_isPatchingDone = true;
		}
	}
}
