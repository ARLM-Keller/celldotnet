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
		RR,
		/// <summary>
		/// The RR variant where the bits for the first register (b, bits 11-17) are not used.
		/// Not an "offical" format variant.
		/// </summary>
		RR2,
		/// <summary>
		/// An RR variant where only the last register (rt) is used.
		/// </summary>
		RR1,
		/// <summary>
		/// An RR variant used for branching where only register ra is used and the D and E 
		/// branch bits are part of the instruction.
		/// </summary>
		RR1DE,
		/// <summary>
		/// An RR variant used for branching where only register ra and rt are used and the D and E 
		/// branch bits are part of the instruction.
		/// </summary>
		RR2DE,
		RRR,
		RI7,
		/// <summary>
		/// This one is not listed in the layout list, but it is used for "Convert Signed Integer to Floating".
		/// <para>
		/// <div>10 bit instruction number.</div>
		/// <div>8 bit immediate value.</div>
		/// <div>Register RA</div>
		/// <div>Register RT</div>
		/// </para>
		/// </summary>
		RI8,
		RI10,
		RI16,
		/// <summary>
		/// An RI16 variant where the last register bits are ignored.
		/// </summary>
		RI16x,
		RI18
	}

	/// <summary>
	/// SPU instruction definitions as taken from the 
	/// "Synergistic Processor Unit Instruction Set Architecture" version 1.2
	/// </summary>
	class SpuInstructionDefinition
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


		public SpuInstructionDefinition(string _name, string _title, SpuInstructionFormat _format)
		{
			this._name = _name;
			this._title = _title;
			this._format = _format;
		}

		/// <summary>
		/// TODO: Add instruction numbers to SpuInstructionDefinition and this list.
		/// TODO: Add the rest of the instructions: hint-for-branch, control instructions, channel instructions.
		/// </summary>
		static SpuInstructionDefinition[] Instructions = new SpuInstructionDefinition[]
			{
				new SpuInstructionDefinition("lqd", "Load Quadword (d-form)", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("lqx", "Load Quadword (x-form)", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("lqa", "Load Quadword (a-form)", SpuInstructionFormat.RI16), 
				new SpuInstructionDefinition("lqr", "Load Quadword Instruction Relative (a-form)", SpuInstructionFormat.RI16), 
				new SpuInstructionDefinition("stqd", "Store Quadword (d-form)", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("stqx", "Store Quadword (x-form)", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("stqa", "Store Quadword (a-form)", SpuInstructionFormat.RI16), 
				new SpuInstructionDefinition("stqr", "Store Quadword Instruction Relative (a-form)", SpuInstructionFormat.RI16), 
				new SpuInstructionDefinition("cbd", "Generate Controls for Byte Insertion (d-form)", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("cbx", "Generate Controls for Byte Insertion (x-form)", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("chd", "Generate Controls for Halfword Insertion (d-form)", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("chx", "Generate Controls for Halfword Insertion (x-form)", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("cwd", "Generate Controls for Word Insertion (d-form)", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("cwx", "Generate Controls for Word Insertion (x-form)", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("cdd", "Generate Controls for Doubleword Insertion (d-form)", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("cdx", "Generate Controls for Doubleword Insertion (x-form)", SpuInstructionFormat.RR), 
				// Constant form section.
				new SpuInstructionDefinition("ilh", "Immediate Load Halfword", SpuInstructionFormat.RI16), 
				new SpuInstructionDefinition("ilhu", "Immediate Load Halfword Upper", SpuInstructionFormat.RI16), 
				new SpuInstructionDefinition("il", "Immediate Load Word", SpuInstructionFormat.RI16), 
				new SpuInstructionDefinition("ila", "Immediate Load Address", SpuInstructionFormat.RI18), 
				new SpuInstructionDefinition("iohl", "Immediate Or Halfword Lower", SpuInstructionFormat.RI16), 
				new SpuInstructionDefinition("fsmbi", "Form Select Mask for Bytes Immediate", SpuInstructionFormat.RI16), 
				// 5. Integer and Logical Instructions
				new SpuInstructionDefinition("ah", "Add Halfword", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("ahi", "Add Halfword Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("a", "Add Word", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("ai", "Add Word Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("sfh", "Subtract from Halfword", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("sfhi", "Subtract from Halfword Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("sf", "Subtract from Word", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("sfi", "Subtract from Word Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("addx", "Add Extended", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("cg", "Carry Generate", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("cgx", "Carry Generate Extended", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("sfx", "Subtract from Extended", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("bg", "Borrow Generate", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("bgx", "Borrow Generate Extended", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("mpy", "Multiply", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("mpyu", "Multiply Unsigned", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("mpyi", "Multiply Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("mpyui", "Multiply Unsigned Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("mpya", "Multiply and Add", SpuInstructionFormat.RRR), 
				new SpuInstructionDefinition("mpyh", "Multiply High", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("mpys", "Multiply and Shift Right", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("mpyhh", "Multiply High High", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("mpyhha", "Multiply High High and Add", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("mpyhhu", "Multiply High High Unsigned", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("mpyhhau", "Multiply High High Unsigned and Add", SpuInstructionFormat.RR), 
				// p83 clz
				new SpuInstructionDefinition("clz", "Count Leading Zeros", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("cntb", "Count Ones in Bytes", SpuInstructionFormat.RR2),
				new SpuInstructionDefinition("fsmb", "Form Select Mask for Bytes", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("fsmh", "Form Select Mask for Halfwords", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("fsm", "Form Select Mask for Words", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("gbb", "Gather Bits from Bytes", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("gbh", "Gather Bits from Halfwords", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("gb", "Gather Bits from Words", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("avgb", "Average Bytes", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("absdb", "Absolute Differences of Bytes", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("sumb", "Sum Bytes into Halfwords", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("xsbh", "Extend Sign Byte to Halfword", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("xshw", "Extend Sign Halfword to Word", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("xswd", "Extend Sign Word to Doubleword", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("and", "And", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("andc", "And with Complement", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("andbi", "And Byte Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("andhi", "And Halfword Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("andi", "And Word Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("or", "Or", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("orc", "Or with Complement", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("orbi", "Or Byte Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("orhi", "Or Halfword Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("ori", "Or Word Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("orx", "Or Across", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("xor", "Exclusive Or", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("xorbi", "Exclusive Or Byte Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("xorhi", "Exclusive Or Halfword Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("xori", "Exclusive Or Word Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("nand", "Nand", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("nor", "Nor", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("eqv", "Equivalent", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("selb", "Select Bits", SpuInstructionFormat.RRR), 
				new SpuInstructionDefinition("shufb", "Shuffle Bytes", SpuInstructionFormat.RRR), 
				// 6. Shift and Rotate Instructions
				new SpuInstructionDefinition("shlh", "Shift Left Halfword", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("shlhi", "Shift Left Halfword Immediate", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("shl", "Shift Left Word", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("shli", "Shift Left Word Immediate", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("shlqbi", "Shift Left Quadword by Bits", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("shlqbii", "Shift Left Quadword by Bits Immediate", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("shlqby", "Shift Left Quadword by Bytes", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("sqlqbyi", "Shift Left Quadword by Bytes Immediate", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("shlqbybi", "Shift Left Quadword by Bytes from Bit Shift Count", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("roth", "Rotate Halfword", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("rothi", "Rotate Halfword Immediate", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("rot", "Rotate Word", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("roti", "Rotate Word Immediate", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("rotqby", "Rotate Quadword by Bytes", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("rotqbyi", "Rotate Quadword by Bytes Immediate", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("rotqbybi", "Rotate Quadword by Bytes from Bit Shift Count", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("rotqbi", "Rotate Quadword by Bits", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("rotqbii", "Rotate Quadword by Bits Immediate", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("rothm", "Rotate and Mask Halfword", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("rothmi", "Rotate and Mask Halfword Immediate", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("rotm", "Rotate and Mask Word", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("rotmi", "Rotate and Mask Word Immediate", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("rotqmby", "Rotate and Mask Quadword by Bytes", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("rotqmbyi", "Rotate and Mask Quadword by Bytes Immediate", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("rotqmbybi", "Rotate and Mask Quadword Bytes from Bit Shift Count", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("rotqmbi", "Rotate and Mask Quadword by Bits", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("rotqmbii", "Rotate and Mask Quadword by Bits Immediate", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("rotmah", "Rotate and Mask Algebraic Halfword", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("rotmahi", "Rotate and Mask Algebraic Halfword Immediate", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("rotma", "Rotate and Mask Algebraic Word", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("rotmai", "Rotate and Mask Algebraic Word Immediate", SpuInstructionFormat.RI7), 
				// 7. Compare, Branch, and Halt Instructions
				new SpuInstructionDefinition("heq", "Halt If Equal", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("heqi", "Halt If Equal Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("hgt", "Halt If Greater Than", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("hgti", "Halt If Greater Than Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("hlgt", "Halt If Logically Greater Than", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("hlgti", "Halt If Logically Greater Than Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("ceqb", "Compare Equal Byte", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("ceqbi", "Compare Equal Byte Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("ceqh", "Compare Equal Halfword", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("ceqhi", "Compare Equal Halfword Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("ceq", "Compare Equal Word", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("ceqi", "Compare Equal Word Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("cgtb", "Compare Greater Than Byte", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("cgtbi", "Compare Greater Than Byte Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("cgth", "Compare Greater Than Halfword", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("cgthi", "Compare Greater Than Halfword Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("cgt", "Compare Greater Than Word", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("cgti", "Compare Greater Than Word Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("clgtb", "Compare Logical Greater Than Byte", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("clgtbi", "Compare Logical Greater Than Byte Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("clgth", "Compare Logical Greater Than Halfword", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("clgthi", "Compare Logical Greater Than Halfword Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("clgt", "Compare Logical Greater Than Word", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("clgti", "Compare Logical Greater Than Word Immediate", SpuInstructionFormat.RI10), 
				new SpuInstructionDefinition("br", "Branch Relative", SpuInstructionFormat.RI16x), 
				new SpuInstructionDefinition("bra", "Branch Absolute", SpuInstructionFormat.RI16x), 
				new SpuInstructionDefinition("brsl", "Branch Relative and Set Link", SpuInstructionFormat.RI16), 
				new SpuInstructionDefinition("brasl", "Branch Absolute and Set Link", SpuInstructionFormat.RI16), 
				// p175
				new SpuInstructionDefinition("bi", "Branch Indirect", SpuInstructionFormat.RR1DE), 
				new SpuInstructionDefinition("iret", "Interrupt Return", SpuInstructionFormat.RR1DE), 
				new SpuInstructionDefinition("bisled", "Branch Indirect and Set Link if External Data", SpuInstructionFormat.RR2DE), 
				new SpuInstructionDefinition("bisl", "Branch Indirect and Set Link", SpuInstructionFormat.RR2DE), 
				new SpuInstructionDefinition("brnz", "Branch If Not Zero Word", SpuInstructionFormat.RI16), 
				new SpuInstructionDefinition("brz", "Branch If Zero Word", SpuInstructionFormat.RI16), 
				new SpuInstructionDefinition("brhnz", "Branch If Not Zero Halfword", SpuInstructionFormat.RI16), 
				new SpuInstructionDefinition("brhz", "Branch If Zero Halfword", SpuInstructionFormat.RI16), 
				new SpuInstructionDefinition("biz", "Branch Indirect If Zero", SpuInstructionFormat.RR2DE), 
				new SpuInstructionDefinition("binz", "Branch Indirect If Not Zero", SpuInstructionFormat.RR2DE), 
				new SpuInstructionDefinition("bihz", "Branch Indirect If Zero Halfword", SpuInstructionFormat.RR2DE), 
				new SpuInstructionDefinition("bihnz", "Branch Indirect If Not Zero Halfword", SpuInstructionFormat.RR2DE), 
				// 8. Hint-for-Branch Instructions: Unusual instruction format, so currently omitted.

				// 9. Floating point.
				new SpuInstructionDefinition("fa", "Floating Add", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("dfa", "Double Floating Add", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("fs", "Floating Subtract", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("dfs", "Double Floating Subtract", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("fm", "Floating Multiply", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("dfm", "Double Floating Multiply", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("fma", "Floating Multiply and Add", SpuInstructionFormat.RRR), 
				new SpuInstructionDefinition("dfma", "Double Floating Multiply and Add", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("fnms", "Floating Negative Multiply and Subtract", SpuInstructionFormat.RRR), 
				new SpuInstructionDefinition("dfnms", "Double Floating Negative Multiply and Subtract", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("fms", "Floating Multiply and Subtract", SpuInstructionFormat.RRR), 
				new SpuInstructionDefinition("dfms", "Double Floating Multiply and Subtract", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("dfnma", "Double Floating Negative Multiply and Add", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("frest", "Floating Reciprocal Estimate", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("frsqest", "Floating Reciprocal Absolute Square Root Estimate", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("fi", "Floating Interpolate", SpuInstructionFormat.RR), 
				// p220
				new SpuInstructionDefinition("csflt", "Convert Signed Integer to Floating", SpuInstructionFormat.RI8), 
				new SpuInstructionDefinition("cflts", "Convert Floating to Signed Integer", SpuInstructionFormat.RI8), 
				new SpuInstructionDefinition("cuflt", "Convert Unsigned Integer to Floating", SpuInstructionFormat.RI8), 
				new SpuInstructionDefinition("cfltu", "Convert Floating to Unsigned Integer", SpuInstructionFormat.RI8), 
				new SpuInstructionDefinition("frds", "Floating Round Double to Single", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("fesd", "Floating Extend Single to Double", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("dfceq", "Double Floating Compare Equal", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("dfcmeq", "Double Floating Compare Magnitude Equal", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("dfcgt", "Double Floating Compare Greater Than", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("dfcmgt", "Double Floating Compare Magnitude Greater Than", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("dftsv", "Double Floating Test Special Value", SpuInstructionFormat.RI7), 
				new SpuInstructionDefinition("fceq", "Floating Compare Equal", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("fcmeq", "Floating Compare Magnitude Equal", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("fcgt", "Floating Compare Greater Than", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("fcmgt", "Floating Compare Magnitude Greater Than", SpuInstructionFormat.RR), 
				new SpuInstructionDefinition("fscrwr", "Floating-Point Status and Control Register Write", SpuInstructionFormat.RR2), 
				new SpuInstructionDefinition("fscrrd", "Floating-Point Status and Control Register Read", SpuInstructionFormat.RR1), 
				// 10. Control Instructions
				// p238
//				new SpuInstructionDefinition(), 
			};
	}
}
