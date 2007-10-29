// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace CellDotNet.Intermediate
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

		public override string ToString()
		{
			return _name;
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

		private OpCode? _reflectionOpCode;
		/// <summary>
		/// The IL opcode that this IR opcode is based upon, if any.
		/// </summary>
		public OpCode? ReflectionOpCode
		{
			get { return _reflectionOpCode; }
		}

		private IRCode _irCode;
		public IRCode IRCode
		{
			get { return _irCode; }
		}

		public IROpCode(string name, IRCode irCode, FlowControl flowControl, OpCode? reflectionOpCode)
		{
			_flowControl = flowControl;
			_name = name;
			_reflectionOpCode = reflectionOpCode;
			_irCode = irCode;

			Utilities.PretendVariableIsUsed(DebuggerDisplay);
		}

		public static PopBehavior GetPopBehavior(StackBehaviour stackBehaviourPop)
		{
			PopBehavior pb;

			switch (stackBehaviourPop)
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
					throw new ArgumentOutOfRangeException("stackBehaviourPop");
			}

			return pb;
		}
	}
}
