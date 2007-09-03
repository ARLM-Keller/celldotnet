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

		private MethodInfo _intrinsicMethod;
		/// <summary>
		/// Intrinsic methods are exposed via this property so that the type deriver can do its job.
		/// </summary>
		public MethodInfo IntrinsicMethod
		{
			get { return _intrinsicMethod; }
		}

		/// <summary>
		/// The reason that this ctor also takes the method as an argument is so that the type deriver
		/// can do its job.
		/// </summary>
		/// <param name="intrinsic"></param>
		/// <param name="method"></param>
		public MethodCallInstruction(SpuIntrinsicMethod intrinsic, MethodInfo method)
		{
			Utilities.AssertArgument(intrinsic != SpuIntrinsicMethod.None, "intrinsic != SpuIntrinsicMethod.None");
			Operand = intrinsic;
			Opcode = IROpCodes.IntrinsicMethod;
			_intrinsicMethod = method;
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
	}
}
