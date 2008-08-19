using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CellDotNet.Cuda
{
	[TestFixture]
	public class PtxCompilerTest : UnitTest
	{
		[Test]
		public void Test()
		{
			string ptx =
				@"
	.version 1.2
	.target sm_10, map_f64_to_f32

	.reg .u32 %ra<17>;
	.reg .u64 %rda<17>;
	.reg .f32 %fa<17>;
	.reg .f64 %fda<17>;
	.reg .u32 %rv<5>;
	.reg .u64 %rdv<5>;
	.reg .f32 %fv<5>;
	.reg .f64 %fdv<5>;	


	.entry __globfunc__Z3xxxPifii
	{
	.reg .u32 %r<7>;
	.param .u32 __cudaparm___globfunc__Z3xxxPifii_arr;
	.param .f32 __cudaparm___globfunc__Z3xxxPifii_f;
	.param .s32 __cudaparm___globfunc__Z3xxxPifii_index;
	.param .s32 __cudaparm___globfunc__Z3xxxPifii_val;
$LBB1___globfunc__Z3xxxPifii:
	ld.param.s32 	%r1, [__cudaparm___globfunc__Z3xxxPifii_val];
	ld.param.u32 	%r2, [__cudaparm___globfunc__Z3xxxPifii_arr];
	ld.param.u32 	%r3, [__cudaparm___globfunc__Z3xxxPifii_index];
	mul.lo.u32 	%r4, %r3, 4;
	add.u32 	%r5, %r2, %r4;
	st.global.s32 	[%r5+0], %r1;
	exit;
$LDWend___globfunc__Z3xxxPifii:
	}


";
			var cubin = new PtxCompiler().Compile(ptx);
			IsNotNull(cubin);
			IsTrue(cubin != "");
			IsTrue(cubin.StartsWith("architecture"));
		}
	}
}
