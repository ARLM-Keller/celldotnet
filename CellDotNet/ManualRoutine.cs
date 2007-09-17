using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CellDotNet
{
	/// <summary>
	/// Represents code which is not the result of a method compilation, but
	/// rather generated "manually".
	/// </summary>
	class ManualRoutine : SpuDynamicRoutine
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
