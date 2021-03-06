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
using System.Reflection.Emit;

namespace CellDotNet.Intermediate
{
	// This class is generated by CellDotNet.CodeGenUtils. DO NOT EDIT.
	partial class IROpCodes
	{
		public static readonly IROpCode Nop = new IROpCode("nop", IRCode.Nop, FlowControl.Next, OpCodes.Nop);
		public static readonly IROpCode Break = new IROpCode("break", IRCode.Break, FlowControl.Break, OpCodes.Break);
		public static readonly IROpCode Ldnull = new IROpCode("ldnull", IRCode.Ldnull, FlowControl.Next, OpCodes.Ldnull);
		public static readonly IROpCode Ldc_I4 = new IROpCode("ldc.i4", IRCode.Ldc_I4, FlowControl.Next, OpCodes.Ldc_I4);
		public static readonly IROpCode Ldc_I8 = new IROpCode("ldc.i8", IRCode.Ldc_I8, FlowControl.Next, OpCodes.Ldc_I8);
		public static readonly IROpCode Ldc_R4 = new IROpCode("ldc.r4", IRCode.Ldc_R4, FlowControl.Next, OpCodes.Ldc_R4);
		public static readonly IROpCode Ldc_R8 = new IROpCode("ldc.r8", IRCode.Ldc_R8, FlowControl.Next, OpCodes.Ldc_R8);
		public static readonly IROpCode Dup = new IROpCode("dup", IRCode.Dup, FlowControl.Next, OpCodes.Dup);
		public static readonly IROpCode Pop = new IROpCode("pop", IRCode.Pop, FlowControl.Next, OpCodes.Pop);
		public static readonly IROpCode Jmp = new IROpCode("jmp", IRCode.Jmp, FlowControl.Call, OpCodes.Jmp);
		public static readonly IROpCode Call = new IROpCode("call", IRCode.Call, FlowControl.Call, OpCodes.Call);
		public static readonly IROpCode Calli = new IROpCode("calli", IRCode.Calli, FlowControl.Call, OpCodes.Calli);
		public static readonly IROpCode Ret = new IROpCode("ret", IRCode.Ret, FlowControl.Return, OpCodes.Ret);
		public static readonly IROpCode Br = new IROpCode("br", IRCode.Br, FlowControl.Branch, OpCodes.Br);
		public static readonly IROpCode Brfalse = new IROpCode("brfalse", IRCode.Brfalse, FlowControl.Cond_Branch, OpCodes.Brfalse);
		public static readonly IROpCode Brtrue = new IROpCode("brtrue", IRCode.Brtrue, FlowControl.Cond_Branch, OpCodes.Brtrue);
		public static readonly IROpCode Beq = new IROpCode("beq", IRCode.Beq, FlowControl.Cond_Branch, OpCodes.Beq);
		public static readonly IROpCode Bge = new IROpCode("bge", IRCode.Bge, FlowControl.Cond_Branch, OpCodes.Bge);
		public static readonly IROpCode Bgt = new IROpCode("bgt", IRCode.Bgt, FlowControl.Cond_Branch, OpCodes.Bgt);
		public static readonly IROpCode Ble = new IROpCode("ble", IRCode.Ble, FlowControl.Cond_Branch, OpCodes.Ble);
		public static readonly IROpCode Blt = new IROpCode("blt", IRCode.Blt, FlowControl.Cond_Branch, OpCodes.Blt);
		public static readonly IROpCode Bne_Un = new IROpCode("bne.un", IRCode.Bne_Un, FlowControl.Cond_Branch, OpCodes.Bne_Un);
		public static readonly IROpCode Bge_Un = new IROpCode("bge.un", IRCode.Bge_Un, FlowControl.Cond_Branch, OpCodes.Bge_Un);
		public static readonly IROpCode Bgt_Un = new IROpCode("bgt.un", IRCode.Bgt_Un, FlowControl.Cond_Branch, OpCodes.Bgt_Un);
		public static readonly IROpCode Ble_Un = new IROpCode("ble.un", IRCode.Ble_Un, FlowControl.Cond_Branch, OpCodes.Ble_Un);
		public static readonly IROpCode Blt_Un = new IROpCode("blt.un", IRCode.Blt_Un, FlowControl.Cond_Branch, OpCodes.Blt_Un);
		public static readonly IROpCode Switch = new IROpCode("switch", IRCode.Switch, FlowControl.Cond_Branch, OpCodes.Switch);
		public static readonly IROpCode Ldind_I1 = new IROpCode("ldind.i1", IRCode.Ldind_I1, FlowControl.Next, OpCodes.Ldind_I1);
		public static readonly IROpCode Ldind_U1 = new IROpCode("ldind.u1", IRCode.Ldind_U1, FlowControl.Next, OpCodes.Ldind_U1);
		public static readonly IROpCode Ldind_I2 = new IROpCode("ldind.i2", IRCode.Ldind_I2, FlowControl.Next, OpCodes.Ldind_I2);
		public static readonly IROpCode Ldind_U2 = new IROpCode("ldind.u2", IRCode.Ldind_U2, FlowControl.Next, OpCodes.Ldind_U2);
		public static readonly IROpCode Ldind_I4 = new IROpCode("ldind.i4", IRCode.Ldind_I4, FlowControl.Next, OpCodes.Ldind_I4);
		public static readonly IROpCode Ldind_U4 = new IROpCode("ldind.u4", IRCode.Ldind_U4, FlowControl.Next, OpCodes.Ldind_U4);
		public static readonly IROpCode Ldind_I8 = new IROpCode("ldind.i8", IRCode.Ldind_I8, FlowControl.Next, OpCodes.Ldind_I8);
		public static readonly IROpCode Ldind_I = new IROpCode("ldind.i", IRCode.Ldind_I, FlowControl.Next, OpCodes.Ldind_I);
		public static readonly IROpCode Ldind_R4 = new IROpCode("ldind.r4", IRCode.Ldind_R4, FlowControl.Next, OpCodes.Ldind_R4);
		public static readonly IROpCode Ldind_R8 = new IROpCode("ldind.r8", IRCode.Ldind_R8, FlowControl.Next, OpCodes.Ldind_R8);
		public static readonly IROpCode Ldind_Ref = new IROpCode("ldind.ref", IRCode.Ldind_Ref, FlowControl.Next, OpCodes.Ldind_Ref);
		public static readonly IROpCode Stind_Ref = new IROpCode("stind.ref", IRCode.Stind_Ref, FlowControl.Next, OpCodes.Stind_Ref);
		public static readonly IROpCode Stind_I1 = new IROpCode("stind.i1", IRCode.Stind_I1, FlowControl.Next, OpCodes.Stind_I1);
		public static readonly IROpCode Stind_I2 = new IROpCode("stind.i2", IRCode.Stind_I2, FlowControl.Next, OpCodes.Stind_I2);
		public static readonly IROpCode Stind_I4 = new IROpCode("stind.i4", IRCode.Stind_I4, FlowControl.Next, OpCodes.Stind_I4);
		public static readonly IROpCode Stind_I8 = new IROpCode("stind.i8", IRCode.Stind_I8, FlowControl.Next, OpCodes.Stind_I8);
		public static readonly IROpCode Stind_R4 = new IROpCode("stind.r4", IRCode.Stind_R4, FlowControl.Next, OpCodes.Stind_R4);
		public static readonly IROpCode Stind_R8 = new IROpCode("stind.r8", IRCode.Stind_R8, FlowControl.Next, OpCodes.Stind_R8);
		public static readonly IROpCode Add = new IROpCode("add", IRCode.Add, FlowControl.Next, OpCodes.Add);
		public static readonly IROpCode Sub = new IROpCode("sub", IRCode.Sub, FlowControl.Next, OpCodes.Sub);
		public static readonly IROpCode Mul = new IROpCode("mul", IRCode.Mul, FlowControl.Next, OpCodes.Mul);
		public static readonly IROpCode Div = new IROpCode("div", IRCode.Div, FlowControl.Next, OpCodes.Div);
		public static readonly IROpCode Div_Un = new IROpCode("div.un", IRCode.Div_Un, FlowControl.Next, OpCodes.Div_Un);
		public static readonly IROpCode Rem = new IROpCode("rem", IRCode.Rem, FlowControl.Next, OpCodes.Rem);
		public static readonly IROpCode Rem_Un = new IROpCode("rem.un", IRCode.Rem_Un, FlowControl.Next, OpCodes.Rem_Un);
		public static readonly IROpCode And = new IROpCode("and", IRCode.And, FlowControl.Next, OpCodes.And);
		public static readonly IROpCode Or = new IROpCode("or", IRCode.Or, FlowControl.Next, OpCodes.Or);
		public static readonly IROpCode Xor = new IROpCode("xor", IRCode.Xor, FlowControl.Next, OpCodes.Xor);
		public static readonly IROpCode Shl = new IROpCode("shl", IRCode.Shl, FlowControl.Next, OpCodes.Shl);
		public static readonly IROpCode Shr = new IROpCode("shr", IRCode.Shr, FlowControl.Next, OpCodes.Shr);
		public static readonly IROpCode Shr_Un = new IROpCode("shr.un", IRCode.Shr_Un, FlowControl.Next, OpCodes.Shr_Un);
		public static readonly IROpCode Neg = new IROpCode("neg", IRCode.Neg, FlowControl.Next, OpCodes.Neg);
		public static readonly IROpCode Not = new IROpCode("not", IRCode.Not, FlowControl.Next, OpCodes.Not);
		public static readonly IROpCode Conv_I1 = new IROpCode("conv.i1", IRCode.Conv_I1, FlowControl.Next, OpCodes.Conv_I1);
		public static readonly IROpCode Conv_I2 = new IROpCode("conv.i2", IRCode.Conv_I2, FlowControl.Next, OpCodes.Conv_I2);
		public static readonly IROpCode Conv_I4 = new IROpCode("conv.i4", IRCode.Conv_I4, FlowControl.Next, OpCodes.Conv_I4);
		public static readonly IROpCode Conv_I8 = new IROpCode("conv.i8", IRCode.Conv_I8, FlowControl.Next, OpCodes.Conv_I8);
		public static readonly IROpCode Conv_R4 = new IROpCode("conv.r4", IRCode.Conv_R4, FlowControl.Next, OpCodes.Conv_R4);
		public static readonly IROpCode Conv_R8 = new IROpCode("conv.r8", IRCode.Conv_R8, FlowControl.Next, OpCodes.Conv_R8);
		public static readonly IROpCode Conv_U4 = new IROpCode("conv.u4", IRCode.Conv_U4, FlowControl.Next, OpCodes.Conv_U4);
		public static readonly IROpCode Conv_U8 = new IROpCode("conv.u8", IRCode.Conv_U8, FlowControl.Next, OpCodes.Conv_U8);
		public static readonly IROpCode Callvirt = new IROpCode("callvirt", IRCode.Callvirt, FlowControl.Call, OpCodes.Callvirt);
		public static readonly IROpCode Cpobj = new IROpCode("cpobj", IRCode.Cpobj, FlowControl.Next, OpCodes.Cpobj);
		public static readonly IROpCode Ldobj = new IROpCode("ldobj", IRCode.Ldobj, FlowControl.Next, OpCodes.Ldobj);
		public static readonly IROpCode Ldstr = new IROpCode("ldstr", IRCode.Ldstr, FlowControl.Next, OpCodes.Ldstr);
		public static readonly IROpCode Newobj = new IROpCode("newobj", IRCode.Newobj, FlowControl.Call, OpCodes.Newobj);
		public static readonly IROpCode Castclass = new IROpCode("castclass", IRCode.Castclass, FlowControl.Next, OpCodes.Castclass);
		public static readonly IROpCode Isinst = new IROpCode("isinst", IRCode.Isinst, FlowControl.Next, OpCodes.Isinst);
		public static readonly IROpCode Conv_R_Un = new IROpCode("conv.r.un", IRCode.Conv_R_Un, FlowControl.Next, OpCodes.Conv_R_Un);
		public static readonly IROpCode Unbox = new IROpCode("unbox", IRCode.Unbox, FlowControl.Next, OpCodes.Unbox);
		public static readonly IROpCode Throw = new IROpCode("throw", IRCode.Throw, FlowControl.Throw, OpCodes.Throw);
		public static readonly IROpCode Ldfld = new IROpCode("ldfld", IRCode.Ldfld, FlowControl.Next, OpCodes.Ldfld);
		public static readonly IROpCode Ldflda = new IROpCode("ldflda", IRCode.Ldflda, FlowControl.Next, OpCodes.Ldflda);
		public static readonly IROpCode Stfld = new IROpCode("stfld", IRCode.Stfld, FlowControl.Next, OpCodes.Stfld);
		public static readonly IROpCode Ldsfld = new IROpCode("ldsfld", IRCode.Ldsfld, FlowControl.Next, OpCodes.Ldsfld);
		public static readonly IROpCode Ldsflda = new IROpCode("ldsflda", IRCode.Ldsflda, FlowControl.Next, OpCodes.Ldsflda);
		public static readonly IROpCode Stsfld = new IROpCode("stsfld", IRCode.Stsfld, FlowControl.Next, OpCodes.Stsfld);
		public static readonly IROpCode Stobj = new IROpCode("stobj", IRCode.Stobj, FlowControl.Next, OpCodes.Stobj);
		public static readonly IROpCode Conv_Ovf_I1_Un = new IROpCode("conv.ovf.i1.un", IRCode.Conv_Ovf_I1_Un, FlowControl.Next, OpCodes.Conv_Ovf_I1_Un);
		public static readonly IROpCode Conv_Ovf_I2_Un = new IROpCode("conv.ovf.i2.un", IRCode.Conv_Ovf_I2_Un, FlowControl.Next, OpCodes.Conv_Ovf_I2_Un);
		public static readonly IROpCode Conv_Ovf_I4_Un = new IROpCode("conv.ovf.i4.un", IRCode.Conv_Ovf_I4_Un, FlowControl.Next, OpCodes.Conv_Ovf_I4_Un);
		public static readonly IROpCode Conv_Ovf_I8_Un = new IROpCode("conv.ovf.i8.un", IRCode.Conv_Ovf_I8_Un, FlowControl.Next, OpCodes.Conv_Ovf_I8_Un);
		public static readonly IROpCode Conv_Ovf_U1_Un = new IROpCode("conv.ovf.u1.un", IRCode.Conv_Ovf_U1_Un, FlowControl.Next, OpCodes.Conv_Ovf_U1_Un);
		public static readonly IROpCode Conv_Ovf_U2_Un = new IROpCode("conv.ovf.u2.un", IRCode.Conv_Ovf_U2_Un, FlowControl.Next, OpCodes.Conv_Ovf_U2_Un);
		public static readonly IROpCode Conv_Ovf_U4_Un = new IROpCode("conv.ovf.u4.un", IRCode.Conv_Ovf_U4_Un, FlowControl.Next, OpCodes.Conv_Ovf_U4_Un);
		public static readonly IROpCode Conv_Ovf_U8_Un = new IROpCode("conv.ovf.u8.un", IRCode.Conv_Ovf_U8_Un, FlowControl.Next, OpCodes.Conv_Ovf_U8_Un);
		public static readonly IROpCode Conv_Ovf_I_Un = new IROpCode("conv.ovf.i.un", IRCode.Conv_Ovf_I_Un, FlowControl.Next, OpCodes.Conv_Ovf_I_Un);
		public static readonly IROpCode Conv_Ovf_U_Un = new IROpCode("conv.ovf.u.un", IRCode.Conv_Ovf_U_Un, FlowControl.Next, OpCodes.Conv_Ovf_U_Un);
		public static readonly IROpCode Box = new IROpCode("box", IRCode.Box, FlowControl.Next, OpCodes.Box);
		public static readonly IROpCode Newarr = new IROpCode("newarr", IRCode.Newarr, FlowControl.Next, OpCodes.Newarr);
		public static readonly IROpCode Ldlen = new IROpCode("ldlen", IRCode.Ldlen, FlowControl.Next, OpCodes.Ldlen);
		public static readonly IROpCode Ldelema = new IROpCode("ldelema", IRCode.Ldelema, FlowControl.Next, OpCodes.Ldelema);
		public static readonly IROpCode Ldelem_I1 = new IROpCode("ldelem.i1", IRCode.Ldelem_I1, FlowControl.Next, OpCodes.Ldelem_I1);
		public static readonly IROpCode Ldelem_U1 = new IROpCode("ldelem.u1", IRCode.Ldelem_U1, FlowControl.Next, OpCodes.Ldelem_U1);
		public static readonly IROpCode Ldelem_I2 = new IROpCode("ldelem.i2", IRCode.Ldelem_I2, FlowControl.Next, OpCodes.Ldelem_I2);
		public static readonly IROpCode Ldelem_U2 = new IROpCode("ldelem.u2", IRCode.Ldelem_U2, FlowControl.Next, OpCodes.Ldelem_U2);
		public static readonly IROpCode Ldelem_I4 = new IROpCode("ldelem.i4", IRCode.Ldelem_I4, FlowControl.Next, OpCodes.Ldelem_I4);
		public static readonly IROpCode Ldelem_U4 = new IROpCode("ldelem.u4", IRCode.Ldelem_U4, FlowControl.Next, OpCodes.Ldelem_U4);
		public static readonly IROpCode Ldelem_I8 = new IROpCode("ldelem.i8", IRCode.Ldelem_I8, FlowControl.Next, OpCodes.Ldelem_I8);
		public static readonly IROpCode Ldelem_I = new IROpCode("ldelem.i", IRCode.Ldelem_I, FlowControl.Next, OpCodes.Ldelem_I);
		public static readonly IROpCode Ldelem_R4 = new IROpCode("ldelem.r4", IRCode.Ldelem_R4, FlowControl.Next, OpCodes.Ldelem_R4);
		public static readonly IROpCode Ldelem_R8 = new IROpCode("ldelem.r8", IRCode.Ldelem_R8, FlowControl.Next, OpCodes.Ldelem_R8);
		public static readonly IROpCode Ldelem_Ref = new IROpCode("ldelem.ref", IRCode.Ldelem_Ref, FlowControl.Next, OpCodes.Ldelem_Ref);
		public static readonly IROpCode Stelem_I = new IROpCode("stelem.i", IRCode.Stelem_I, FlowControl.Next, OpCodes.Stelem_I);
		public static readonly IROpCode Stelem_I1 = new IROpCode("stelem.i1", IRCode.Stelem_I1, FlowControl.Next, OpCodes.Stelem_I1);
		public static readonly IROpCode Stelem_I2 = new IROpCode("stelem.i2", IRCode.Stelem_I2, FlowControl.Next, OpCodes.Stelem_I2);
		public static readonly IROpCode Stelem_I4 = new IROpCode("stelem.i4", IRCode.Stelem_I4, FlowControl.Next, OpCodes.Stelem_I4);
		public static readonly IROpCode Stelem_I8 = new IROpCode("stelem.i8", IRCode.Stelem_I8, FlowControl.Next, OpCodes.Stelem_I8);
		public static readonly IROpCode Stelem_R4 = new IROpCode("stelem.r4", IRCode.Stelem_R4, FlowControl.Next, OpCodes.Stelem_R4);
		public static readonly IROpCode Stelem_R8 = new IROpCode("stelem.r8", IRCode.Stelem_R8, FlowControl.Next, OpCodes.Stelem_R8);
		public static readonly IROpCode Stelem_Ref = new IROpCode("stelem.ref", IRCode.Stelem_Ref, FlowControl.Next, OpCodes.Stelem_Ref);
		public static readonly IROpCode Ldelem = new IROpCode("ldelem", IRCode.Ldelem, FlowControl.Next, OpCodes.Ldelem);
		public static readonly IROpCode Stelem = new IROpCode("stelem", IRCode.Stelem, FlowControl.Next, OpCodes.Stelem);
		public static readonly IROpCode Unbox_Any = new IROpCode("unbox.any", IRCode.Unbox_Any, FlowControl.Next, OpCodes.Unbox_Any);
		public static readonly IROpCode Conv_Ovf_I1 = new IROpCode("conv.ovf.i1", IRCode.Conv_Ovf_I1, FlowControl.Next, OpCodes.Conv_Ovf_I1);
		public static readonly IROpCode Conv_Ovf_U1 = new IROpCode("conv.ovf.u1", IRCode.Conv_Ovf_U1, FlowControl.Next, OpCodes.Conv_Ovf_U1);
		public static readonly IROpCode Conv_Ovf_I2 = new IROpCode("conv.ovf.i2", IRCode.Conv_Ovf_I2, FlowControl.Next, OpCodes.Conv_Ovf_I2);
		public static readonly IROpCode Conv_Ovf_U2 = new IROpCode("conv.ovf.u2", IRCode.Conv_Ovf_U2, FlowControl.Next, OpCodes.Conv_Ovf_U2);
		public static readonly IROpCode Conv_Ovf_I4 = new IROpCode("conv.ovf.i4", IRCode.Conv_Ovf_I4, FlowControl.Next, OpCodes.Conv_Ovf_I4);
		public static readonly IROpCode Conv_Ovf_U4 = new IROpCode("conv.ovf.u4", IRCode.Conv_Ovf_U4, FlowControl.Next, OpCodes.Conv_Ovf_U4);
		public static readonly IROpCode Conv_Ovf_I8 = new IROpCode("conv.ovf.i8", IRCode.Conv_Ovf_I8, FlowControl.Next, OpCodes.Conv_Ovf_I8);
		public static readonly IROpCode Conv_Ovf_U8 = new IROpCode("conv.ovf.u8", IRCode.Conv_Ovf_U8, FlowControl.Next, OpCodes.Conv_Ovf_U8);
		public static readonly IROpCode Refanyval = new IROpCode("refanyval", IRCode.Refanyval, FlowControl.Next, OpCodes.Refanyval);
		public static readonly IROpCode Ckfinite = new IROpCode("ckfinite", IRCode.Ckfinite, FlowControl.Next, OpCodes.Ckfinite);
		public static readonly IROpCode Mkrefany = new IROpCode("mkrefany", IRCode.Mkrefany, FlowControl.Next, OpCodes.Mkrefany);
		public static readonly IROpCode Ldtoken = new IROpCode("ldtoken", IRCode.Ldtoken, FlowControl.Next, OpCodes.Ldtoken);
		public static readonly IROpCode Conv_U2 = new IROpCode("conv.u2", IRCode.Conv_U2, FlowControl.Next, OpCodes.Conv_U2);
		public static readonly IROpCode Conv_U1 = new IROpCode("conv.u1", IRCode.Conv_U1, FlowControl.Next, OpCodes.Conv_U1);
		public static readonly IROpCode Conv_I = new IROpCode("conv.i", IRCode.Conv_I, FlowControl.Next, OpCodes.Conv_I);
		public static readonly IROpCode Conv_Ovf_I = new IROpCode("conv.ovf.i", IRCode.Conv_Ovf_I, FlowControl.Next, OpCodes.Conv_Ovf_I);
		public static readonly IROpCode Conv_Ovf_U = new IROpCode("conv.ovf.u", IRCode.Conv_Ovf_U, FlowControl.Next, OpCodes.Conv_Ovf_U);
		public static readonly IROpCode Add_Ovf = new IROpCode("add.ovf", IRCode.Add_Ovf, FlowControl.Next, OpCodes.Add_Ovf);
		public static readonly IROpCode Add_Ovf_Un = new IROpCode("add.ovf.un", IRCode.Add_Ovf_Un, FlowControl.Next, OpCodes.Add_Ovf_Un);
		public static readonly IROpCode Mul_Ovf = new IROpCode("mul.ovf", IRCode.Mul_Ovf, FlowControl.Next, OpCodes.Mul_Ovf);
		public static readonly IROpCode Mul_Ovf_Un = new IROpCode("mul.ovf.un", IRCode.Mul_Ovf_Un, FlowControl.Next, OpCodes.Mul_Ovf_Un);
		public static readonly IROpCode Sub_Ovf = new IROpCode("sub.ovf", IRCode.Sub_Ovf, FlowControl.Next, OpCodes.Sub_Ovf);
		public static readonly IROpCode Sub_Ovf_Un = new IROpCode("sub.ovf.un", IRCode.Sub_Ovf_Un, FlowControl.Next, OpCodes.Sub_Ovf_Un);
		public static readonly IROpCode Endfinally = new IROpCode("endfinally", IRCode.Endfinally, FlowControl.Return, OpCodes.Endfinally);
		public static readonly IROpCode Leave = new IROpCode("leave", IRCode.Leave, FlowControl.Branch, OpCodes.Leave);
		public static readonly IROpCode Leave_S = new IROpCode("leave.s", IRCode.Leave_S, FlowControl.Branch, OpCodes.Leave_S);
		public static readonly IROpCode Stind_I = new IROpCode("stind.i", IRCode.Stind_I, FlowControl.Next, OpCodes.Stind_I);
		public static readonly IROpCode Conv_U = new IROpCode("conv.u", IRCode.Conv_U, FlowControl.Next, OpCodes.Conv_U);
		public static readonly IROpCode Prefix7 = new IROpCode("prefix7", IRCode.Prefix7, FlowControl.Meta, OpCodes.Prefix7);
		public static readonly IROpCode Prefix6 = new IROpCode("prefix6", IRCode.Prefix6, FlowControl.Meta, OpCodes.Prefix6);
		public static readonly IROpCode Prefix5 = new IROpCode("prefix5", IRCode.Prefix5, FlowControl.Meta, OpCodes.Prefix5);
		public static readonly IROpCode Prefix4 = new IROpCode("prefix4", IRCode.Prefix4, FlowControl.Meta, OpCodes.Prefix4);
		public static readonly IROpCode Prefix3 = new IROpCode("prefix3", IRCode.Prefix3, FlowControl.Meta, OpCodes.Prefix3);
		public static readonly IROpCode Prefix2 = new IROpCode("prefix2", IRCode.Prefix2, FlowControl.Meta, OpCodes.Prefix2);
		public static readonly IROpCode Prefix1 = new IROpCode("prefix1", IRCode.Prefix1, FlowControl.Meta, OpCodes.Prefix1);
		public static readonly IROpCode Prefixref = new IROpCode("prefixref", IRCode.Prefixref, FlowControl.Meta, OpCodes.Prefixref);
		public static readonly IROpCode Arglist = new IROpCode("arglist", IRCode.Arglist, FlowControl.Next, OpCodes.Arglist);
		public static readonly IROpCode Ceq = new IROpCode("ceq", IRCode.Ceq, FlowControl.Next, OpCodes.Ceq);
		public static readonly IROpCode Cgt = new IROpCode("cgt", IRCode.Cgt, FlowControl.Next, OpCodes.Cgt);
		public static readonly IROpCode Cgt_Un = new IROpCode("cgt.un", IRCode.Cgt_Un, FlowControl.Next, OpCodes.Cgt_Un);
		public static readonly IROpCode Clt = new IROpCode("clt", IRCode.Clt, FlowControl.Next, OpCodes.Clt);
		public static readonly IROpCode Clt_Un = new IROpCode("clt.un", IRCode.Clt_Un, FlowControl.Next, OpCodes.Clt_Un);
		public static readonly IROpCode Ldftn = new IROpCode("ldftn", IRCode.Ldftn, FlowControl.Next, OpCodes.Ldftn);
		public static readonly IROpCode Ldvirtftn = new IROpCode("ldvirtftn", IRCode.Ldvirtftn, FlowControl.Next, OpCodes.Ldvirtftn);
		public static readonly IROpCode Ldarg = new IROpCode("ldarg", IRCode.Ldarg, FlowControl.Next, OpCodes.Ldarg);
		public static readonly IROpCode Ldarga = new IROpCode("ldarga", IRCode.Ldarga, FlowControl.Next, OpCodes.Ldarga);
		public static readonly IROpCode Starg = new IROpCode("starg", IRCode.Starg, FlowControl.Next, OpCodes.Starg);
		public static readonly IROpCode Ldloc = new IROpCode("ldloc", IRCode.Ldloc, FlowControl.Next, OpCodes.Ldloc);
		public static readonly IROpCode Ldloca = new IROpCode("ldloca", IRCode.Ldloca, FlowControl.Next, OpCodes.Ldloca);
		public static readonly IROpCode Stloc = new IROpCode("stloc", IRCode.Stloc, FlowControl.Next, OpCodes.Stloc);
		public static readonly IROpCode Localloc = new IROpCode("localloc", IRCode.Localloc, FlowControl.Next, OpCodes.Localloc);
		public static readonly IROpCode Endfilter = new IROpCode("endfilter", IRCode.Endfilter, FlowControl.Return, OpCodes.Endfilter);
		public static readonly IROpCode Unaligned = new IROpCode("unaligned.", IRCode.Unaligned, FlowControl.Meta, OpCodes.Unaligned);
		public static readonly IROpCode Volatile = new IROpCode("volatile.", IRCode.Volatile, FlowControl.Meta, OpCodes.Volatile);
		public static readonly IROpCode Tailcall = new IROpCode("tail.", IRCode.Tailcall, FlowControl.Meta, OpCodes.Tailcall);
		public static readonly IROpCode Initobj = new IROpCode("initobj", IRCode.Initobj, FlowControl.Next, OpCodes.Initobj);
		public static readonly IROpCode Constrained = new IROpCode("constrained.", IRCode.Constrained, FlowControl.Meta, OpCodes.Constrained);
		public static readonly IROpCode Cpblk = new IROpCode("cpblk", IRCode.Cpblk, FlowControl.Next, OpCodes.Cpblk);
		public static readonly IROpCode Initblk = new IROpCode("initblk", IRCode.Initblk, FlowControl.Next, OpCodes.Initblk);
		public static readonly IROpCode Rethrow = new IROpCode("rethrow", IRCode.Rethrow, FlowControl.Throw, OpCodes.Rethrow);
		public static readonly IROpCode Sizeof = new IROpCode("sizeof", IRCode.Sizeof, FlowControl.Next, OpCodes.Sizeof);
		public static readonly IROpCode Refanytype = new IROpCode("refanytype", IRCode.Refanytype, FlowControl.Next, OpCodes.Refanytype);
		public static readonly IROpCode Readonly = new IROpCode("readonly.", IRCode.Readonly, FlowControl.Meta, OpCodes.Readonly);
	}


	partial class IROpCodes
	{
		/// <summary>
		/// This is a custom opcode that may be used with <see cref="MethodCallInstruction"/> to
		/// signal that the method is an intrinsic method.
		/// </summary>
		public static readonly IROpCode IntrinsicCall = new IROpCode("_intrinsic_call", IRCode.IntrinsicCall, FlowControl.Call, null);
		public static readonly IROpCode IntrinsicNewObj = new IROpCode("_intrinsic_newobj", IRCode.IntrinsicNewObj, FlowControl.Call, null);
		public static readonly IROpCode SpuInstructionMethod = new IROpCode("_spu_instruction_method", IRCode.SpuInstructionMethodCall, FlowControl.Call, null);
		public static readonly IROpCode PpeCall = new IROpCode("_ppe_call", IRCode.PpeCall, FlowControl.Call, null);
	}
}
