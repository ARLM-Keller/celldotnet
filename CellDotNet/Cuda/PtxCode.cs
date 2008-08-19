using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellDotNet.Cuda
{
	enum PtxCode
	{
		None,
		/// <summary>
		/// Not a PTX instruction, but this ensures that PTX and IR opcodes do not overlap and cause confusion.
		/// </summary>
		Ptx_First = 500,
		Add_S32,
		Add_F32,
		Sub_S32,
		Sub_F32,
		Mul_Lo_S32,
		Mul_Lo_F32,
		Div_S32,
		Div_F32,
		Rem_S32,
		Abs_S32,
		Abs_F32,
		Neg_S32,
		Neg_F32,
		Min_S32,
		Min_F32,
		Max_S32,
		Max_F32,

		Bra,
		Ld_Param_S32,
		Ld_Param_F32,
		Ret,
		Call,
		Exit,
		Mov,
		Shl_S32,
		St_Global_S32,
		St_Global_F32
	}
}
