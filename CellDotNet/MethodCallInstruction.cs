using System;
using System.Collections.Generic;
using System.Reflection;

namespace CellDotNet
{
	class MethodCallInstruction : TreeInstruction
	{
		/// <summary>
		/// The Operand casted as a method.
		/// </summary>
		public MethodBase Method
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

		public MethodCallInstruction(MethodBase method, IROpCode opcode)
		{
			Operand = method;
			Opcode = opcode;
		}

		public MethodCallInstruction(SpuIntrinsicFunction intrinsic)
		{
			Utilities.AssertArgument(intrinsic != SpuIntrinsicFunction.None, "intrinsic != SpuIntrinsicFunction.None");
			Operand = intrinsic;
			Opcode = IROpCodes.IntrinsicMethod;
		}

		private List<TreeInstruction> _parameters = new List<TreeInstruction>();
		public List<TreeInstruction> Parameters
		{
			get { return _parameters; }
		}

		internal override void BuildPreorder(List<TreeInstruction> list)
		{
			list.Add(this);
			foreach (TreeInstruction param in Parameters)
			{
				param.BuildPreorder(list);
			}
//			base.BuildPreorder(list);
		}

		public override IEnumerable<TreeInstruction> GetChildInstructions()
		{
			return Parameters;
		}
	}

	/// <summary>
	/// These values represents intrinsic functions.
	/// </summary>
	internal enum SpuIntrinsicFunction
	{
		None,
		Runtime_Stop,

	}
}
