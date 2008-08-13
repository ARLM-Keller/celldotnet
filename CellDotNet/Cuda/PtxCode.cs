﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellDotNet.Cuda
{
	enum PtxVariations
	{
		None,
//		U16,
//		S16,
		U32,
		S32,
		U64,
		S64,
		F32,
		F64,
	}

	enum PtxCode
	{
		None,
		Add_S32,
		Add_F32,
		Sub_S32,
		Sub_F32,
		Mul_S32,
		Mul_F32,
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

	}
}
