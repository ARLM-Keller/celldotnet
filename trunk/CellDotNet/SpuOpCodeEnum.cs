using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// The enumeration members correspond with the opcodes in <see cref="SpuOpCode"/> and is generated from them. 
	/// This enumeration is generated to be able to attach opcodes to methods attributes. 
	/// Therefore, use of those class members is preferred over use of this enumeration.
	/// </summary>
	// This enumeration is generated by CellDotNet.CodeGenUtils. DO NOT EDIT.
	enum SpuOpCodeEnum
	{
		None,
		/// <summary>
		/// Load Quadword (d-form)
		/// </summary>
		Lqd = 1,
		/// <summary>
		/// Load Quadword (x-form)
		/// </summary>
		Lqx = 2,
		/// <summary>
		/// Load Quadword (a-form)
		/// </summary>
		Lqa = 3,
		/// <summary>
		/// Load Quadword Instruction Relative (a-form)
		/// </summary>
		Lqr = 4,
		/// <summary>
		/// Store Quadword (d-form)
		/// </summary>
		Stqd = 5,
		/// <summary>
		/// Store Quadword (x-form)
		/// </summary>
		Stqx = 6,
		/// <summary>
		/// Store Quadword (a-form)
		/// </summary>
		Stqa = 7,
		/// <summary>
		/// Store Quadword Instruction Relative (a-form)
		/// </summary>
		Stqr = 8,
		/// <summary>
		/// Generate Controls for Byte Insertion (d-form)
		/// </summary>
		Cbd = 9,
		/// <summary>
		/// Generate Controls for Byte Insertion (x-form)
		/// </summary>
		Cbx = 10,
		/// <summary>
		/// Generate Controls for Halfword Insertion (d-form)
		/// </summary>
		Chd = 11,
		/// <summary>
		/// Generate Controls for Halfword Insertion (x-form)
		/// </summary>
		Chx = 12,
		/// <summary>
		/// Generate Controls for Word Insertion (d-form)
		/// </summary>
		Cwd = 13,
		/// <summary>
		/// Generate Controls for Word Insertion (x-form)
		/// </summary>
		Cwx = 14,
		/// <summary>
		/// Generate Controls for Doubleword Insertion (d-form)
		/// </summary>
		Cdd = 15,
		/// <summary>
		/// Generate Controls for Doubleword Insertion (x-form)
		/// </summary>
		Cdx = 16,
		/// <summary>
		/// Immediate Load Halfword
		/// </summary>
		Ilh = 17,
		/// <summary>
		/// Immediate Load Halfword Upper
		/// </summary>
		Ilhu = 18,
		/// <summary>
		/// Immediate Load Word
		/// </summary>
		Il = 19,
		/// <summary>
		/// Immediate Load Address
		/// </summary>
		Ila = 20,
		/// <summary>
		/// Immediate Or Halfword Lower
		/// </summary>
		Iohl = 21,
		/// <summary>
		/// Form Select Mask for Bytes Immediate
		/// </summary>
		Fsmbi = 22,
		/// <summary>
		/// Add Halfword
		/// </summary>
		Ah = 23,
		/// <summary>
		/// Add Halfword Immediate
		/// </summary>
		Ahi = 24,
		/// <summary>
		/// Add Word
		/// </summary>
		A = 25,
		/// <summary>
		/// Add Word Immediate
		/// </summary>
		Ai = 26,
		/// <summary>
		/// Subtract from Halfword
		/// </summary>
		Sfh = 27,
		/// <summary>
		/// Subtract from Halfword Immediate
		/// </summary>
		Sfhi = 28,
		/// <summary>
		/// Subtract from Word
		/// </summary>
		Sf = 29,
		/// <summary>
		/// Subtract from Word Immediate
		/// </summary>
		Sfi = 30,
		/// <summary>
		/// Add Extended
		/// </summary>
		Addx = 31,
		/// <summary>
		/// Carry Generate
		/// </summary>
		Cg = 32,
		/// <summary>
		/// Carry Generate Extended
		/// </summary>
		Cgx = 33,
		/// <summary>
		/// Subtract from Extended
		/// </summary>
		Sfx = 34,
		/// <summary>
		/// Borrow Generate
		/// </summary>
		Bg = 35,
		/// <summary>
		/// Borrow Generate Extended
		/// </summary>
		Bgx = 36,
		/// <summary>
		/// Multiply
		/// </summary>
		Mpy = 37,
		/// <summary>
		/// Multiply Unsigned
		/// </summary>
		Mpyu = 38,
		/// <summary>
		/// Multiply Immediate
		/// </summary>
		Mpyi = 39,
		/// <summary>
		/// Multiply Unsigned Immediate
		/// </summary>
		Mpyui = 40,
		/// <summary>
		/// Multiply and Add
		/// </summary>
		Mpya = 41,
		/// <summary>
		/// Multiply High
		/// </summary>
		Mpyh = 42,
		/// <summary>
		/// Multiply and Shift Right
		/// </summary>
		Mpys = 43,
		/// <summary>
		/// Multiply High High
		/// </summary>
		Mpyhh = 44,
		/// <summary>
		/// Multiply High High and Add
		/// </summary>
		Mpyhha = 45,
		/// <summary>
		/// Multiply High High Unsigned
		/// </summary>
		Mpyhhu = 46,
		/// <summary>
		/// Multiply High High Unsigned and Add
		/// </summary>
		Mpyhhau = 47,
		/// <summary>
		/// Count Leading Zeros
		/// </summary>
		Clz = 48,
		/// <summary>
		/// Count Ones in Bytes
		/// </summary>
		Cntb = 49,
		/// <summary>
		/// Form Select Mask for Bytes
		/// </summary>
		Fsmb = 50,
		/// <summary>
		/// Form Select Mask for Halfwords
		/// </summary>
		Fsmh = 51,
		/// <summary>
		/// Form Select Mask for Words
		/// </summary>
		Fsm = 52,
		/// <summary>
		/// Gather Bits from Bytes
		/// </summary>
		Gbb = 53,
		/// <summary>
		/// Gather Bits from Halfwords
		/// </summary>
		Gbh = 54,
		/// <summary>
		/// Gather Bits from Words
		/// </summary>
		Gb = 55,
		/// <summary>
		/// Average Bytes
		/// </summary>
		Avgb = 56,
		/// <summary>
		/// Absolute Differences of Bytes
		/// </summary>
		Absdb = 57,
		/// <summary>
		/// Sum Bytes into Halfwords
		/// </summary>
		Sumb = 58,
		/// <summary>
		/// Extend Sign Byte to Halfword
		/// </summary>
		Xsbh = 59,
		/// <summary>
		/// Extend Sign Halfword to Word
		/// </summary>
		Xshw = 60,
		/// <summary>
		/// Extend Sign Word to Doubleword
		/// </summary>
		Xswd = 61,
		/// <summary>
		/// And
		/// </summary>
		And = 62,
		/// <summary>
		/// And with Complement
		/// </summary>
		Andc = 63,
		/// <summary>
		/// And Byte Immediate
		/// </summary>
		Andbi = 64,
		/// <summary>
		/// And Halfword Immediate
		/// </summary>
		Andhi = 65,
		/// <summary>
		/// And Word Immediate
		/// </summary>
		Andi = 66,
		/// <summary>
		/// Or
		/// </summary>
		Or = 67,
		/// <summary>
		/// Or with Complement
		/// </summary>
		Orc = 68,
		/// <summary>
		/// Or Byte Immediate
		/// </summary>
		Orbi = 69,
		/// <summary>
		/// Or Halfword Immediate
		/// </summary>
		Orhi = 70,
		/// <summary>
		/// Or Word Immediate
		/// </summary>
		Ori = 71,
		/// <summary>
		/// Or Across
		/// </summary>
		Orx = 72,
		/// <summary>
		/// Exclusive Or
		/// </summary>
		Xor = 73,
		/// <summary>
		/// Exclusive Or Byte Immediate
		/// </summary>
		Xorbi = 74,
		/// <summary>
		/// Exclusive Or Halfword Immediate
		/// </summary>
		Xorhi = 75,
		/// <summary>
		/// Exclusive Or Word Immediate
		/// </summary>
		Xori = 76,
		/// <summary>
		/// Nand
		/// </summary>
		Nand = 77,
		/// <summary>
		/// Nor
		/// </summary>
		Nor = 78,
		/// <summary>
		/// Equivalent
		/// </summary>
		Eqv = 79,
		/// <summary>
		/// Select Bits
		/// </summary>
		Selb = 80,
		/// <summary>
		/// Shuffle Bytes
		/// </summary>
		Shufb = 81,
		/// <summary>
		/// Shift Left Halfword
		/// </summary>
		Shlh = 82,
		/// <summary>
		/// Shift Left Halfword Immediate
		/// </summary>
		Shlhi = 83,
		/// <summary>
		/// Shift Left Word
		/// </summary>
		Shl = 84,
		/// <summary>
		/// Shift Left Word Immediate
		/// </summary>
		Shli = 85,
		/// <summary>
		/// Shift Left Quadword by Bits
		/// </summary>
		Shlqbi = 86,
		/// <summary>
		/// Shift Left Quadword by Bits Immediate
		/// </summary>
		Shlqbii = 87,
		/// <summary>
		/// Shift Left Quadword by Bytes
		/// </summary>
		Shlqby = 88,
		/// <summary>
		/// Shift Left Quadword by Bytes Immediate
		/// </summary>
		Sqlqbyi = 89,
		/// <summary>
		/// Shift Left Quadword by Bytes from Bit Shift Count
		/// </summary>
		Shlqbybi = 90,
		/// <summary>
		/// Rotate Halfword
		/// </summary>
		Roth = 91,
		/// <summary>
		/// Rotate Halfword Immediate
		/// </summary>
		Rothi = 92,
		/// <summary>
		/// Rotate Word
		/// </summary>
		Rot = 93,
		/// <summary>
		/// Rotate Word Immediate
		/// </summary>
		Roti = 94,
		/// <summary>
		/// Rotate Quadword by Bytes
		/// </summary>
		Rotqby = 95,
		/// <summary>
		/// Rotate Quadword by Bytes Immediate
		/// </summary>
		Rotqbyi = 96,
		/// <summary>
		/// Rotate Quadword by Bytes from Bit Shift Count
		/// </summary>
		Rotqbybi = 97,
		/// <summary>
		/// Rotate Quadword by Bits
		/// </summary>
		Rotqbi = 98,
		/// <summary>
		/// Rotate Quadword by Bits Immediate
		/// </summary>
		Rotqbii = 99,
		/// <summary>
		/// Rotate and Mask Halfword
		/// </summary>
		Rothm = 100,
		/// <summary>
		/// Rotate and Mask Halfword Immediate
		/// </summary>
		Rothmi = 101,
		/// <summary>
		/// Rotate and Mask Word
		/// </summary>
		Rotm = 102,
		/// <summary>
		/// Rotate and Mask Word Immediate
		/// </summary>
		Rotmi = 103,
		/// <summary>
		/// Rotate and Mask Quadword by Bytes
		/// </summary>
		Rotqmby = 104,
		/// <summary>
		/// Rotate and Mask Quadword by Bytes Immediate
		/// </summary>
		Rotqmbyi = 105,
		/// <summary>
		/// Rotate and Mask Quadword Bytes from Bit Shift Count
		/// </summary>
		Rotqmbybi = 106,
		/// <summary>
		/// Rotate and Mask Quadword by Bits
		/// </summary>
		Rotqmbi = 107,
		/// <summary>
		/// Rotate and Mask Quadword by Bits Immediate
		/// </summary>
		Rotqmbii = 108,
		/// <summary>
		/// Rotate and Mask Algebraic Halfword
		/// </summary>
		Rotmah = 109,
		/// <summary>
		/// Rotate and Mask Algebraic Halfword Immediate
		/// </summary>
		Rotmahi = 110,
		/// <summary>
		/// Rotate and Mask Algebraic Word
		/// </summary>
		Rotma = 111,
		/// <summary>
		/// Rotate and Mask Algebraic Word Immediate
		/// </summary>
		Rotmai = 112,
		/// <summary>
		/// Halt If Equal
		/// </summary>
		Heq = 113,
		/// <summary>
		/// Halt If Equal Immediate
		/// </summary>
		Heqi = 114,
		/// <summary>
		/// Halt If Greater Than
		/// </summary>
		Hgt = 115,
		/// <summary>
		/// Halt If Greater Than Immediate
		/// </summary>
		Hgti = 116,
		/// <summary>
		/// Halt If Logically Greater Than
		/// </summary>
		Hlgt = 117,
		/// <summary>
		/// Halt If Logically Greater Than Immediate
		/// </summary>
		Hlgti = 118,
		/// <summary>
		/// Compare Equal Byte
		/// </summary>
		Ceqb = 119,
		/// <summary>
		/// Compare Equal Byte Immediate
		/// </summary>
		Ceqbi = 120,
		/// <summary>
		/// Compare Equal Halfword
		/// </summary>
		Ceqh = 121,
		/// <summary>
		/// Compare Equal Halfword Immediate
		/// </summary>
		Ceqhi = 122,
		/// <summary>
		/// Compare Equal Word
		/// </summary>
		Ceq = 123,
		/// <summary>
		/// Compare Equal Word Immediate
		/// </summary>
		Ceqi = 124,
		/// <summary>
		/// Compare Greater Than Byte
		/// </summary>
		Cgtb = 125,
		/// <summary>
		/// Compare Greater Than Byte Immediate
		/// </summary>
		Cgtbi = 126,
		/// <summary>
		/// Compare Greater Than Halfword
		/// </summary>
		Cgth = 127,
		/// <summary>
		/// Compare Greater Than Halfword Immediate
		/// </summary>
		Cgthi = 128,
		/// <summary>
		/// Compare Greater Than Word
		/// </summary>
		Cgt = 129,
		/// <summary>
		/// Compare Greater Than Word Immediate
		/// </summary>
		Cgti = 130,
		/// <summary>
		/// Compare Logical Greater Than Byte
		/// </summary>
		Clgtb = 131,
		/// <summary>
		/// Compare Logical Greater Than Byte Immediate
		/// </summary>
		Clgtbi = 132,
		/// <summary>
		/// Compare Logical Greater Than Halfword
		/// </summary>
		Clgth = 133,
		/// <summary>
		/// Compare Logical Greater Than Halfword Immediate
		/// </summary>
		Clgthi = 134,
		/// <summary>
		/// Compare Logical Greater Than Word
		/// </summary>
		Clgt = 135,
		/// <summary>
		/// Compare Logical Greater Than Word Immediate
		/// </summary>
		Clgti = 136,
		/// <summary>
		/// Branch Relative
		/// </summary>
		Br = 137,
		/// <summary>
		/// Branch Absolute
		/// </summary>
		Bra = 138,
		/// <summary>
		/// Branch Relative and Set Link
		/// </summary>
		Brsl = 139,
		/// <summary>
		/// Branch Absolute and Set Link
		/// </summary>
		Brasl = 140,
		/// <summary>
		/// Branch Indirect
		/// </summary>
		Bi = 141,
		/// <summary>
		/// Interrupt Return
		/// </summary>
		Iret = 142,
		/// <summary>
		/// Branch Indirect and Set Link if External Data
		/// </summary>
		Bisled = 143,
		/// <summary>
		/// Branch Indirect and Set Link
		/// </summary>
		Bisl = 144,
		/// <summary>
		/// Branch If Not Zero Word
		/// </summary>
		Brnz = 145,
		/// <summary>
		/// Branch If Zero Word
		/// </summary>
		Brz = 146,
		/// <summary>
		/// Branch If Not Zero Halfword
		/// </summary>
		Brhnz = 147,
		/// <summary>
		/// Branch If Zero Halfword
		/// </summary>
		Brhz = 148,
		/// <summary>
		/// Branch Indirect If Zero
		/// </summary>
		Biz = 149,
		/// <summary>
		/// Branch Indirect If Not Zero
		/// </summary>
		Binz = 150,
		/// <summary>
		/// Branch Indirect If Zero Halfword
		/// </summary>
		Bihz = 151,
		/// <summary>
		/// Branch Indirect If Not Zero Halfword
		/// </summary>
		Bihnz = 152,
		/// <summary>
		/// Floating Add
		/// </summary>
		Fa = 153,
		/// <summary>
		/// Double Floating Add
		/// </summary>
		Dfa = 154,
		/// <summary>
		/// Floating Subtract
		/// </summary>
		Fs = 155,
		/// <summary>
		/// Double Floating Subtract
		/// </summary>
		Dfs = 156,
		/// <summary>
		/// Floating Multiply
		/// </summary>
		Fm = 157,
		/// <summary>
		/// Double Floating Multiply
		/// </summary>
		Dfm = 158,
		/// <summary>
		/// Floating Multiply and Add
		/// </summary>
		Fma = 159,
		/// <summary>
		/// Double Floating Multiply and Add
		/// </summary>
		Dfma = 160,
		/// <summary>
		/// Floating Negative Multiply and Subtract
		/// </summary>
		Fnms = 161,
		/// <summary>
		/// Double Floating Negative Multiply and Subtract
		/// </summary>
		Dfnms = 162,
		/// <summary>
		/// Floating Multiply and Subtract
		/// </summary>
		Fms = 163,
		/// <summary>
		/// Double Floating Multiply and Subtract
		/// </summary>
		Dfms = 164,
		/// <summary>
		/// Double Floating Negative Multiply and Add
		/// </summary>
		Dfnma = 165,
		/// <summary>
		/// Floating Reciprocal Estimate
		/// </summary>
		Frest = 166,
		/// <summary>
		/// Floating Reciprocal Absolute Square Root Estimate
		/// </summary>
		Frsqest = 167,
		/// <summary>
		/// Floating Interpolate
		/// </summary>
		Fi = 168,
		/// <summary>
		/// Convert Signed Integer to Floating
		/// </summary>
		Csflt = 169,
		/// <summary>
		/// Convert Floating to Signed Integer
		/// </summary>
		Cflts = 170,
		/// <summary>
		/// Convert Unsigned Integer to Floating
		/// </summary>
		Cuflt = 171,
		/// <summary>
		/// Convert Floating to Unsigned Integer
		/// </summary>
		Cfltu = 172,
		/// <summary>
		/// Floating Round Double to Single
		/// </summary>
		Frds = 173,
		/// <summary>
		/// Floating Extend Single to Double
		/// </summary>
		Fesd = 174,
		/// <summary>
		/// Double Floating Compare Equal
		/// </summary>
		Dfceq = 175,
		/// <summary>
		/// Double Floating Compare Magnitude Equal
		/// </summary>
		Dfcmeq = 176,
		/// <summary>
		/// Double Floating Compare Greater Than
		/// </summary>
		Dfcgt = 177,
		/// <summary>
		/// Double Floating Compare Magnitude Greater Than
		/// </summary>
		Dfcmgt = 178,
		/// <summary>
		/// Double Floating Test Special Value
		/// </summary>
		Dftsv = 179,
		/// <summary>
		/// Floating Compare Equal
		/// </summary>
		Fceq = 180,
		/// <summary>
		/// Floating Compare Magnitude Equal
		/// </summary>
		Fcmeq = 181,
		/// <summary>
		/// Floating Compare Greater Than
		/// </summary>
		Fcgt = 182,
		/// <summary>
		/// Floating Compare Magnitude Greater Than
		/// </summary>
		Fcmgt = 183,
		/// <summary>
		/// Floating-Point Status and Control Register Write
		/// </summary>
		Fscrwr = 184,
		/// <summary>
		/// Floating-Point Status and Control Register Read
		/// </summary>
		Fscrrd = 185,
		/// <summary>
		/// Stop and Signal
		/// </summary>
		Stop = 186,
		/// <summary>
		/// No Operation (Load)
		/// </summary>
		Lnop = 187,
		/// <summary>
		/// No Operation (Execute)
		/// </summary>
		Nop = 188,
		/// <summary>
		/// Read Channel
		/// </summary>
		Rdch = 189,
		/// <summary>
		/// Read Channel Count
		/// </summary>
		Rchcnt = 190,
		/// <summary>
		/// Write Channel
		/// </summary>
		Wrch = 191,
		/// <summary>
		/// Move (pseudo)
		/// </summary>
		Move = 192,
		/// <summary>
		/// Function return (pseudo)
		/// </summary>
		Ret = 193,
	}
}