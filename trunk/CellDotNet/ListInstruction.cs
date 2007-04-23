using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil.Cil;

namespace CellDotNet
{
	/// <summary>
	/// Represents an instruction that exists in a list and uses registers.
	/// </summary>
	class ListInstruction
	{
		private OpCode _opcode;

		public OpCode OpCode
		{
			get { return _opcode; }
			set { _opcode = value; }
		}

		private object _operand;

		public object Operand
		{
			get { return _operand; }
			set { _operand = value; }
		}

		private Register _source1;

		public Register Source1
		{
			get { return _source1; }
			set { _source1 = value; }
		}

		private Register _Source2;

		public Register Source2
		{
			get { return _Source2; }
			set { _Source2 = value; }
		}

		private Register _destination;

		public Register Destination
		{
			get { return _destination; }
			set { _destination = value; }
		}
	}
}
