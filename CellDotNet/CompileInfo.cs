using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;

namespace CellDotNet
{
	/// <summary>
	/// Data used during compilation of a method.
	/// </summary>
	class CompileInfo
	{
		private List<BasicBlock> _blocks = new List<BasicBlock>();
		public List<BasicBlock> Blocks
		{
			get { return _blocks; }
		}


		public CompileInfo(MethodDefinition	method)
		{
			method.Body.Simplify();

			BuildBasicBlocks(method);
			CheckTreeInstructionCount(method.Body.Instructions.Count);
			DeriveTypes();
		}


		/// <summary>
		/// Checks that the number of instructions in the constructed tree is equal to the number of IL instructions in the cecil model.
		/// </summary>
		/// <param name="correctCount"></param>
		private void CheckTreeInstructionCount(int correctCount)
		{
			int sum = 0;
			foreach (BasicBlock block in _blocks)
			{
				foreach (TreeInstruction root in block.Roots)
				{
					sum += root.TreeSize;
				}
			}

			if (sum != correctCount)
			{
				string msg = string.Format("Invalid tree instruction count of {0}. Should have been {1}.", sum, correctCount);
				throw new Exception(msg);
//				Console.Error.WriteLine(msg);
			}
		}

		/// <summary>
		/// Derives the types of each tree node using a bottom-up analysis.
		/// </summary>
		private void DeriveTypes()
		{
//			throw new NotImplementedException();
			foreach (BasicBlock block in Blocks)
			{
				foreach (TreeInstruction root in block.Roots)
				{
					DeriveType(root, 0);
				}
			}
		}

		/// <summary>
		/// This is the recursive part of DeriveTypes.
		/// </summary>
		/// <param name="inst"></param>
		private static void DeriveType(TreeInstruction inst, int level)
		{
			if (inst.Left != null)
				DeriveType(inst.Left, level + 1);
			if (inst.Right != null)
				DeriveType(inst.Right, level + 1);

			CilType t;
			switch (inst.Opcode.FlowControl)
			{
				case FlowControl.Branch:
				case FlowControl.Break:
					if (level != 0)
						throw new NotImplementedException("Only root branches are implemented.");
					t = CilType.None;
					break;
				case FlowControl.Call:
					throw new NotImplementedException("Message call not implemented.");
				case FlowControl.Cond_Branch:
					throw new NotImplementedException();
					break;
				case FlowControl.Meta:
				case FlowControl.Phi:
					throw new ILException("Meta or Phi.");
				case FlowControl.Next:
//					t = CilType.None;
					try
					{
						t = DeriveFlowNextType(inst, level);
					}
					catch (NotImplementedException e)
					{
						throw new NotImplementedException("Error while deriving flow instruction opcode: " + inst.Opcode.Code, e);
					}
					break;
				case FlowControl.Return:
					if (inst.Left != null)
						t = inst.Left.CilType;
					else
						t = CilType.None;
					break;
				case FlowControl.Throw:
					t = CilType.None;
					break;
				default:
					throw new ILException("Default");
			}


			// TODO: 
			inst.CilType = t;
		}

		/// <summary>
		/// Used by <see cref="DeriveType"/> to derive types for instructions 
		/// that do not change the flow; that is, OpCode.Flow == Next.
		/// </summary>
		/// <param name="inst"></param>
		/// <param name="level"></param>
		private static CilType DeriveFlowNextType(TreeInstruction inst, int level)
		{
			// The cases are generated and all opcodes with flow==next are present 
			// (except macro codes such as ldc.i4.3).
			CilType t;
//			Type typeToken = (Type) inst.Operand;
			Type customType = null;
			TypeReference optype;
			if (inst.Operand is TypeReference)
				optype = ((ParameterReference) inst.Operand).ParameterType;
			else if (inst.Operand is VariableReference)
				optype = ((VariableReference)inst.Operand).VariableType;
			else if (inst.Operand is ParameterReference)
				optype = ((ParameterReference)inst.Operand).ParameterType;
			else 
				optype = null;

			switch (inst.Opcode.Code)
			{
				case Code.Nop: // nop
				case Code.Ldnull: // ldnull
					t = CilType.ObjectType;
					break;
				case Code.Ldc_I4: // ldc.i4
					t = CilType.Int32;
					break;
				case Code.Ldc_I8: // ldc.i8
					t = CilType.Int64;
					break;
				case Code.Ldc_R4: // ldc.r4
					t = CilType.Float32;
					break;
				case Code.Ldc_R8: // ldc.r8
					t = CilType.Float64;
					break;
				case Code.Dup: // dup
				case Code.Pop: // pop
					throw new NotImplementedException("dup, pop");
				case Code.Ldind_I1: // ldind.i1
					t = CilType.Int8;
					break;
				case Code.Ldind_U1: // ldind.u1
					t = CilType.UInt8;
					break;
				case Code.Ldind_I2: // ldind.i2
					t = CilType.Int16;
					break;
				case Code.Ldind_U2: // ldind.u2
					t = CilType.UInt16;
					break;
				case Code.Ldind_I4: // ldind.i4
					t = CilType.Int32;
					break;
				case Code.Ldind_U4: // ldind.u4
					t = CilType.UInt32;
					break;
				case Code.Ldind_I8: // ldind.i8
					t = CilType.Int64;
					break;
				case Code.Ldind_I: // ldind.i
					t = CilType.NativeInt;
					break;
				case Code.Ldind_R4: // ldind.r4
					t = CilType.Float32;
					break;
				case Code.Ldind_R8: // ldind.r8
					t = CilType.Float64;
					break;
				case Code.Ldind_Ref: // ldind.ref
					t = CilType.ObjectType;
					break;
				case Code.Stind_Ref: // stind.ref
				case Code.Stind_I1: // stind.i1
				case Code.Stind_I2: // stind.i2
				case Code.Stind_I4: // stind.i4
				case Code.Stind_I8: // stind.i8
				case Code.Stind_R4: // stind.r4
				case Code.Stind_R8: // stind.r8
					if (level != 0)
						throw new NotSupportedException();
					t = CilType.None;
					break;
				case Code.Add: // add
				case Code.Div: // div
				case Code.Sub: // sub
				case Code.Mul: // mul
				case Code.Rem: // rem

				case Code.Add_Ovf: // add.ovf
				case Code.Add_Ovf_Un: // add.ovf.un
				case Code.Mul_Ovf: // mul.ovf
				case Code.Mul_Ovf_Un: // mul.ovf.un
				case Code.Sub_Ovf: // sub.ovf
				case Code.Sub_Ovf_Un: // sub.ovf.un
					t = GetNumericResultType(inst.Left.CilType, inst.Right.CilType);
					break;
				case Code.And: // and
				case Code.Div_Un: // div.un
				case Code.Not: // not
				case Code.Or: // or
				case Code.Rem_Un: // rem.un
				case Code.Xor: // xor
					// From CIL table 5.
					if (inst.Left.CilType == inst.Right.CilType)
						t = inst.Left.CilType;
					else
					{
						// Must be native (u)int.
						if (inst.Left.CilType == CilType.NativeInt)
							t = CilType.NativeInt;
						else 
							t = CilType.NativeUInt;
					}
					break;
				case Code.Shl: // shl
				case Code.Shr: // shr
				case Code.Shr_Un: // shr.un
					// CIL table 6.
					t = inst.Left.CilType;
					break;
				case Code.Neg: // neg
					t = inst.Left.CilType;
					break;
				case Code.Cpobj: // cpobj
				case Code.Ldobj: // ldobj
				case Code.Ldstr: // ldstr
				case Code.Castclass: // castclass
				case Code.Isinst: // isinst
				case Code.Unbox: // unbox
				case Code.Ldfld: // ldfld
				case Code.Ldflda: // ldflda
				case Code.Stfld: // stfld
				case Code.Ldsfld: // ldsfld
				case Code.Ldsflda: // ldsflda
				case Code.Stsfld: // stsfld
				case Code.Stobj: // stobj
					throw new NotImplementedException(inst.Opcode.Code.ToString());
				case Code.Conv_Ovf_I8_Un: // conv.ovf.i8.un
				case Code.Conv_I8: // conv.i8
				case Code.Conv_Ovf_I8: // conv.ovf.i8
					t = CilType.Int64;
					break;
				case Code.Conv_R4: // conv.r4
					t = CilType.Float32;
					break;
				case Code.Conv_R8: // conv.r8
					t = CilType.Float64;
					break;
				case Code.Conv_I1: // conv.i1
				case Code.Conv_Ovf_I1_Un: // conv.ovf.i1.un
				case Code.Conv_Ovf_I1: // conv.ovf.i1
					t = CilType.Int8;
					break;
				case Code.Conv_I2: // conv.i2
				case Code.Conv_Ovf_I2: // conv.ovf.i2
				case Code.Conv_Ovf_I2_Un: // conv.ovf.i2.un
					t = CilType.Int16;
					break;
				case Code.Conv_Ovf_I4: // conv.ovf.i4
				case Code.Conv_I4: // conv.i4
				case Code.Conv_Ovf_I4_Un: // conv.ovf.i4.un
					t = CilType.Int32;
					break;
				case Code.Conv_U4: // conv.u4
				case Code.Conv_Ovf_U4: // conv.ovf.u4
				case Code.Conv_Ovf_U4_Un: // conv.ovf.u4.un
					t = CilType.UInt32;
					break;
				case Code.Conv_R_Un: // conv.r.un
					t = CilType.Float32; // really F, but we're 32 bit.
					break;
				case Code.Conv_Ovf_U1_Un: // conv.ovf.u1.un
				case Code.Conv_U1: // conv.u1
				case Code.Conv_Ovf_U1: // conv.ovf.u1
					t = CilType.UInt8;
					break;
				case Code.Conv_Ovf_U2_Un: // conv.ovf.u2.un
				case Code.Conv_U2: // conv.u2
				case Code.Conv_Ovf_U2: // conv.ovf.u2
					t = CilType.UInt16;
					break;
				case Code.Conv_U8: // conv.u8
				case Code.Conv_Ovf_U8_Un: // conv.ovf.u8.un
				case Code.Conv_Ovf_U8: // conv.ovf.u8
					t = CilType.UInt64;
					break;
				case Code.Conv_I: // conv.i
				case Code.Conv_Ovf_I: // conv.ovf.i
				case Code.Conv_Ovf_I_Un: // conv.ovf.i.un
					t = CilType.NativeInt;
					break;
				case Code.Conv_Ovf_U_Un: // conv.ovf.u.un
				case Code.Conv_Ovf_U: // conv.ovf.u
				case Code.Conv_U: // conv.u
					t = CilType.NativeUInt;
					break;
				case Code.Box: // box
					throw new NotImplementedException();
//					t = CilType.ObjectType;
//					customType = typeof (object);
					break;
				case Code.Newarr: // newarr
					t = CilType.ObjectType;

					throw new NotImplementedException();
//					customType = Array.;
				case Code.Ldlen: // ldlen
					t = CilType.NativeUInt;
					break;
				case Code.Ldelema: // ldelema
					t = CilType.ManagedPointer;
//					customty
					break;
				case Code.Ldelem_I1: // ldelem.i1
					t = CilType.Int8;
					break;
				case Code.Ldelem_U1: // ldelem.u1
					t = CilType.UInt8;
					break;
				case Code.Ldelem_I2: // ldelem.i2
					t = CilType.Int32;
					break;
				case Code.Ldelem_U2: // ldelem.u2
					t = CilType.UInt16;
					break;
				case Code.Ldelem_I4: // ldelem.i4
					t = CilType.Int32;
					break;
				case Code.Ldelem_U4: // ldelem.u4
					t = CilType.UInt32;
					break;
				case Code.Ldelem_I8: // ldelem.i8
					t = CilType.Int64;
					break;
				case Code.Ldelem_I: // ldelem.i
					// Guess this can also be unsigned?
					t = CilType.NativeInt;
					break;
				case Code.Ldelem_R4: // ldelem.r4
					t = CilType.Float32;
					break;
				case Code.Ldelem_R8: // ldelem.r8
					t = CilType.Float64;
					break;
				case Code.Ldelem_Ref: // ldelem.ref
					throw new NotImplementedException();
				case Code.Stelem_I: // stelem.i
				case Code.Stelem_I1: // stelem.i1
				case Code.Stelem_I2: // stelem.i2
				case Code.Stelem_I4: // stelem.i4
				case Code.Stelem_I8: // stelem.i8
				case Code.Stelem_R4: // stelem.r4
				case Code.Stelem_R8: // stelem.r8
				case Code.Stelem_Ref: // stelem.ref
					t = CilType.None;
					break;
				case Code.Ldelem_Any: // ldelem.any
				case Code.Stelem_Any: // stelem.any
					throw new ILException("ldelem_any and stelem_any are invalid.");
				case Code.Unbox_Any: // unbox.any
				case Code.Refanyval: // refanyval
				case Code.Ckfinite: // ckfinite
				case Code.Mkrefany: // mkrefany
				case Code.Ldtoken: // ldtoken
				case Code.Stind_I: // stind.i
				case Code.Arglist: // arglist
					throw new NotImplementedException();
				case Code.Ceq: // ceq
				case Code.Cgt: // cgt
				case Code.Cgt_Un: // cgt.un
				case Code.Clt: // clt
				case Code.Clt_Un: // clt.un
					t = CilType.Bool;
					break;
				case Code.Ldftn: // ldftn
				case Code.Ldvirtftn: // ldvirtftn
					throw new NotImplementedException();
				case Code.Ldarg: // ldarg
				case Code.Ldloca: // ldloca
					t = GetCilType(optype.MetadataToken);
					if (!IsNumeric(t))
						throw new NotImplementedException("Only numeric CIL types are implemented.");
					if (t == CilType.None)
						throw new Exception("Error...");

					break;
				case Code.Ldloc: // ldloc
				case Code.Ldarga: // ldarga
					t = GetCilType(optype.MetadataToken);
					if (!IsNumeric(t))
						throw new NotImplementedException("Only numeric CIL types are implemented.");
					if (t == CilType.None)
						throw new Exception("Error...");
					break;
				case Code.Starg: // starg
				case Code.Stloc: // stloc
					t = CilType.None;
					break;
				case Code.Localloc: // localloc
					throw new NotImplementedException();
				case Code.Initobj: // initobj
				case Code.Constrained: // constrained.
				case Code.Cpblk: // cpblk
				case Code.Initblk: // initblk
				case Code.No: // no.
				case Code.Sizeof: // sizeof
				case Code.Refanytype: // refanytype
				case Code.Readonly: // readonly.
					throw new NotImplementedException();
				default:
					throw new ILException();
			}

			return t;
			throw new NotImplementedException();

		}

		private static bool IsNumeric(CilType type)
		{
			return type != CilType.ManagedPointer &&
			       type != CilType.MethodPointer &&
			       type != CilType.ValueType &&
			       type != CilType.None &&
			       type != CilType.ObjectType &&
			       type != CilType.ValueType;
		}

		private static Dictionary<uint, CilType> s_metadataCilTypes = BuildBasicMetadataCilDictionary();
		private static Dictionary<uint, CilType> BuildBasicMetadataCilDictionary()
		{
			Dictionary<uint, CilType> dict = new Dictionary<uint, CilType>();

			// TODO: the typeof() token values are not what cecil returns...
			dict.Add((uint) typeof(sbyte).MetadataToken, CilType.Int8);
			dict.Add((uint) typeof(byte).MetadataToken, CilType.UInt8);
			dict.Add((uint) typeof(short).MetadataToken, CilType.Int16);
			dict.Add((uint) typeof(ushort).MetadataToken, CilType.UInt16);
			dict.Add((uint) typeof(int).MetadataToken, CilType.Int32);
			dict.Add((uint) typeof(uint).MetadataToken, CilType.UInt32);
			dict.Add((uint) typeof(long).MetadataToken, CilType.Int64);
			dict.Add((uint) typeof(ulong).MetadataToken, CilType.UInt64);
			dict.Add((uint) typeof(IntPtr).MetadataToken, CilType.NativeInt);
			dict.Add((uint) typeof(UIntPtr).MetadataToken, CilType.NativeUInt);
			dict.Add((uint) typeof(float).MetadataToken, CilType.Float32);
			dict.Add((uint) typeof(double).MetadataToken, CilType.Float64);

			return dict;
		}


		/// <summary>
		/// If the token is recognized as a CIL numeric type, that type is returned;
		/// otherwise, None is returned.
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		private static CilType GetCilType(MetadataToken token)
		{
			CilType ct;
			if (s_metadataCilTypes.TryGetValue(token.ToUInt(), out ct))
				return ct;

			return CilType.None;
		}

		/// <summary>
		/// Computes the result type of binary numeric operations given the specified input types.
		/// Computation is done according to table 2 and table 7 in the CIL spec plus intuition.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		private static CilType GetNumericResultType(CilType left, CilType right)
		{
			CliType rleft = GetReducedType(left);
			CliType rright = GetReducedType(right);

			// We are relying on the fact that the enumeration values are sorted by size.
			if (rleft == rright)
			{
				if (rleft != CliType.ManagedPointer) 
					return (CilType) Math.Max((int) rleft, (int) rright);
				else 
					return CilType.NativeInt;
			}

			if (rleft == CliType.ManagedPointer || rright == CliType.ManagedPointer)
			{
				if (rleft == CliType.ManagedPointer && rright == CliType.ManagedPointer)
					return CilType.NativeInt;

				return CilType.ManagedPointer;
			}

			if (rleft == CliType.NativeInt || rright == CliType.NativeInt)
				return CilType.NativeInt;

			throw new ArgumentException(
				string.Format("Argument types are not valid cil binary numeric opcodes: Left: {0}; right: {1}.", left, right));
		}

		private static CliType GetReducedType(CilType type)
		{
			switch (type)
			{
				case CilType.None:
					throw new ArgumentOutOfRangeException("type");
				case CilType.Int8:
				case CilType.UInt8:
				case CilType.Bool:
				case CilType.Int16:
				case CilType.UInt16:
				case CilType.Char:
				case CilType.Int32:
				case CilType.UInt32:
					return CliType.Int32;
				case CilType.Int64:
				case CilType.UInt64:
					return CliType.Int64;
				case CilType.NativeInt:
				case CilType.NativeUInt:
					return CliType.NativeInt;
				case CilType.Float32:
				case CilType.Float64:
					return CliType.F;
				case CilType.ValueType:
					// Is this correct?
					return CliType.ManagedPointer;
				case CilType.ObjectType:
					return CliType.O;
				case CilType.MethodPointer:
					throw new NotSupportedException("Can't handle method pointers.");
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}


		private void BuildBasicBlocks(MethodDefinition method)
		{
			BasicBlock currblock = new BasicBlock();
			List<TreeInstruction> stack = new List<TreeInstruction>();
			List<TreeInstruction> branches = new List<TreeInstruction>();

			TreeInstruction prevroot = null;

			foreach (Instruction inst in method.Body.Instructions)
			{
				TreeInstruction treeinst;
				PopBehavior popbehavior = GetPopCount(inst.OpCode);
				int pushcount = GetPushCount(inst.OpCode);

				treeinst = new TreeInstruction();
				treeinst.Opcode = inst.OpCode;
				treeinst.Operand = inst.Operand;
				treeinst.Offset = inst.Offset;


				// Pop
				switch (popbehavior)
				{
					case PopBehavior.Pop1:
						treeinst.Left = stack[stack.Count - 1];
						stack.RemoveRange(stack.Count - 1, 1);
						break;
					case PopBehavior.Pop2:
						treeinst.Left = stack[stack.Count - 2];
						treeinst.Right = stack[stack.Count - 1];
						stack.RemoveRange(stack.Count - 2, 2);
						break;
					case PopBehavior.PopAll: // "leave"
						throw new NotImplementedException("PopAll");
					case PopBehavior.VarPop: // "ret"
						if (inst.OpCode != OpCodes.Ret)
							throw new Exception("Method calls are not supported.");
						// CLI: "The 10 evaluation stack for the current method must be empty except for the value to be returned."
						if (stack.Count > 0)
						{
							treeinst.Left = stack[0];
							if (stack.Count > 1)
								throw new ILException("Stack.Count > 1 ??");
							stack.Clear();
						}
						break;
					default:
						if (popbehavior != PopBehavior.Pop0)
							throw new Exception("Invalid PopBehavior: " + popbehavior + ". Only two-argument method calls are supported.");
						break;
				}

				// Push
				if (pushcount == 1)
					stack.Add(treeinst);
				else if (pushcount != 0)
					throw new Exception("Only 1-push is supported.");

				bool endsblock = false;
				switch (inst.OpCode.FlowControl)
				{
					case FlowControl.Branch:
					case FlowControl.Cond_Branch:
					case FlowControl.Return:
					case FlowControl.Throw:
						endsblock = true;

						if (inst.OpCode.FlowControl == FlowControl.Branch ||
							inst.OpCode.FlowControl == FlowControl.Cond_Branch)
						{
							// For now, just store the target offset; this is fixed below.
							treeinst.Operand = ((Instruction) inst.Operand).Offset;
							branches.Add(treeinst);
						}
						break;
					case FlowControl.Call:
					case FlowControl.Meta:
					case FlowControl.Next:
					case FlowControl.Phi:
					case FlowControl.Break:
						break;
					default:
						throw new ILException();
				}

				if (endsblock)
				{
					currblock.Roots.Add(treeinst);
					Blocks.Add(currblock);
					currblock = new BasicBlock();
				}
				else if (stack.Count == 0)
				{
					// It is a root exactly when the stack is empty.
					currblock.Roots.Add(treeinst);
				}
				else
				{
				
				}
			}

			if (currblock.Roots.Count > 0)
				Blocks.Add(currblock);

			// Fix branches.
			foreach (TreeInstruction branchinst in branches)
			{
				int targetOffset = (int) branchinst.Operand;
				foreach (BasicBlock block in Blocks)
				{
					foreach (TreeInstruction root in block.Roots)
					{
						foreach (TreeInstruction inst in root.IterateInorder())
						{
							if (inst.Offset == targetOffset)
								branchinst.Operand = inst;
						}
					}
				}
			}
		}

		static int GetPushCount(OpCode code)
		{
			int pushCount;

			switch (code.StackBehaviourPush)
			{
				case StackBehaviour.Push0:
					pushCount = 0;
					break;
				case StackBehaviour.Push1:
				case StackBehaviour.Pushi:
				case StackBehaviour.Pushi8:
				case StackBehaviour.Pushr4:
				case StackBehaviour.Pushr8:
				case StackBehaviour.Pushref:
				case StackBehaviour.Varpush:
					pushCount = 1;
					break;
				case StackBehaviour.Push1_push1:
					pushCount = 2;
					break;
				default:
					pushCount = -1;
					break;
			}

			return pushCount;
		}

		enum PopBehavior
		{
			Pop0 = 0,
			Pop1 = 1,
			Pop2 = 2,
			Pop3 = 3,
			PopAll = 1000,
			VarPop = 1001

		}

		static PopBehavior GetPopCount(OpCode code)
		{
			PopBehavior pb;

			switch (code.StackBehaviourPop)
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
				case StackBehaviour.Popref_popi_popi:
				case StackBehaviour.Popref_popi_popi8:
				case StackBehaviour.Popref_popi_popr4:
				case StackBehaviour.Popref_popi_popr8:
				case StackBehaviour.Popref_popi_popref:
					pb = PopBehavior.Pop3;
					break;
				case StackBehaviour.PopAll:
					pb = PopBehavior.PopAll; // Special...
					break;
				default:
					throw new ArgumentOutOfRangeException("code");
			}

			return pb;
		}
	}
}
