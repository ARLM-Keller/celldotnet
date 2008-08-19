using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CellDotNet.Intermediate;

namespace CellDotNet.Cuda
{
	/// <summary>
	/// This class is used for the IL-based IR, but also for the output of instruction selection, so it needs to accomodate to
	/// multiple ISAs.
	/// </summary>
	[DebuggerDisplay("{IRCode} - {PtxCode}")]
	class ListInstruction
	{
		public ListInstruction(IRCode opcode, object operand)
		{
			_opCode = (int) opcode;
			Operand = operand;
		}

		public ListInstruction(PtxCode opcode)
		{
			_opCode = (int) opcode;
		}

		public ListInstruction(IRCode opcode)
		{
			_opCode = (int) opcode;
		}

		public ListInstruction(PtxCode opcode, object operand)
		{
			_opCode = (int) opcode;
			Operand = operand;
		}

		/// <summary>
		/// Copy constructor. Next and previous are not copied.
		/// </summary>
		public ListInstruction(PtxCode opcode, ListInstruction template)
		{
			_opCode = (int) opcode;
			// Yes, virtcalls here, so MethodCallListInstruction probably shouldn't use this constructor.
			Destination = template.Destination;
			Source1 = template.Source1;
			Source2 = template.Source2;
			Source3 = template.Source3;
			Predicate = template.Predicate;
			PredicateNegation = template.PredicateNegation;
			Operand = template.Operand;
		}

		private readonly int _opCode;

		/// <summary>
		/// The opcode cast as an IR opcode.
		/// </summary>
		public IRCode IRCode
		{
			get { return (IRCode) _opCode; }
		}

		/// <summary>
		/// The opcode cast as a PTX opcode.
		/// </summary>
		public PtxCode PtxCode
		{
			get { return (PtxCode) _opCode; }
		}

		public object Operand { get; set; }
		public GlobalVReg OperandAsGlobalVReg
		{
			get { return Operand as GlobalVReg; }
		}

		public ListInstruction Next { get; set; }
		public ListInstruction Previous { get; set; }

		public virtual GlobalVReg Source1 { get; set; }
		public virtual GlobalVReg Source2 { get; set; }
		public virtual GlobalVReg Source3 { get; set; }
		public GlobalVReg Destination { get; set; }

		public GlobalVReg Predicate { get; set; }
		public bool PredicateNegation { get; set; }
	}

	class MethodCallListInstruction : ListInstruction
	{
		public MethodCallListInstruction(IRCode opcode, object operand) : base(opcode, operand)
		{
			Parameters = new List<GlobalVReg>(2);
		}

		public override GlobalVReg Source1
		{
			get { throw new InvalidOperationException(); }
			set { throw new InvalidOperationException(); }
		}

		public override GlobalVReg Source2
		{
			get { throw new InvalidOperationException(); }
			set { throw new InvalidOperationException(); }
		}

		public List<GlobalVReg> Parameters { get; private set; }
	}
}
