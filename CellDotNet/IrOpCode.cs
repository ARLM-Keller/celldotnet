using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// IR-codes for linear representation; that is, 3-register instructions.
	/// TODO: How do we/should we represent the SPU 4-register multiply-add instruction?
	/// </summary>
	enum IrOpCode
	{
//		cee: Nop,
		Nop,
//		cee: Break,
//		cee: Ldarg_0,
//		cee: Ldarg_1,
//		cee: Ldarg_2,
//		cee: Ldarg_3,
//		cee: Ldloc_0,
//		cee: Ldloc_1,
//		cee: Ldloc_2,
//		cee: Ldloc_3,
//		cee: Stloc_0,
//		cee: Stloc_1,
//		cee: Stloc_2,
//		cee: Stloc_3,
//		cee: Ldarg_S,
//		cee: Ldarga_S,
//		cee: Starg_S,
//		cee: Ldloc_S,
//		cee: Ldloca_S,
//		cee: Stloc_S,
//		cee: Ldnull,
//		cee: Ldc_I4_M1,
//		cee: Ldc_I4_0,
//		cee: Ldc_I4_1,
//		cee: Ldc_I4_2,
//		cee: Ldc_I4_3,
//		cee: Ldc_I4_4,
//		cee: Ldc_I4_5,
//		cee: Ldc_I4_6,
//		cee: Ldc_I4_7,
//		cee: Ldc_I4_8,
//		cee: Ldc_I4_S,
//		cee: Ldc_I4,
//		cee: Ldc_I8,
//		cee: Ldc_R4,
//		cee: Ldc_R8,
//		cee: Dup,
//		cee: Pop,
//		cee: Jmp,
//		cee: Call,
//		cee: Calli,
//		cee: Ret,
//		cee: Br_S,
//		cee: Brfalse_S,
//		cee: Brtrue_S,
//		cee: Beq_S,
//		cee: Bge_S,
//		cee: Bgt_S,
//		cee: Ble_S,
//		cee: Blt_S,
//		cee: Bne_Un_S,
//		cee: Bge_Un_S,
//		cee: Bgt_Un_S,
//		cee: Ble_Un_S,
//		cee: Blt_Un_S,
//		cee: Br,
//		cee: Brfalse,
//		cee: Brtrue,
//		cee: Beq,
//		cee: Bge,
//		cee: Bgt,
//		cee: Ble,
//		cee: Blt,
//		cee: Bne_Un,
//		cee: Bge_Un,
//		cee: Bgt_Un,
//		cee: Ble_Un,
//		cee: Blt_Un,
//		cee: Switch,
//		cee: Ldind_I1,
//		cee: Ldind_U1,
//		cee: Ldind_I2,
//		cee: Ldind_U2,
//		cee: Ldind_I4,
//		cee: Ldind_U4,
//		cee: Ldind_I8,
//		cee: Ldind_I,
//		cee: Ldind_R4,
//		cee: Ldind_R8,
//		cee: Ldind_Ref,
//		cee: Stind_Ref,
//		cee: Stind_I1,
//		cee: Stind_I2,
//		cee: Stind_I4,
//		cee: Stind_I8,
//		cee: Stind_R4,
//		cee: Stind_R8,

//		cee: Add,
		AddI,
		AddL,
		Add

//		cee: Sub,
//		cee: Mul,
//		cee: Div,
//		cee: Div_Un,
//		cee: Rem,
//		cee: Rem_Un,
//		cee: And,
//		cee: Or,
//		cee: Xor,
//		cee: Shl,
//		cee: Shr,
//		cee: Shr_Un,
//		cee: Neg,
//		cee: Not,
//		cee: Conv_I1,
//		cee: Conv_I2,
//		cee: Conv_I4,
//		cee: Conv_I8,
//		cee: Conv_R4,
//		cee: Conv_R8,
//		cee: Conv_U4,
//		cee: Conv_U8,
//		cee: Callvirt,
//		cee: Cpobj,
//		cee: Ldobj,
//		cee: Ldstr,
//		cee: Newobj,
//		cee: Castclass,
//		cee: Isinst,
//		cee: Conv_R_Un,
//		cee: Unbox,
//		cee: Throw,
//		cee: Ldfld,
//		cee: Ldflda,
//		cee: Stfld,
//		cee: Ldsfld,
//		cee: Ldsflda,
//		cee: Stsfld,
//		cee: Stobj,
//		cee: Conv_Ovf_I1_Un,
//		cee: Conv_Ovf_I2_Un,
//		cee: Conv_Ovf_I4_Un,
//		cee: Conv_Ovf_I8_Un,
//		cee: Conv_Ovf_U1_Un,
//		cee: Conv_Ovf_U2_Un,
//		cee: Conv_Ovf_U4_Un,
//		cee: Conv_Ovf_U8_Un,
//		cee: Conv_Ovf_I_Un,
//		cee: Conv_Ovf_U_Un,
//		cee: Box,
//		cee: Newarr,
//		cee: Ldlen,
//		cee: Ldelema,
//		cee: Ldelem_I1,
//		cee: Ldelem_U1,
//		cee: Ldelem_I2,
//		cee: Ldelem_U2,
//		cee: Ldelem_I4,
//		cee: Ldelem_U4,
//		cee: Ldelem_I8,
//		cee: Ldelem_I,
//		cee: Ldelem_R4,
//		cee: Ldelem_R8,
//		cee: Ldelem_Ref,
//		cee: Stelem_I,
//		cee: Stelem_I1,
//		cee: Stelem_I2,
//		cee: Stelem_I4,
//		cee: Stelem_I8,
//		cee: Stelem_R4,
//		cee: Stelem_R8,
//		cee: Stelem_Ref,
//		cee: Ldelem_Any,
//		cee: Stelem_Any,
//		cee: Unbox_Any,
//		cee: Conv_Ovf_I1,
//		cee: Conv_Ovf_U1,
//		cee: Conv_Ovf_I2,
//		cee: Conv_Ovf_U2,
//		cee: Conv_Ovf_I4,
//		cee: Conv_Ovf_U4,
//		cee: Conv_Ovf_I8,
//		cee: Conv_Ovf_U8,
//		cee: Refanyval,
//		cee: Ckfinite,
//		cee: Mkrefany,
//		cee: Ldtoken,
//		cee: Conv_U2,
//		cee: Conv_U1,
//		cee: Conv_I,
//		cee: Conv_Ovf_I,
//		cee: Conv_Ovf_U,
//		cee: Add_Ovf,
//		cee: Add_Ovf_Un,
//		cee: Mul_Ovf,
//		cee: Mul_Ovf_Un,
//		cee: Sub_Ovf,
//		cee: Sub_Ovf_Un,
//		cee: Endfinally,
//		cee: Leave,
//		cee: Leave_S,
//		cee: Stind_I,
//		cee: Conv_U,
//		cee: Arglist,
//		cee: Ceq,
//		cee: Cgt,
//		cee: Cgt_Un,
//		cee: Clt,
//		cee: Clt_Un,
//		cee: Ldftn,
//		cee: Ldvirtftn,
//		cee: Ldarg,
//		cee: Ldarga,
//		cee: Starg,
//		cee: Ldloc,
//		cee: Ldloca,
//		cee: Stloc,
//		cee: Localloc,
//		cee: Endfilter,
//		cee: Unaligned,
//		cee: Volatile,
//		cee: Tail,
//		cee: Initobj,
//		cee: Constrained,
//		cee: Cpblk,
//		cee: Initblk,
//		cee: No,
//		cee: Rethrow,
//		cee: Sizeof,
//		cee: Refanytype,
//		cee: Readonly,

	}
}