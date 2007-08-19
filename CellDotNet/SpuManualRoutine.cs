using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CellDotNet
{
	/// <summary>
	/// Represents code which is not the result of a method compilation, but
	/// rather generated "manually".
	/// <para>
	/// Is this one needed? Currently (20070731) it seems we'll be subclassing
	/// <see cref="ObjectWithAddress"/> to create specialized classes...
	/// </para>
	/// </summary>
	class SpuManualRoutine : SpuRoutine
	{
		private bool _omitEpilog = false;
		/// <summary>
		/// Is set to true when the routine should no longer be written to.
		/// </summary>
		private bool _isPatchingDone;

		public SpuManualRoutine(bool omitEpilog)
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
			int[] bodybin = SpuInstruction.emit(Writer.GetAsList());
			return bodybin;
		}

		public override IEnumerable<SpuInstruction> GetInstructions()
		{
			if (!_isPatchingDone)
				throw new InvalidOperationException();

			return _writer.GetAsList();
		}

		public override void PerformAddressPatching()
		{
			if (!_isPatchingDone)
			{
				if (_omitEpilog)
					PerformAddressPatching(_writer.BasicBlocks, null);
				else
				{
					Writer.BeginNewBasicBlock();
					SpuAbiUtilities.WriteEpilog(Writer);
					PerformAddressPatching(Writer.BasicBlocks, Writer.CurrentBlock);
				}
			}
			_isPatchingDone = true;
		}
	}
}
