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

using System.Collections.Generic;
using System.Reflection;
using CellDotNet.Spe;

namespace CellDotNet.Intermediate
{
	class MethodCallInstruction : TreeInstruction
	{
		public MethodCallInstruction(MethodBase method, IROpCode opcode) : base(opcode)
		{
			Operand = method;
			Opcode = opcode;
		}

		/// <summary>
		/// The reason that this ctor also takes the method as an argument is so that the type deriver
		/// can do its job.
		/// </summary>
		/// <param name="intrinsic"></param>
		/// <param name="method"></param>
		/// <param name="intrinsicCallOpCode"></param>
		public MethodCallInstruction(MethodBase method, SpuIntrinsicMethod intrinsic, IROpCode intrinsicCallOpCode) : base(intrinsicCallOpCode)
		{
			Utilities.AssertArgument(intrinsic != SpuIntrinsicMethod.None, "intrinsic != SpuIntrinsicMethod.None");
			Operand = intrinsic;
			_intrinsicMethod = method;
		}

		public MethodCallInstruction(MethodInfo method, SpuOpCode spuOpCode) : base(IROpCodes.SpuInstructionMethod)
		{
			Operand = spuOpCode;
			_intrinsicMethod = method;
		}

		/// <summary>
		/// The Operand casted as a method.
		/// </summary>
		public MethodBase OperandMethod
		{
			get { return (MethodBase) Operand; }
		}

		/// <summary>
		/// The operand cast as a <see cref="MethodCompiler"/>.
		/// </summary>
		public MethodCompiler TargetMethodCompiler
		{
			get { return (MethodCompiler) Operand; }
		}

		/// <summary>
		/// The operand cast as a <see cref="SpuRoutine"/>.
		/// </summary>
		public SpuRoutine TargetRoutine
		{
			get { return (SpuRoutine) Operand; }
		}

		private MethodBase _intrinsicMethod;
		/// <summary>
		/// Intrinsic methods are exposed via this property so that the type deriver can do its job.
		/// </summary>
		public MethodBase IntrinsicMethod
		{
			get { return _intrinsicMethod; }
		}

		private List<TreeInstruction> _parameters = new List<TreeInstruction>();
		public List<TreeInstruction> Parameters
		{
			get { return _parameters; }
		}

		public SpuOpCode OperandSpuOpCode
		{
			get { return (SpuOpCode) Operand; }
		}

		internal override void BuildPreorder(List<TreeInstruction> list)
		{
			list.Add(this);
			foreach (TreeInstruction param in Parameters)
			{
				param.BuildPreorder(list);
			}
		}

		public override TreeInstruction[] GetChildInstructions()
		{
			return Parameters.ToArray();
		}

		public override void ReplaceChild(int childIndex, TreeInstruction newchild)
		{
			Parameters[childIndex] = newchild;
		}

		public void SetCalledMethod(SpuRoutine routine, IROpCode callOpCode)
		{
			Utilities.AssertArgumentNotNull(routine, "routine");

			Operand = routine;
			Opcode = callOpCode;
		}
	}
}
