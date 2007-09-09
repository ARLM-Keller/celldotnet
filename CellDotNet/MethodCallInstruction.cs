using System;
using System.Collections.Generic;
using System.Reflection;

namespace CellDotNet
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
		public MethodCallInstruction(MethodInfo method, SpuIntrinsicMethod intrinsic) : base(IROpCodes.IntrinsicMethod)
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

		private MethodInfo _intrinsicMethod;
		/// <summary>
		/// Intrinsic methods are exposed via this property so that the type deriver can do its job.
		/// </summary>
		public MethodInfo IntrinsicMethod
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

		public override IEnumerable<TreeInstruction> GetChildInstructions()
		{
			return Parameters;
		}
	}

	/// <summary>
	/// These values represents intrinsic functions.
	/// </summary>
	internal enum SpuIntrinsicMethod
	{
		None,
		Runtime_Stop,

		Mfc_GetAvailableQueueEntries,
		Mfc_Put,
		Mfc_Get,
		MainStorageArea_get_EffectiveAddress
	}
}
