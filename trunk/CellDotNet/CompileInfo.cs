using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

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

		private MethodDefinition _methodDefinition;
		public MethodDefinition MethodDefinition
		{
			get { return _methodDefinition; }
		}

		public CompileInfo(MethodDefinition	method)
		{
			method.Body.Simplify();
			_methodDefinition = method;

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
			}
		}

		/// <summary>
		/// Derives the types of each tree node using a bottom-up analysis.
		/// </summary>
		private void DeriveTypes()
		{
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
		/// <param name="level"></param>
		private static void DeriveType(TreeInstruction inst, int level)
		{
			if (inst.Left != null)
				DeriveType(inst.Left, level + 1);
			if (inst.Right != null)
				DeriveType(inst.Right, level + 1);

			StackTypeDescription t;
			switch (inst.Opcode.FlowControl)
			{
				case FlowControl.Branch:
				case FlowControl.Break:
					if (level != 0)
						throw new NotImplementedException("Only root branches are implemented.");
					t = StackTypeDescription.None;
					break;
				case FlowControl.Call:
					MethodCallInstruction mci = (MethodCallInstruction) inst;

					// TODO: Handle void type.
					t = GetCilNumericType(mci.Method.ReturnType.ReturnType);
					foreach (TreeInstruction param in mci.Parameters)
					{
						DeriveType(param, level + 1);
					}
//					throw new NotImplementedException("Message call not implemented.");
					break;
				case FlowControl.Cond_Branch:
					if (level != 0)
						throw new NotImplementedException("Only root branches are implemented.");
					t = StackTypeDescription.None;
					break;
				case FlowControl.Meta:
				case FlowControl.Phi:
					throw new ILException("Meta or Phi.");
				case FlowControl.Next:
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
						t = inst.Left.StackType;
					else
						t = StackTypeDescription.None;
					break;
				case FlowControl.Throw:
					t = StackTypeDescription.None;
					break;
				default:
					throw new ILException("Default");
			}


			inst.StackType = t;
		}

		/// <summary>
		/// Used by <see cref="DeriveType"/> to derive types for instructions 
		/// that do not change the flow; that is, OpCode.Flow == Next.
		/// </summary>
		/// <param name="inst"></param>
		/// <param name="level"></param>
		private static StackTypeDescription DeriveFlowNextType(TreeInstruction inst, int level)
		{
			// The cases are generated and all opcodes with flow==next are present 
			// (except macro codes such as ldc.i4.3).
			StackTypeDescription t;

			TypeReference optype;
			if (inst.Operand is TypeReference)
				optype = ((TypeReference) inst.Operand);
			else if (inst.Operand is VariableReference)
				optype = ((VariableReference)inst.Operand).VariableType;
			else if (inst.Operand is ParameterReference)
				optype = ((ParameterReference)inst.Operand).ParameterType;
			else 
				optype = null;

			switch (inst.Opcode.Code)
			{
				case Code.Nop: // nop
					t = StackTypeDescription.None;
					break;
				case Code.Ldnull: // ldnull
					t = StackTypeDescription.ObjectType;
					break;
				case Code.Ldc_I4: // ldc.i4
					t = StackTypeDescription.Int32;
					break;
				case Code.Ldc_I8: // ldc.i8
					t = StackTypeDescription.Int64;
					break;
				case Code.Ldc_R4: // ldc.r4
					t = StackTypeDescription.Float32;
					break;
				case Code.Ldc_R8: // ldc.r8
					t = StackTypeDescription.Float64;
					break;
				case Code.Dup: // dup
					throw new NotImplementedException("dup, pop");
				case Code.Pop: // pop
					if (level != 0)
						throw new NotImplementedException("Pop only supported at root level.");
					t = StackTypeDescription.None;
					break;
				case Code.Ldind_I1: // ldind.i1
					t = StackTypeDescription.Int8;
					break;
				case Code.Ldind_U1: // ldind.u1
					t = StackTypeDescription.UInt8;
					break;
				case Code.Ldind_I2: // ldind.i2
					t = StackTypeDescription.Int16;
					break;
				case Code.Ldind_U2: // ldind.u2
					t = StackTypeDescription.UInt16;
					break;
				case Code.Ldind_I4: // ldind.i4
					t = StackTypeDescription.Int32;
					break;
				case Code.Ldind_U4: // ldind.u4
					t = StackTypeDescription.UInt32;
					break;
				case Code.Ldind_I8: // ldind.i8
					t = StackTypeDescription.Int64;
					break;
				case Code.Ldind_I: // ldind.i
					t = StackTypeDescription.NativeInt;
					break;
				case Code.Ldind_R4: // ldind.r4
					t = StackTypeDescription.Float32;
					break;
				case Code.Ldind_R8: // ldind.r8
					t = StackTypeDescription.Float64;
					break;
				case Code.Ldind_Ref: // ldind.ref
					t = StackTypeDescription.ObjectType;
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
					t = StackTypeDescription.None;
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
					t = GetNumericResultType(inst.Left.StackType, inst.Right.StackType);
					break;
				case Code.And: // and
				case Code.Div_Un: // div.un
				case Code.Not: // not
				case Code.Or: // or
				case Code.Rem_Un: // rem.un
				case Code.Xor: // xor
					// From CIL table 5.
					if (inst.Left.StackType == inst.Right.StackType)
						t = inst.Left.StackType;
					else
					{
						// Must be native (u)int.
						if (inst.Left.StackType.IsSigned)
							t = StackTypeDescription.NativeInt;
						else 
							t = StackTypeDescription.NativeUInt;
					}
					break;
				case Code.Shl: // shl
				case Code.Shr: // shr
				case Code.Shr_Un: // shr.un
					// CIL table 6.
					t = inst.Left.StackType;
					break;
				case Code.Neg: // neg
					t = inst.Left.StackType;
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
					t = StackTypeDescription.Int64;
					break;
				case Code.Conv_R4: // conv.r4
					t = StackTypeDescription.Float32;
					break;
				case Code.Conv_R8: // conv.r8
					t = StackTypeDescription.Float64;
					break;
				case Code.Conv_I1: // conv.i1
				case Code.Conv_Ovf_I1_Un: // conv.ovf.i1.un
				case Code.Conv_Ovf_I1: // conv.ovf.i1
					t = StackTypeDescription.Int8;
					break;
				case Code.Conv_I2: // conv.i2
				case Code.Conv_Ovf_I2: // conv.ovf.i2
				case Code.Conv_Ovf_I2_Un: // conv.ovf.i2.un
					t = StackTypeDescription.Int16;
					break;
				case Code.Conv_Ovf_I4: // conv.ovf.i4
				case Code.Conv_I4: // conv.i4
				case Code.Conv_Ovf_I4_Un: // conv.ovf.i4.un
					t = StackTypeDescription.Int32;
					break;
				case Code.Conv_U4: // conv.u4
				case Code.Conv_Ovf_U4: // conv.ovf.u4
				case Code.Conv_Ovf_U4_Un: // conv.ovf.u4.un
					t = StackTypeDescription.UInt32;
					break;
				case Code.Conv_R_Un: // conv.r.un
					t = StackTypeDescription.Float32; // really F, but we're 32 bit.
					break;
				case Code.Conv_Ovf_U1_Un: // conv.ovf.u1.un
				case Code.Conv_U1: // conv.u1
				case Code.Conv_Ovf_U1: // conv.ovf.u1
					t = StackTypeDescription.UInt8;
					break;
				case Code.Conv_Ovf_U2_Un: // conv.ovf.u2.un
				case Code.Conv_U2: // conv.u2
				case Code.Conv_Ovf_U2: // conv.ovf.u2
					t = StackTypeDescription.UInt16;
					break;
				case Code.Conv_U8: // conv.u8
				case Code.Conv_Ovf_U8_Un: // conv.ovf.u8.un
				case Code.Conv_Ovf_U8: // conv.ovf.u8
					t = StackTypeDescription.UInt64;
					break;
				case Code.Conv_I: // conv.i
				case Code.Conv_Ovf_I: // conv.ovf.i
				case Code.Conv_Ovf_I_Un: // conv.ovf.i.un
					t = StackTypeDescription.NativeInt;
					break;
				case Code.Conv_Ovf_U_Un: // conv.ovf.u.un
				case Code.Conv_Ovf_U: // conv.ovf.u
				case Code.Conv_U: // conv.u
					t = StackTypeDescription.NativeUInt;
					break;
				case Code.Box: // box
					throw new NotImplementedException();
				case Code.Newarr: // newarr
					throw new NotImplementedException();
				case Code.Ldlen: // ldlen
					t = StackTypeDescription.NativeUInt;
					break;
				case Code.Ldelema: // ldelema
					throw new NotImplementedException();
				case Code.Ldelem_I1: // ldelem.i1
					t = StackTypeDescription.Int8;
					break;
				case Code.Ldelem_U1: // ldelem.u1
					t = StackTypeDescription.UInt8;
					break;
				case Code.Ldelem_I2: // ldelem.i2
					t = StackTypeDescription.Int32;
					break;
				case Code.Ldelem_U2: // ldelem.u2
					t = StackTypeDescription.UInt16;
					break;
				case Code.Ldelem_I4: // ldelem.i4
					t = StackTypeDescription.Int32;
					break;
				case Code.Ldelem_U4: // ldelem.u4
					t = StackTypeDescription.UInt32;
					break;
				case Code.Ldelem_I8: // ldelem.i8
					t = StackTypeDescription.Int64;
					break;
				case Code.Ldelem_I: // ldelem.i
					// Guess this can also be unsigned?
					t = StackTypeDescription.NativeInt;
					break;
				case Code.Ldelem_R4: // ldelem.r4
					t = StackTypeDescription.Float32;
					break;
				case Code.Ldelem_R8: // ldelem.r8
					t = StackTypeDescription.Float64;
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
					t = StackTypeDescription.None;
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
					t = StackTypeDescription.Int8; // CLI says int32, but let's try...
					break;
				case Code.Ldftn: // ldftn
				case Code.Ldvirtftn: // ldvirtftn
					throw new NotImplementedException();
				case Code.Ldarg: // ldarg
				case Code.Ldloca: // ldloca
					t = GetCilNumericType(optype);
					if (t == StackTypeDescription.None)
						throw new NotImplementedException("Only numeric CIL types are implemented.");

					break;
				case Code.Ldloc: // ldloc
				case Code.Ldarga: // ldarga
					t = GetCilNumericType(optype);
					if (t == StackTypeDescription.None)
						throw new NotImplementedException("Only numeric CIL types are implemented.");

					break;
				case Code.Starg: // starg
				case Code.Stloc: // stloc
					t = StackTypeDescription.None;
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
		}

//		private static Dictionary<uint, CliType> s_metadataCilTypes = BuildBasicMetadataCilDictionary();
//		private static Dictionary<uint, CliType> BuildBasicMetadataCilDictionary()
//		{
//			Dictionary<uint, CliType> dict = new Dictionary<uint, CliType>();
//
//			// TODO: the typeof() token values are not what cecil returns...
//			dict.Add((uint) typeof(sbyte).MetadataToken, CliType.Int8);
//			dict.Add((uint) typeof(byte).MetadataToken, CliType.UInt8);
//			dict.Add((uint) typeof(short).MetadataToken, CliType.Int16);
//			dict.Add((uint) typeof(ushort).MetadataToken, CliType.UInt16);
//			dict.Add((uint) typeof(int).MetadataToken, CliType.Int32);
//			dict.Add((uint) typeof(uint).MetadataToken, CliType.UInt32);
//			dict.Add((uint) typeof(long).MetadataToken, CliType.Int64);
//			dict.Add((uint) typeof(ulong).MetadataToken, CliType.UInt64);
//			dict.Add((uint) typeof(IntPtr).MetadataToken, CliType.NativeInt);
//			dict.Add((uint) typeof(UIntPtr).MetadataToken, CliType.NativeUInt);
//			dict.Add((uint) typeof(float).MetadataToken, CliType.Float32);
//			dict.Add((uint) typeof(double).MetadataToken, CliType.Float64);
//
//			return dict;
//		}


		/// <summary>
		/// If the token is recognized as a CIL numeric type (or bool or char), that type is returned;
		/// otherwise, None is returned.
		/// </summary>
		/// <param name="tref"></param>
		/// <returns></returns>
		private static StackTypeDescription GetCilNumericType(TypeReference tref)
		{
			// Should be a faster way to do the lookup than by name...
			string fullname;

			// Is it a byref type?
			if (tref is ReferenceType)
				fullname = ((ReferenceType) tref).ElementType.FullName;
			else if (tref is PointerType)
			{
				// HACK: pretend it's a managed pointer.
				fullname = ((PointerType) tref).ElementType.FullName;
			}
			else
				fullname = tref.FullName;

			StackTypeDescription std;
			switch (fullname)
			{
				case "System.Boolean":
					std = StackTypeDescription.Int8;
					break;
				case "System.Char":
					std = StackTypeDescription.UInt16;
					break;
				case "System.Byte":
					std = StackTypeDescription.UInt8;
					break;
				case "System.SByte":
					std = StackTypeDescription.Int8;
					break;
				case "System.Short":
					std = StackTypeDescription.Int16;
					break;
				case "System.UShort":
					std = StackTypeDescription.UInt16;
					break;
				case "System.Int32":
					std = StackTypeDescription.Int32;
					break;
				case "System.UInt32":
					std = StackTypeDescription.UInt32;
					break;
				case "System.Int64":
					std = StackTypeDescription.Int64;
					break;
				case "System.UInt64":
					std = StackTypeDescription.UInt64;
					break;
				case "System.Single":
					std = StackTypeDescription.Float32;
					break;
				case "System.Double":
					std = StackTypeDescription.Float64;
					break;
				case "System.IntPtr":
					std = StackTypeDescription.NativeInt;
					break;
				case "System.UIntPtr":
					std = StackTypeDescription.NativeUInt;
					break;
				default:
					return StackTypeDescription.None;
			}

			if (tref is ReferenceType || tref is PointerType)
				std = std.GetByRef();

			return std;
		}

		/// <summary>
		/// Computes the result type of binary numeric operations given the specified input types.
		/// Computation is done according to table 2 and table 7 in the CIL spec plus intuition.
		/// </summary>
		/// <returns></returns>
		private static StackTypeDescription GetNumericResultType(StackTypeDescription tleft, StackTypeDescription tright)
		{
			// We are relying on the fact that the enumeration values are sorted by size.
			if (tleft.CliBasicType == tright.CliBasicType)
			{
				if (!tleft.IsByRef)
					return new StackTypeDescription(tleft.CliBasicType,  
						(CliNumericSize) Math.Max((int)tleft.NumericSize, (int)tright.NumericSize), tleft.IsSigned);
				else
					return StackTypeDescription.NativeInt;
			}

			if (tleft.IsByRef || tright.IsByRef)
			{
				if (tleft.IsByRef && tright.IsByRef)
					return StackTypeDescription.NativeInt;

				return tleft;
			}

			if (tleft.CliBasicType == CliBasicType.NativeInt || tright.CliBasicType == CliBasicType.NativeInt)
				return tleft;

			throw new ArgumentException(
				string.Format("Argument types are not valid cil binary numeric opcodes: Left: {0}; right: {1}.", tleft.CliType, tright.CliType));
		}

		private void BuildBasicBlocks(MethodDefinition method)
		{
			BasicBlock currblock = new BasicBlock();
			List<TreeInstruction> stack = new List<TreeInstruction>();
			List<TreeInstruction> branches = new List<TreeInstruction>();

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
						if (inst.OpCode == OpCodes.Ret)
						{
							// CLI: "The 10 evaluation stack for the current method must be empty except for the value to be returned."
							if (stack.Count > 0)
							{
								treeinst.Left = stack[0];
								if (stack.Count > 1)
									throw new ILException("Stack.Count > 1 ??");
								stack.Clear();
							}							
						}
						else if (inst.OpCode.FlowControl == FlowControl.Call)
						{
							// Build a method call from the stack.
							MethodReference mr = (MethodReference) inst.Operand;
							if (stack.Count < mr.Parameters.Count)
								throw new ILException("Too few parameters on stack.");

							MethodCallInstruction mci = new MethodCallInstruction(mr, inst.OpCode);
							mci.Offset = inst.Offset;
							for (int i = 0; i < mr.Parameters.Count; i++)
							{
								mci.Parameters.Add(stack[stack.Count - mr.Parameters.Count + i]);
							}
							stack.RemoveRange(stack.Count - mr.Parameters.Count, mr.Parameters.Count);

							// HACK: Only works for non-void methods.
							pushcount = 1;

							treeinst = mci;
						}
						else 
							throw new Exception("Unknown VarPop.");
//							throw new Exception("Method calls are not supported.");
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

		/// <summary>
		/// Returns the number of values pushed by the opcode. -1 is returned for function calls.
		/// </summary>
		/// <param name="code"></param>
		/// <returns></returns>
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
					pushCount = 1;
					break;
				case StackBehaviour.Push1_push1:
					pushCount = 2;
					break;
				case StackBehaviour.Varpush:
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
