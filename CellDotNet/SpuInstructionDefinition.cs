using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// Bit-layout of an instruction.
	/// </summary>
	enum SpuInstructionFormat
	{
		None,
		/// <summary>
		/// Register rb, ra, rt.
		/// Assembler format: "rt,ra,rb".
		/// 11 bit instruction code.
		/// </summary>
		RR,
		/// <summary>
		/// Register ra, rt.
		/// Assembler format: "rt,ra".
		/// 11 bit instruction code.
		/// The RR variant where the bits for the first register (b, bits 11-17) are not used.
		/// </summary>
		RR2,
		/// <summary>
		/// Register rt.
		/// Assembler format: "rt".
		/// 11 bit instruction code.
		/// </summary>
		RR1,
		/// <summary>
		/// Register ra.
		/// D at bit 12.
		/// E at bit 13.
		/// Assembler format: "ra" (no assembler support for the D and E bits).
		/// 11 bit instruction code.
		/// An RR variant used for branching where only register ra is used and the D and E 
		/// branch bits are part of the instruction.
		/// </summary>
		RR1DE,
		/// <summary>
		/// Register ra, rt.
		/// 11 bit instruction code.
		/// D at bit 12.
		/// E at bit 13.
		/// Assembler format: "rt,ra" (no assembler support for the D and E bits).
		/// An RR variant used for branching where only register ra and rt are used and the D and E 
		/// branch bits are part of the instruction.
		/// </summary>
		RR2DE,
		/// <summary>
		/// Register rt, rb, ra, rc.
		/// Assembler format: "rt,ra,rb,rc".
		/// 4 bit instruction code.
		/// </summary>
		RRR,
		/// <summary>
		/// Register ra, rt.
		/// Assembler format: "rt,ra,value".
		/// 11 bit instruction code.
		/// 7 bit immediate.
		/// </summary>
		RI7,
		/// <summary>
		/// Register ra, rt.
		/// Assembler format: "rt,ra,scale".
		/// 10 bit instruction code.
		/// 8 bit immediate.
		/// </summary>
		RI8,
		/// <summary>
		/// Register ra, rt.
		/// Assembler format: "rt,ra,value".
		/// 8 bit instruction code.
		/// 8 bit immediate.
		/// </summary>
		RI10,
		/// <summary>
		/// Register rt.
		/// Assembler format: "rt,symbol".
		/// 9 bit instruction code.
		/// 10 bit immediate.
		/// </summary>
		RI16,
		/// <summary>
		/// No registers.
		/// Assembler format: "symbol".
		/// 9 bit instruction code.
		/// 16 bit immediate.
		/// An RI16 variant where the last register bits are ignored.
		/// </summary>
		RI16x,
		/// <summary>
		/// Register rt.
		/// Assembler format: "rt,symbol".
		/// 7 bit instruction code.
		/// 18 bit immediate.
		/// </summary>
		RI18
	}

	/// <summary>
	/// SPU instruction definitions as taken from the 
	/// "Synergistic Processor Unit Instruction Set Architecture" version 1.2
	/// </summary>
	class SpuOpCode
	{
		private string _name;
		public string Name
		{
			get { return _name; }
		}

		private string _title;
		public string Title
		{
			get { return _title; }
		}

		private SpuInstructionFormat _format;
		public SpuInstructionFormat Format
		{
			get { return _format; }
		}


		public SpuOpCode(string _name, string _title, SpuInstructionFormat _format)
		{
			this._name = _name;
			this._title = _title;
			this._format = _format;
		}

		/// <summary>
		/// TODO: Add instruction numbers to SpuOpCode and this list.
		/// TODO: Add the rest of the instructions: hint-for-branch, control instructions, channel instructions.
		/// </summary>
//		public static SpuOpCode[] OpCodes = new SpuOpCode[]
//			{
		public static readonly SpuOpCode lqd =
			new SpuOpCode("lqd", "Load Quadword (d-form)", SpuInstructionFormat.RI10);

		public static readonly SpuOpCode lqx =
			new SpuOpCode("lqx", "Load Quadword (x-form)", SpuInstructionFormat.RR);

		public static readonly SpuOpCode lqa =
			new SpuOpCode("lqa", "Load Quadword (a-form)", SpuInstructionFormat.RI16);

		public static readonly SpuOpCode lqr =
			new SpuOpCode("lqr", "Load Quadword Instruction Relative (a-form)", SpuInstructionFormat.RI16);

		public static readonly SpuOpCode stqd =
			new SpuOpCode("stqd", "Store Quadword (d-form)", SpuInstructionFormat.RI10);

		public static readonly SpuOpCode stqx =
			new SpuOpCode("stqx", "Store Quadword (x-form)", SpuInstructionFormat.RR);

		public static readonly SpuOpCode stqa =
			new SpuOpCode("stqa", "Store Quadword (a-form)", SpuInstructionFormat.RI16);

		public static readonly SpuOpCode stqr =
			new SpuOpCode("stqr", "Store Quadword Instruction Relative (a-form)", SpuInstructionFormat.RI16);

		public static readonly SpuOpCode cbd =
			new SpuOpCode("cbd", "Generate Controls for Byte Insertion (d-form)", SpuInstructionFormat.RI7);

		public static readonly SpuOpCode cbx =
			new SpuOpCode("cbx", "Generate Controls for Byte Insertion (x-form)", SpuInstructionFormat.RR);

		public static readonly SpuOpCode chd =
			new SpuOpCode("chd", "Generate Controls for Halfword Insertion (d-form)", SpuInstructionFormat.RI7);

		public static readonly SpuOpCode chx =
			new SpuOpCode("chx", "Generate Controls for Halfword Insertion (x-form)", SpuInstructionFormat.RR);
		public static readonly SpuOpCode cwd =
				new SpuOpCode("cwd", "Generate Controls for Word Insertion (d-form)", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode cwx =
				new SpuOpCode("cwx", "Generate Controls for Word Insertion (x-form)", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode cdd =
				new SpuOpCode("cdd", "Generate Controls for Doubleword Insertion (d-form)", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode cdx =
				new SpuOpCode("cdx", "Generate Controls for Doubleword Insertion (x-form)", SpuInstructionFormat.RR); 
				// Constant form section.
		public static readonly SpuOpCode ilh =
				new SpuOpCode("ilh", "Immediate Load Halfword", SpuInstructionFormat.RI16); 
		public static readonly SpuOpCode ilhu =
				new SpuOpCode("ilhu", "Immediate Load Halfword Upper", SpuInstructionFormat.RI16); 
		public static readonly SpuOpCode il =
				new SpuOpCode("il", "Immediate Load Word", SpuInstructionFormat.RI16); 
		public static readonly SpuOpCode ila =
				new SpuOpCode("ila", "Immediate Load Address", SpuInstructionFormat.RI18); 
		public static readonly SpuOpCode iohl =
				new SpuOpCode("iohl", "Immediate Or Halfword Lower", SpuInstructionFormat.RI16); 
		public static readonly SpuOpCode fsmbi =
				new SpuOpCode("fsmbi", "Form Select Mask for Bytes Immediate", SpuInstructionFormat.RI16); 
				// 5. Integer and Logical OpCodes
		public static readonly SpuOpCode ah =
				new SpuOpCode("ah", "Add Halfword", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode ahi =
				new SpuOpCode("ahi", "Add Halfword Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode a =
				new SpuOpCode("a", "Add Word", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode ai =
				new SpuOpCode("ai", "Add Word Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode sfh =
				new SpuOpCode("sfh", "Subtract from Halfword", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode sfhi =
				new SpuOpCode("sfhi", "Subtract from Halfword Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode sf =
				new SpuOpCode("sf", "Subtract from Word", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode sfi =
				new SpuOpCode("sfi", "Subtract from Word Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode addx =
				new SpuOpCode("addx", "Add Extended", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode cg =
				new SpuOpCode("cg", "Carry Generate", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode cgx =
				new SpuOpCode("cgx", "Carry Generate Extended", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode sfx =
				new SpuOpCode("sfx", "Subtract from Extended", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode bg =
				new SpuOpCode("bg", "Borrow Generate", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode bgx =
				new SpuOpCode("bgx", "Borrow Generate Extended", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode mpy =
				new SpuOpCode("mpy", "Multiply", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode mpyu =
				new SpuOpCode("mpyu", "Multiply Unsigned", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode mpyi =
				new SpuOpCode("mpyi", "Multiply Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode mpyui =
				new SpuOpCode("mpyui", "Multiply Unsigned Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode mpya =
				new SpuOpCode("mpya", "Multiply and Add", SpuInstructionFormat.RRR); 
		public static readonly SpuOpCode mpyh =
				new SpuOpCode("mpyh", "Multiply High", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode mpys =
				new SpuOpCode("mpys", "Multiply and Shift Right", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode mpyhh =
				new SpuOpCode("mpyhh", "Multiply High High", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode mpyhha =
				new SpuOpCode("mpyhha", "Multiply High High and Add", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode mpyhhu =
				new SpuOpCode("mpyhhu", "Multiply High High Unsigned", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode mpyhhau =
				new SpuOpCode("mpyhhau", "Multiply High High Unsigned and Add", SpuInstructionFormat.RR); 
				// p83 clz
		public static readonly SpuOpCode clz =
				new SpuOpCode("clz", "Count Leading Zeros", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode cntb =
				new SpuOpCode("cntb", "Count Ones in Bytes", SpuInstructionFormat.RR2);
		public static readonly SpuOpCode fsmb =
				new SpuOpCode("fsmb", "Form Select Mask for Bytes", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode fsmh =
				new SpuOpCode("fsmh", "Form Select Mask for Halfwords", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode fsm =
				new SpuOpCode("fsm", "Form Select Mask for Words", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode gbb =
				new SpuOpCode("gbb", "Gather Bits from Bytes", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode gbh =
				new SpuOpCode("gbh", "Gather Bits from Halfwords", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode gb =
				new SpuOpCode("gb", "Gather Bits from Words", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode avgb =
				new SpuOpCode("avgb", "Average Bytes", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode absdb =
				new SpuOpCode("absdb", "Absolute Differences of Bytes", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode sumb =
				new SpuOpCode("sumb", "Sum Bytes into Halfwords", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode xsbh =
				new SpuOpCode("xsbh", "Extend Sign Byte to Halfword", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode xshw =
				new SpuOpCode("xshw", "Extend Sign Halfword to Word", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode xswd =
				new SpuOpCode("xswd", "Extend Sign Word to Doubleword", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode and =
				new SpuOpCode("and", "And", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode andc =
				new SpuOpCode("andc", "And with Complement", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode andb =
				new SpuOpCode("andbi", "And Byte Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode andhi =
				new SpuOpCode("andhi", "And Halfword Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode andi =
				new SpuOpCode("andi", "And Word Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode or =
				new SpuOpCode("or", "Or", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode orc =
				new SpuOpCode("orc", "Or with Complement", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode orbi =
				new SpuOpCode("orbi", "Or Byte Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode orhi =
				new SpuOpCode("orhi", "Or Halfword Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode ori =
				new SpuOpCode("ori", "Or Word Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode orx =
				new SpuOpCode("orx", "Or Across", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode xor =
				new SpuOpCode("xor", "Exclusive Or", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode xorbi =
				new SpuOpCode("xorbi", "Exclusive Or Byte Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode xorhi =
				new SpuOpCode("xorhi", "Exclusive Or Halfword Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode xori =
				new SpuOpCode("xori", "Exclusive Or Word Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode nand =
				new SpuOpCode("nand", "Nand", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode nor =
				new SpuOpCode("nor", "Nor", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode eqv =
				new SpuOpCode("eqv", "Equivalent", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode selb =
				new SpuOpCode("selb", "Select Bits", SpuInstructionFormat.RRR); 
		public static readonly SpuOpCode shufb =
				new SpuOpCode("shufb", "Shuffle Bytes", SpuInstructionFormat.RRR); 
				// 6. Shift and Rotate OpCodes
		public static readonly SpuOpCode shlh =
				new SpuOpCode("shlh", "Shift Left Halfword", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode shlhi =
				new SpuOpCode("shlhi", "Shift Left Halfword Immediate", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode shl =
				new SpuOpCode("shl", "Shift Left Word", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode shli =
				new SpuOpCode("shli", "Shift Left Word Immediate", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode shlqbi =
				new SpuOpCode("shlqbi", "Shift Left Quadword by Bits", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode shlqbii =
				new SpuOpCode("shlqbii", "Shift Left Quadword by Bits Immediate", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode shlqby =
				new SpuOpCode("shlqby", "Shift Left Quadword by Bytes", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode sqlqbyi =
				new SpuOpCode("sqlqbyi", "Shift Left Quadword by Bytes Immediate", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode shlqbybi =
				new SpuOpCode("shlqbybi", "Shift Left Quadword by Bytes from Bit Shift Count", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode roth =
				new SpuOpCode("roth", "Rotate Halfword", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode rothi =
				new SpuOpCode("rothi", "Rotate Halfword Immediate", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode rot =
				new SpuOpCode("rot", "Rotate Word", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode roti =
				new SpuOpCode("roti", "Rotate Word Immediate", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode rotqby =
				new SpuOpCode("rotqby", "Rotate Quadword by Bytes", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode rotqbyi =
				new SpuOpCode("rotqbyi", "Rotate Quadword by Bytes Immediate", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode rotqbybi =
				new SpuOpCode("rotqbybi", "Rotate Quadword by Bytes from Bit Shift Count", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode rotqbi =
				new SpuOpCode("rotqbi", "Rotate Quadword by Bits", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode rotqbii =
				new SpuOpCode("rotqbii", "Rotate Quadword by Bits Immediate", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode rothm =
				new SpuOpCode("rothm", "Rotate and Mask Halfword", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode rothmi =
				new SpuOpCode("rothmi", "Rotate and Mask Halfword Immediate", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode rotm =
				new SpuOpCode("rotm", "Rotate and Mask Word", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode rotmi =
				new SpuOpCode("rotmi", "Rotate and Mask Word Immediate", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode rotqmby =
				new SpuOpCode("rotqmby", "Rotate and Mask Quadword by Bytes", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode rotqmbyi =
				new SpuOpCode("rotqmbyi", "Rotate and Mask Quadword by Bytes Immediate", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode rotqmbybi =
				new SpuOpCode("rotqmbybi", "Rotate and Mask Quadword Bytes from Bit Shift Count", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode rotqmbi =
				new SpuOpCode("rotqmbi", "Rotate and Mask Quadword by Bits", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode rotqmbii =
				new SpuOpCode("rotqmbii", "Rotate and Mask Quadword by Bits Immediate", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode rotmah =
				new SpuOpCode("rotmah", "Rotate and Mask Algebraic Halfword", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode rotmahi =
				new SpuOpCode("rotmahi", "Rotate and Mask Algebraic Halfword Immediate", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode rotma =
				new SpuOpCode("rotma", "Rotate and Mask Algebraic Word", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode rotmai =
				new SpuOpCode("rotmai", "Rotate and Mask Algebraic Word Immediate", SpuInstructionFormat.RI7); 
				// 7. Compare, Branch, and Halt OpCodes
		public static readonly SpuOpCode heq =
				new SpuOpCode("heq", "Halt If Equal", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode heqi =
				new SpuOpCode("heqi", "Halt If Equal Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode hgt =
				new SpuOpCode("hgt", "Halt If Greater Than", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode hgti =
				new SpuOpCode("hgti", "Halt If Greater Than Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode hlgt =
				new SpuOpCode("hlgt", "Halt If Logically Greater Than", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode hlgti =
				new SpuOpCode("hlgti", "Halt If Logically Greater Than Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode ceqb =
				new SpuOpCode("ceqb", "Compare Equal Byte", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode ceqbi =
				new SpuOpCode("ceqbi", "Compare Equal Byte Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode ceqh =
				new SpuOpCode("ceqh", "Compare Equal Halfword", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode ceqhi =
				new SpuOpCode("ceqhi", "Compare Equal Halfword Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode ceq =
				new SpuOpCode("ceq", "Compare Equal Word", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode ceqi =
				new SpuOpCode("ceqi", "Compare Equal Word Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode cgtb =
				new SpuOpCode("cgtb", "Compare Greater Than Byte", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode cgtbi =
				new SpuOpCode("cgtbi", "Compare Greater Than Byte Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode cgth =
				new SpuOpCode("cgth", "Compare Greater Than Halfword", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode cgthi =
				new SpuOpCode("cgthi", "Compare Greater Than Halfword Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode cgt =
				new SpuOpCode("cgt", "Compare Greater Than Word", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode cgti =
				new SpuOpCode("cgti", "Compare Greater Than Word Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode clgtb =
				new SpuOpCode("clgtb", "Compare Logical Greater Than Byte", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode clbtbi =
				new SpuOpCode("clgtbi", "Compare Logical Greater Than Byte Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode clgth =
				new SpuOpCode("clgth", "Compare Logical Greater Than Halfword", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode clgthi =
				new SpuOpCode("clgthi", "Compare Logical Greater Than Halfword Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode clgt =
				new SpuOpCode("clgt", "Compare Logical Greater Than Word", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode clgti =
				new SpuOpCode("clgti", "Compare Logical Greater Than Word Immediate", SpuInstructionFormat.RI10); 
		public static readonly SpuOpCode br =
				new SpuOpCode("br", "Branch Relative", SpuInstructionFormat.RI16x); 
		public static readonly SpuOpCode bra =
				new SpuOpCode("bra", "Branch Absolute", SpuInstructionFormat.RI16x); 
		public static readonly SpuOpCode brsl =
				new SpuOpCode("brsl", "Branch Relative and Set Link", SpuInstructionFormat.RI16); 
		public static readonly SpuOpCode brasl =
				new SpuOpCode("brasl", "Branch Absolute and Set Link", SpuInstructionFormat.RI16); 
				// p175
		public static readonly SpuOpCode bi =
				new SpuOpCode("bi", "Branch Indirect", SpuInstructionFormat.RR1DE); 
		public static readonly SpuOpCode iret =
				new SpuOpCode("iret", "Interrupt Return", SpuInstructionFormat.RR1DE); 
		public static readonly SpuOpCode bisled =
				new SpuOpCode("bisled", "Branch Indirect and Set Link if External Data", SpuInstructionFormat.RR2DE); 
		public static readonly SpuOpCode bisl =
				new SpuOpCode("bisl", "Branch Indirect and Set Link", SpuInstructionFormat.RR2DE); 
		public static readonly SpuOpCode brnz	 =
				new SpuOpCode("brnz", "Branch If Not Zero Word", SpuInstructionFormat.RI16); 
		public static readonly SpuOpCode brz =
				new SpuOpCode("brz", "Branch If Zero Word", SpuInstructionFormat.RI16); 
		public static readonly SpuOpCode brhnz =
				new SpuOpCode("brhnz", "Branch If Not Zero Halfword", SpuInstructionFormat.RI16); 
		public static readonly SpuOpCode brhz =
				new SpuOpCode("brhz", "Branch If Zero Halfword", SpuInstructionFormat.RI16); 
		public static readonly SpuOpCode biz =
				new SpuOpCode("biz", "Branch Indirect If Zero", SpuInstructionFormat.RR2DE); 
		public static readonly SpuOpCode binz =
				new SpuOpCode("binz", "Branch Indirect If Not Zero", SpuInstructionFormat.RR2DE); 
		public static readonly SpuOpCode bihz =
				new SpuOpCode("bihz", "Branch Indirect If Zero Halfword", SpuInstructionFormat.RR2DE); 
		public static readonly SpuOpCode bignz =
				new SpuOpCode("bihnz", "Branch Indirect If Not Zero Halfword", SpuInstructionFormat.RR2DE); 
				// 8. Hint-for-Branch OpCodes: Unusual instruction format, so currently omitted.

				// 9. Floating point.
		public static readonly SpuOpCode fa =
				new SpuOpCode("fa", "Floating Add", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode dfa =
				new SpuOpCode("dfa", "Double Floating Add", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode fs =
				new SpuOpCode("fs", "Floating Subtract", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode dfs =
				new SpuOpCode("dfs", "Double Floating Subtract", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode fm =
				new SpuOpCode("fm", "Floating Multiply", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode dfm =
				new SpuOpCode("dfm", "Double Floating Multiply", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode fma =
				new SpuOpCode("fma", "Floating Multiply and Add", SpuInstructionFormat.RRR); 
		public static readonly SpuOpCode dfma =
				new SpuOpCode("dfma", "Double Floating Multiply and Add", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode fnms =
				new SpuOpCode("fnms", "Floating Negative Multiply and Subtract", SpuInstructionFormat.RRR); 
		public static readonly SpuOpCode dfnms =
				new SpuOpCode("dfnms", "Double Floating Negative Multiply and Subtract", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode fms =
				new SpuOpCode("fms", "Floating Multiply and Subtract", SpuInstructionFormat.RRR); 
		public static readonly SpuOpCode dfms =
				new SpuOpCode("dfms", "Double Floating Multiply and Subtract", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode dfnma =
				new SpuOpCode("dfnma", "Double Floating Negative Multiply and Add", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode frest =
				new SpuOpCode("frest", "Floating Reciprocal Estimate", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode frsqest =
				new SpuOpCode("frsqest", "Floating Reciprocal Absolute Square Root Estimate", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode fi =
				new SpuOpCode("fi", "Floating Interpolate", SpuInstructionFormat.RR); 
				// p220
		public static readonly SpuOpCode csflt =
				new SpuOpCode("csflt", "Convert Signed Integer to Floating", SpuInstructionFormat.RI8); 
		public static readonly SpuOpCode cflts =
				new SpuOpCode("cflts", "Convert Floating to Signed Integer", SpuInstructionFormat.RI8); 
		public static readonly SpuOpCode cuflt =
				new SpuOpCode("cuflt", "Convert Unsigned Integer to Floating", SpuInstructionFormat.RI8); 
		public static readonly SpuOpCode cfltu =
				new SpuOpCode("cfltu", "Convert Floating to Unsigned Integer", SpuInstructionFormat.RI8); 
		public static readonly SpuOpCode frds =
				new SpuOpCode("frds", "Floating Round Double to Single", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode fesd =
				new SpuOpCode("fesd", "Floating Extend Single to Double", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode dfceq =
				new SpuOpCode("dfceq", "Double Floating Compare Equal", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode dfcmeq =
				new SpuOpCode("dfcmeq", "Double Floating Compare Magnitude Equal", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode dfcgt =
				new SpuOpCode("dfcgt", "Double Floating Compare Greater Than", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode dfcmgt =
				new SpuOpCode("dfcmgt", "Double Floating Compare Magnitude Greater Than", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode dftsv =
				new SpuOpCode("dftsv", "Double Floating Test Special Value", SpuInstructionFormat.RI7); 
		public static readonly SpuOpCode fceq =
				new SpuOpCode("fceq", "Floating Compare Equal", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode fcmeq =
				new SpuOpCode("fcmeq", "Floating Compare Magnitude Equal", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode fcgt =
				new SpuOpCode("fcgt", "Floating Compare Greater Than", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode fcmgt =
				new SpuOpCode("fcmgt", "Floating Compare Magnitude Greater Than", SpuInstructionFormat.RR); 
		public static readonly SpuOpCode fscrwr =
				new SpuOpCode("fscrwr", "Floating-Point Status and Control Register Write", SpuInstructionFormat.RR2); 
		public static readonly SpuOpCode fscrrd =
				new SpuOpCode("fscrrd", "Floating-Point Status and Control Register Read", SpuInstructionFormat.RR1); 
				// 10. Control OpCodes
				// p238
//			};


	}

}
