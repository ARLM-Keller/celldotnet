using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// KMH Reminder, jeg tolker den som en RI7 variant.
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
        /// KMH Reminder, jeg tolker den som en RI7 variant.
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
        RI18,
		WEIRD
    }

    /// <summary>
    /// SPU instruction definitions as taken from the 
    /// "Synergistic Processor Unit Instruction Set Architecture" version 1.2
    /// </summary>
    [DebuggerDisplay("{Name}")]
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

        private int _opCodeWidth;
        public int OpCodeWidth
        {
            get { return _opCodeWidth; }
            set { _opCodeWidth = value; }
        }

        private int _opCode;
        public int OpCode
        {
            get { return _opCode; }
            set { _opCode = value; }
        }

		private bool _noRegisterWrite;
		/// <summary>
		/// Some instructions (store) have a common layout, but the rt register is not written to.
		/// For those instructions, this property returns true.
		/// </summary>
		public bool NoRegisterWrite
		{
			get { return _noRegisterWrite; }
		}

        public SpuOpCode(string _name, string _title, SpuInstructionFormat _format, String opcode)
        {
            this._name = _name;
            this._title = _title;
            this._format = _format;
            this._opCodeWidth = opcode.Length;
            this._opCode = Convert.ToInt32(opcode, 2) << 32 - this.OpCodeWidth;
			if (_name.StartsWith("st"))
				_noRegisterWrite = true;
        }

        /// <summary>
        /// TODO: Add instruction numbers to SpuOpCode and this list.
        /// TODO: Add the rest of the instructions: hint-for-branch, control instructions, channel instructions.
        /// </summary>
        //		public static SpuOpCode[] OpCodes = new SpuOpCode[]
        //			{
        public static readonly SpuOpCode lqd =
            new SpuOpCode("lqd", "Load Quadword (d-form)", SpuInstructionFormat.RI10, "00110100");
        public static readonly SpuOpCode lqx =
            new SpuOpCode("lqx", "Load Quadword (x-form)", SpuInstructionFormat.RR, "00111000100");
        public static readonly SpuOpCode lqa =
            new SpuOpCode("lqa", "Load Quadword (a-form)", SpuInstructionFormat.RI16, "001100001");
        public static readonly SpuOpCode lqr =
            new SpuOpCode("lqr", "Load Quadword Instruction Relative (a-form)", SpuInstructionFormat.RI16, "001100111");
        public static readonly SpuOpCode stqd =
            new SpuOpCode("stqd", "Store Quadword (d-form)", SpuInstructionFormat.RI10, "00100100");
        public static readonly SpuOpCode stqx =
            new SpuOpCode("stqx", "Store Quadword (x-form)", SpuInstructionFormat.RR, "00101000100");
        public static readonly SpuOpCode stqa =
            new SpuOpCode("stqa", "Store Quadword (a-form)", SpuInstructionFormat.RI16, "001000001");
        public static readonly SpuOpCode stqr =
            new SpuOpCode("stqr", "Store Quadword Instruction Relative (a-form)", SpuInstructionFormat.RI16, "001000111");
        public static readonly SpuOpCode cbd =
            new SpuOpCode("cbd", "Generate Controls for Byte Insertion (d-form)", SpuInstructionFormat.RI7, "00111110100");
        public static readonly SpuOpCode cbx =
            new SpuOpCode("cbx", "Generate Controls for Byte Insertion (x-form)", SpuInstructionFormat.RR, "00111010100");
        public static readonly SpuOpCode chd =
            new SpuOpCode("chd", "Generate Controls for Halfword Insertion (d-form)", SpuInstructionFormat.RI7, "00111110101");
        public static readonly SpuOpCode chx =
            new SpuOpCode("chx", "Generate Controls for Halfword Insertion (x-form)", SpuInstructionFormat.RR, "00111010101");
        public static readonly SpuOpCode cwd =
                new SpuOpCode("cwd", "Generate Controls for Word Insertion (d-form)", SpuInstructionFormat.RI7, "00111110110");
        public static readonly SpuOpCode cwx =
                new SpuOpCode("cwx", "Generate Controls for Word Insertion (x-form)", SpuInstructionFormat.RR, "00111010110");
        public static readonly SpuOpCode cdd =
                new SpuOpCode("cdd", "Generate Controls for Doubleword Insertion (d-form)", SpuInstructionFormat.RI7, "00111110111");
        public static readonly SpuOpCode cdx =
                new SpuOpCode("cdx", "Generate Controls for Doubleword Insertion (x-form)", SpuInstructionFormat.RR, "00111010111");

        // Constant form section.
        public static readonly SpuOpCode ilh =
                new SpuOpCode("ilh", "Immediate Load Halfword", SpuInstructionFormat.RI16, "010000011");
        public static readonly SpuOpCode ilhu =
                new SpuOpCode("ilhu", "Immediate Load Halfword Upper", SpuInstructionFormat.RI16, "010000010");
        public static readonly SpuOpCode il =
                new SpuOpCode("il", "Immediate Load Word", SpuInstructionFormat.RI16, "010000001");
        public static readonly SpuOpCode ila =
                new SpuOpCode("ila", "Immediate Load Address", SpuInstructionFormat.RI18, "0100001");
        public static readonly SpuOpCode iohl =
                new SpuOpCode("iohl", "Immediate Or Halfword Lower", SpuInstructionFormat.RI16, "011000001");
        public static readonly SpuOpCode fsmbi =
                new SpuOpCode("fsmbi", "Form Select Mask for Bytes Immediate", SpuInstructionFormat.RI16, "001100101");

        // 5. Integer and Logical OpCodes
        public static readonly SpuOpCode ah =
                new SpuOpCode("ah", "Add Halfword", SpuInstructionFormat.RR, "00011001000");
        public static readonly SpuOpCode ahi =
                new SpuOpCode("ahi", "Add Halfword Immediate", SpuInstructionFormat.RI10, "00011101");
        public static readonly SpuOpCode a =
                new SpuOpCode("a", "Add Word", SpuInstructionFormat.RR, "00011000000");
        public static readonly SpuOpCode ai =
                new SpuOpCode("ai", "Add Word Immediate", SpuInstructionFormat.RI10, "00011100");
        public static readonly SpuOpCode sfh =
                new SpuOpCode("sfh", "Subtract from Halfword", SpuInstructionFormat.RR, "00001001000");
        public static readonly SpuOpCode sfhi =
                new SpuOpCode("sfhi", "Subtract from Halfword Immediate", SpuInstructionFormat.RI10, "00001101");
        public static readonly SpuOpCode sf =
                new SpuOpCode("sf", "Subtract from Word", SpuInstructionFormat.RR, "00001000000");
        public static readonly SpuOpCode sfi =
                new SpuOpCode("sfi", "Subtract from Word Immediate", SpuInstructionFormat.RI10, "00001100");
        public static readonly SpuOpCode addx =
                new SpuOpCode("addx", "Add Extended", SpuInstructionFormat.RR, "01101000000");
        public static readonly SpuOpCode cg =
                new SpuOpCode("cg", "Carry Generate", SpuInstructionFormat.RR, "00011000010");
        public static readonly SpuOpCode cgx =
                new SpuOpCode("cgx", "Carry Generate Extended", SpuInstructionFormat.RR, "01101000010");
        public static readonly SpuOpCode sfx =
                new SpuOpCode("sfx", "Subtract from Extended", SpuInstructionFormat.RR, "01101000001");
        public static readonly SpuOpCode bg =
                new SpuOpCode("bg", "Borrow Generate", SpuInstructionFormat.RR, "00001000010");
        public static readonly SpuOpCode bgx =
                new SpuOpCode("bgx", "Borrow Generate Extended", SpuInstructionFormat.RR, "01101000011");
        public static readonly SpuOpCode mpy =
                new SpuOpCode("mpy", "Multiply", SpuInstructionFormat.RR, "01111000100");
        public static readonly SpuOpCode mpyu =
                new SpuOpCode("mpyu", "Multiply Unsigned", SpuInstructionFormat.RR, "01111001100");
        public static readonly SpuOpCode mpyi =
                new SpuOpCode("mpyi", "Multiply Immediate", SpuInstructionFormat.RI10, "01110100");
        public static readonly SpuOpCode mpyui =
                new SpuOpCode("mpyui", "Multiply Unsigned Immediate", SpuInstructionFormat.RI10, "01110101");
        public static readonly SpuOpCode mpya =
                new SpuOpCode("mpya", "Multiply and Add", SpuInstructionFormat.RRR, "1100");
        public static readonly SpuOpCode mpyh =
                new SpuOpCode("mpyh", "Multiply High", SpuInstructionFormat.RR, "01111000101");
        public static readonly SpuOpCode mpys =
                new SpuOpCode("mpys", "Multiply and Shift Right", SpuInstructionFormat.RR, "01111000111");
        public static readonly SpuOpCode mpyhh =
                new SpuOpCode("mpyhh", "Multiply High High", SpuInstructionFormat.RR, "01111000110");
        public static readonly SpuOpCode mpyhha =
                new SpuOpCode("mpyhha", "Multiply High High and Add", SpuInstructionFormat.RR, "01101000110");
        public static readonly SpuOpCode mpyhhu =
                new SpuOpCode("mpyhhu", "Multiply High High Unsigned", SpuInstructionFormat.RR, "01111001110");
        public static readonly SpuOpCode mpyhhau =
                new SpuOpCode("mpyhhau", "Multiply High High Unsigned and Add", SpuInstructionFormat.RR, "01101001110");
        // p83 clz
        public static readonly SpuOpCode clz =
                new SpuOpCode("clz", "Count Leading Zeros", SpuInstructionFormat.RR2, "01010100101");
        public static readonly SpuOpCode cntb =
                new SpuOpCode("cntb", "Count Ones in Bytes", SpuInstructionFormat.RR2, "01010110100");
        public static readonly SpuOpCode fsmb =
                new SpuOpCode("fsmb", "Form Select Mask for Bytes", SpuInstructionFormat.RR2, "00110110110");
        public static readonly SpuOpCode fsmh =
                new SpuOpCode("fsmh", "Form Select Mask for Halfwords", SpuInstructionFormat.RR2, "00110110101");
        public static readonly SpuOpCode fsm =
                new SpuOpCode("fsm", "Form Select Mask for Words", SpuInstructionFormat.RR2, "00110110100");
        public static readonly SpuOpCode gbb =
                new SpuOpCode("gbb", "Gather Bits from Bytes", SpuInstructionFormat.RR2, "00110110010");
        public static readonly SpuOpCode gbh =
                new SpuOpCode("gbh", "Gather Bits from Halfwords", SpuInstructionFormat.RR2, "00110110001");
        public static readonly SpuOpCode gb =
                new SpuOpCode("gb", "Gather Bits from Words", SpuInstructionFormat.RR2, "00110110000");
        public static readonly SpuOpCode avgb =
                new SpuOpCode("avgb", "Average Bytes", SpuInstructionFormat.RR, "00011010011");
        public static readonly SpuOpCode absdb =
                new SpuOpCode("absdb", "Absolute Differences of Bytes", SpuInstructionFormat.RR, "00001010011");
        public static readonly SpuOpCode sumb =
                new SpuOpCode("sumb", "Sum Bytes into Halfwords", SpuInstructionFormat.RR, "01001010011");
        public static readonly SpuOpCode xsbh =
                new SpuOpCode("xsbh", "Extend Sign Byte to Halfword", SpuInstructionFormat.RR2, "01010110110");
        public static readonly SpuOpCode xshw =
                new SpuOpCode("xshw", "Extend Sign Halfword to Word", SpuInstructionFormat.RR2, "01010101110");
        public static readonly SpuOpCode xswd =
                new SpuOpCode("xswd", "Extend Sign Word to Doubleword", SpuInstructionFormat.RR2, "01010100110");
        public static readonly SpuOpCode and =
                new SpuOpCode("and", "And", SpuInstructionFormat.RR, "00011000001");
        public static readonly SpuOpCode andc =
                new SpuOpCode("andc", "And with Complement", SpuInstructionFormat.RR, "01011000001");

        public static readonly SpuOpCode andbi =
                new SpuOpCode("andbi", "And Byte Immediate", SpuInstructionFormat.RI10, "00010110");
        public static readonly SpuOpCode andhi =
                new SpuOpCode("andhi", "And Halfword Immediate", SpuInstructionFormat.RI10, "00010101");
        public static readonly SpuOpCode andi =
                new SpuOpCode("andi", "And Word Immediate", SpuInstructionFormat.RI10, "00010100");
        public static readonly SpuOpCode or =
                new SpuOpCode("or", "Or", SpuInstructionFormat.RR, "00001000001");
        public static readonly SpuOpCode orc =
                new SpuOpCode("orc", "Or with Complement", SpuInstructionFormat.RR, "01011001001");
        public static readonly SpuOpCode orbi =
                new SpuOpCode("orbi", "Or Byte Immediate", SpuInstructionFormat.RI10, "00000110");
        public static readonly SpuOpCode orhi =
                new SpuOpCode("orhi", "Or Halfword Immediate", SpuInstructionFormat.RI10, "00000101");
        public static readonly SpuOpCode ori =
                new SpuOpCode("ori", "Or Word Immediate", SpuInstructionFormat.RI10, "00000100");
        public static readonly SpuOpCode orx =
                new SpuOpCode("orx", "Or Across", SpuInstructionFormat.RR2, "00111110000");
        public static readonly SpuOpCode xor =
                new SpuOpCode("xor", "Exclusive Or", SpuInstructionFormat.RR, "01001000001");
        public static readonly SpuOpCode xorbi =
                new SpuOpCode("xorbi", "Exclusive Or Byte Immediate", SpuInstructionFormat.RI10, "01000110");
        public static readonly SpuOpCode xorhi =
                new SpuOpCode("xorhi", "Exclusive Or Halfword Immediate", SpuInstructionFormat.RI10, "01000101");
        public static readonly SpuOpCode xori =
                new SpuOpCode("xori", "Exclusive Or Word Immediate", SpuInstructionFormat.RI10, "01000100");
        public static readonly SpuOpCode nand =
                new SpuOpCode("nand", "Nand", SpuInstructionFormat.RR, "00011001001");
        public static readonly SpuOpCode nor =
                new SpuOpCode("nor", "Nor", SpuInstructionFormat.RR, "00001001001");
        public static readonly SpuOpCode eqv =
                new SpuOpCode("eqv", "Equivalent", SpuInstructionFormat.RR, "01001001001");
        public static readonly SpuOpCode selb =
                new SpuOpCode("selb", "Select Bits", SpuInstructionFormat.RRR, "1000");
        public static readonly SpuOpCode shufb =
                new SpuOpCode("shufb", "Shuffle Bytes", SpuInstructionFormat.RRR, "1011");

        // 6. Shift and Rotate OpCodes
        public static readonly SpuOpCode shlh =
                new SpuOpCode("shlh", "Shift Left Halfword", SpuInstructionFormat.RR, "00001011111");
        public static readonly SpuOpCode shlhi =
                new SpuOpCode("shlhi", "Shift Left Halfword Immediate", SpuInstructionFormat.RI7, "00001111111");
        public static readonly SpuOpCode shl =
                new SpuOpCode("shl", "Shift Left Word", SpuInstructionFormat.RR, "00001011011");
        public static readonly SpuOpCode shli =
                new SpuOpCode("shli", "Shift Left Word Immediate", SpuInstructionFormat.RI7, "00001111011");
        public static readonly SpuOpCode shlqbi =
                new SpuOpCode("shlqbi", "Shift Left Quadword by Bits", SpuInstructionFormat.RR, "00111011011");
        public static readonly SpuOpCode shlqbii =
                new SpuOpCode("shlqbii", "Shift Left Quadword by Bits Immediate", SpuInstructionFormat.RI7, "00111111011");
        public static readonly SpuOpCode shlqby =
                new SpuOpCode("shlqby", "Shift Left Quadword by Bytes", SpuInstructionFormat.RR, "00111011111");
        public static readonly SpuOpCode sqlqbyi =
                new SpuOpCode("sqlqbyi", "Shift Left Quadword by Bytes Immediate", SpuInstructionFormat.RI7, "00111111111");
        public static readonly SpuOpCode shlqbybi =
                new SpuOpCode("shlqbybi", "Shift Left Quadword by Bytes from Bit Shift Count", SpuInstructionFormat.RR, "00111001111");
        public static readonly SpuOpCode roth =
                new SpuOpCode("roth", "Rotate Halfword", SpuInstructionFormat.RR, "00001011100");
        public static readonly SpuOpCode rothi =
                new SpuOpCode("rothi", "Rotate Halfword Immediate", SpuInstructionFormat.RI7, "00001111100");
        public static readonly SpuOpCode rot =
                new SpuOpCode("rot", "Rotate Word", SpuInstructionFormat.RR, "00001011000");
        public static readonly SpuOpCode roti =
                new SpuOpCode("roti", "Rotate Word Immediate", SpuInstructionFormat.RI7, "00001111000");
        public static readonly SpuOpCode rotqby =
                new SpuOpCode("rotqby", "Rotate Quadword by Bytes", SpuInstructionFormat.RR, "00111011100");
        public static readonly SpuOpCode rotqbyi =
                new SpuOpCode("rotqbyi", "Rotate Quadword by Bytes Immediate", SpuInstructionFormat.RI7, "00111111100");
        public static readonly SpuOpCode rotqbybi =
                new SpuOpCode("rotqbybi", "Rotate Quadword by Bytes from Bit Shift Count", SpuInstructionFormat.RR, "00111001100");
        public static readonly SpuOpCode rotqbi =
                new SpuOpCode("rotqbi", "Rotate Quadword by Bits", SpuInstructionFormat.RR, "00111011000");
        public static readonly SpuOpCode rotqbii =
                new SpuOpCode("rotqbii", "Rotate Quadword by Bits Immediate", SpuInstructionFormat.RI7, "00111111000");
        public static readonly SpuOpCode rothm =
                new SpuOpCode("rothm", "Rotate and Mask Halfword", SpuInstructionFormat.RR, "00001011101");
        public static readonly SpuOpCode rothmi =
                new SpuOpCode("rothmi", "Rotate and Mask Halfword Immediate", SpuInstructionFormat.RI7, "00001111101");
        public static readonly SpuOpCode rotm =
                new SpuOpCode("rotm", "Rotate and Mask Word", SpuInstructionFormat.RR, "00001011001");
        public static readonly SpuOpCode rotmi =
                new SpuOpCode("rotmi", "Rotate and Mask Word Immediate", SpuInstructionFormat.RI7, "00001111001");
        public static readonly SpuOpCode rotqmby =
                new SpuOpCode("rotqmby", "Rotate and Mask Quadword by Bytes", SpuInstructionFormat.RR, "00111011101");
        public static readonly SpuOpCode rotqmbyi =
                new SpuOpCode("rotqmbyi", "Rotate and Mask Quadword by Bytes Immediate", SpuInstructionFormat.RI7, "00111111101");
        public static readonly SpuOpCode rotqmbybi =
                new SpuOpCode("rotqmbybi", "Rotate and Mask Quadword Bytes from Bit Shift Count", SpuInstructionFormat.RR, "00111001101");
        public static readonly SpuOpCode rotqmbi =
                new SpuOpCode("rotqmbi", "Rotate and Mask Quadword by Bits", SpuInstructionFormat.RR, "00111011001");
        public static readonly SpuOpCode rotqmbii =
                new SpuOpCode("rotqmbii", "Rotate and Mask Quadword by Bits Immediate", SpuInstructionFormat.RI7, "00111111001");
        public static readonly SpuOpCode rotmah =
                new SpuOpCode("rotmah", "Rotate and Mask Algebraic Halfword", SpuInstructionFormat.RR, "00001011110");
        public static readonly SpuOpCode rotmahi =
                new SpuOpCode("rotmahi", "Rotate and Mask Algebraic Halfword Immediate", SpuInstructionFormat.RI7, "00001111110");
        public static readonly SpuOpCode rotma =
                new SpuOpCode("rotma", "Rotate and Mask Algebraic Word", SpuInstructionFormat.RR, "00001011010");
        public static readonly SpuOpCode rotmai =
                new SpuOpCode("rotmai", "Rotate and Mask Algebraic Word Immediate", SpuInstructionFormat.RI7, "00001111010");

        // 7. Compare, Branch, and Halt OpCodes
        public static readonly SpuOpCode heq =
                new SpuOpCode("heq", "Halt If Equal", SpuInstructionFormat.RR, "01111011000");
        public static readonly SpuOpCode heqi =
                new SpuOpCode("heqi", "Halt If Equal Immediate", SpuInstructionFormat.RI10, "01111111");
        public static readonly SpuOpCode hgt =
                new SpuOpCode("hgt", "Halt If Greater Than", SpuInstructionFormat.RR, "01001011000");
        public static readonly SpuOpCode hgti =
                new SpuOpCode("hgti", "Halt If Greater Than Immediate", SpuInstructionFormat.RI10, "01001111");
        public static readonly SpuOpCode hlgt =
                new SpuOpCode("hlgt", "Halt If Logically Greater Than", SpuInstructionFormat.RR, "01011011000");
        public static readonly SpuOpCode hlgti =
                new SpuOpCode("hlgti", "Halt If Logically Greater Than Immediate", SpuInstructionFormat.RI10, "01011111");
        public static readonly SpuOpCode ceqb =
                new SpuOpCode("ceqb", "Compare Equal Byte", SpuInstructionFormat.RR, "01111010000");
        public static readonly SpuOpCode ceqbi =
                new SpuOpCode("ceqbi", "Compare Equal Byte Immediate", SpuInstructionFormat.RI10, "01111110");
        public static readonly SpuOpCode ceqh =
                new SpuOpCode("ceqh", "Compare Equal Halfword", SpuInstructionFormat.RR, "01111001000");
        public static readonly SpuOpCode ceqhi =
                new SpuOpCode("ceqhi", "Compare Equal Halfword Immediate", SpuInstructionFormat.RI10, "01111101");
        public static readonly SpuOpCode ceq =
                new SpuOpCode("ceq", "Compare Equal Word", SpuInstructionFormat.RR, "01111000000");
        public static readonly SpuOpCode ceqi =
                new SpuOpCode("ceqi", "Compare Equal Word Immediate", SpuInstructionFormat.RI10, "01111100");
        public static readonly SpuOpCode cgtb =
                new SpuOpCode("cgtb", "Compare Greater Than Byte", SpuInstructionFormat.RR, "01001010000");
        public static readonly SpuOpCode cgtbi =
                new SpuOpCode("cgtbi", "Compare Greater Than Byte Immediate", SpuInstructionFormat.RI10, "01001110");
        public static readonly SpuOpCode cgth =
                new SpuOpCode("cgth", "Compare Greater Than Halfword", SpuInstructionFormat.RR, "01001001000");
        public static readonly SpuOpCode cgthi =
                new SpuOpCode("cgthi", "Compare Greater Than Halfword Immediate", SpuInstructionFormat.RI10, "01001101");
        public static readonly SpuOpCode cgt =
                new SpuOpCode("cgt", "Compare Greater Than Word", SpuInstructionFormat.RR, "01001000000");
        public static readonly SpuOpCode cgti =
                new SpuOpCode("cgti", "Compare Greater Than Word Immediate", SpuInstructionFormat.RI10, "01001100");
        public static readonly SpuOpCode clgtb =
                new SpuOpCode("clgtb", "Compare Logical Greater Than Byte", SpuInstructionFormat.RR, "01011010000");
        public static readonly SpuOpCode clgtbi =
                new SpuOpCode("clgtbi", "Compare Logical Greater Than Byte Immediate", SpuInstructionFormat.RI10, "01011110");
        public static readonly SpuOpCode clgth =
                new SpuOpCode("clgth", "Compare Logical Greater Than Halfword", SpuInstructionFormat.RR, "01011001000");
        public static readonly SpuOpCode clgthi =
                new SpuOpCode("clgthi", "Compare Logical Greater Than Halfword Immediate", SpuInstructionFormat.RI10, "01011101");
        public static readonly SpuOpCode clgt =
                new SpuOpCode("clgt", "Compare Logical Greater Than Word", SpuInstructionFormat.RR, "01011000000");
        public static readonly SpuOpCode clgti =
                new SpuOpCode("clgti", "Compare Logical Greater Than Word Immediate", SpuInstructionFormat.RI10, "01011100");
        public static readonly SpuOpCode br =
                new SpuOpCode("br", "Branch Relative", SpuInstructionFormat.RI16x, "001100100");
        public static readonly SpuOpCode bra =
                new SpuOpCode("bra", "Branch Absolute", SpuInstructionFormat.RI16x, "001100000");
        public static readonly SpuOpCode brsl =
                new SpuOpCode("brsl", "Branch Relative and Set Link", SpuInstructionFormat.RI16, "001100110");
        public static readonly SpuOpCode brasl =
                new SpuOpCode("brasl", "Branch Absolute and Set Link", SpuInstructionFormat.RI16, "001100010");
        // p175
        public static readonly SpuOpCode bi =
                new SpuOpCode("bi", "Branch Indirect", SpuInstructionFormat.RR1DE, "00110101000");
        public static readonly SpuOpCode iret =
                new SpuOpCode("iret", "Interrupt Return", SpuInstructionFormat.RR1DE, "00110101010");
        public static readonly SpuOpCode bisled =
                new SpuOpCode("bisled", "Branch Indirect and Set Link if External Data", SpuInstructionFormat.RR2DE, "00110101011");
        public static readonly SpuOpCode bisl =
                new SpuOpCode("bisl", "Branch Indirect and Set Link", SpuInstructionFormat.RR2DE, "00110101001");
        public static readonly SpuOpCode brnz =
                new SpuOpCode("brnz", "Branch If Not Zero Word", SpuInstructionFormat.RI16, "001000010");
        public static readonly SpuOpCode brz =
                new SpuOpCode("brz", "Branch If Zero Word", SpuInstructionFormat.RI16, "001000000");
        public static readonly SpuOpCode brhnz =
                new SpuOpCode("brhnz", "Branch If Not Zero Halfword", SpuInstructionFormat.RI16, "001000110");
        public static readonly SpuOpCode brhz =
                new SpuOpCode("brhz", "Branch If Zero Halfword", SpuInstructionFormat.RI16, "001000100");
        public static readonly SpuOpCode biz =
                new SpuOpCode("biz", "Branch Indirect If Zero", SpuInstructionFormat.RR2DE, "00100101000");
        public static readonly SpuOpCode binz =
                new SpuOpCode("binz", "Branch Indirect If Not Zero", SpuInstructionFormat.RR2DE, "00100101001");
        public static readonly SpuOpCode bihz =
                new SpuOpCode("bihz", "Branch Indirect If Zero Halfword", SpuInstructionFormat.RR2DE, "0100101010");
        public static readonly SpuOpCode bihnz =
                new SpuOpCode("bihnz", "Branch Indirect If Not Zero Halfword", SpuInstructionFormat.RR2DE, "00100101011");
        // 8. Hint-for-Branch OpCodes: Unusual instruction format, so currently omitted.

        // 9. Floating point.
        public static readonly SpuOpCode fa =
                new SpuOpCode("fa", "Floating Add", SpuInstructionFormat.RR, "01011000100");
        public static readonly SpuOpCode dfa =
                new SpuOpCode("dfa", "Double Floating Add", SpuInstructionFormat.RR, "01011001100");
        public static readonly SpuOpCode fs =
                new SpuOpCode("fs", "Floating Subtract", SpuInstructionFormat.RR, "01011000101");
        public static readonly SpuOpCode dfs =
                new SpuOpCode("dfs", "Double Floating Subtract", SpuInstructionFormat.RR, "01011001101");
        public static readonly SpuOpCode fm =
                new SpuOpCode("fm", "Floating Multiply", SpuInstructionFormat.RR, "01011000110");
        public static readonly SpuOpCode dfm =
                new SpuOpCode("dfm", "Double Floating Multiply", SpuInstructionFormat.RR, "01011001110");
        public static readonly SpuOpCode fma =
                new SpuOpCode("fma", "Floating Multiply and Add", SpuInstructionFormat.RRR, "1110");
        public static readonly SpuOpCode dfma =
                new SpuOpCode("dfma", "Double Floating Multiply and Add", SpuInstructionFormat.RR, "01101011100");
        public static readonly SpuOpCode fnms =
                new SpuOpCode("fnms", "Floating Negative Multiply and Subtract", SpuInstructionFormat.RRR, "1101");
        public static readonly SpuOpCode dfnms =
                new SpuOpCode("dfnms", "Double Floating Negative Multiply and Subtract", SpuInstructionFormat.RR, "01101011110");
        public static readonly SpuOpCode fms =
                new SpuOpCode("fms", "Floating Multiply and Subtract", SpuInstructionFormat.RRR, "1111");
        public static readonly SpuOpCode dfms =
                new SpuOpCode("dfms", "Double Floating Multiply and Subtract", SpuInstructionFormat.RR, "01101011101");
        public static readonly SpuOpCode dfnma =
                new SpuOpCode("dfnma", "Double Floating Negative Multiply and Add", SpuInstructionFormat.RR, "01101011111");
        public static readonly SpuOpCode frest =
                new SpuOpCode("frest", "Floating Reciprocal Estimate", SpuInstructionFormat.RR2, "00110111000");
        public static readonly SpuOpCode frsqest =
                new SpuOpCode("frsqest", "Floating Reciprocal Absolute Square Root Estimate", SpuInstructionFormat.RR2, "00110111001");
        public static readonly SpuOpCode fi =
                new SpuOpCode("fi", "Floating Interpolate", SpuInstructionFormat.RR, "01111010100");
        // p220
        public static readonly SpuOpCode csflt =
                new SpuOpCode("csflt", "Convert Signed Integer to Floating", SpuInstructionFormat.RI8, "0111011010");
        public static readonly SpuOpCode cflts =
                new SpuOpCode("cflts", "Convert Floating to Signed Integer", SpuInstructionFormat.RI8, "0111011000");
        public static readonly SpuOpCode cuflt =
                new SpuOpCode("cuflt", "Convert Unsigned Integer to Floating", SpuInstructionFormat.RI8, "0111011011");
        public static readonly SpuOpCode cfltu =
                new SpuOpCode("cfltu", "Convert Floating to Unsigned Integer", SpuInstructionFormat.RI8, "0111011001");
        public static readonly SpuOpCode frds =
                new SpuOpCode("frds", "Floating Round Double to Single", SpuInstructionFormat.RR2, "01110111001");
        public static readonly SpuOpCode fesd =
                new SpuOpCode("fesd", "Floating Extend Single to Double", SpuInstructionFormat.RR2, "01110111000");
        public static readonly SpuOpCode dfceq =
                new SpuOpCode("dfceq", "Double Floating Compare Equal", SpuInstructionFormat.RR, "01111000011");
        public static readonly SpuOpCode dfcmeq =
                new SpuOpCode("dfcmeq", "Double Floating Compare Magnitude Equal", SpuInstructionFormat.RR, "01111001011");
        public static readonly SpuOpCode dfcgt =
                new SpuOpCode("dfcgt", "Double Floating Compare Greater Than", SpuInstructionFormat.RR, "01011000011");
        public static readonly SpuOpCode dfcmgt =
                new SpuOpCode("dfcmgt", "Double Floating Compare Magnitude Greater Than", SpuInstructionFormat.RR, "01011001011");
        public static readonly SpuOpCode dftsv =
                new SpuOpCode("dftsv", "Double Floating Test Special Value", SpuInstructionFormat.RI7, "01110111111");
        public static readonly SpuOpCode fceq =
                new SpuOpCode("fceq", "Floating Compare Equal", SpuInstructionFormat.RR, "01111000010");
        public static readonly SpuOpCode fcmeq =
                new SpuOpCode("fcmeq", "Floating Compare Magnitude Equal", SpuInstructionFormat.RR, "01111001010");
        public static readonly SpuOpCode fcgt =
                new SpuOpCode("fcgt", "Floating Compare Greater Than", SpuInstructionFormat.RR, "01011000010");
        public static readonly SpuOpCode fcmgt =
                new SpuOpCode("fcmgt", "Floating Compare Magnitude Greater Than", SpuInstructionFormat.RR, "01011001010");
        public static readonly SpuOpCode fscrwr =
                new SpuOpCode("fscrwr", "Floating-Point Status and Control Register Write", SpuInstructionFormat.RR2, "01110111010");
        public static readonly SpuOpCode fscrrd =
                new SpuOpCode("fscrrd", "Floating-Point Status and Control Register Read", SpuInstructionFormat.RR1, "01110011000");
        // 10. Control OpCodes
        // p238
        //			};
		public static readonly SpuOpCode stop =
				new SpuOpCode("stop", "Stop and Signal", SpuInstructionFormat.WEIRD, "00000000000");



    }

}
