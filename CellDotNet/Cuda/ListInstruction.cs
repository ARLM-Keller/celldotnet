using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CellDotNet.Intermediate;

namespace CellDotNet.Cuda
{
	class ListInstruction
	{
		public ListInstruction(IROpCode opcode, object operand)
		{
			this.OpCode = opcode;
			Operand = operand;
		}

		public IROpCode OpCode { get; private set; }
		public object Operand { get; private set; }

		public ListInstruction Next { get; set; }
		public ListInstruction Previous { get; set; }

		public virtual MethodVariable Source1 { get; set; }
		public virtual MethodVariable Source2 { get; set; }
		public MethodVariable Destination { get; set; }
	}

	class MethodCallListInstruction : ListInstruction
	{
		public MethodCallListInstruction(IROpCode opcode, object operand) : base(opcode, operand)
		{
		}
	
		public override MethodVariable Source1
		{
			get { throw new InvalidOperationException(); }
			set { throw new InvalidOperationException(); }
		}

		public override MethodVariable Source2
		{
			get { throw new InvalidOperationException(); }
			set { throw new InvalidOperationException(); }
		}

		public List<MethodVariable> Parameters { get; set; }
	}
}
