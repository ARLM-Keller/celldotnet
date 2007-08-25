using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CellDotNet
{
	/// <summary>
	/// Describes how an <see cref="IROpCode"/> behaves wrt. stack popping.
	/// </summary>
	internal enum PopBehavior
	{
		Pop0 = 0,
		Pop1 = 1,
		Pop2 = 2,
		Pop3 = 3,
		PopAll = 1000,
		VarPop = 1001
	}

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

			Utilities.PretendVariableIsUsed(DebuggerDisplay);
		}

		public PopBehavior GetPopBehavior()
		{
			PopBehavior pb;

			switch (StackBehaviourPop)
			{
				case StackBehaviour.Pop0:
					pb = PopBehavior.Pop0;
					break;
				case StackBehaviour.Varpop:
					pb = PopBehavior.VarPop;
					break;
				case StackBehaviour.Pop1:
				case StackBehaviour.Popi:
				case StackBehaviour.Popref:
					pb = PopBehavior.Pop1;
					break;
				case StackBehaviour.Pop1_pop1:
				case StackBehaviour.Popi_pop1:
				case StackBehaviour.Popi_popi:
				case StackBehaviour.Popi_popi8:
				case StackBehaviour.Popi_popr4:
				case StackBehaviour.Popi_popr8:
				case StackBehaviour.Popref_pop1:
				case StackBehaviour.Popref_popi:
					pb = PopBehavior.Pop2;
					break;
				case StackBehaviour.Popi_popi_popi:
				case StackBehaviour.Popref_popi_pop1:
				case StackBehaviour.Popref_popi_popi:
				case StackBehaviour.Popref_popi_popi8:
				case StackBehaviour.Popref_popi_popr4:
				case StackBehaviour.Popref_popi_popr8:
				case StackBehaviour.Popref_popi_popref:
					pb = PopBehavior.Pop3;
					break;
				default:
					throw new ArgumentOutOfRangeException("code");
			}

			return pb;
		}
	}
}
