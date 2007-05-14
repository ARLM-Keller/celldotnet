using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// Describes the layout and behavior of a SPU instruction.
	/// TODO: Instruction layout.
	/// </summary>
	class SpuOpCode
	{
		private int _destinationRegisterCount;
		public int DestinationRegisterCount
		{
			get
			{
				return _destinationRegisterCount;
			}
		}

		private int _sourceRegisterCount;
		public int SourceRegisterCount
		{
			get { return _sourceRegisterCount; }
		}

		private int _constantWidth;
		public int ConstantWidth
		{
			get { return _constantWidth; }
		}

		private SpuCode _spuCode;
		public SpuCode SpuCode
		{
			get { return _spuCode; }
		}


	}
}
