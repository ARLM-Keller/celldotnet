using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// This attribute indicates that the method to which it is applied should be translated directly into the
	/// specified SPU opcode.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	sealed class SpuOpCodeAttribute : Attribute
	{
		private SpuOpCodeEnum _spuOpCode;
		public SpuOpCodeEnum SpuOpCode
		{
			get { return _spuOpCode; }
		}

		public SpuOpCodeAttribute(SpuOpCodeEnum opcode)
		{
			_spuOpCode = opcode;
		}
	}

	/// <summary>
	/// Indicates which SPU instruction part that a parameter or return value should go into.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = false)]
	sealed class SpuInstructionPartAttribute : Attribute
	{
		private SpuInstructionPart _part;
		public SpuInstructionPart Part
		{
			get { return _part; }
		}

		public SpuInstructionPartAttribute(SpuInstructionPart part)
		{
			_part = part;
		}
	}
}
