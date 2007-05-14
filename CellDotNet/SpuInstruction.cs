using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil.Cil;

namespace CellDotNet
{
	/// <summary>
	/// Represents an SPU instruction.
	/// </summary>
	class SpuInstruction
	{
		private SpuOpCode _opcode;
		public SpuOpCode OpCode
		{
			get { return _opcode; }
			set { _opcode = value; }
		}

		private int  _constant;
		public int Constant
		{
			get { return _constant; }
			set { _constant = value; }
		}

		private VirtualRegister _source1;

		public VirtualRegister Source1
		{
			get { return _source1; }
			set { _source1 = value; }
		}

		private VirtualRegister _Source2;

		public VirtualRegister Source2
		{
			get { return _Source2; }
			set { _Source2 = value; }
		}

		private VirtualRegister _destination;

		public VirtualRegister Destination
		{
			get { return _destination; }
			set { _destination = value; }
		}
	}
}
