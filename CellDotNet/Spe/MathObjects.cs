using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace CellDotNet.Spe
{
	internal struct QuadWord : IEquatable<QuadWord>
	{
		public readonly uint I1;
		public readonly uint I2;
		public readonly uint I3;
		public readonly uint I4;

		public QuadWord(uint i1, uint i2, uint i3, uint i4)
		{
			I1 = i1;
			I2 = i2;
			I3 = i3;
			I4 = i4;
		}

		public override bool Equals(object obj)
		{
			var other = obj as QuadWord?;
			if (other == null) return false;

			return Equals(other.Value);
		}

		public override int GetHashCode()
		{
			return (int) (I1 + I2 + I3 + I4);
		}

		public bool Equals(QuadWord other)
		{
			return I1 == other.I1 && I2 == other.I2 && I3 == other.I3 && I4 == other.I4;
		}
	}


	/// <summary>
	/// Data used for math implementation.
	/// </summary>
	class MathObjects
	{
		/// <summary>
		/// Static data used for some DP ops.
		/// </summary>
		public ObjectWithAddress DoubleCompareDataArea { get; private set; }
		public ObjectWithAddress DoubleSignFilter { get; private set; }
		public ObjectWithAddress DoubleExponentFilter { get; private set; }
		public ObjectWithAddress DoubleCeqMagic1 { get; private set; }
		public ObjectWithAddress DoubleCeqShuffleMask2 { get; private set; }
		public ObjectWithAddress DoubleFractionPreferred { get; private set; }
		public ObjectWithAddress DoubleExponentPreferred { get; private set; }
		public ObjectWithAddress Double04050607_8 { get; private set; }
		public ObjectWithAddress Double04050607_c { get; private set; }
		public ObjectWithAddress Double60FractionBitsPreferred { get; private set; }
		public ObjectWithAddress Double11FractionBitsPreferred { get; private set; }
		public ObjectWithAddress Double61FractionBitsPreferred { get; private set; }
		public ObjectWithAddress ConvR8_I4Magic1 { get; private set; }
		public ObjectWithAddress ConvR8_I4Magic2 { get; private set; }



		private static readonly int[] s_doubleCompareData = unchecked(new int[] {
			0x7fffffff, (int)0xffffffff, 0x7fffffff, (int)0xffffffff, // sign filter
			0x7ff00000,      0x00000000, 0x7ff00000,      0x00000000, // exponent filter
			0x04050607, (int)0xc0c0c0c0, 0x0c0d0e0f, (int)0xc0c0c0c0, // Double04050607_c
			0x00010203,      0x00010203, 0x08090a0b,      0x08090a0b, // DoubleCeqMagic2
			0x00010203,      0x10111213, 0x08090a0b,      0x18191a1b, // DoubleCeqMagic1
			0x04050607, (int)0x80808080, 0x0c0d0e0f, (int)0x80808080, // Double04050607_8
			0x000fffff, (int)0xffffffff,          0,               0, // DoubleFractionPreferred
			0x7ff00000,               0,          0,               0, // DoubleExponentPreferred
			0x0fffffff, (int)0xffffffff,          0,               0, // Double60FractionBitsPreferred
			         0,      0x000007ff,          0,               0, // Double11FractionBitsPreferred
		    0x1fffffff, (int)0xffffffff,          0,               0, // Double61FractionBitsPreferred
			0x02031011,      0x12138080, 0x06071415,      0x16178080, // ConvR8_I4Magic1 1300
			0x41e00000,               0,          0,               0, // ConvR8_I4Magic2 1310
		});

		public MathObjects()
		{
			DoubleCompareDataArea = DataObject.FromQuadWords(s_doubleCompareData.Length / 4, "DoubleCompareDataArea", s_doubleCompareData);
			DoubleSignFilter = new ObjectOffset(DoubleCompareDataArea, 0);
			DoubleExponentFilter = new ObjectOffset(DoubleCompareDataArea, 0x10);
			DoubleCeqMagic1 = new ObjectOffset(DoubleCompareDataArea, 0x40);
			DoubleCeqShuffleMask2 = new ObjectOffset(DoubleCompareDataArea, 0x30);
			DoubleFractionPreferred = new ObjectOffset(DoubleCompareDataArea, 0x60);
			DoubleExponentPreferred = new ObjectOffset(DoubleCompareDataArea, 0x70);
			Double04050607_8 = new ObjectOffset(DoubleCompareDataArea, 0x50);
			Double04050607_c = new ObjectOffset(DoubleCompareDataArea, 0x20);
			Double60FractionBitsPreferred = new ObjectOffset(DoubleCompareDataArea, 0x80);
			Double11FractionBitsPreferred = new ObjectOffset(DoubleCompareDataArea, 0x90);
			Double61FractionBitsPreferred = new ObjectOffset(DoubleCompareDataArea, 0xa0);
			ConvR8_I4Magic1 = new ObjectOffset(DoubleCompareDataArea, 0xb0);
			ConvR8_I4Magic2 = new ObjectOffset(DoubleCompareDataArea, 0xc0);
		}

		public ObjectWithAddress[] GetAllObjectsWithStorage()
		{
			if (_constantsObjects.Value == null)
			{
				_constantsObjects.Resize(_constantDict.Count*16);
				var values = new int[_constantDict.Count*4];
				foreach (KeyValuePair<QuadWord, ObjectOffset> pair in _constantDict)
				{
					values[pair.Value.OffsetFromParent/4] = (int) pair.Key.I1;
					values[pair.Value.OffsetFromParent/4+1] = (int) pair.Key.I2;
					values[pair.Value.OffsetFromParent/4+2] = (int) pair.Key.I3;
					values[pair.Value.OffsetFromParent/4+3] = (int) pair.Key.I4;
				}
				_constantsObjects.SetValue(values);
			}

			var all = new ObjectWithAddress[]
			          	{
			          		DoubleCompareDataArea,
			          		_packd,
			          		_unpackd,
			          		_fixdfsi,
			          		_divdf3,
							_constantsObjects,
			          	};

			return all.Where(o => o != null).ToArray();
		}

		readonly DataObject _constantsObjects = new DataObject(0, "MathConstants");
		readonly Dictionary<QuadWord, ObjectOffset> _constantDict = new Dictionary<QuadWord, ObjectOffset>();

		private ObjectWithAddress RegisterConstant(uint i1, uint i2, uint i3, uint i4)
		{
			QuadWord qw = new QuadWord(i1, i2, i3, i4);

			ObjectOffset oo;
			if (_constantDict.TryGetValue(qw, out oo))
				return oo;

			oo = new ObjectOffset(_constantsObjects, _constantDict.Count * 16);
			_constantDict.Add(qw, oo);

			return oo;
		}


		// 000011f0 <_fini>:
		// 11f0:  24 00 40 80   stqd  $0,16($1)
		// 11f4:  24 ff 80 81   stqd  $1,-32($1)
		// 11f8:  1c f8 00 81   ai  $1,$1,-32
		// 11fc:  33 7d d2 80   brsl  $0,90 <__do_global_dtors_aux>  # 90
		// 1200:  1c 08 00 81   ai  $1,$1,32  # 20
		// 1204:  34 00 40 80   lqd  $0,16($1)
		// 1208:  35 00 00 00   bi  $0
		// Disassembly of section .rodata:
		// 
		// 00001210 <__thenan_df-0x50>:
		// 1210:  7f ff ff ff   heqi  $127,$127,-1
		// 1214:  ff ff ff ff   fms  $127,$127,$127,$127
		// 1218:  7f ff ff ff   heqi  $127,$127,-1
		// 121c:  ff ff ff ff   fms  $127,$127,$127,$127
		// 1220:  00 01 02 03   stop
		// 1224:  10 11 12 13   hbra  1270 <__thenan_df+0x10>,8890 <_end+0x7220>
		// 1228:  08 09 0a 0b   sf  $11,$20,$36
		// 122c:  18 19 1a 1b   a  $27,$52,$100
		// 1230:  25 64 00 00   bihnze  $0,$0
		// ...
		// 1240:  04 05 06 07   ori  $7,$12,20  # 14
		// 1244:  c0 c0 c0 c0   mpya  $6,$1,$3,$64
		// 1248:  0c 0d 0e 0f   sfi  $15,$28,52  # 34
		// 124c:  c0 c0 c0 c0   mpya  $6,$1,$3,$64
		// 1250:  04 05 06 07   ori  $7,$12,20  # 14
		// 1254:  80 80 80 80   selb  $4,$1,$2,$0
		// 1258:  0c 0d 0e 0f   sfi  $15,$28,52  # 34
		// 125c:  80 80 80 80   selb  $4,$1,$2,$0
		// 
		// 00001260 <__thenan_df>:
		// ...
		// 1280:  7f f0 00 00   heqi  $0,$0,-64
		// ...
		// 1290:  00 0f ff ff   stop
		// 1294:  ff ff ff ff   fms  $127,$127,$127,$127
		// ...
		// 12a0:  0f ff ff ff   shlhi  $127,$127,-1
		// 12a4:  ff ff ff ff   fms  $127,$127,$127,$127
		// ...
		// 12b4:  00 00 07 ff   stop
		// ...
		// 12c0:  1f ff ff ff   .long 0x1fffffff
		// 12c4:  ff ff ff ff   fms  $127,$127,$127,$127
		// ...
		// 
		// 000012d0 <_global_impure_ptr>:


		private static readonly int[] s_packdRawCode = GetPackdRawCode();

		#region GetPackdRawCode

		static int[] GetPackdRawCode()
		{
			unchecked
			{
				return new int[]
				{
					// 000005a8 <__pack_d>:
					/*5a8:*/  (int)0x1c010186,  // ai  $6,$3,4
					/*5ac:*/  (int)0x34000188,  // lqd  $8,0($3)
					/*5b0:*/  (int)0x04000187,  // ori  $7,$3,0
					/*5b4:*/  (int)0x34004189,  // lqd  $9,16($3)
					/*5b8:*/  (int)0x0400040a,  // ori  $10,$8,0
					/*5bc:*/  (int)0x3b80c402,  // rotqby  $2,$8,$3
					/*5c0:*/  (int)0x3b80c483,  // rotqby  $3,$9,$3
					/*5c4:*/  (int)0x3b818504,  // rotqby  $4,$10,$6
					/*5c8:*/  (int)0x5c004105,  // clgti  $5,$2,1
					/*5cc:*/  (int)0x04000189,  // ori  $9,$3,0
					/*5d0:*/  (int)0x0400020a,  // ori  $10,$4,0
					/*5d4:*/  (int)0x00200000,  // lnop
					/*5d8:*/  (int)0x21000885,  // brnz  $5,61c <__pack_d+0x74>  # 61c
					/*5dc:*/  (int)0x32a0200d,  // fsmbi  $13,16448  # 4040
					/*5e0:*/  (int)0x33819607,  // lqr  $7,1290 <__thenan_df+0x30>  # 1290
					/*5e4:*/  (int)0x33819384,  // lqr  $4,1280 <__thenan_df+0x20>  # 1280
					/*5e8:*/  (int)0x1602068c,  // andbi  $12,$13,8
					/*5ec:*/  (int)0x0823018b,  // or  $11,$3,$12
					/*5f0:*/  (int)0x1821c585,  // and  $5,$11,$7
					/*5f4:*/  (int)0x35800009,  // hbr  618 <__pack_d+0x70>,$0
					/*5f8:*/  (int)0x4020007f,  // nop  $127
					/*5fc:*/  (int)0x3fbf053e,  // rotqmbyi  $62,$10,-4
					/*600:*/  (int)0x54c01f3d,  // xswd  $61,$62
					/*604:*/  (int)0x3f821ebc,  // rotqbyi  $60,$61,8
					/*608:*/  (int)0x3fe3de3b,  // shlqbyi  $59,$60,15
					/*60c:*/  (int)0x3f61ddba,  // shlqbii  $58,$59,7
					/*610:*/  (int)0x082e828a,  // or  $10,$5,$58
					/*614:*/  (int)0x08210503,  // or  $3,$10,$4
					/*618:*/  (int)0x35000000,  // bi  $0
					/*61c:*/  (int)0x7c01010e,  // ceqi  $14,$2,4
					/*620:*/  (int)0x2100290e,  // brnz  $14,768 <__pack_d+0x1c0>  # 768
					/*624:*/  (int)0x7c00810f,  // ceqi  $15,$2,2
					/*628:*/  (int)0x21002a0f,  // brnz  $15,778 <__pack_d+0x1d0>  # 778
					/*62c:*/  (int)0x4080000b,  // il  $11,0
					/*630:*/  (int)0x7802c192,  // ceq  $18,$3,$11
					/*634:*/  (int)0x36000911,  // gb  $17,$18
					/*638:*/  (int)0x4c02c890,  // cgti  $16,$17,11
					/*63c:*/  (int)0x21002790,  // brnz  $16,778 <__pack_d+0x1d0>  # 778
					/*640:*/  (int)0x40800417,  // il  $23,8
					/*644:*/  (int)0x1c020396,  // ai  $22,$7,8
					/*648:*/  (int)0x40fe0093,  // il  $19,-1023
					/*64c:*/  (int)0x3885c395,  // lqx  $21,$7,$23
					/*650:*/  (int)0x3b858a94,  // rotqby  $20,$21,$22
					/*654:*/  (int)0x4804ca03,  // cgt  $3,$20,$19
					/*658:*/  (int)0x4020007f,  // nop  $127
					/*65c:*/  (int)0x21002503,  // brnz  $3,784 <__pack_d+0x1dc>  # 784
					/*660:*/  (int)0x40fe0119,  // il  $25,-1022
					/*664:*/  (int)0x32800005,  // fsmbi  $5,0
					/*668:*/  (int)0x08064a08,  // sf  $8,$20,$25
					/*66c:*/  (int)0x4c0e0418,  // cgti  $24,$8,56  # 38
					/*670:*/  (int)0x4020007f,  // nop  $127
					/*674:*/  (int)0x21001898,  // brnz  $24,738 <__pack_d+0x190>  # 738
					/*678:*/  (int)0x1c100431,  // ai  $49,$8,64  # 40
					/*67c:*/  (int)0x328080b5,  // fsmbi  $53,257  # 101
					/*680:*/  (int)0x40ffffab,  // il  $43,-1
					/*684:*/  (int)0x3381798c,  // lqr  $12,1250 <_fini+0x60>  # 1250
					/*688:*/  (int)0x0c000434,  // sfi  $52,$8,0
					/*68c:*/  (int)0x338176a2,  // lqr  $34,1240 <_fini+0x50>  # 1240
					/*690:*/  (int)0x0c01c42e,  // sfi  $46,$8,7
					/*694:*/  (int)0x328080af,  // fsmbi  $47,257  # 101
					/*698:*/  (int)0x15405ab3,  // andhi  $51,$53,257  # 101
					/*69c:*/  (int)0x3b2d04ad,  // rotqmbi  $45,$9,$52
					/*6a0:*/  (int)0x32808082,  // fsmbi  $2,257  # 101
					/*6a4:*/  (int)0x3f8219b2,  // rotqbyi  $50,$51,8
					/*6a8:*/  (int)0x16e0179d,  // andbi  $29,$47,-128
					/*6ac:*/  (int)0x39ab969f,  // rotqmbybi  $31,$45,$46
					/*6b0:*/  (int)0x39ec5930,  // shlqbybi  $48,$50,$49
					/*6b4:*/  (int)0x3b6c582a,  // shlqbi  $42,$48,$49
					/*6b8:*/  (int)0x184ad52c,  // cg  $44,$42,$43
					/*6bc:*/  (int)0xb52b160c,  // shufb  $41,$44,$44,$12
					/*6c0:*/  (int)0x680ad529,  // addx  $41,$42,$43
					/*6c4:*/  (int)0x182a44a8,  // and  $40,$9,$41
					/*6c8:*/  (int)0x7802d427,  // ceq  $39,$40,$11
					/*6cc:*/  (int)0x360013a6,  // gb  $38,$39
					/*6d0:*/  (int)0x4c02d325,  // cgti  $37,$38,11
					/*6d4:*/  (int)0x092952a4,  // nor  $36,$37,$37
					/*6d8:*/  (int)0x3fbf1223,  // rotqmbyi  $35,$36,-4
					/*6dc:*/  (int)0x54c011a0,  // xswd  $32,$35
					/*6e0:*/  (int)0x0842d021,  // bg  $33,$32,$11
					/*6e4:*/  (int)0xb3c850a2,  // shufb  $30,$33,$33,$34
					/*6e8:*/  (int)0x6822d01e,  // sfx  $30,$32,$11
					/*6ec:*/  (int)0x08278f87,  // or  $7,$31,$30
					/*6f0:*/  (int)0x18208383,  // and  $3,$7,$2
					/*6f4:*/  (int)0x7807419c,  // ceq  $28,$3,$29
					/*6f8:*/  (int)0x36000e1b,  // gb  $27,$28
					/*6fc:*/  (int)0x4c02cd9a,  // cgti  $26,$27,11
					/*700:*/  (int)0x4020007f,  // nop  $127
					/*704:*/  (int)0x21002d9a,  // brnz  $26,870 <__pack_d+0x2c8>  # 870
					/*708:*/  (int)0x161fc13c,  // andbi  $60,$2,127  # 7f
					/*70c:*/  (int)0x338172bb,  // lqr  $59,12a0 <__thenan_df+0x40>  # 12a0
					/*710:*/  (int)0x184f03bd,  // cg  $61,$7,$60
					/*714:*/  (int)0xb6ef5e8c,  // shufb  $55,$61,$61,$12
					/*718:*/  (int)0x680f03b7,  // addx  $55,$7,$60
					/*71c:*/  (int)0x00200000,  // lnop
					/*720:*/  (int)0x580edbba,  // clgt  $58,$55,$59
					/*724:*/  (int)0x3fbfdb85,  // rotqmbyi  $5,$55,-1
					/*728:*/  (int)0x780edbb8,  // ceq  $56,$55,$59
					/*72c:*/  (int)0x54c01d39,  // xswd  $57,$58
					/*730:*/  (int)0x86ce5d38,  // selb  $54,$58,$57,$56
					/*734:*/  (int)0x0c001b03,  // sfi  $3,$54,0
					/*738:*/  (int)0x3fbf018f,  // rotqmbyi  $15,$3,-4
					/*73c:*/  (int)0x33816e8e,  // lqr  $14,12b0 <__thenan_df+0x50>  # 12b0
					/*740:*/  (int)0x127fd689,  // hbrr  764 <__pack_d+0x1bc>,5f4 <__pack_d+0x4c>  # 5f4
					/*744:*/  (int)0x33816990,  // lqr  $16,1290 <__thenan_df+0x30>  # 1290
					/*748:*/  (int)0x54c0078c,  // xswd  $12,$15
					/*74c:*/  (int)0x1823860b,  // and  $11,$12,$14
					/*750:*/  (int)0x18240285,  // and  $5,$5,$16
					/*754:*/  (int)0x3f820587,  // rotqbyi  $7,$11,8
					/*758:*/  (int)0x3fe38383,  // shlqbyi  $3,$7,14
					/*75c:*/  (int)0x3f610184,  // shlqbii  $4,$3,4
					/*760:*/  (int)0x4020007f,  // nop  $127
					/*764:*/  (int)0x327fd200,  // br  5f4 <__pack_d+0x4c>  # 5f4
					/*768:*/  (int)0x40800005,  // il  $5,0
					/*76c:*/  (int)0x33816284,  // lqr  $4,1280 <__thenan_df+0x20>  # 1280
					/*770:*/  (int)0x4020007f,  // nop  $127
					/*774:*/  (int)0x327fd000,  // br  5f4 <__pack_d+0x4c>  # 5f4
					/*778:*/  (int)0x40800005,  // il  $5,0
					/*77c:*/  (int)0x32800004,  // fsmbi  $4,0
					/*780:*/  (int)0x327fce80,  // br  5f4 <__pack_d+0x4c>  # 5f4
					/*784:*/  (int)0x4081ff83,  // il  $3,1023  # 3ff
					/*788:*/  (int)0x4800ca11,  // cgt  $17,$20,$3
					/*78c:*/  (int)0x217ffb91,  // brnz  $17,768 <__pack_d+0x1c0>  # 768
					/*790:*/  (int)0x1800ca05,  // a  $5,$20,$3
					/*794:*/  (int)0x32808096,  // fsmbi  $22,257  # 101
					/*798:*/  (int)0x32808097,  // fsmbi  $23,257  # 101
					/*79c:*/  (int)0x16e00b15,  // andbi  $21,$22,-128
					/*7a0:*/  (int)0x1825c487,  // and  $7,$9,$23
					/*7a4:*/  (int)0x78054394,  // ceq  $20,$7,$21
					/*7a8:*/  (int)0x36000a13,  // gb  $19,$20
					/*7ac:*/  (int)0x4c02c992,  // cgti  $18,$19,11
					/*7b0:*/  (int)0x21001092,  // brnz  $18,834 <__pack_d+0x28c>  # 834
					/*7b4:*/  (int)0x328080a5,  // fsmbi  $37,257  # 101
					/*7b8:*/  (int)0x33815324,  // lqr  $36,1250 <_fini+0x60>  # 1250
					/*7bc:*/  (int)0x161fd2a2,  // andbi  $34,$37,127  # 7f
					/*7c0:*/  (int)0x184884a3,  // cg  $35,$9,$34
					/*7c4:*/  (int)0xb428d1a4,  // shufb  $33,$35,$35,$36
					/*7c8:*/  (int)0x680884a1,  // addx  $33,$9,$34
					/*7cc:*/  (int)0x04001089,  // ori  $9,$33,0
					/*7d0:*/  (int)0x1c0042b3,  // ai  $51,$5,1
					/*7d4:*/  (int)0x33815db9,  // lqr  $57,12c0 <__thenan_df+0x60>  # 12c0
					/*7d8:*/  (int)0x33815b2c,  // lqr  $44,12b0 <__thenan_df+0x50>  # 12b0
					/*7dc:*/  (int)0x127fc315,  // hbrr  830 <__pack_d+0x288>,5f4 <__pack_d+0x4c>  # 5f4
					/*7e0:*/  (int)0x3f3fc4b0,  // rotqmbii  $48,$9,-1
					/*7e4:*/  (int)0x338155aa,  // lqr  $42,1290 <__thenan_df+0x30>  # 1290
					/*7e8:*/  (int)0x580e44b8,  // clgt  $56,$9,$57
					/*7ec:*/  (int)0x780e44b6,  // ceq  $54,$9,$57
					/*7f0:*/  (int)0x54c01c37,  // xswd  $55,$56
					/*7f4:*/  (int)0x86addc36,  // selb  $53,$56,$55,$54
					/*7f8:*/  (int)0x7c001ab4,  // ceqi  $52,$53,0
					/*7fc:*/  (int)0x00200000,  // lnop
					/*800:*/  (int)0x862159b4,  // selb  $49,$51,$5,$52
					/*804:*/  (int)0x36801a2f,  // fsm  $47,$52
					/*808:*/  (int)0x3fbf18ad,  // rotqmbyi  $45,$49,-4
					/*80c:*/  (int)0x85c2582f,  // selb  $46,$48,$9,$47
					/*810:*/  (int)0x54c016ab,  // xswd  $43,$45
					/*814:*/  (int)0x3fbfd729,  // rotqmbyi  $41,$46,-1
					/*818:*/  (int)0x182b15a8,  // and  $40,$43,$44
					/*81c:*/  (int)0x00200000,  // lnop
					/*820:*/  (int)0x182a9485,  // and  $5,$41,$42
					/*824:*/  (int)0x3f821427,  // rotqbyi  $39,$40,8
					/*828:*/  (int)0x3fe393a6,  // shlqbyi  $38,$39,14
					/*82c:*/  (int)0x3f611304,  // shlqbii  $4,$38,4
					/*830:*/  (int)0x327fb880,  // br  5f4 <__pack_d+0x4c>  # 5f4
					/*834:*/  (int)0x3281011d,  // fsmbi  $29,514  # 202
					/*838:*/  (int)0x127ff30d,  // hbrr  86c <__pack_d+0x2c4>,7d0 <__pack_d+0x228>  # 7d0
					/*83c:*/  (int)0x15404e9c,  // andhi  $28,$29,257  # 101
					/*840:*/  (int)0x1827049b,  // and  $27,$9,$28
					/*844:*/  (int)0x7802cd9a,  // ceq  $26,$27,$11
					/*848:*/  (int)0x36000d19,  // gb  $25,$26
					/*84c:*/  (int)0x4c02cc98,  // cgti  $24,$25,11
					/*850:*/  (int)0x4020007f,  // nop  $127
					/*854:*/  (int)0x217fef98,  // brnz  $24,7d0 <__pack_d+0x228>  # 7d0
					/*858:*/  (int)0x1841c49f,  // cg  $31,$9,$7
					/*85c:*/  (int)0x33813ea0,  // lqr  $32,1250 <_fini+0x60>  # 1250
					/*860:*/  (int)0xb3c7cfa0,  // shufb  $30,$31,$31,$32
					/*864:*/  (int)0x6801c49e,  // addx  $30,$9,$7
					/*868:*/  (int)0x04000f09,  // ori  $9,$30,0
					/*86c:*/  (int)0x327fec80,  // br  7d0 <__pack_d+0x228>  # 7d0
					/*870:*/  (int)0x1840c382,  // cg  $2,$7,$3
					/*874:*/  (int)0x3281010d,  // fsmbi  $13,514  # 202
					/*878:*/  (int)0x127fd81b,  // hbrr  8e4 <__pack_d+0x33c>,738 <__pack_d+0x190>  # 738
					/*87c:*/  (int)0x33814488,  // lqr  $8,12a0 <__thenan_df+0x40>  # 12a0
					/*880:*/  (int)0x4020007f,  // nop  $127
					/*884:*/  (int)0x338143cc,  // lqr  $76,12a0 <__thenan_df+0x40>  # 12a0
					/*888:*/  (int)0x15404684,  // andhi  $4,$13,257  # 101
					/*88c:*/  (int)0xb900810c,  // shufb  $72,$2,$2,$12
					/*890:*/  (int)0x3fbfc3c0,  // rotqmbyi  $64,$7,-1
					/*894:*/  (int)0x182103cf,  // and  $79,$7,$4
					/*898:*/  (int)0x7802e7ce,  // ceq  $78,$79,$11
					/*89c:*/  (int)0x6800c3c8,  // addx  $72,$7,$3
					/*8a0:*/  (int)0x58020386,  // clgt  $6,$7,$8
					/*8a4:*/  (int)0x3600274d,  // gb  $77,$78
					/*8a8:*/  (int)0x5813244b,  // clgt  $75,$72,$76
					/*8ac:*/  (int)0x3fbfe43f,  // rotqmbyi  $63,$72,-1
					/*8b0:*/  (int)0x54c00305,  // xswd  $5,$6
					/*8b4:*/  (int)0x78020389,  // ceq  $9,$7,$8
					/*8b8:*/  (int)0x4c02e6c7,  // cgti  $71,$77,11
					/*8bc:*/  (int)0x78132449,  // ceq  $73,$72,$76
					/*8c0:*/  (int)0x7c0023c4,  // ceqi  $68,$71,0
					/*8c4:*/  (int)0x54c025ca,  // xswd  $74,$75
					/*8c8:*/  (int)0x88a14309,  // selb  $69,$6,$5,$9
					/*8cc:*/  (int)0x3680223e,  // fsm  $62,$68
					/*8d0:*/  (int)0x88d2a5c9,  // selb  $70,$75,$74,$73
					/*8d4:*/  (int)0x0c0022c3,  // sfi  $67,$69,0
					/*8d8:*/  (int)0x0c002341,  // sfi  $65,$70,0
					/*8dc:*/  (int)0x80afe03e,  // selb  $5,$64,$63,$62
					/*8e0:*/  (int)0x807061c4,  // selb  $3,$67,$65,$68
					/*8e4:*/  (int)0x327fca80,  // br  738 <__pack_d+0x190>  # 738
				};
			}
		}

		#endregion

		private static readonly int[] s_unpackdRawCode = GetPackdRawCode();

		#region GetUnpackdRawCode

		static int[] GetUnpackdRawCode()
		{
			unchecked
			{
				return new int[]
				{
					// 000008e8 <__unpack_d>:
					/*8e8:*/  (int)0x40800208,  // il  $8,4
					/*8ec:*/  (int)0x3400018f,  // lqd  $15,0($3)
					/*8f0:*/  (int)0x0400020a,  // ori  $10,$4,0
					/*8f4:*/  (int)0x35900000,  // hbrp  8f4 <__unpack_d+0xc>,$0
					/*8f8:*/  (int)0x4020007f,  // nop  $127
					/*8fc:*/  (int)0x3ec10206,  // cwd  $6,4($4)
					/*900:*/  (int)0x4083ff89,  // il  $9,2047  # 7ff
					/*904:*/  (int)0x3882020b,  // lqx  $11,$4,$8
					/*908:*/  (int)0x3381310d,  // lqr  $13,1290 <__thenan_df+0x30>  # 1290
					/*90c:*/  (int)0x3b80c78e,  // rotqby  $14,$15,$3
					/*910:*/  (int)0x0f3b0705,  // rotmi  $5,$14,-20
					/*914:*/  (int)0x3f3e470c,  // rotqmbii  $12,$14,-7
					/*918:*/  (int)0x18234707,  // and  $7,$14,$13
					/*91c:*/  (int)0x00200000,  // lnop
					/*920:*/  (int)0x18224285,  // and  $5,$5,$9
					/*924:*/  (int)0x3fbe4603,  // rotqmbyi  $3,$12,-7
					/*928:*/  (int)0x3fe10182,  // shlqbyi  $2,$3,4
					/*92c:*/  (int)0xb082c106,  // shufb  $4,$2,$11,$6
					/*930:*/  (int)0x35900000,  // hbrp  930 <__unpack_d+0x48>,$0
					/*934:*/  (int)0x28820504,  // stqx  $4,$10,$8
					/*938:*/  (int)0x21001905,  // brnz  $5,a00 <__unpack_d+0x118>  # a00
					/*93c:*/  (int)0x40800011,  // il  $17,0
					/*940:*/  (int)0x78044390,  // ceq  $16,$7,$17
					/*944:*/  (int)0x36000809,  // gb  $9,$16
					/*948:*/  (int)0x4c02c488,  // cgti  $8,$9,11
					/*94c:*/  (int)0x00200000,  // lnop
					/*950:*/  (int)0x21002308,  // brnz  $8,a68 <__unpack_d+0x180>  # a68
					/*954:*/  (int)0x00200000,  // lnop
					/*958:*/  (int)0x4080041b,  // il  $27,8
					/*95c:*/  (int)0x3ec2051e,  // cwd  $30,8($10)
					/*960:*/  (int)0x40fe0109,  // il  $9,-1022
					/*964:*/  (int)0x1200099c,  // hbrr  9d4 <__unpack_d+0xec>,9b0 <__unpack_d+0xc8>  # 9b0
					/*968:*/  (int)0x40800199,  // il  $25,3
					/*96c:*/  (int)0x3886c51d,  // lqx  $29,$10,$27
					/*970:*/  (int)0x3f8203a2,  // rotqbyi  $34,$7,8
					/*974:*/  (int)0x33812588,  // lqr  $8,12a0 <__thenan_df+0x40>  # 12a0
					/*978:*/  (int)0x3ec0051a,  // cwd  $26,0($10)
					/*97c:*/  (int)0x3fe25107,  // shlqbyi  $7,$34,9
					/*980:*/  (int)0xb387449e,  // shufb  $28,$9,$29,$30
					/*984:*/  (int)0x580203a1,  // clgt  $33,$7,$8
					/*988:*/  (int)0x7802039f,  // ceq  $31,$7,$8
					/*98c:*/  (int)0x2886c51c,  // stqx  $28,$10,$27
					/*990:*/  (int)0x54c010a0,  // xswd  $32,$33
					/*994:*/  (int)0x34000518,  // lqd  $24,0($10)
					/*998:*/  (int)0x82c8109f,  // selb  $22,$33,$32,$31
					/*99c:*/  (int)0xb2e60c9a,  // shufb  $23,$25,$24,$26
					/*9a0:*/  (int)0x24000517,  // stqd  $23,0($10)
					/*9a4:*/  (int)0x21000916,  // brnz  $22,9ec <__unpack_d+0x104>  # 9ec
					/*9a8:*/  (int)0x04000485,  // ori  $5,$9,0
					/*9ac:*/  (int)0x3fe00403,  // shlqbyi  $3,$8,0
					/*9b0:*/  (int)0x1cffc285,  // ai  $5,$5,-1
					/*9b4:*/  (int)0x3f8203a8,  // rotqbyi  $40,$7,8
					/*9b8:*/  (int)0x3fe21427,  // shlqbyi  $39,$40,8
					/*9bc:*/  (int)0x3f605387,  // shlqbii  $7,$39,1
					/*9c0:*/  (int)0x5800c3a6,  // clgt  $38,$7,$3
					/*9c4:*/  (int)0x7800c3a4,  // ceq  $36,$7,$3
					/*9c8:*/  (int)0x54c01325,  // xswd  $37,$38
					/*9cc:*/  (int)0x84695324,  // selb  $35,$38,$37,$36
					/*9d0:*/  (int)0x4020007f,  // nop  $127
					/*9d4:*/  (int)0x207ffba3,  // brz  $35,9b0 <__unpack_d+0xc8>  # 9b0
					/*9d8:*/  (int)0x40800429,  // il  $41,8
					/*9dc:*/  (int)0x3ec2052c,  // cwd  $44,8($10)
					/*9e0:*/  (int)0x388a452b,  // lqx  $43,$10,$41
					/*9e4:*/  (int)0xb54ac2ac,  // shufb  $42,$5,$43,$44
					/*9e8:*/  (int)0x288a452a,  // stqx  $42,$10,$41
					/*9ec:*/  (int)0x3400452e,  // lqd  $46,16($10)
					/*9f0:*/  (int)0x3ee0052f,  // cdd  $47,0($10)
					/*9f4:*/  (int)0xb5ab83af,  // shufb  $45,$7,$46,$47
					/*9f8:*/  (int)0x2400452d,  // stqd  $45,16($10)
					/*9fc:*/  (int)0x35000000,  // bi  $0
					/*a00:*/  (int)0x780242b0,  // ceq  $48,$5,$9
					/*a04:*/  (int)0x21000fb0,  // brnz  $48,a80 <__unpack_d+0x198>  # a80
					/*a08:*/  (int)0x40800449,  // il  $73,8
					/*a0c:*/  (int)0x3ec2054d,  // cwd  $77,8($10)
					/*a10:*/  (int)0x40fe0082,  // il  $2,-1023
					/*a14:*/  (int)0x35800014,  // hbr  a64 <__unpack_d+0x17c>,$0
					/*a18:*/  (int)0x408001c7,  // il  $71,3
					/*a1c:*/  (int)0x3892454b,  // lqx  $75,$10,$73
					/*a20:*/  (int)0x180082cc,  // a  $76,$5,$2
					/*a24:*/  (int)0x3f82038b,  // rotqbyi  $11,$7,8
					/*a28:*/  (int)0x32c04004,  // fsmbi  $4,32896  # 8080
					/*a2c:*/  (int)0x3ec00548,  // cwd  $72,0($10)
					/*a30:*/  (int)0x3ee00545,  // cdd  $69,0($10)
					/*a34:*/  (int)0x3fe245ce,  // shlqbyi  $78,$11,9
					/*a38:*/  (int)0x1604024f,  // andbi  $79,$4,16
					/*a3c:*/  (int)0xb952e64d,  // shufb  $74,$76,$75,$77
					/*a40:*/  (int)0x0833e744,  // or  $68,$78,$79
					/*a44:*/  (int)0x2892454a,  // stqx  $74,$10,$73
					/*a48:*/  (int)0x34000546,  // lqd  $70,0($10)
					/*a4c:*/  (int)0x34004543,  // lqd  $67,16($10)
					/*a50:*/  (int)0xb851a3c8,  // shufb  $66,$71,$70,$72
					/*a54:*/  (int)0xb830e245,  // shufb  $65,$68,$67,$69
					/*a58:*/  (int)0x24000542,  // stqd  $66,0($10)
					/*a5c:*/  (int)0x24004541,  // stqd  $65,16($10)
					/*a60:*/  (int)0x4020007f,  // nop  $127
					/*a64:*/  (int)0x35000000,  // bi  $0
					/*a68:*/  (int)0x40800114,  // il  $20,2
					/*a6c:*/  (int)0x34000513,  // lqd  $19,0($10)
					/*a70:*/  (int)0x3ec00515,  // cwd  $21,0($10)
					/*a74:*/  (int)0xb244ca15,  // shufb  $18,$20,$19,$21
					/*a78:*/  (int)0x24000512,  // stqd  $18,0($10)
					/*a7c:*/  (int)0x35000000,  // bi  $0
					/*a80:*/  (int)0x40800034,  // il  $52,0
					/*a84:*/  (int)0x35800009,  // hbr  aa8 <__unpack_d+0x1c0>,$0
					/*a88:*/  (int)0x780d03b3,  // ceq  $51,$7,$52
					/*a8c:*/  (int)0x360019b2,  // gb  $50,$51
					/*a90:*/  (int)0x4c02d931,  // cgti  $49,$50,11
					/*a94:*/  (int)0x20000331,  // brz  $49,aac <__unpack_d+0x1c4>  # aac
					/*a98:*/  (int)0x34000536,  // lqd  $54,0($10)
					/*a9c:*/  (int)0x3ec00537,  // cwd  $55,0($10)
					/*aa0:*/  (int)0xb6ad8437,  // shufb  $53,$8,$54,$55
					/*aa4:*/  (int)0x24000535,  // stqd  $53,0($10)
					/*aa8:*/  (int)0x35000000,  // bi  $0
					/*aac:*/  (int)0x3580000c,  // hbr  adc <__unpack_d+0x1f4>,$0
					/*ab0:*/  (int)0x3f3f43c0,  // rotqmbii  $64,$7,-3
					/*ab4:*/  (int)0x34000539,  // lqd  $57,0($10)
					/*ab8:*/  (int)0x3400453e,  // lqd  $62,16($10)
					/*abc:*/  (int)0x3ec0053b,  // cwd  $59,0($10)
					/*ac0:*/  (int)0x3fbea03d,  // rotqmbyi  $61,$64,-6
					/*ac4:*/  (int)0x3ee0053f,  // cdd  $63,0($10)
					/*ac8:*/  (int)0x3fe11eba,  // shlqbyi  $58,$61,4
					/*acc:*/  (int)0xb78f83bf,  // shufb  $60,$7,$62,$63
					/*ad0:*/  (int)0xb70e5d3b,  // shufb  $56,$58,$57,$59
					/*ad4:*/  (int)0x2400453c,  // stqd  $60,16($10)
					/*ad8:*/  (int)0x24000538,  // stqd  $56,0($10)
					/*adc:*/  (int)0x35000000,  // bi  $0
				};
			}
		}

		#endregion

		private static readonly int[] s_fixdfsiRawCode = GetPackdRawCode();

		#region GetFixdfsiRawCode
		static int[] GetFixdfsiRawCode()
		{
			unchecked
			{
				return new int[]
				{
					// 000004d8 <__fixdfsi>:
					/*4d8:*/  (int)0x1200820c,  // hbrr  508 <__fixdfsi+0x30>,8e8 <__unpack_d>  # 8e8
					/*4dc:*/  (int)0x24004080,  // stqd  $0,16($1)
					/*4e0:*/  (int)0x24ffc0fe,  // stqd  $126,-16($1)
					/*4e4:*/  (int)0x24fe8081,  // stqd  $1,-96($1)
					/*4e8:*/  (int)0x1ce80081,  // ai  $1,$1,-96
					/*4ec:*/  (int)0x00200000,  // lnop
					/*4f0:*/  (int)0x1c0c0084,  // ai  $4,$1,48  # 30
					/*4f4:*/  (int)0x34008087,  // lqd  $7,32($1)  # 20
					/*4f8:*/  (int)0x3ee00085,  // cdd  $5,0($1)
					/*4fc:*/  (int)0xb0c1c185,  // shufb  $6,$3,$7,$5
					/*500:*/  (int)0x1c080083,  // ai  $3,$1,32  # 20
					/*504:*/  (int)0x24008086,  // stqd  $6,32($1)  # 20
					/*508:*/  (int)0x33007c00,  // brsl  $0,8e8 <__unpack_d>  # 8e8
					/*50c:*/  (int)0x3400c084,  // lqd  $4,48($1)  # 30
					/*510:*/  (int)0x7c008202,  // ceqi  $2,$4,2
					/*514:*/  (int)0x21000502,  // brnz  $2,53c <__fixdfsi+0x64>  # 53c
					/*518:*/  (int)0x5c004208,  // clgti  $8,$4,1
					/*51c:*/  (int)0x20000408,  // brz  $8,53c <__fixdfsi+0x64>  # 53c
					/*520:*/  (int)0x7c010209,  // ceqi  $9,$4,4
					/*524:*/  (int)0x20000589,  // brz  $9,550 <__fixdfsi+0x78>  # 550
					/*528:*/  (int)0x3f81020c,  // rotqbyi  $12,$4,4
					/*52c:*/  (int)0x21000e0c,  // brnz  $12,59c <__fixdfsi+0xc4>  # 59c
					/*530:*/  (int)0x413fff83,  // ilhu  $3,32767  # 7fff
					/*534:*/  (int)0x60ffff83,  // iohl  $3,65535  # ffff
					/*538:*/  (int)0x32000100,  // br  540 <__fixdfsi+0x68>  # 540
					/*53c:*/  (int)0x40800003,  // il  $3,0
					/*540:*/  (int)0x1c180081,  // ai  $1,$1,96  # 60
					/*544:*/  (int)0x34004080,  // lqd  $0,16($1)
					/*548:*/  (int)0x34ffc0fe,  // lqd  $126,-16($1)
					/*54c:*/  (int)0x35000000,  // bi  $0
					/*550:*/  (int)0x3f82020a,  // rotqbyi  $10,$4,8
					/*554:*/  (int)0x4cffc503,  // cgti  $3,$10,-1
					/*558:*/  (int)0x207ffc83,  // brz  $3,53c <__fixdfsi+0x64>  # 53c
					/*55c:*/  (int)0x4c07850b,  // cgti  $11,$10,30  # 1e
					/*560:*/  (int)0x4020007f,  // nop  $127
					/*564:*/  (int)0x217ff88b,  // brnz  $11,528 <__fixdfsi+0x50>  # 528
					/*568:*/  (int)0x0c0f0512,  // sfi  $18,$10,60  # 3c
					/*56c:*/  (int)0x34010090,  // lqd  $16,64($1)  # 40
					/*570:*/  (int)0x4020007f,  // nop  $127
					/*574:*/  (int)0x127ff989,  // hbrr  598 <__fixdfsi+0xc0>,540 <__fixdfsi+0x68>  # 540
					/*578:*/  (int)0x0c000911,  // sfi  $17,$18,0
					/*57c:*/  (int)0x3f810204,  // rotqbyi  $4,$4,4
					/*580:*/  (int)0x0c01c90f,  // sfi  $15,$18,7
					/*584:*/  (int)0x3b24480e,  // rotqmbi  $14,$16,$17
					/*588:*/  (int)0x39a3c70d,  // rotqmbybi  $13,$14,$15
					/*58c:*/  (int)0x3fe10683,  // shlqbyi  $3,$13,4
					/*590:*/  (int)0x207ff604,  // brz  $4,540 <__fixdfsi+0x68>  # 540
					/*594:*/  (int)0x0c000183,  // sfi  $3,$3,0
					/*598:*/  (int)0x327ff500,  // br  540 <__fixdfsi+0x68>  # 540
					/*59c:*/  (int)0x41400003,  // ilhu  $3,32768  # 8000
					/*5a0:*/  (int)0x327ff400,  // br  540 <__fixdfsi+0x68>  # 540
					/*5a4:*/  (int)0x00200000,  // lnop
				};
				
			}
		}
		#endregion

		private static readonly int[] s_divdf3RawCode = GetDivdf3RawCode();

		#region GetDivdf3RawCode
		private static int[] GetDivdf3RawCode()
		{
			unchecked
			{
				return new int[]
				{
					// 00000218 <__divdf3>:
					/*218:*/  (int)0x1200da17,  // hbrr  274 <__divdf3+0x5c>,8e8 <__unpack_d>  # 8e8
					/*21c:*/  (int)0x24004080,  // stqd  $0,16($1)
					/*220:*/  (int)0x24ffc0d0,  // stqd  $80,-16($1)
					/*224:*/  (int)0x24ff80d1,  // stqd  $81,-32($1)
					/*228:*/  (int)0x24ff40d2,  // stqd  $82,-48($1)
					/*22c:*/  (int)0x24ff00fe,  // stqd  $126,-64($1)
					/*230:*/  (int)0x42011c09,  // ila  $9,568  # 238
					/*234:*/  (int)0x330000fe,  // brsl  $126,238 <__divdf3+0x20>  # 238
					/*238:*/  (int)0x24fd0081,  // stqd  $1,-192($1)
					/*23c:*/  (int)0x1cd00081,  // ai  $1,$1,-192
					/*240:*/  (int)0x1c1000d2,  // ai  $82,$1,64  # 40
					/*244:*/  (int)0x3400c087,  // lqd  $7,48($1)  # 30
					/*248:*/  (int)0x1c1800d0,  // ai  $80,$1,96  # 60
					/*24c:*/  (int)0x34008088,  // lqd  $8,32($1)  # 20
					/*250:*/  (int)0x081f84fe,  // sf  $126,$9,$126
					/*254:*/  (int)0x3ee000d1,  // cdd  $81,0($1)
					/*258:*/  (int)0x4020007f,  // nop  $127
					/*25c:*/  (int)0xb0a1c251,  // shufb  $5,$4,$7,$81
					/*260:*/  (int)0x04002904,  // ori  $4,$82,0
					/*264:*/  (int)0xb0c201d1,  // shufb  $6,$3,$8,$81
					/*268:*/  (int)0x1c080083,  // ai  $3,$1,32  # 20
					/*26c:*/  (int)0x2400c085,  // stqd  $5,48($1)  # 30
					/*270:*/  (int)0x24008086,  // stqd  $6,32($1)  # 20
					/*274:*/  (int)0x3300ce80,  // brsl  $0,8e8 <__unpack_d>  # 8e8
					/*278:*/  (int)0x1c0c0083,  // ai  $3,$1,48  # 30
					/*27c:*/  (int)0x3fe02804,  // shlqbyi  $4,$80,0
					/*280:*/  (int)0x4020007f,  // nop  $127
					/*284:*/  (int)0x3300cc80,  // brsl  $0,8e8 <__unpack_d>  # 8e8
					/*288:*/  (int)0x0400290a,  // ori  $10,$82,0
					/*28c:*/  (int)0x34010087,  // lqd  $7,64($1)  # 40
					/*290:*/  (int)0x5c004382,  // clgti  $2,$7,1
					/*294:*/  (int)0x20000a82,  // brz  $2,2e8 <__divdf3+0xd0>  # 2e8
					/*298:*/  (int)0x0400280a,  // ori  $10,$80,0
					/*29c:*/  (int)0x3401808b,  // lqd  $11,96($1)  # 60
					/*2a0:*/  (int)0x5c004583,  // clgti  $3,$11,1
					/*2a4:*/  (int)0x20000883,  // brz  $3,2e8 <__divdf3+0xd0>  # 2e8
					/*2a8:*/  (int)0x7c008395,  // ceqi  $21,$7,2
					/*2ac:*/  (int)0x3f810393,  // rotqbyi  $19,$7,4
					/*2b0:*/  (int)0x4209300a,  // ila  $10,4704  # 1260
					/*2b4:*/  (int)0x3f810594,  // rotqbyi  $20,$11,4
					/*2b8:*/  (int)0x7c01038f,  // ceqi  $15,$7,4
					/*2bc:*/  (int)0x3ec10091,  // cwd  $17,4($1)
					/*2c0:*/  (int)0x09254a92,  // nor  $18,$21,$21
					/*2c4:*/  (int)0x7801c584,  // ceq  $4,$11,$7
					/*2c8:*/  (int)0x48250990,  // xor  $16,$19,$20
					/*2cc:*/  (int)0x181f850d,  // a  $13,$10,$126
					/*2d0:*/  (int)0x5823c90c,  // andc  $12,$18,$15
					/*2d4:*/  (int)0xb0c1c811,  // shufb  $6,$16,$7,$17
					/*2d8:*/  (int)0x81436904,  // selb  $10,$82,$13,$4
					/*2dc:*/  (int)0x24010086,  // stqd  $6,64($1)  # 40
					/*2e0:*/  (int)0x2100060c,  // brnz  $12,310 <__divdf3+0xf8>  # 310
					/*2e4:*/  (int)0x00200000,  // lnop
					/*2e8:*/  (int)0x04000503,  // ori  $3,$10,0
					/*2ec:*/  (int)0x00200000,  // lnop
					/*2f0:*/  (int)0x33005700,  // brsl  $0,5a8 <__pack_d>  # 5a8
					/*2f4:*/  (int)0x1c300081,  // ai  $1,$1,192  # c0
					/*2f8:*/  (int)0x34004080,  // lqd  $0,16($1)
					/*2fc:*/  (int)0x34ffc0d0,  // lqd  $80,-16($1)
					/*300:*/  (int)0x34ff80d1,  // lqd  $81,-32($1)
					/*304:*/  (int)0x34ff40d2,  // lqd  $82,-48($1)
					/*308:*/  (int)0x34ff00fe,  // lqd  $126,-64($1)
					/*30c:*/  (int)0x35000000,  // bi  $0
					/*310:*/  (int)0x7c010596,  // ceqi  $22,$11,4
					/*314:*/  (int)0x127ffa8c,  // hbrr  344 <__divdf3+0x12c>,2e8 <__divdf3+0xd0>  # 2e8
					/*318:*/  (int)0x4020007f,  // nop  $127
					/*31c:*/  (int)0x20000596,  // brz  $22,348 <__divdf3+0x130>  # 348
					/*320:*/  (int)0x4080001a,  // il  $26,0
					/*324:*/  (int)0x34014098,  // lqd  $24,80($1)  # 50
					/*328:*/  (int)0x40800019,  // il  $25,0
					/*32c:*/  (int)0x3ec2009b,  // cwd  $27,8($1)
					/*330:*/  (int)0x0400290a,  // ori  $10,$82,0
					/*334:*/  (int)0xb2e18d1b,  // shufb  $23,$26,$6,$27
					/*338:*/  (int)0xb1660cd1,  // shufb  $11,$25,$24,$81
					/*33c:*/  (int)0x24010097,  // stqd  $23,64($1)  # 40
					/*340:*/  (int)0x2401408b,  // stqd  $11,80($1)  # 50
					/*344:*/  (int)0x327ff480,  // br  2e8 <__divdf3+0xd0>  # 2e8
					/*348:*/  (int)0x7c00859c,  // ceqi  $28,$11,2
					/*34c:*/  (int)0x21002e9c,  // brnz  $28,4c0 <__divdf3+0x2a8>  # 4c0
					/*350:*/  (int)0x340140a6,  // lqd  $38,80($1)  # 50
					/*354:*/  (int)0x3401c0a5,  // lqd  $37,112($1)  # 70
					/*358:*/  (int)0x3f8205a1,  // rotqbyi  $33,$11,8
					/*35c:*/  (int)0x3f820320,  // rotqbyi  $32,$6,8
					/*360:*/  (int)0x3ec20087,  // cwd  $7,8($1)
					/*364:*/  (int)0x04001304,  // ori  $4,$38,0
					/*368:*/  (int)0x580992a4,  // clgt  $36,$37,$38
					/*36c:*/  (int)0x3fe01288,  // shlqbyi  $8,$37,0
					/*370:*/  (int)0x08081082,  // sf  $2,$33,$32
					/*374:*/  (int)0x780992a2,  // ceq  $34,$37,$38
					/*378:*/  (int)0x54c01223,  // xswd  $35,$36
					/*37c:*/  (int)0xb0618107,  // shufb  $3,$2,$6,$7
					/*380:*/  (int)0x80a8d222,  // selb  $5,$36,$35,$34
					/*384:*/  (int)0x24010083,  // stqd  $3,64($1)  # 40
					/*388:*/  (int)0x4020007f,  // nop  $127
					/*38c:*/  (int)0x20000585,  // brz  $5,3b8 <__divdf3+0x1a0>  # 3b8
					/*390:*/  (int)0x1cffc12b,  // ai  $43,$2,-1
					/*394:*/  (int)0x3f82132d,  // rotqbyi  $45,$38,8
					/*398:*/  (int)0xb4e0d587,  // shufb  $39,$43,$3,$7
					/*39c:*/  (int)0x3fe216ac,  // shlqbyi  $44,$45,8
					/*3a0:*/  (int)0x240100a7,  // stqd  $39,64($1)  # 40
					/*3a4:*/  (int)0x3f605604,  // shlqbii  $4,$44,1
					/*3a8:*/  (int)0x580112aa,  // clgt  $42,$37,$4
					/*3ac:*/  (int)0x780112a8,  // ceq  $40,$37,$4
					/*3b0:*/  (int)0x54c01529,  // xswd  $41,$42
					/*3b4:*/  (int)0x80aa5528,  // selb  $5,$42,$41,$40
					/*3b8:*/  (int)0x4080000c,  // il  $12,0
					/*3bc:*/  (int)0x12000298,  // hbrr  41c <__divdf3+0x204>,3d0 <__divdf3+0x1b8>  # 3d0
					/*3c0:*/  (int)0x40801e8a,  // il  $10,61  # 3d
					/*3c4:*/  (int)0x32c0402e,  // fsmbi  $46,32896  # 8080
					/*3c8:*/  (int)0x3381cf0d,  // lqr  $13,1240 <_fini+0x50>  # 1240
					/*3cc:*/  (int)0x1604170b,  // andbi  $11,$46,16
					/*3d0:*/  (int)0x0841043a,  // bg  $58,$8,$4
					/*3d4:*/  (int)0x7c0002b9,  // ceqi  $57,$5,0
					/*3d8:*/  (int)0x0822c636,  // or  $54,$12,$11
					/*3dc:*/  (int)0xb70e9d0d,  // shufb  $56,$58,$58,$13
					/*3e0:*/  (int)0x1cffc50a,  // ai  $10,$10,-1
					/*3e4:*/  (int)0x36801cb7,  // fsm  $55,$57
					/*3e8:*/  (int)0x3f3fc58b,  // rotqmbii  $11,$11,-1
					/*3ec:*/  (int)0x68210438,  // sfx  $56,$8,$4
					/*3f0:*/  (int)0x818d8637,  // selb  $12,$12,$54,$55
					/*3f4:*/  (int)0x868e0237,  // selb  $52,$4,$56,$55
					/*3f8:*/  (int)0x3f821a33,  // rotqbyi  $51,$52,8
					/*3fc:*/  (int)0x3fe219b2,  // shlqbyi  $50,$51,8
					/*400:*/  (int)0x4020007f,  // nop  $127
					/*404:*/  (int)0x3f605909,  // shlqbii  $9,$50,1
					/*408:*/  (int)0x58024431,  // clgt  $49,$8,$9
					/*40c:*/  (int)0x3fe00484,  // shlqbyi  $4,$9,0
					/*410:*/  (int)0x7802442f,  // ceq  $47,$8,$9
					/*414:*/  (int)0x54c018b0,  // xswd  $48,$49
					/*418:*/  (int)0x80ac18af,  // selb  $5,$49,$48,$47
					/*41c:*/  (int)0x217ff68a,  // brnz  $10,3d0 <__divdf3+0x1b8>  # 3d0
					/*420:*/  (int)0x40800002,  // il  $2,0
					/*424:*/  (int)0x3281010e,  // fsmbi  $14,514  # 202
					/*428:*/  (int)0x0400290a,  // ori  $10,$82,0
					/*42c:*/  (int)0x32808086,  // fsmbi  $6,257  # 101
					/*430:*/  (int)0x32808085,  // fsmbi  $5,257  # 101
					/*434:*/  (int)0x3381c38d,  // lqr  $13,1250 <_fini+0x60>  # 1250
					/*438:*/  (int)0x15404703,  // andhi  $3,$14,257  # 101
					/*43c:*/  (int)0x127fd5a0,  // hbrr  4bc <__divdf3+0x2a4>,2e8 <__divdf3+0xd0>  # 2e8
					/*440:*/  (int)0x16e0034f,  // andbi  $79,$6,-128
					/*444:*/  (int)0x32ff7f49,  // fsmbi  $73,65278  # fefe
					/*448:*/  (int)0x1820c607,  // and  $7,$12,$3
					/*44c:*/  (int)0x340140bc,  // lqd  $60,80($1)  # 50
					/*450:*/  (int)0x1821464e,  // and  $78,$12,$5
					/*454:*/  (int)0x3ee000be,  // cdd  $62,0($1)
					/*458:*/  (int)0x7801c489,  // ceq  $9,$9,$7
					/*45c:*/  (int)0x780083d2,  // ceq  $82,$7,$2
					/*460:*/  (int)0x18538604,  // cg  $4,$12,$78
					/*464:*/  (int)0x36000488,  // gb  $8,$9
					/*468:*/  (int)0x7813e74d,  // ceq  $77,$78,$79
					/*46c:*/  (int)0x36002951,  // gb  $81,$82
					/*470:*/  (int)0xb901020d,  // shufb  $72,$4,$4,$13
					/*474:*/  (int)0x360026cc,  // gb  $76,$77
					/*478:*/  (int)0x4c02c450,  // cgti  $80,$8,11
					/*47c:*/  (int)0x4c02e8ca,  // cgti  $74,$81,11
					/*480:*/  (int)0x7c00284b,  // ceqi  $75,$80,0
					/*484:*/  (int)0x68138648,  // addx  $72,$12,$78
					/*488:*/  (int)0x4c02e647,  // cgti  $71,$76,11
					/*48c:*/  (int)0x368025c5,  // fsm  $69,$75
					/*490:*/  (int)0x7c002546,  // ceqi  $70,$74,0
					/*494:*/  (int)0x18326444,  // and  $68,$72,$73
					/*498:*/  (int)0x7c0023c3,  // ceqi  $67,$71,0
					/*49c:*/  (int)0x36802341,  // fsm  $65,$70
					/*4a0:*/  (int)0x88510645,  // selb  $66,$12,$68,$69
					/*4a4:*/  (int)0x368021bf,  // fsm  $63,$67
					/*4a8:*/  (int)0x88032141,  // selb  $64,$66,$12,$65
					/*4ac:*/  (int)0x87a3203f,  // selb  $61,$64,$12,$63
					/*4b0:*/  (int)0xb76f1ebe,  // shufb  $59,$61,$60,$62
					/*4b4:*/  (int)0x240140bb,  // stqd  $59,80($1)  # 50
					/*4b8:*/  (int)0x4020007f,  // nop  $127
					/*4bc:*/  (int)0x327fc580,  // br  2e8 <__divdf3+0xd0>  # 2e8
					/*4c0:*/  (int)0x4080021e,  // il  $30,4
					/*4c4:*/  (int)0x3ec0009f,  // cwd  $31,0($1)
					/*4c8:*/  (int)0x0400290a,  // ori  $10,$82,0
					/*4cc:*/  (int)0xb3a18f1f,  // shufb  $29,$30,$6,$31
					/*4d0:*/  (int)0x2401009d,  // stqd  $29,64($1)  # 40
					/*4d4:*/  (int)0x327fc280,  // br  2e8 <__divdf3+0xd0>  # 2e8
				};
			}
		}
		#endregion


		private PatchRoutine _packd;
		public SpuRoutine Packd
		{
			get
			{
				if (_packd == null)
				{
					_packd = new PatchRoutine("Packd", s_packdRawCode);
					const int packpos = 0x5a8;


//					_packd.Seek(0x1234);
//					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(7), RegisterConstant(1, 2, 3, 4)); // lqr  $7,1290 <__thenan_df+0x30>  # 1290

					_packd.Seek(0x5e0 - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(7), DoubleFractionPreferred); // lqr  $7,1290 <__thenan_df+0x30>  # 1290
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(4), DoubleExponentPreferred); // lqr  $4,1280 <__thenan_df+0x20>  # 1280

					_packd.Seek(0x684 - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(12), Double04050607_8); // lqr  $12,1250 <_fini+0x60>  # 1250

					_packd.Seek(0x68c - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(34), Double04050607_c); // lqr  $34,1240 <_fini+0x50>  # 1240

					_packd.Seek(0x7c0 - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(59), Double60FractionBitsPreferred); // lqr  $59,12a0 <__thenan_df+0x40>  # 12a0

					_packd.Seek(0x73c - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(14), Double11FractionBitsPreferred); // lqr  $14,12b0 <__thenan_df+0x50>  # 12b0

					_packd.Seek(0x744 - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(16), DoubleFractionPreferred); // lqr  $16,1290 <__thenan_df+0x30>  # 1290

					_packd.Seek(0x76c - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(4), DoubleExponentPreferred); // lqr  $4,1280 <__thenan_df+0x20>  # 1280

					_packd.Seek(0x7b8 - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(36), Double04050607_8); // lqr  $36,1250 <_fini+0x60>  # 1250

					_packd.Seek(0x7d4 - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(57), Double61FractionBitsPreferred); // lqr  $57,12c0 <__thenan_df+0x60>  # 12c0
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(44), Double11FractionBitsPreferred); // lqr  $44,12b0 <__thenan_df+0x50>  # 12b0

					_packd.Seek(0x7e4 - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(42), DoubleFractionPreferred); // lqr  $42,1290 <__thenan_df+0x30>  # 1290

					_packd.Seek(0x85c - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(32), Double04050607_8); // lqr  $32,1250 <_fini+0x60>  # 1250

					_packd.Seek(0x87c - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(8), Double60FractionBitsPreferred); // lqr  $8,12a0 <__thenan_df+0x40>  # 12a0

					_packd.Seek(0x884 - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(76), Double60FractionBitsPreferred); // lqr  $76,12a0 <__thenan_df+0x40>  # 12a0
				}
				return _packd;
			}
		}

		private PatchRoutine _unpackd;
		public SpuRoutine Unpackd
		{
			get
			{
				if (_unpackd == null)
				{
					_unpackd = new PatchRoutine("Unpackd", s_unpackdRawCode);
					const int packpos = 0x8e8;

					_unpackd.Seek(0x908 - packpos);
					_unpackd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(13), DoubleFractionPreferred); // lqr  $13,1290 <__thenan_df+0x30>  # 1290

					_unpackd.Seek(0x974 - packpos);
					_unpackd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(8), Double60FractionBitsPreferred); // lqr  $8,12a0 <__thenan_df+0x40>  # 12a0
				}
				return _unpackd;
			}
		}

		private PatchRoutine _fixdfsi;
		public SpuRoutine Fixdfsi
		{
			get
			{
				if (_fixdfsi == null)
				{
					_fixdfsi = new PatchRoutine("Fixdfsi", s_fixdfsiRawCode);
					const int packpos = 0x4d8;

					// TODO Patch the branch hint:
					// /*4d8:*/  (int)0x1200820c,  // hbrr  508 <__fixdfsi+0x30>,8e8 <__unpack_d>  # 8e8

					_fixdfsi.Seek(0x508 - packpos);
					_fixdfsi.Writer.WriteRelativeAddressInstruction(SpuOpCode.brsl, HardwareRegister.LR, Unpackd); // brsl  $0,8e8 <__unpack_d>  # 8e8
				}
				return _fixdfsi;
			}
		}

		private PatchRoutine _divdf3;
		public SpuRoutine Divdf3
		{
			get
			{
				if (_divdf3 == null)
				{
					_divdf3 = new PatchRoutine("Divdf3", s_divdf3RawCode);
					const int packpos = 0x218;

					// TODO Patch the branch hint:
					// /*218:*/  (int)0x1200da17,  // hbrr  274 <__divdf3+0x5c>,8e8 <__unpack_d>  # 8e8

					_divdf3.Seek(0x274 - packpos);
					_divdf3.Writer.WriteRelativeAddressInstruction(SpuOpCode.brsl, HardwareRegister.LR, Unpackd); // brsl  $0,8e8 <__unpack_d>  # 8e8

					_divdf3.Seek(0x284 - packpos);
					_divdf3.Writer.WriteRelativeAddressInstruction(SpuOpCode.brsl, HardwareRegister.LR, Unpackd); // brsl  $0,8e8 <__unpack_d>  # 8e8

					_divdf3.Seek(0x2f0 - packpos);
					_divdf3.Writer.WriteRelativeAddressInstruction(SpuOpCode.brsl, HardwareRegister.LR, Packd); // brsl  $0,5a8 <__pack_d>  # 5a8

					_divdf3.Seek(0x3c8 - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(13), Double04050607_c); // lqr  $13,1240 <_fini+0x50>  # 1240

					_divdf3.Seek(0x434 - packpos);
					_packd.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(36), Double04050607_8); // lqr  $36,1250 <_fini+0x60>  # 1250
				}
				return _divdf3;
			}
		}

		public void Patch([NotNull] PatchRoutine routine)
		{
			switch (routine.Name)
			{
				case "acosd2":
					PatchAcosd2(routine);
					break;
				case "asind2":
					PatchAsind2(routine);
					break;
				case "atand2":
					PatchAtand2(routine);
					break;
				case "atan2d2":
					PatchAtan2d2(routine);
					break;
//				case "logf4":
//					PatchLogf4(routine);
//					break;
//				case "sqrtf4":
//					PatchAtan2f4(routine);
//					break;
				case "cosd2":
					PatchCosd2(routine);
					break;
				case "sind2":
					PatchSind2(routine);
					break;
				case "tand2":
					PatchTand2(routine);
					break;
				case "logd2":
					PatchLogd2(routine);
					break;
				case "sqrtd2":
					PatchSqrtd2(routine);
					break;
				case "divd2":
					PatchDivd2(routine);
					break;
				default:
					throw new ArgumentException("Don't know how to patch '" + routine.Name + "'.");
			}
		}

		private void PatchSqrtd2(PatchRoutine sqrtd2)
		{
			sqrtd2.Seek(0x4);
			sqrtd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(60), RegisterConstant(0xfff00000, 0x0, 0xfff00000, 0x0)); //     3254:	33 83 bf bc 	lqr	$60,5050 <_fini+0xd0>	# 5050

			sqrtd2.Seek(0x10);
			sqrtd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(58), RegisterConstant(0x3ff00000, 0x0, 0x3ff00000, 0x0)); //     3260:	33 83 c0 3a 	lqr	$58,5060 <_fini+0xe0>	# 5060

			sqrtd2.Seek(0x14);
			sqrtd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(59), RegisterConstant(0x43300000, 0x0, 0x43300000, 0x0)); //     3264:	33 83 c3 bb 	lqr	$59,5080 <_fini+0x100>	# 5080

			sqrtd2.Seek(0x18);
			sqrtd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(50), RegisterConstant(0x7fe00000, 0x0, 0x7fe00000, 0x0)); //     3268:	33 83 c1 32 	lqr	$50,5070 <_fini+0xf0>	# 5070

			sqrtd2.Seek(0x1c);
			sqrtd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(15), RegisterConstant(0x3fe00000, 0x0, 0x3fe00000, 0x0)); //     326c:	33 83 ba 8f 	lqr	$15,5040 <_fini+0xc0>	# 5040

			sqrtd2.Seek(0x24);
			sqrtd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(51), RegisterConstant(0x7fffffff, 0xffffffff, 0x7fffffff, 0xffffffff)); //     3274:	33 83 a5 b3 	lqr	$51,4fa0 <_fini+0x20>	# 4fa0

			sqrtd2.Seek(0x2c);
			sqrtd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(49), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //     327c:	33 83 a6 b1 	lqr	$49,4fb0 <_fini+0x30>	# 4fb0

			sqrtd2.Seek(0x34);
			sqrtd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(46), RegisterConstant(0x3fe00000, 0x0, 0x3fe00000, 0x0)); //     3284:	33 83 b7 ae 	lqr	$46,5040 <_fini+0xc0>	# 5040

			sqrtd2.Seek(0x3c);
			sqrtd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(55), RegisterConstant(0x10203, 0x10203, 0x8090a0b, 0x8090a0b)); //     328c:	33 83 a8 b7 	lqr	$55,4fd0 <_fini+0x50>	# 4fd0

			sqrtd2.Seek(0x50);
			sqrtd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(53), RegisterConstant(0x1a00000, 0x0, 0x1a00000, 0x0)); //     32a0:	33 83 be 35 	lqr	$53,5090 <_fini+0x110>	# 5090

			sqrtd2.Seek(0x54);
			sqrtd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(24), RegisterConstant(0x10203, 0x10111213, 0x8090a0b, 0x18191a1b)); //     32a4:	33 83 a3 98 	lqr	$24,4fc0 <_fini+0x40>	# 4fc0
		}

		private void PatchLogd2(PatchRoutine logd2)
		{
			logd2.Seek(0x4);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(37), RegisterConstant(0x3ff00000, 0x0, 0x3ff00000, 0x0)); //     292c:	33 84 e6 a5 	lqr	$37,5060 <_fini+0xe0>	# 5060

			logd2.Seek(0x10);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(38), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //     2938:	33 84 cf 26 	lqr	$38,4fb0 <_fini+0x30>	# 4fb0

			logd2.Seek(0x14);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(33), RegisterConstant(0xbf3b4e6e, 0x3586f61f, 0xbf3b4e6e, 0x3586f61f)); //     293c:	33 85 58 a1 	lqr	$33,5400 <_fini+0x480>	# 5400

			logd2.Seek(0x1c);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(35), RegisterConstant(0x3f02f0f8, 0x6b9ddbc6, 0x3f02f0f8, 0x6b9ddbc6)); //     2944:	33 85 67 a3 	lqr	$35,5480 <_fini+0x500>	# 5480

			logd2.Seek(0x24);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(32), RegisterConstant(0x3fc482ed, 0xd53203cf, 0x3fc482ed, 0xd53203cf)); //     294c:	33 85 58 a0 	lqr	$32,5410 <_fini+0x490>	# 5410

			logd2.Seek(0x2c);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(30), RegisterConstant(0x3f62b191, 0x5d28ec91, 0x3f62b191, 0x5d28ec91)); //     2954:	33 85 59 9e 	lqr	$30,5420 <_fini+0x4a0>	# 5420

			logd2.Seek(0x34);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(34), RegisterConstant(0xbfc26e35, 0x13571695, 0xbfc26e35, 0x13571695)); //     295c:	33 85 66 a2 	lqr	$34,5490 <_fini+0x510>	# 5490

			logd2.Seek(0x3c);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(29), RegisterConstant(0xbfc71505, 0x9a278867, 0xbfc71505, 0x9a278867)); //     2964:	33 85 59 9d 	lqr	$29,5430 <_fini+0x4b0>	# 5430

			logd2.Seek(0x44);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(20), RegisterConstant(0xbf8041c4, 0xaf4039d, 0xbf8041c4, 0xaf4039d)); //     296c:	33 85 5a 94 	lqr	$20,5440 <_fini+0x4c0>	# 5440

			logd2.Seek(0x4c);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(21), RegisterConstant(0x3fca616f, 0x12be5488, 0x3fca616f, 0x12be5488)); //     2974:	33 85 5b 95 	lqr	$21,5450 <_fini+0x4d0>	# 5450

			logd2.Seek(0x54);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(22), RegisterConstant(0x3f94528d, 0x53531e8, 0x3f94528d, 0x53531e8)); //     297c:	33 85 5c 96 	lqr	$22,5460 <_fini+0x4e0>	# 5460

			logd2.Seek(0x5c);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(23), RegisterConstant(0xbfcec709, 0x512b3c1f, 0xbfcec709, 0x512b3c1f)); //     2984:	33 85 5d 97 	lqr	$23,5470 <_fini+0x4f0>	# 5470

			logd2.Seek(0x64);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(15), RegisterConstant(0xbfa3aa64, 0x11ba1f37, 0xbfa3aa64, 0x11ba1f37)); //     298c:	33 85 62 8f 	lqr	$15,54a0 <_fini+0x520>	# 54a0

			logd2.Seek(0x68);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(16), RegisterConstant(0x3fd2776c, 0x4d5d60d6, 0x3fd2776c, 0x4d5d60d6)); //     2990:	33 85 64 10 	lqr	$16,54b0 <_fini+0x530>	# 54b0

			logd2.Seek(0x6c);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(17), RegisterConstant(0x3faf2594, 0x771a38ce, 0x3faf2594, 0x771a38ce)); //     2994:	33 85 65 91 	lqr	$17,54c0 <_fini+0x540>	# 54c0

			logd2.Seek(0x70);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(18), RegisterConstant(0xbfd71547, 0x650ea6a3, 0xbfd71547, 0x650ea6a3)); //     2998:	33 85 67 12 	lqr	$18,54d0 <_fini+0x550>	# 54d0

			logd2.Seek(0x74);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(19), RegisterConstant(0xbfb531f7, 0xe1a22620, 0xbfb531f7, 0xe1a22620)); //     299c:	33 85 68 93 	lqr	$19,54e0 <_fini+0x560>	# 54e0

			logd2.Seek(0x7c);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(14), RegisterConstant(0x3fdec709, 0xdc39880e, 0x3fdec709, 0xdc39880e)); //     29a4:	33 85 69 8e 	lqr	$14,54f0 <_fini+0x570>	# 54f0

			logd2.Seek(0x84);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(9), RegisterConstant(0x3fb9f4d9, 0x89634135, 0x3fb9f4d9, 0x89634135)); //     29ac:	33 85 6a 89 	lqr	$9,5500 <_fini+0x580>	# 5500

			logd2.Seek(0x8c);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(10), RegisterConstant(0xbfe71547, 0x652b8294, 0xbfe71547, 0x652b8294)); //     29b4:	33 85 6b 8a 	lqr	$10,5510 <_fini+0x590>	# 5510

			logd2.Seek(0x90);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(11), RegisterConstant(0xbfbdd4f1, 0x35d4671c, 0xbfbdd4f1, 0x35d4671c)); //     29b8:	33 85 6d 0b 	lqr	$11,5520 <_fini+0x5a0>	# 5520

			logd2.Seek(0x98);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(6), RegisterConstant(0x3fdc551d, 0x94ae0bf8, 0x3fdc551d, 0x94ae0bf8)); //     29c0:	33 85 6e 06 	lqr	$6,5530 <_fini+0x5b0>	# 5530

			logd2.Seek(0x9c);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(7), RegisterConstant(0x3fc0a424, 0x28d49027, 0x3fc0a424, 0x28d49027)); //     29c4:	33 85 6f 87 	lqr	$7,5540 <_fini+0x5c0>	# 5540

			logd2.Seek(0xa4);
			logd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(2), RegisterConstant(0x3fe62e42, 0xfefa39ef, 0x3fe62e42, 0xfefa39ef)); //     29cc:	33 85 70 82 	lqr	$2,5550 <_fini+0x5d0>	# 5550
		}

		private void PatchAtan2d2(PatchRoutine atan2d2)
		{
			atan2d2.Seek(0x0);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(69), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //     1010:	33 86 48 45 	lqr	$69,4250 <_fini+0x38>	# 4250

			atan2d2.Seek(0x4);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(17), RegisterConstant(0x4050607, 0x10203, 0xc0d0e0f, 0x8090a0b)); //     1014:	33 86 6d 91 	lqr	$17,4380 <_fini+0x168>	# 4380

			atan2d2.Seek(0x10);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(24), RegisterConstant(0x3ff00000, 0x0, 0x3ff00000, 0x0)); //     1020:	33 86 5c 18 	lqr	$24,4300 <_fini+0xe8>	# 4300

			atan2d2.Seek(0x14);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(27), RegisterConstant(0xfff00000, 0x0, 0xfff00000, 0x0)); //     1024:	33 86 59 9b 	lqr	$27,42f0 <_fini+0xd8>	# 42f0

			atan2d2.Seek(0x2c);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(41), RegisterConstant(0xc020656c, 0x6ceafd5, 0xc020656c, 0x6ceafd5)); //     103c:	33 86 72 a9 	lqr	$41,43d0 <_fini+0x1b8>	# 43d0

			atan2d2.Seek(0x5c);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(21), RegisterConstant(0xfffff, 0xffffffff, 0xfffff, 0xffffffff)); //     106c:	33 86 6e 95 	lqr	$21,43e0 <_fini+0x1c8>	# 43e0

			atan2d2.Seek(0x8c);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(44), RegisterConstant(0x3fe, 0x0, 0x3fe, 0x0)); //     109c:	33 86 6a ac 	lqr	$44,43f0 <_fini+0x1d8>	# 43f0

			atan2d2.Seek(0xc4);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(67), RegisterConstant(0x7fe, 0x0, 0x7fe, 0x0)); //     10d4:	33 86 6f c3 	lqr	$67,4450 <_fini+0x238>	# 4450

			atan2d2.Seek(0xcc);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(55), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //     10dc:	33 86 2e b7 	lqr	$55,4250 <_fini+0x38>	# 4250

			atan2d2.Seek(0xdc);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(8), RegisterConstant(0x4050607, 0xc0c0c0c0, 0xc0d0e0f, 0xc0c0c0c0)); //     10ec:	33 86 32 88 	lqr	$8,4280 <_fini+0x68>	# 4280

			atan2d2.Seek(0xe4);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(14), RegisterConstant(0x3ff00000, 0x0, 0x3ff00000, 0x0)); //     10f4:	33 86 41 8e 	lqr	$14,4300 <_fini+0xe8>	# 4300

			atan2d2.Seek(0xec);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(30), RegisterConstant(0x10203, 0x10203, 0x8090a0b, 0x8090a0b)); //     10fc:	33 86 2e 9e 	lqr	$30,4270 <_fini+0x58>	# 4270

			atan2d2.Seek(0xf4);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(22), RegisterConstant(0x43300000, 0x0, 0x43300000, 0x0)); //     1104:	33 86 43 96 	lqr	$22,4320 <_fini+0x108>	# 4320

			atan2d2.Seek(0x104);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(25), RegisterConstant(0x10203, 0x10203, 0x8090a0b, 0x8090a0b)); //     1114:	33 86 2b 99 	lqr	$25,4270 <_fini+0x58>	# 4270

			atan2d2.Seek(0x10c);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(50), RegisterConstant(0x1a00000, 0x0, 0x1a00000, 0x0)); //     111c:	33 86 42 b2 	lqr	$50,4330 <_fini+0x118>	# 4330

			atan2d2.Seek(0x114);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(53), RegisterConstant(0x7fffffff, 0xffffffff, 0x7fffffff, 0xffffffff)); //     1124:	33 86 23 b5 	lqr	$53,4240 <_fini+0x28>	# 4240

			atan2d2.Seek(0x11c);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(79), RegisterConstant(0x7fe00000, 0x0, 0x7fe00000, 0x0)); //     112c:	33 86 3c cf 	lqr	$79,4310 <_fini+0xf8>	# 4310

			atan2d2.Seek(0x124);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(60), RegisterConstant(0x3fe00000, 0x0, 0x3fe00000, 0x0)); //     1134:	33 86 35 bc 	lqr	$60,42e0 <_fini+0xc8>	# 42e0

			atan2d2.Seek(0x12c);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(77), RegisterConstant(0x3fe00000, 0x0, 0x3fe00000, 0x0)); //     113c:	33 86 34 cd 	lqr	$77,42e0 <_fini+0xc8>	# 42e0

			atan2d2.Seek(0x134);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(34), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //     1144:	33 86 21 a2 	lqr	$34,4250 <_fini+0x38>	# 4250

			atan2d2.Seek(0x144);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(56), RegisterConstant(0x7ff80000, 0x0, 0x7ff80000, 0x0)); //     1154:	33 86 61 b8 	lqr	$56,4460 <_fini+0x248>	# 4460

			atan2d2.Seek(0x154);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(43), RegisterConstant(0x10203, 0x10111213, 0x8090a0b, 0x18191a1b)); //     1164:	33 86 1f ab 	lqr	$43,4260 <_fini+0x48>	# 4260

			atan2d2.Seek(0x15c);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(51), RegisterConstant(0x40519fc0, 0x25fe9054, 0x40519fc0, 0x25fe9054)); //     116c:	33 86 3a b3 	lqr	$51,4340 <_fini+0x128>	# 4340

			atan2d2.Seek(0x164);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(52), RegisterConstant(0xc06265bb, 0x6d3576d7, 0xc06265bb, 0x6d3576d7)); //     1174:	33 86 3b b4 	lqr	$52,4350 <_fini+0x138>	# 4350

			atan2d2.Seek(0x16c);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(54), RegisterConstant(0x40617056, 0x84ffbf9d, 0x40617056, 0x84ffbf9d)); //     117c:	33 86 3c b6 	lqr	$54,4360 <_fini+0x148>	# 4360

			atan2d2.Seek(0x174);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(46), RegisterConstant(0xbfe34341, 0x333e5c16, 0xbfe34341, 0x333e5c16)); //     1184:	33 86 41 ae 	lqr	$46,4390 <_fini+0x178>	# 4390

			atan2d2.Seek(0x17c);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(47), RegisterConstant(0x4015c74b, 0x178a2dd9, 0x4015c74b, 0x178a2dd9)); //     118c:	33 86 42 af 	lqr	$47,43a0 <_fini+0x188>	# 43a0

			atan2d2.Seek(0x184);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(48), RegisterConstant(0xc0304331, 0xde27907b, 0xc0304331, 0xde27907b)); //     1194:	33 86 43 b0 	lqr	$48,43b0 <_fini+0x198>	# 43b0

			atan2d2.Seek(0x18c);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(49), RegisterConstant(0x40339007, 0xda779259, 0x40339007, 0xda779259)); //     119c:	33 86 44 b1 	lqr	$49,43c0 <_fini+0x1a8>	# 43c0

			atan2d2.Seek(0x1ac);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(57), RegisterConstant(0xc0489822, 0xa3607ac, 0xc0489822, 0xa3607ac)); //     11bc:	33 86 36 b9 	lqr	$57,4370 <_fini+0x158>	# 4370

			atan2d2.Seek(0x1bc);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(65), RegisterConstant(0x3ff921fb, 0x54442d18, 0x3ff921fb, 0x54442d18)); //     11cc:	33 86 48 c1 	lqr	$65,4410 <_fini+0x1f8>	# 4410

			atan2d2.Seek(0x1c4);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(64), RegisterConstant(0x400921fb, 0x54442d18, 0x400921fb, 0x54442d18)); //     11d4:	33 86 45 c0 	lqr	$64,4400 <_fini+0x1e8>	# 4400

			atan2d2.Seek(0x4d4);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(16), RegisterConstant(0xbff00000, 0x0, 0xbff00000, 0x0)); //     14e4:	33 85 e7 90 	lqr	$16,4420 <_fini+0x208>	# 4420

			atan2d2.Seek(0x5bc);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(37), RegisterConstant(0xc02d7b59, 0xb5e0eab, 0xc02d7b59, 0xb5e0eab)); //     15cc:	33 85 cc a5 	lqr	$37,4430 <_fini+0x218>	# 4430

			atan2d2.Seek(0x5d8);
			atan2d2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(18), RegisterConstant(0x3f716b9b, 0xbd48ad3, 0x3f716b9b, 0xbd48ad3)); //     15e8:	33 85 cb 12 	lqr	$18,4440 <_fini+0x228>	# 4440
		}

		private void PatchAtand2(PatchRoutine atand2)
		{
			atand2.Seek(0x10);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(45), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //     1000:	33 85 14 2d 	lqr	$45,38a0 <_fini+0x38>	# 38a0

			atand2.Seek(0x14);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(30), RegisterConstant(0x7fffffff, 0xffffffff, 0x7fffffff, 0xffffffff)); //     1004:	33 85 11 9e 	lqr	$30,3890 <_fini+0x28>	# 3890

			atand2.Seek(0x1c);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(34), RegisterConstant(0x4050607, 0xc0c0c0c0, 0xc0d0e0f, 0xc0c0c0c0)); //     100c:	33 85 18 a2 	lqr	$34,38d0 <_fini+0x68>	# 38d0

			atand2.Seek(0x24);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(39), RegisterConstant(0x10203, 0x10203, 0x8090a0b, 0x8090a0b)); //     1014:	33 85 15 a7 	lqr	$39,38c0 <_fini+0x58>	# 38c0

			atand2.Seek(0x2c);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(65), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //     101c:	33 85 10 c1 	lqr	$65,38a0 <_fini+0x38>	# 38a0

			atand2.Seek(0x3c);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(78), RegisterConstant(0x3ff00000, 0x0, 0x3ff00000, 0x0)); //     102c:	33 85 24 ce 	lqr	$78,3950 <_fini+0xe8>	# 3950

			atand2.Seek(0xdc);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(70), RegisterConstant(0x43300000, 0x0, 0x43300000, 0x0)); //     10cc:	33 85 14 c6 	lqr	$70,3970 <_fini+0x108>	# 3970

			atand2.Seek(0xe4);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(49), RegisterConstant(0x4050607, 0x10203, 0xc0d0e0f, 0x8090a0b)); //     10d4:	33 85 1f b1 	lqr	$49,39d0 <_fini+0x168>	# 39d0

			atand2.Seek(0xec);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(69), RegisterConstant(0x1a00000, 0x0, 0x1a00000, 0x0)); //     10dc:	33 85 14 c5 	lqr	$69,3980 <_fini+0x118>	# 3980

			atand2.Seek(0xf4);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(67), RegisterConstant(0x3fe00000, 0x0, 0x3fe00000, 0x0)); //     10e4:	33 85 09 c3 	lqr	$67,3930 <_fini+0xc8>	# 3930

			atand2.Seek(0x104);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(66), RegisterConstant(0x7fe00000, 0x0, 0x7fe00000, 0x0)); //     10f4:	33 85 0d c2 	lqr	$66,3960 <_fini+0xf8>	# 3960

			atand2.Seek(0x108);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(36), RegisterConstant(0x10203, 0x10203, 0x8090a0b, 0x8090a0b)); //     10f8:	33 84 f9 24 	lqr	$36,38c0 <_fini+0x58>	# 38c0

			atand2.Seek(0x11c);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(54), RegisterConstant(0x3fe00000, 0x0, 0x3fe00000, 0x0)); //     110c:	33 85 04 b6 	lqr	$54,3930 <_fini+0xc8>	# 3930

			atand2.Seek(0x124);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(78), RegisterConstant(0x10203, 0x10111213, 0x8090a0b, 0x18191a1b)); //     1114:	33 84 f3 ce 	lqr	$78,38b0 <_fini+0x48>	# 38b0

			atand2.Seek(0x12c);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(25), RegisterConstant(0x3ff00000, 0x0, 0x3ff00000, 0x0)); //     111c:	33 85 06 99 	lqr	$25,3950 <_fini+0xe8>	# 3950

			atand2.Seek(0x134);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(20), RegisterConstant(0xfff00000, 0x0, 0xfff00000, 0x0)); //     1124:	33 85 03 94 	lqr	$20,3940 <_fini+0xd8>	# 3940

			atand2.Seek(0x13c);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(37), RegisterConstant(0xfffff, 0xffffffff, 0xfffff, 0xffffffff)); //     112c:	33 85 20 a5 	lqr	$37,3a30 <_fini+0x1c8>	# 3a30

			atand2.Seek(0x144);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(50), RegisterConstant(0x3fe, 0x0, 0x3fe, 0x0)); //     1134:	33 85 21 b2 	lqr	$50,3a40 <_fini+0x1d8>	# 3a40

			atand2.Seek(0x14c);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(63), RegisterConstant(0x7fe, 0x0, 0x7fe, 0x0)); //     113c:	33 85 2c bf 	lqr	$63,3aa0 <_fini+0x238>	# 3aa0

			atand2.Seek(0x154);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(56), RegisterConstant(0x7ff80000, 0x0, 0x7ff80000, 0x0)); //     1144:	33 85 2d b8 	lqr	$56,3ab0 <_fini+0x248>	# 3ab0

			atand2.Seek(0x160);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(46), RegisterConstant(0x40519fc0, 0x25fe9054, 0x40519fc0, 0x25fe9054)); //     1150:	33 85 08 2e 	lqr	$46,3990 <_fini+0x128>	# 3990

			atand2.Seek(0x164);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(47), RegisterConstant(0xc06265bb, 0x6d3576d7, 0xc06265bb, 0x6d3576d7)); //     1154:	33 85 09 af 	lqr	$47,39a0 <_fini+0x138>	# 39a0

			atand2.Seek(0x170);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(44), RegisterConstant(0x40617056, 0x84ffbf9d, 0x40617056, 0x84ffbf9d)); //     1160:	33 85 0a 2c 	lqr	$44,39b0 <_fini+0x148>	# 39b0

			atand2.Seek(0x174);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(18), RegisterConstant(0xc0489822, 0xa3607ac, 0xc0489822, 0xa3607ac)); //     1164:	33 85 0b 92 	lqr	$18,39c0 <_fini+0x158>	# 39c0

			atand2.Seek(0x178);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(41), RegisterConstant(0xbfe34341, 0x333e5c16, 0xbfe34341, 0x333e5c16)); //     1168:	33 85 0f 29 	lqr	$41,39e0 <_fini+0x178>	# 39e0

			atand2.Seek(0x17c);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(42), RegisterConstant(0x4015c74b, 0x178a2dd9, 0x4015c74b, 0x178a2dd9)); //     116c:	33 85 10 aa 	lqr	$42,39f0 <_fini+0x188>	# 39f0

			atand2.Seek(0x180);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(43), RegisterConstant(0xc0304331, 0xde27907b, 0xc0304331, 0xde27907b)); //     1170:	33 85 12 2b 	lqr	$43,3a00 <_fini+0x198>	# 3a00

			atand2.Seek(0x184);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(39), RegisterConstant(0x40339007, 0xda779259, 0x40339007, 0xda779259)); //     1174:	33 85 13 a7 	lqr	$39,3a10 <_fini+0x1a8>	# 3a10

			atand2.Seek(0x18c);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(57), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //     117c:	33 84 e4 b9 	lqr	$57,38a0 <_fini+0x38>	# 38a0

			atand2.Seek(0x19c);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(61), RegisterConstant(0x3ff921fb, 0x54442d18, 0x3ff921fb, 0x54442d18)); //     118c:	33 85 1a bd 	lqr	$61,3a60 <_fini+0x1f8>	# 3a60

			atand2.Seek(0x1a0);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(17), RegisterConstant(0xc020656c, 0x6ceafd5, 0xc020656c, 0x6ceafd5)); //     1190:	33 85 12 11 	lqr	$17,3a20 <_fini+0x1b8>	# 3a20

			atand2.Seek(0x374);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(19), RegisterConstant(0xbff00000, 0x0, 0xbff00000, 0x0)); //     1364:	33 84 e1 93 	lqr	$19,3a70 <_fini+0x208>	# 3a70

			atand2.Seek(0x444);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(30), RegisterConstant(0xc02d7b59, 0xb5e0eab, 0xc02d7b59, 0xb5e0eab)); //     1434:	33 84 c9 9e 	lqr	$30,3a80 <_fini+0x218>	# 3a80

			atand2.Seek(0x46c);
			atand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(16), RegisterConstant(0x3f716b9b, 0xbd48ad3, 0x3f716b9b, 0xbd48ad3)); //     145c:	33 84 c6 90 	lqr	$16,3a90 <_fini+0x228>	# 3a90
		}

		private void PatchAsind2(PatchRoutine asind2)
		{
			asind2.Seek(0x4);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(50), RegisterConstant(0x3fe00000, 0x0, 0x3fe00000, 0x0)); //      a84:	33 85 d5 b2 	lqr	$50,3930 <_fini+0xc8>	# 3930

			asind2.Seek(0x1c);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(16), RegisterConstant(0xfff00000, 0x0, 0xfff00000, 0x0)); //      a9c:	33 85 d4 90 	lqr	$16,3940 <_fini+0xd8>	# 3940

			asind2.Seek(0x24);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(25), RegisterConstant(0x10203, 0x10203, 0x8090a0b, 0x8090a0b)); //      aa4:	33 85 c3 99 	lqr	$25,38c0 <_fini+0x58>	# 38c0

			asind2.Seek(0x2c);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(32), RegisterConstant(0x43300000, 0x0, 0x43300000, 0x0)); //      aac:	33 85 d8 a0 	lqr	$32,3970 <_fini+0x108>	# 3970

			asind2.Seek(0x34);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(34), RegisterConstant(0x3ff00000, 0x0, 0x3ff00000, 0x0)); //      ab4:	33 85 d3 a2 	lqr	$34,3950 <_fini+0xe8>	# 3950

			asind2.Seek(0x3c);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(26), RegisterConstant(0x1a00000, 0x0, 0x1a00000, 0x0)); //      abc:	33 85 d8 9a 	lqr	$26,3980 <_fini+0x118>	# 3980

			asind2.Seek(0x44);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(56), RegisterConstant(0x3ff921fb, 0x54442d18, 0x3ff921fb, 0x54442d18)); //      ac4:	33 85 f3 b8 	lqr	$56,3a60 <_fini+0x1f8>	# 3a60

			asind2.Seek(0x54);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(6), RegisterConstant(0x7fe00000, 0x0, 0x7fe00000, 0x0)); //      ad4:	33 85 d1 86 	lqr	$6,3960 <_fini+0xf8>	# 3960

			asind2.Seek(0x5c);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(27), RegisterConstant(0x7fffffff, 0xffffffff, 0x7fffffff, 0xffffffff)); //      adc:	33 85 b6 9b 	lqr	$27,3890 <_fini+0x28>	# 3890

			asind2.Seek(0x6c);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(14), RegisterConstant(0x3fe00000, 0x0, 0x3fe00000, 0x0)); //      aec:	33 85 c8 8e 	lqr	$14,3930 <_fini+0xc8>	# 3930

			asind2.Seek(0x7c);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(41), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //      afc:	33 85 b4 a9 	lqr	$41,38a0 <_fini+0x38>	# 38a0

			asind2.Seek(0x84);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(39), RegisterConstant(0x10203, 0x10111213, 0x8090a0b, 0x18191a1b)); //      b04:	33 85 b5 a7 	lqr	$39,38b0 <_fini+0x48>	# 38b0

			asind2.Seek(0x8c);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(22), RegisterConstant(0xc02d7b59, 0xb5e0eab, 0xc02d7b59, 0xb5e0eab)); //      b0c:	33 85 ee 96 	lqr	$22,3a80 <_fini+0x218>	# 3a80

			asind2.Seek(0x94);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(19), RegisterConstant(0x3f716b9b, 0xbd48ad3, 0x3f716b9b, 0xbd48ad3)); //      b14:	33 85 ef 93 	lqr	$19,3a90 <_fini+0x228>	# 3a90

			asind2.Seek(0x9c);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(23), RegisterConstant(0xbfe34341, 0x333e5c16, 0xbfe34341, 0x333e5c16)); //      b1c:	33 85 d8 97 	lqr	$23,39e0 <_fini+0x178>	# 39e0

			asind2.Seek(0xa4);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(28), RegisterConstant(0xbff00000, 0x0, 0xbff00000, 0x0)); //      b24:	33 85 e9 9c 	lqr	$28,3a70 <_fini+0x208>	# 3a70

			asind2.Seek(0xbc);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(31), RegisterConstant(0x40519fc0, 0x25fe9054, 0x40519fc0, 0x25fe9054)); //      b3c:	33 85 ca 9f 	lqr	$31,3990 <_fini+0x128>	# 3990

			asind2.Seek(0xcc);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(24), RegisterConstant(0x4015c74b, 0x178a2dd9, 0x4015c74b, 0x178a2dd9)); //      b4c:	33 85 d4 98 	lqr	$24,39f0 <_fini+0x188>	# 39f0

			asind2.Seek(0xd4);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(32), RegisterConstant(0xc06265bb, 0x6d3576d7, 0xc06265bb, 0x6d3576d7)); //      b54:	33 85 c9 a0 	lqr	$32,39a0 <_fini+0x138>	# 39a0

			asind2.Seek(0xdc);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(26), RegisterConstant(0xc0304331, 0xde27907b, 0xc0304331, 0xde27907b)); //      b5c:	33 85 d4 9a 	lqr	$26,3a00 <_fini+0x198>	# 3a00

			asind2.Seek(0xe4);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(25), RegisterConstant(0x40617056, 0x84ffbf9d, 0x40617056, 0x84ffbf9d)); //      b64:	33 85 c9 99 	lqr	$25,39b0 <_fini+0x148>	# 39b0

			asind2.Seek(0xec);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(17), RegisterConstant(0x40339007, 0xda779259, 0x40339007, 0xda779259)); //      b6c:	33 85 d4 91 	lqr	$17,3a10 <_fini+0x1a8>	# 3a10

			asind2.Seek(0x134);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(15), RegisterConstant(0xfffff, 0xffffffff, 0xfffff, 0xffffffff)); //      bb4:	33 85 cf 8f 	lqr	$15,3a30 <_fini+0x1c8>	# 3a30

			asind2.Seek(0x14c);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(7), RegisterConstant(0xc020656c, 0x6ceafd5, 0xc020656c, 0x6ceafd5)); //      bcc:	33 85 ca 87 	lqr	$7,3a20 <_fini+0x1b8>	# 3a20

			asind2.Seek(0x154);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(36), RegisterConstant(0x3fe, 0x0, 0x3fe, 0x0)); //      bd4:	33 85 cd a4 	lqr	$36,3a40 <_fini+0x1d8>	# 3a40

			asind2.Seek(0x15c);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(10), RegisterConstant(0xc0489822, 0xa3607ac, 0xc0489822, 0xa3607ac)); //      bdc:	33 85 bc 8a 	lqr	$10,39c0 <_fini+0x158>	# 39c0

			asind2.Seek(0x16c);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(73), RegisterConstant(0x4050607, 0x10203, 0xc0d0e0f, 0x8090a0b)); //      bec:	33 85 bc c9 	lqr	$73,39d0 <_fini+0x168>	# 39d0

			asind2.Seek(0x174);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(37), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //      bf4:	33 85 95 a5 	lqr	$37,38a0 <_fini+0x38>	# 38a0

			asind2.Seek(0x1d4);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(29), RegisterConstant(0x7fe, 0x0, 0x7fe, 0x0)); //      c54:	33 85 c9 9d 	lqr	$29,3aa0 <_fini+0x238>	# 3aa0

			asind2.Seek(0x23c);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(2), RegisterConstant(0x7ff80000, 0x0, 0x7ff80000, 0x0)); //      cbc:	33 85 be 82 	lqr	$2,3ab0 <_fini+0x248>	# 3ab0

			asind2.Seek(0x244);
			asind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(10), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //      cc4:	33 85 7b 8a 	lqr	$10,38a0 <_fini+0x38>	# 38a0
		}

		private void PatchTand2(PatchRoutine tand2)
		{
			tand2.Seek(0x4);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(53), RegisterConstant(0x3ff45f30, 0x6dcd2b16, 0x3ff45f30, 0x6dcd2b16)); //     2174:	33 83 29 b5 	lqr	$53,3ac0 <_fini+0x258>	# 3ac0

			tand2.Seek(0x8);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(29), RegisterConstant(0x3ff921fb, 0x54400000, 0x3ff921fb, 0x54400000)); //     2178:	33 83 41 1d 	lqr	$29,3b80 <_fini+0x318>	# 3b80

			tand2.Seek(0x10);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(44), RegisterConstant(0x3dd0b461, 0x1a626331, 0x3dd0b461, 0x1a626331)); //     2180:	33 83 42 2c 	lqr	$44,3b90 <_fini+0x328>	# 3b90

			tand2.Seek(0x14);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(43), RegisterConstant(0xbd6ae7f3, 0xe733b81f, 0xbd6ae7f3, 0xe733b81f)); //     2184:	33 83 45 ab 	lqr	$43,3bb0 <_fini+0x348>	# 3bb0

			tand2.Seek(0x18);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(42), RegisterConstant(0xbf2a01a0, 0x1a01a01a, 0xbf2a01a0, 0x1a01a01a)); //     2188:	33 83 47 2a 	lqr	$42,3bc0 <_fini+0x358>	# 3bc0

			tand2.Seek(0x1c);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(38), RegisterConstant(0x3de61246, 0x13a86d09, 0x3de61246, 0x13a86d09)); //     218c:	33 83 2a a6 	lqr	$38,3ae0 <_fini+0x278>	# 3ae0

			tand2.Seek(0x24);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(37), RegisterConstant(0x3f811111, 0x11111111, 0x3f811111, 0x11111111)); //     2194:	33 83 2b a5 	lqr	$37,3af0 <_fini+0x288>	# 3af0

			tand2.Seek(0x28);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(41), RegisterConstant(0xbda9079a, 0x27f38837, 0xbda9079a, 0x27f38837)); //     2198:	33 83 47 29 	lqr	$41,3bd0 <_fini+0x368>	# 3bd0

			tand2.Seek(0x2c);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(40), RegisterConstant(0xbf56c16c, 0x16c16955, 0xbf56c16c, 0x16c16955)); //     219c:	33 83 40 a8 	lqr	$40,3ba0 <_fini+0x338>	# 3ba0

			tand2.Seek(0x30);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(22), RegisterConstant(0x3e21eeb6, 0x358665fc, 0x3e21eeb6, 0x358665fc)); //     21a0:	33 83 2c 16 	lqr	$22,3b00 <_fini+0x298>	# 3b00

			tand2.Seek(0x34);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(36), RegisterConstant(0x3fa55555, 0x55555555, 0x3fa55555, 0x55555555)); //     21a4:	33 83 25 a4 	lqr	$36,3ad0 <_fini+0x268>	# 3ad0

			tand2.Seek(0x38);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(23), RegisterConstant(0xbe5ae645, 0x67f544e4, 0xbe5ae645, 0x67f544e4)); //     21a8:	33 83 2f 17 	lqr	$23,3b20 <_fini+0x2b8>	# 3b20

			tand2.Seek(0x3c);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(24), RegisterConstant(0xbfc55555, 0x55555555, 0xbfc55555, 0x55555555)); //     21ac:	33 83 30 98 	lqr	$24,3b30 <_fini+0x2c8>	# 3b30

			tand2.Seek(0x40);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(33), RegisterConstant(0xbe927e4f, 0xa1280e5c, 0xbe927e4f, 0xa1280e5c)); //     21b0:	33 83 32 21 	lqr	$33,3b40 <_fini+0x2d8>	# 3b40

			tand2.Seek(0x4c);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(25), RegisterConstant(0x3ff00000, 0x0, 0x3ff00000, 0x0)); //     21bc:	33 82 f2 99 	lqr	$25,3950 <_fini+0xe8>	# 3950

			tand2.Seek(0x50);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(32), RegisterConstant(0xbfe00000, 0x0, 0xbfe00000, 0x0)); //     21c0:	33 83 2a 20 	lqr	$32,3b10 <_fini+0x2a8>	# 3b10

			tand2.Seek(0x54);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(12), RegisterConstant(0x3ec71de3, 0xa556c734, 0x3ec71de3, 0xa556c734)); //     21c4:	33 83 31 8c 	lqr	$12,3b50 <_fini+0x2e8>	# 3b50

			tand2.Seek(0x58);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(26), RegisterConstant(0x3efa01a0, 0x19f4a8f3, 0x3efa01a0, 0x19f4a8f3)); //     21c8:	33 83 33 1a 	lqr	$26,3b60 <_fini+0x2f8>	# 3b60

			tand2.Seek(0x5c);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(48), RegisterConstant(0x10203, 0x10203, 0x8090a0b, 0x8090a0b)); //     21cc:	33 82 de b0 	lqr	$48,38c0 <_fini+0x58>	# 38c0

			tand2.Seek(0x64);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(71), RegisterConstant(0x4050607, 0x10203, 0xc0d0e0f, 0x8090a0b)); //     21d4:	33 82 ff c7 	lqr	$71,39d0 <_fini+0x168>	# 39d0

			tand2.Seek(0x74);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(10), RegisterConstant(0xfff00000, 0x0, 0xfff00000, 0x0)); //     21e4:	33 82 eb 8a 	lqr	$10,3940 <_fini+0xd8>	# 3940

			tand2.Seek(0x7c);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(13), RegisterConstant(0xfffff, 0xffffffff, 0xfffff, 0xffffffff)); //     21ec:	33 83 08 8d 	lqr	$13,3a30 <_fini+0x1c8>	# 3a30

			tand2.Seek(0x84);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(39), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //     21f4:	33 82 d5 a7 	lqr	$39,38a0 <_fini+0x38>	# 38a0

			tand2.Seek(0xa8);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(27), RegisterConstant(0x3fe, 0x0, 0x3fe, 0x0)); //     2218:	33 83 05 1b 	lqr	$27,3a40 <_fini+0x1d8>	# 3a40

			tand2.Seek(0xc4);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(28), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //     2234:	33 82 cd 9c 	lqr	$28,38a0 <_fini+0x38>	# 38a0

			tand2.Seek(0x114);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(32), RegisterConstant(0x7fe, 0x0, 0x7fe, 0x0)); //     2284:	33 83 03 a0 	lqr	$32,3aa0 <_fini+0x238>	# 3aa0

			tand2.Seek(0x15c);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(2), RegisterConstant(0x7ff80000, 0x0, 0x7ff80000, 0x0)); //     22cc:	33 82 fc 82 	lqr	$2,3ab0 <_fini+0x248>	# 3ab0

			tand2.Seek(0x16c);
			tand2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(7), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //     22dc:	33 82 b8 87 	lqr	$7,38a0 <_fini+0x38>	# 38a0
		}

		private void PatchSind2(PatchRoutine sind2)
		{
			sind2.Seek(0x4);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(42), RegisterConstant(0x3ff45f30, 0x6dcd2b16, 0x3ff45f30, 0x6dcd2b16)); //     1dbc:	33 83 a0 aa 	lqr	$42,3ac0 <_fini+0x258>	# 3ac0

			sind2.Seek(0x8);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(28), RegisterConstant(0x3ff921fb, 0x54400000, 0x3ff921fb, 0x54400000)); //     1dc0:	33 83 b8 1c 	lqr	$28,3b80 <_fini+0x318>	# 3b80

			sind2.Seek(0x10);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(26), RegisterConstant(0x3dd0b461, 0x1a626331, 0x3dd0b461, 0x1a626331)); //     1dc8:	33 83 b9 1a 	lqr	$26,3b90 <_fini+0x328>	# 3b90

			sind2.Seek(0x14);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(15), RegisterConstant(0x3de61246, 0x13a86d09, 0x3de61246, 0x13a86d09)); //     1dcc:	33 83 a2 8f 	lqr	$15,3ae0 <_fini+0x278>	# 3ae0

			sind2.Seek(0x18);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(16), RegisterConstant(0x3f811111, 0x11111111, 0x3f811111, 0x11111111)); //     1dd0:	33 83 a4 10 	lqr	$16,3af0 <_fini+0x288>	# 3af0

			sind2.Seek(0x1c);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(24), RegisterConstant(0xbd6ae7f3, 0xe733b81f, 0xbd6ae7f3, 0xe733b81f)); //     1dd4:	33 83 bb 98 	lqr	$24,3bb0 <_fini+0x348>	# 3bb0

			sind2.Seek(0x24);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(23), RegisterConstant(0xbf2a01a0, 0x1a01a01a, 0xbf2a01a0, 0x1a01a01a)); //     1ddc:	33 83 bc 97 	lqr	$23,3bc0 <_fini+0x358>	# 3bc0

			sind2.Seek(0x28);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(14), RegisterConstant(0x3fa55555, 0x55555555, 0x3fa55555, 0x55555555)); //     1de0:	33 83 9e 0e 	lqr	$14,3ad0 <_fini+0x268>	# 3ad0

			sind2.Seek(0x2c);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(10), RegisterConstant(0x3e21eeb6, 0x358665fc, 0x3e21eeb6, 0x358665fc)); //     1de4:	33 83 a3 8a 	lqr	$10,3b00 <_fini+0x298>	# 3b00

			sind2.Seek(0x30);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(18), RegisterConstant(0xbe5ae645, 0x67f544e4, 0xbe5ae645, 0x67f544e4)); //     1de8:	33 83 a7 12 	lqr	$18,3b20 <_fini+0x2b8>	# 3b20

			sind2.Seek(0x34);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(19), RegisterConstant(0xbfc55555, 0x55555555, 0xbfc55555, 0x55555555)); //     1dec:	33 83 a8 93 	lqr	$19,3b30 <_fini+0x2c8>	# 3b30

			sind2.Seek(0x38);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(25), RegisterConstant(0xbf56c16c, 0x16c16955, 0xbf56c16c, 0x16c16955)); //     1df0:	33 83 b6 19 	lqr	$25,3ba0 <_fini+0x338>	# 3ba0

			sind2.Seek(0x3c);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(22), RegisterConstant(0xbda9079a, 0x27f38837, 0xbda9079a, 0x27f38837)); //     1df4:	33 83 bb 96 	lqr	$22,3bd0 <_fini+0x368>	# 3bd0

			sind2.Seek(0x40);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(20), RegisterConstant(0x3ff00000, 0x0, 0x3ff00000, 0x0)); //     1df8:	33 83 6b 14 	lqr	$20,3950 <_fini+0xe8>	# 3950

			sind2.Seek(0x4c);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(17), RegisterConstant(0xbfe00000, 0x0, 0xbfe00000, 0x0)); //     1e04:	33 83 a1 91 	lqr	$17,3b10 <_fini+0x2a8>	# 3b10

			sind2.Seek(0x50);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(11), RegisterConstant(0xbe927e4f, 0xa1280e5c, 0xbe927e4f, 0xa1280e5c)); //     1e08:	33 83 a7 0b 	lqr	$11,3b40 <_fini+0x2d8>	# 3b40

			sind2.Seek(0x54);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(12), RegisterConstant(0x3ec71de3, 0xa556c734, 0x3ec71de3, 0xa556c734)); //     1e0c:	33 83 a8 8c 	lqr	$12,3b50 <_fini+0x2e8>	# 3b50

			sind2.Seek(0x5c);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(13), RegisterConstant(0x3efa01a0, 0x19f4a8f3, 0x3efa01a0, 0x19f4a8f3)); //     1e14:	33 83 a9 8d 	lqr	$13,3b60 <_fini+0x2f8>	# 3b60

			sind2.Seek(0x64);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(36), RegisterConstant(0x10203, 0x10203, 0x8090a0b, 0x8090a0b)); //     1e1c:	33 83 54 a4 	lqr	$36,38c0 <_fini+0x58>	# 38c0

			sind2.Seek(0x6c);
			sind2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(31), RegisterConstant(0x1d, 0x20, 0x1d, 0x20)); //     1e24:	33 83 a9 9f 	lqr	$31,3b70 <_fini+0x308>	# 3b70
		}

		private void PatchDivd2(PatchRoutine divd2)
		{
			divd2.Seek(0x4);
			divd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(36), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //     1b1c:	33 83 b0 a4 	lqr	$36,38a0 <_fini+0x38>	# 38a0

			divd2.Seek(0x10);
			divd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(69), RegisterConstant(0x4050607, 0x10203, 0xc0d0e0f, 0x8090a0b)); //     1b28:	33 83 d5 45 	lqr	$69,39d0 <_fini+0x168>	# 39d0

			divd2.Seek(0x18);
			divd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(21), RegisterConstant(0x3ff00000, 0x0, 0x3ff00000, 0x0)); //     1b30:	33 83 c4 15 	lqr	$21,3950 <_fini+0xe8>	# 3950

			divd2.Seek(0x1c);
			divd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(9), RegisterConstant(0xfff00000, 0x0, 0xfff00000, 0x0)); //     1b34:	33 83 c1 89 	lqr	$9,3940 <_fini+0xd8>	# 3940

			divd2.Seek(0x24);
			divd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(79), RegisterConstant(0xfffff, 0xffffffff, 0xfffff, 0xffffffff)); //     1b3c:	33 83 de cf 	lqr	$79,3a30 <_fini+0x1c8>	# 3a30

			divd2.Seek(0x2c);
			divd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(22), RegisterConstant(0x3fe, 0x0, 0x3fe, 0x0)); //     1b44:	33 83 df 96 	lqr	$22,3a40 <_fini+0x1d8>	# 3a40

			divd2.Seek(0x3c);
			divd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(29), RegisterConstant(0x7fe, 0x0, 0x7fe, 0x0)); //     1b54:	33 83 e9 9d 	lqr	$29,3aa0 <_fini+0x238>	# 3aa0

			divd2.Seek(0x5c);
			divd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(10), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //     1b74:	33 83 a5 8a 	lqr	$10,38a0 <_fini+0x38>	# 38a0

			divd2.Seek(0x6c);
			divd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(7), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //     1b84:	33 83 a3 87 	lqr	$7,38a0 <_fini+0x38>	# 38a0

			divd2.Seek(0x94);
			divd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(2), RegisterConstant(0x7ff80000, 0x0, 0x7ff80000, 0x0)); //     1bac:	33 83 e0 82 	lqr	$2,3ab0 <_fini+0x248>	# 3ab0
		}

		private void PatchCosd2(PatchRoutine cosd2)
		{
			// cosd2 

			cosd2.Seek(0x4);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(43), RegisterConstant(0x3ff45f30, 0x6dcd2b16, 0x3ff45f30, 0x6dcd2b16)); //     175c:	33 84 3a ab 	lqr	$43,3930 <_fini+0x258>	# 3930

			cosd2.Seek(0x8);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(28), RegisterConstant(0x3ff921fb, 0x54400000, 0x3ff921fb, 0x54400000)); //     1760:	33 84 52 1c 	lqr	$28,39f0 <_fini+0x318>	# 39f0

			cosd2.Seek(0x10);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(26), RegisterConstant(0x3dd0b461, 0x1a626331, 0x3dd0b461, 0x1a626331)); //     1768:	33 84 53 1a 	lqr	$26,3a00 <_fini+0x328>	# 3a00

			cosd2.Seek(0x14);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(15), RegisterConstant(0x3de61246, 0x13a86d09, 0x3de61246, 0x13a86d09)); //     176c:	33 84 3c 8f 	lqr	$15,3950 <_fini+0x278>	# 3950

			cosd2.Seek(0x18);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(16), RegisterConstant(0x3f811111, 0x11111111, 0x3f811111, 0x11111111)); //     1770:	33 84 3e 10 	lqr	$16,3960 <_fini+0x288>	# 3960

			cosd2.Seek(0x1c);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(24), RegisterConstant(0xbd6ae7f3, 0xe733b81f, 0xbd6ae7f3, 0xe733b81f)); //     1774:	33 84 55 98 	lqr	$24,3a20 <_fini+0x348>	# 3a20

			cosd2.Seek(0x24);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(23), RegisterConstant(0xbf2a01a0, 0x1a01a01a, 0xbf2a01a0, 0x1a01a01a)); //     177c:	33 84 56 97 	lqr	$23,3a30 <_fini+0x358>	# 3a30

			cosd2.Seek(0x28);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(14), RegisterConstant(0x3fa55555, 0x55555555, 0x3fa55555, 0x55555555)); //     1780:	33 84 38 0e 	lqr	$14,3940 <_fini+0x268>	# 3940

			cosd2.Seek(0x2c);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(10), RegisterConstant(0x3e21eeb6, 0x358665fc, 0x3e21eeb6, 0x358665fc)); //     1784:	33 84 3d 8a 	lqr	$10,3970 <_fini+0x298>	# 3970

			cosd2.Seek(0x30);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(18), RegisterConstant(0xbe5ae645, 0x67f544e4, 0xbe5ae645, 0x67f544e4)); //     1788:	33 84 41 12 	lqr	$18,3990 <_fini+0x2b8>	# 3990

			cosd2.Seek(0x34);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(19), RegisterConstant(0xbfc55555, 0x55555555, 0xbfc55555, 0x55555555)); //     178c:	33 84 42 93 	lqr	$19,39a0 <_fini+0x2c8>	# 39a0

			cosd2.Seek(0x38);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(25), RegisterConstant(0xbf56c16c, 0x16c16955, 0xbf56c16c, 0x16c16955)); //     1790:	33 84 50 19 	lqr	$25,3a10 <_fini+0x338>	# 3a10

			cosd2.Seek(0x3c);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(22), RegisterConstant(0xbda9079a, 0x27f38837, 0xbda9079a, 0x27f38837)); //     1794:	33 84 55 96 	lqr	$22,3a40 <_fini+0x368>	# 3a40

			cosd2.Seek(0x40);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(20), RegisterConstant(0x3ff00000, 0x0, 0x3ff00000, 0x0)); //     1798:	33 84 05 14 	lqr	$20,37c0 <_fini+0xe8>	# 37c0

			cosd2.Seek(0x4c);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(17), RegisterConstant(0xbfe00000, 0x0, 0xbfe00000, 0x0)); //     17a4:	33 84 3b 91 	lqr	$17,3980 <_fini+0x2a8>	# 3980

			cosd2.Seek(0x50);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(11), RegisterConstant(0xbe927e4f, 0xa1280e5c, 0xbe927e4f, 0xa1280e5c)); //     17a8:	33 84 41 0b 	lqr	$11,39b0 <_fini+0x2d8>	# 39b0

			cosd2.Seek(0x54);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(12), RegisterConstant(0x3ec71de3, 0xa556c734, 0x3ec71de3, 0xa556c734)); //     17ac:	33 84 42 8c 	lqr	$12,39c0 <_fini+0x2e8>	# 39c0

			cosd2.Seek(0x5c);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(13), RegisterConstant(0x3efa01a0, 0x19f4a8f3, 0x3efa01a0, 0x19f4a8f3)); //     17b4:	33 84 43 8d 	lqr	$13,39d0 <_fini+0x2f8>	# 39d0

			cosd2.Seek(0x64);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(37), RegisterConstant(0x10203, 0x10203, 0x8090a0b, 0x8090a0b)); //     17bc:	33 83 ee a5 	lqr	$37,3730 <_fini+0x58>	# 3730

			cosd2.Seek(0x6c);
			cosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(31), RegisterConstant(0x1d, 0x20, 0x1d, 0x20)); //     17c4:	33 84 43 9f 	lqr	$31,39e0 <_fini+0x308>	# 39e0
		}

		private void PatchAcosd2(PatchRoutine acosd2)
		{
			acosd2.Seek(0x4);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(50), RegisterConstant(0x3fe00000, 0x0, 0x3fe00000, 0x0)); //      45c:	33 86 9a b2 	lqr	$50,3930 <_fini+0xc8>	# 3930

			acosd2.Seek(0x1c);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(17), RegisterConstant(0xfff00000, 0x0, 0xfff00000, 0x0)); //      474:	33 86 99 91 	lqr	$17,3940 <_fini+0xd8>	# 3940

			acosd2.Seek(0x24);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(33), RegisterConstant(0x10203, 0x10203, 0x8090a0b, 0x8090a0b)); //      47c:	33 86 88 a1 	lqr	$33,38c0 <_fini+0x58>	# 38c0

			acosd2.Seek(0x2c);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(25), RegisterConstant(0x43300000, 0x0, 0x43300000, 0x0)); //      484:	33 86 9d 99 	lqr	$25,3970 <_fini+0x108>	# 3970

			acosd2.Seek(0x34);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(35), RegisterConstant(0x3ff00000, 0x0, 0x3ff00000, 0x0)); //      48c:	33 86 98 a3 	lqr	$35,3950 <_fini+0xe8>	# 3950

			acosd2.Seek(0x3c);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(32), RegisterConstant(0x1a00000, 0x0, 0x1a00000, 0x0)); //      494:	33 86 9d a0 	lqr	$32,3980 <_fini+0x118>	# 3980

			acosd2.Seek(0x4c);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(28), RegisterConstant(0x7fe00000, 0x0, 0x7fe00000, 0x0)); //      4a4:	33 86 97 9c 	lqr	$28,3960 <_fini+0xf8>	# 3960

			acosd2.Seek(0x5c);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(26), RegisterConstant(0x7fffffff, 0xffffffff, 0x7fffffff, 0xffffffff)); //      4b4:	33 86 7b 9a 	lqr	$26,3890 <_fini+0x28>	# 3890

			acosd2.Seek(0x64);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(2), RegisterConstant(0x3fe00000, 0x0, 0x3fe00000, 0x0)); //      4bc:	33 86 8e 82 	lqr	$2,3930 <_fini+0xc8>	# 3930

			acosd2.Seek(0x74);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(42), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //      4cc:	33 86 7a aa 	lqr	$42,38a0 <_fini+0x38>	# 38a0

			acosd2.Seek(0x7c);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(40), RegisterConstant(0x10203, 0x10111213, 0x8090a0b, 0x18191a1b)); //      4d4:	33 86 7b a8 	lqr	$40,38b0 <_fini+0x48>	# 38b0

			acosd2.Seek(0x84);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(57), RegisterConstant(0x400921fb, 0x54442d18, 0x400921fb, 0x54442d18)); //      4dc:	33 86 ae b9 	lqr	$57,3a50 <_fini+0x1e8>	# 3a50

			acosd2.Seek(0x8c);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(29), RegisterConstant(0xbff00000, 0x0, 0xbff00000, 0x0)); //      4e4:	33 86 b1 9d 	lqr	$29,3a70 <_fini+0x208>	# 3a70

			acosd2.Seek(0x94);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(22), RegisterConstant(0xc02d7b59, 0xb5e0eab, 0xc02d7b59, 0xb5e0eab)); //      4ec:	33 86 b2 96 	lqr	$22,3a80 <_fini+0x218>	# 3a80

			acosd2.Seek(0x9c);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(3), RegisterConstant(0x3ff921fb, 0x54442d18, 0x3ff921fb, 0x54442d18)); //      4f4:	33 86 ad 83 	lqr	$3,3a60 <_fini+0x1f8>	# 3a60

			acosd2.Seek(0xbc);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(19), RegisterConstant(0x3f716b9b, 0xbd48ad3, 0x3f716b9b, 0xbd48ad3)); //      514:	33 86 af 93 	lqr	$19,3a90 <_fini+0x228>	# 3a90

			acosd2.Seek(0xc4);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(25), RegisterConstant(0xbfe34341, 0x333e5c16, 0xbfe34341, 0x333e5c16)); //      51c:	33 86 98 99 	lqr	$25,39e0 <_fini+0x178>	# 39e0

			acosd2.Seek(0xcc);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(32), RegisterConstant(0x40519fc0, 0x25fe9054, 0x40519fc0, 0x25fe9054)); //      524:	33 86 8d a0 	lqr	$32,3990 <_fini+0x128>	# 3990

			acosd2.Seek(0xd4);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(33), RegisterConstant(0xc06265bb, 0x6d3576d7, 0xc06265bb, 0x6d3576d7)); //      52c:	33 86 8e a1 	lqr	$33,39a0 <_fini+0x138>	# 39a0

			acosd2.Seek(0xdc);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(23), RegisterConstant(0x40617056, 0x84ffbf9d, 0x40617056, 0x84ffbf9d)); //      534:	33 86 8f 97 	lqr	$23,39b0 <_fini+0x148>	# 39b0

			acosd2.Seek(0xe4);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(10), RegisterConstant(0xc0489822, 0xa3607ac, 0xc0489822, 0xa3607ac)); //      53c:	33 86 90 8a 	lqr	$10,39c0 <_fini+0x158>	# 39c0

			acosd2.Seek(0xec);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(26), RegisterConstant(0x4015c74b, 0x178a2dd9, 0x4015c74b, 0x178a2dd9)); //      544:	33 86 95 9a 	lqr	$26,39f0 <_fini+0x188>	# 39f0

			acosd2.Seek(0xfc);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(7), RegisterConstant(0xc020656c, 0x6ceafd5, 0xc020656c, 0x6ceafd5)); //      554:	33 86 99 87 	lqr	$7,3a20 <_fini+0x1b8>	# 3a20

			acosd2.Seek(0x104);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(73), RegisterConstant(0x4050607, 0x10203, 0xc0d0e0f, 0x8090a0b)); //      55c:	33 86 8e c9 	lqr	$73,39d0 <_fini+0x168>	# 39d0

			acosd2.Seek(0x12c);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(28), RegisterConstant(0xc0304331, 0xde27907b, 0xc0304331, 0xde27907b)); //      584:	33 86 8f 9c 	lqr	$28,3a00 <_fini+0x198>	# 3a00

			acosd2.Seek(0x134);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(18), RegisterConstant(0x40339007, 0xda779259, 0x40339007, 0xda779259)); //      58c:	33 86 90 92 	lqr	$18,3a10 <_fini+0x1a8>	# 3a10

			acosd2.Seek(0x14c);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(15), RegisterConstant(0xfffff, 0xffffffff, 0xfffff, 0xffffffff)); //      5a4:	33 86 91 8f 	lqr	$15,3a30 <_fini+0x1c8>	# 3a30

			acosd2.Seek(0x164);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(37), RegisterConstant(0x3fe, 0x0, 0x3fe, 0x0)); //      5bc:	33 86 90 a5 	lqr	$37,3a40 <_fini+0x1d8>	# 3a40

			acosd2.Seek(0x18c);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(38), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //      5e4:	33 86 57 a6 	lqr	$38,38a0 <_fini+0x38>	# 38a0

			acosd2.Seek(0x204);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(28), RegisterConstant(0x7fe, 0x0, 0x7fe, 0x0)); //      65c:	33 86 88 9c 	lqr	$28,3aa0 <_fini+0x238>	# 3aa0

			acosd2.Seek(0x244);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(2), RegisterConstant(0x7ff80000, 0x0, 0x7ff80000, 0x0)); //      69c:	33 86 82 82 	lqr	$2,3ab0 <_fini+0x248>	# 3ab0

			acosd2.Seek(0x24c);
			acosd2.Writer.WriteLoad(HardwareRegister.GetHardwareRegister(10), RegisterConstant(0x7ff00000, 0x0, 0x7ff00000, 0x0)); //      6a4:	33 86 3f 8a 	lqr	$10,38a0 <_fini+0x38>	# 38a0
		}
	}
}
