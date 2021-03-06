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
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;

namespace CellDotNet.Intermediate
{
	/// <summary>
	/// Used to derive types in the IR tree.
	/// </summary>
	class TypeDeriver
	{
		private readonly TypeCache _typecache = new TypeCache();

		/// <summary>
		/// Translates the type reference to a <see cref="TypeDescription"/>.
		/// </summary>
		/// <param name="reference"></param>
		/// <returns></returns>
		private TypeDescription GetTypeDescription(Type reference)
		{
			return _typecache.GetTypeDescription(reference);
		}

		/// <summary>
		/// Creates and caches <see cref="TypeDescription"/> objects.
		/// </summary>
		public class TypeCache
		{
			/// <summary>
			/// Key is assembly name and full type name.
			/// </summary>
			private Dictionary<Type, TypeDescription> _history;

			public TypeCache()
			{
				_history = new Dictionary<Type, TypeDescription>();
			}

			public TypeDescription GetTypeDescription(Type type)
			{
				//				if (!type.IsPrimitive)
				//					throw new ArgumentException();

				TypeDescription desc;
				if (_history.TryGetValue(type, out desc))
					return desc;

				desc = CreateTypeDescription(type);
				_history.Add(type, desc);

				return desc;
			}

			private TypeDescription CreateTypeDescription(Type type)
			{
				// TODO: This reference stuff is wrong.
				if (type.IsByRef)
					return new TypeDescription(type.MakeByRefType());
				else if (type.IsPointer)
					return new TypeDescription(type.MakePointerType());
				else if (type.IsGenericType)
					throw new NotImplementedException("Generic types are not yet implemented.");
				else
					return new TypeDescription(type);
			}
		}

		/// <summary>
		/// Derives the type from the instruction's immediate children, ie. it is not recursive.
		/// </summary>
		/// <param name="inst"></param>
		public void DeriveType(TreeInstruction inst)
		{
			StackTypeDescription t;

			switch (inst.Opcode.FlowControl)
			{
				case FlowControl.Branch:
				case FlowControl.Break:
					t = StackTypeDescription.None;
					break;
				case FlowControl.Call:
					{
						MethodCallInstruction mci = (MethodCallInstruction)inst;

						foreach (TreeInstruction param in mci.Parameters)
							DeriveType(param);

						MethodBase methodBase = mci.IntrinsicMethod ?? mci.OperandMethod;
						MethodInfo mi = methodBase as MethodInfo;
						if (mci.Opcode == IROpCodes.Newobj || mci.Opcode == IROpCodes.IntrinsicNewObj)
							t = GetStackTypeDescription(methodBase.DeclaringType);
						else if (mi != null && mi.ReturnType != typeof (void))
							t = GetStackTypeDescription(mi.ReturnType);
						else
							t = StackTypeDescription.None;
					}
					break;
				case FlowControl.Cond_Branch:
					t = StackTypeDescription.None;
					break;
				//				case FlowControl.Meta:
				//				case FlowControl.Phi:
				//					throw new ILException("Meta or Phi.");
				case FlowControl.Next:
					try
					{
						t = DeriveTypeForFlowNext(inst);
					}
					catch (NotImplementedException e)
					{
						throw new NotImplementedException("Error while deriving flow instruction opcode: " + inst.Opcode.IRCode, e);
					}
					break;
				case FlowControl.Return:
					TreeInstruction firstchild;
					firstchild = inst.GetChildInstructions().FirstOrDefault();
					if (firstchild != null)
						t = inst.Left.StackType;
					else
						t = StackTypeDescription.None;
					break;
				case FlowControl.Throw:
					t = StackTypeDescription.None;
					break;
				default:
					throw new ILSemanticErrorException("Invalid FlowControl: " + inst.Opcode.FlowControl);
			}

			inst.StackType = t;
		}

		/// <summary>
		/// Used by <see cref="DeriveType"/> to derive types for instructions 
		/// that do not change the flow; that is, OpCode.Flow == Next.
		/// </summary>
		/// <param name="inst"></param>
		private StackTypeDescription DeriveTypeForFlowNext(TreeInstruction inst)
		{
			// The cases are generated and all opcodes with flow==next are present 
			// (except macro codes such as ldc.i4.3).
			StackTypeDescription t;

			switch (inst.Opcode.IRCode)
			{
				case IRCode.Nop: // nop
					t = StackTypeDescription.None;
					break;
				case IRCode.Ldnull: // ldnull
					t = StackTypeDescription.ObjectType;
					break;
				case IRCode.Ldc_I4: // ldc.i4
					t = StackTypeDescription.Int32;
					break;
				case IRCode.Ldc_I8: // ldc.i8
					t = StackTypeDescription.Int64;
					break;
				case IRCode.Ldc_R4: // ldc.r4
					t = StackTypeDescription.Float32;
					break;
				case IRCode.Ldc_R8: // ldc.r8
					t = StackTypeDescription.Float64;
					break;
				case IRCode.Dup: // dup
					throw new NotImplementedException("dup, pop");
				case IRCode.Pop: // pop
					t = StackTypeDescription.None;
					break;
				case IRCode.Ldind_I1: // ldind.i1
				case IRCode.Ldind_U1: // ldind.u1
				case IRCode.Ldind_I2: // ldind.i2
				case IRCode.Ldind_U2: // ldind.u2
				case IRCode.Ldind_I4: // ldind.i4
				case IRCode.Ldind_U4: // ldind.u4
					t = StackTypeDescription.Int32;
					break;
				case IRCode.Ldind_I8: // ldind.i8
					t = StackTypeDescription.Int64;
					break;
				case IRCode.Ldind_I: // ldind.i
					t = StackTypeDescription.NativeInt;
					break;
				case IRCode.Ldind_R4: // ldind.r4
					t = StackTypeDescription.Float32;
					break;
				case IRCode.Ldind_R8: // ldind.r8
					t = StackTypeDescription.Float64;
					break;
				case IRCode.Ldind_Ref: // ldind.ref
					t = inst.Left.StackType.Dereference();
					break;
				case IRCode.Stind_Ref: // stind.ref
				case IRCode.Stind_I1: // stind.i1
				case IRCode.Stind_I2: // stind.i2
				case IRCode.Stind_I4: // stind.i4
				case IRCode.Stind_I8: // stind.i8
				case IRCode.Stind_R4: // stind.r4
				case IRCode.Stind_R8: // stind.r8
					t = StackTypeDescription.None;
					break;
				case IRCode.Add: // add
				case IRCode.Div: // div
				case IRCode.Sub: // sub
				case IRCode.Mul: // mul
				case IRCode.Rem: // rem

				case IRCode.Add_Ovf: // add.ovf
				case IRCode.Add_Ovf_Un: // add.ovf.un
				case IRCode.Mul_Ovf: // mul.ovf
				case IRCode.Mul_Ovf_Un: // mul.ovf.un
				case IRCode.Sub_Ovf: // sub.ovf
				case IRCode.Sub_Ovf_Un: // sub.ovf.un
					t = GetNumericResultType(inst.Left.StackType, inst.Right.StackType);
					break;
				case IRCode.And: // and
				case IRCode.Div_Un: // div.un
				case IRCode.Not: // not
				case IRCode.Or: // or
				case IRCode.Rem_Un: // rem.un
				case IRCode.Xor: // xor
					// From CIL table 5.
					if (inst.Left.StackType == inst.Right.StackType)
						t = inst.Left.StackType;
					else
					{
						if ((inst.Left.StackType == StackTypeDescription.Int32 && inst.Right.StackType == StackTypeDescription.NativeInt) ||
							(inst.Left.StackType == StackTypeDescription.NativeInt && inst.Right.StackType == StackTypeDescription.Int32))
							t = StackTypeDescription.NativeInt;
						else
							throw new InvalidIRTreeException();
					}
					break;
				case IRCode.Shl: // shl
				case IRCode.Shr: // shr
				case IRCode.Shr_Un: // shr.un
					// CIL table 6.
					t = inst.Left.StackType;
					break;
				case IRCode.Neg: // neg
					t = inst.Left.StackType;
					break;
				case IRCode.Cpobj: // cpobj
					throw new NotImplementedException(inst.Opcode.IRCode.ToString());
				case IRCode.Ldobj: // ldobj
					t = inst.Left.StackType.Dereference();
					break;
				case IRCode.Stfld: // stfld
					t = StackTypeDescription.None;
					break;
				case IRCode.Ldfld: // ldfld
					t = GetStackTypeDescription(inst.OperandAsField.FieldType);
					break;
				case IRCode.Ldstr: // ldstr
				case IRCode.Castclass: // castclass
				case IRCode.Isinst: // isinst
				case IRCode.Unbox: // unbox
				case IRCode.Ldflda: // ldflda
				case IRCode.Ldsfld: // ldsfld
				case IRCode.Ldsflda: // ldsflda
				case IRCode.Stsfld: // stsfld
					throw new NotImplementedException(inst.Opcode.IRCode.ToString());
				case IRCode.Stobj: // stobj
					t = StackTypeDescription.None;
					break;
				case IRCode.Conv_Ovf_I8_Un: // conv.ovf.i8.un
				case IRCode.Conv_I8: // conv.i8
				case IRCode.Conv_Ovf_I8: // conv.ovf.i8
					t = StackTypeDescription.Int64;
					break;
				case IRCode.Conv_R4: // conv.r4
					t = StackTypeDescription.Float32;
					break;
				case IRCode.Conv_R8: // conv.r8
					t = StackTypeDescription.Float64;
					break;
				case IRCode.Conv_I1: // conv.i1
				case IRCode.Conv_Ovf_I1_Un: // conv.ovf.i1.un
				case IRCode.Conv_Ovf_I1: // conv.ovf.i1
					t = StackTypeDescription.Int32;
					break;
				case IRCode.Conv_I2: // conv.i2
				case IRCode.Conv_Ovf_I2: // conv.ovf.i2
				case IRCode.Conv_Ovf_I2_Un: // conv.ovf.i2.un
					t = StackTypeDescription.Int32;
					break;
				case IRCode.Conv_Ovf_I4: // conv.ovf.i4
				case IRCode.Conv_I4: // conv.i4
				case IRCode.Conv_Ovf_I4_Un: // conv.ovf.i4.un
					t = StackTypeDescription.Int32;
					break;
				case IRCode.Conv_U4: // conv.u4
				case IRCode.Conv_Ovf_U4: // conv.ovf.u4
				case IRCode.Conv_Ovf_U4_Un: // conv.ovf.u4.un
					t = StackTypeDescription.Int32;
					break;
				case IRCode.Conv_R_Un: // conv.r.un
					t = StackTypeDescription.Float32; // really F, but we're 32 bit.
					break;
				case IRCode.Conv_Ovf_U1_Un: // conv.ovf.u1.un
				case IRCode.Conv_U1: // conv.u1
				case IRCode.Conv_Ovf_U1: // conv.ovf.u1
					t = StackTypeDescription.Int32;
					break;
				case IRCode.Conv_Ovf_U2_Un: // conv.ovf.u2.un
				case IRCode.Conv_U2: // conv.u2
				case IRCode.Conv_Ovf_U2: // conv.ovf.u2
					t = StackTypeDescription.Int32;
					break;
				case IRCode.Conv_U8: // conv.u8
				case IRCode.Conv_Ovf_U8_Un: // conv.ovf.u8.un
				case IRCode.Conv_Ovf_U8: // conv.ovf.u8
					t = StackTypeDescription.Int64;
					break;
				case IRCode.Conv_I: // conv.i
				case IRCode.Conv_Ovf_I: // conv.ovf.i
				case IRCode.Conv_Ovf_I_Un: // conv.ovf.i.un
					t = StackTypeDescription.NativeInt;
					break;
				case IRCode.Conv_Ovf_U_Un: // conv.ovf.u.un
				case IRCode.Conv_Ovf_U: // conv.ovf.u
				case IRCode.Conv_U: // conv.u
					t = StackTypeDescription.NativeInt;
					break;
				case IRCode.Box: // box
					throw new NotImplementedException();
				case IRCode.Newarr: // newarr
					{
						StackTypeDescription elementtype = (StackTypeDescription) inst.Operand;
						t = elementtype.GetArrayType();
						break;
					}
				case IRCode.Ldlen: // ldlen
					t = StackTypeDescription.NativeInt;
					break;
				case IRCode.Ldelema: // ldelema
					t = ((StackTypeDescription) inst.Operand).GetManagedPointer();
					break;
				case IRCode.Ldelem_I1: // ldelem.i1
					t = StackTypeDescription.Int32;
					break;
				case IRCode.Ldelem_U1: // ldelem.u1
					t = StackTypeDescription.Int32;
					break;
				case IRCode.Ldelem_I2: // ldelem.i2
					t = StackTypeDescription.Int32;
					break;
				case IRCode.Ldelem_U2: // ldelem.u2
					t = StackTypeDescription.Int32;
					break;
				case IRCode.Ldelem_I4: // ldelem.i4
					t = StackTypeDescription.Int32;
					break;
				case IRCode.Ldelem_U4: // ldelem.u4
					t = StackTypeDescription.Int32;
					break;
				case IRCode.Ldelem_I8: // ldelem.i8
					t = StackTypeDescription.Int64;
					break;
				case IRCode.Ldelem_I: // ldelem.i
					// Guess this can also be unsigned?
					t = StackTypeDescription.NativeInt;
					break;
				case IRCode.Ldelem_R4: // ldelem.r4
					t = StackTypeDescription.Float32;
					break;
				case IRCode.Ldelem_R8: // ldelem.r8
					t = StackTypeDescription.Float64;
					break;
				case IRCode.Ldelem_Ref: // ldelem.ref
					throw new NotImplementedException();
				case IRCode.Stelem_I: // stelem.i
				case IRCode.Stelem_I1: // stelem.i1
				case IRCode.Stelem_I2: // stelem.i2
				case IRCode.Stelem_I4: // stelem.i4
				case IRCode.Stelem_I8: // stelem.i8
				case IRCode.Stelem_R4: // stelem.r4
				case IRCode.Stelem_R8: // stelem.r8
				case IRCode.Stelem_Ref: // stelem.ref
					t = StackTypeDescription.None;
					break;
				//				case IRCode.Ldelem_Any: // ldelem.any
				//				case IRCode.Stelem_Any: // stelem.any
				//					throw new ILException("ldelem_any and stelem_any are invalid.");
				case IRCode.Unbox_Any: // unbox.any
				case IRCode.Refanyval: // refanyval
				case IRCode.Ckfinite: // ckfinite
				case IRCode.Mkrefany: // mkrefany
				case IRCode.Ldtoken: // ldtoken
				case IRCode.Stind_I: // stind.i
				case IRCode.Arglist: // arglist
					throw new NotImplementedException();
				case IRCode.Ceq: // ceq
				case IRCode.Cgt: // cgt
				case IRCode.Cgt_Un: // cgt.un
				case IRCode.Clt: // clt
				case IRCode.Clt_Un: // clt.un
					t = StackTypeDescription.Int32; // CLI says int32, but let's try...
					break;
				case IRCode.Ldftn: // ldftn
				case IRCode.Ldvirtftn: // ldvirtftn
					throw new NotImplementedException();
				case IRCode.Ldarg: // ldarg
				case IRCode.Ldloca: // ldloca
				case IRCode.Ldloc: // ldloc
				case IRCode.Ldarga: // ldarga
					t = ((MethodVariable) inst.Operand).StackType;
					if (t == StackTypeDescription.None)
						throw new NotImplementedException("Invalid variable stack type for load instruction: None.");
					if (inst.Opcode.IRCode == IRCode.Ldloca || inst.Opcode.IRCode == IRCode.Ldarga)
						t = t.GetManagedPointer();

					break;
				case IRCode.Starg: // starg
				case IRCode.Stloc: // stloc
					t = StackTypeDescription.None;
					break;
				case IRCode.Localloc: // localloc
					throw new NotImplementedException();
				case IRCode.Initobj: // initobj
					t = StackTypeDescription.None;
					break;
				case IRCode.Constrained: // constrained.
				case IRCode.Cpblk: // cpblk
				case IRCode.Initblk: // initblk
				//				case IRCode.No: // no.
				case IRCode.Sizeof: // sizeof
				case IRCode.Refanytype: // refanytype
				case IRCode.Readonly: // readonly.
					throw new NotImplementedException();
				default:
					throw new ILSemanticErrorException();
			}

			return t;
		}

		private static readonly Dictionary<Type, StackTypeDescription> s_metadataCilTypes = new Dictionary<Type, StackTypeDescription>
			           	{
			           		{typeof (bool), StackTypeDescription.Int32},
			           		{typeof (sbyte), StackTypeDescription.Int32},
			           		{typeof (byte), StackTypeDescription.Int32},
			           		{typeof (short), StackTypeDescription.Int32},
			           		{typeof (ushort), StackTypeDescription.Int32},
			           		{typeof (char), StackTypeDescription.Int32},
			           		{typeof (int), StackTypeDescription.Int32},
			           		{typeof (uint), StackTypeDescription.Int32},
			           		{typeof (long), StackTypeDescription.Int64},
			           		{typeof (ulong), StackTypeDescription.Int64},
			           		{typeof (IntPtr), StackTypeDescription.NativeInt},
			           		{typeof (UIntPtr), StackTypeDescription.NativeInt},
			           		{typeof (float), StackTypeDescription.Float32},
			           		{typeof (double), StackTypeDescription.Float64},
			           		{typeof (VectorI4), StackTypeDescription.VectorI4},
			           		{typeof (VectorF4), StackTypeDescription.VectorF4},
			           		{typeof (VectorD2), StackTypeDescription.VectorD2}
			           	};



		/// <summary>
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public StackTypeDescription GetStackTypeDescription(Type type)
		{
			if (type.IsPointer)
				throw new NotSupportedException();

			Type realtype;

			if (type.IsByRef)
				realtype = type.GetElementType();
			else
				realtype = type;


			StackTypeDescription std;
			if (realtype.IsPrimitive)
			{
				std = s_metadataCilTypes[realtype];
			}
			else if(realtype == typeof(VectorI4))
				std = StackTypeDescription.VectorI4;
			else if(realtype == typeof(VectorF4))
				std = StackTypeDescription.VectorF4;
			else if(realtype == typeof(VectorD2))
				std = StackTypeDescription.VectorD2;
			else if (realtype == typeof(void))
				std = StackTypeDescription.None;
			else if (realtype.IsArray)
			{
				Type elementtype = realtype.GetElementType();

				if (realtype.GetArrayRank() != 1)
					throw new NotSupportedException("Only 1D arrays are supported.");
				if (!elementtype.IsValueType || !(elementtype.IsPrimitive || elementtype.Equals(typeof(VectorI4)) || elementtype.Equals(typeof(VectorF4))))
					throw new NotSupportedException("Only 1D primitive value type arrays are supported.");

				StackTypeDescription elementstd = s_metadataCilTypes[elementtype];
				return elementstd.GetArrayType();
			}
			else
			{
				TypeDescription td = GetTypeDescription(realtype);
				std = new StackTypeDescription(td);
			}


			if (type.IsByRef)
				std = std.GetManagedPointer();

			return std;
		}

		static private readonly CliType[,] s_binaryNumericOps = GetBinaryOpsTypeTable();
		static CliType[,] GetBinaryOpsTypeTable()
		{
			// The diagonal.
			CliType[,] table = new CliType[(int)CliType.ManagedPointer + 1, (int)CliType.ManagedPointer + 1];
			table[(int) CliType.Int32, (int) CliType.Int32] = CliType.Int32;
			table[(int) CliType.Int64, (int) CliType.Int64] = CliType.Int64;
			table[(int) CliType.NativeInt, (int) CliType.NativeInt] = CliType.NativeInt;
			table[(int) CliType.Float32, (int) CliType.Float32] = CliType.Float32;
			table[(int) CliType.Float64, (int) CliType.Float64] = CliType.Float64;

			
			table[(int) CliType.Int32, (int) CliType.NativeInt] = CliType.NativeInt;
			table[(int) CliType.NativeInt, (int) CliType.Int32] = CliType.NativeInt;
			table[(int) CliType.ManagedPointer, (int) CliType.Int32] = CliType.ManagedPointer;
			table[(int) CliType.Int32, (int) CliType.ManagedPointer] = CliType.ManagedPointer;
			table[(int) CliType.NativeInt, (int) CliType.Int32] = CliType.NativeInt;
			table[(int) CliType.Int32, (int) CliType.NativeInt] = CliType.NativeInt;

			return table;
		}

		/// <summary>
		/// Computes the result type of binary numeric operations given the specified input types.
		/// Computation is done according to table 2 and table 7 in the CIL spec plus intuition.
		/// </summary>
		/// <returns></returns>
		internal static StackTypeDescription GetNumericResultType(StackTypeDescription tleft, StackTypeDescription tright)
		{
			return StackTypeDescription.FromCliType(s_binaryNumericOps[(int) tleft.CliType, (int) tright.CliType]);
		}
	}
}
