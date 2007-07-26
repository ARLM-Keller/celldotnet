using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// Represents code which is not the result of a method compilation.
	/// </summary>
	class SpuRoutine : ObjectWithAddress
	{
		public SpuRoutine()
		{
			_writer = new SpuInstructionWriter();
		}

		private SpuInstructionWriter _writer;
		public SpuInstructionWriter Writer
		{
			get { return _writer; }
		}

		public override int Size
		{
			get { throw new NotImplementedException(); }
		}
	}
}
