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
		Vector_GetWord0,
		Vector_GetWord1,
		Vector_GetWord2,
		Vector_GetWord3,
		Vector_PutWord0,
		Vector_PutWord1,
		Vector_PutWord2,
		Vector_PutWord3,
		Int_Equals,
		Int_NotEquals,
		Float_Equals,
		Float_NotEquals,
		ReturnArgument1,
		CombineFourWords,
		SplatWord,
		CompareGreaterThanIntAndSelect,
		CompareGreaterThanFloatAndSelect,
		CompareEqualsIntAndSelect,
		ConvertIntToFloat,
		ConvertFloatToInteger,
		ConditionalSelectWord,
		ConditionalSelectVector
	}
}