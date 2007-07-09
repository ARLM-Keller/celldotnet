using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CellDotNet
{
	/// <summary>
	/// CellDotNet IR/IL opcode.
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplay}")]
	class IROpCode
	{
		private object DebuggerDisplay
		{
			get { return _name; }
		}

		private FlowControl _flowControl;
		public FlowControl FlowControl
		{
			get { return _flowControl; }
		}

		private string _name;
		public string Name
		{
			get { return _name; }
		}

		private OpCodeType _opcodeType;
		public OpCodeType OpcodeType
		{
			get { return _opcodeType; }
		}

		private OperandType _operandType;
		public OperandType OperandType
		{
			get { return _operandType; }
		}

		private StackBehaviour _stackBehaviourPush;
		public StackBehaviour StackBehaviourPush
		{
			get { return _stackBehaviourPush; }
		}

		private StackBehaviour _stackBehaviourPop;
		public StackBehaviour StackBehaviourPop
		{
			get { return _stackBehaviourPop; }
		}

		private OpCode _reflectionOpCode;
		public OpCode ReflectionOpCode
		{
			get { return _reflectionOpCode; }
		}

		private IRCode _irCode;
		public IRCode IRCode
		{
			get { return _irCode; }
		}


		public IROpCode(string name, IRCode ircode, FlowControl flowControl, OpCodeType opcodeType, OperandType operandType,
						StackBehaviour stackBehaviourPush, StackBehaviour stackBehaviourPop, OpCode opcode)
		{
			_flowControl = flowControl;
			_name = name;
			_irCode = ircode;
			_opcodeType = opcodeType;
			_operandType = operandType;
			_stackBehaviourPush = stackBehaviourPush;
			_stackBehaviourPop = stackBehaviourPop;
			_reflectionOpCode = opcode;
		}
	}
}
