using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Mono.Cecil.Cil;

namespace CellDotNet
{
    /// <summary>
    /// Represents an SPU instruction.
    /// </summary>
	[DebuggerDisplay("{OpCode}")]
    internal class SpuInstruction
    {
        public SpuInstruction(SpuOpCode _opcode)
        {
            this._opcode = _opcode;
        }

        private SpuOpCode _opcode;

        public SpuOpCode OpCode
        {
            get { return _opcode; }
            set { _opcode = value; }
        }

        private int _constant;

        public int Constant
        {
            get { return _constant; }
            set { _constant = value; }
        }

        private VirtualRegister _source1;

        public VirtualRegister Source1
        {
            get { return _source1; }
            set { _source1 = value; }
        }

        private VirtualRegister _source2;

        public VirtualRegister Source2
        {
            get { return _source2; }
            set { _source2 = value; }
        }

        private VirtualRegister _source3;

        public VirtualRegister Source3
        {
            get { return _source3; }
            set { _source3 = value; }
        }

        // Maby a little over kill, but result in more nice/better programming.
        public ICollection<VirtualRegister> Sources
        {
            get
            {
                ICollection<VirtualRegister> s = new LinkedList<VirtualRegister>();
                if (_source1 != null) s.Add(_source1);
                if (_source2 != null) s.Add(_source2);
                if (_source3 != null) s.Add(_source3);
                return s;
            }
        }

        private VirtualRegister _destination;

        public VirtualRegister Destination
        {
            get { return _destination; }
            set { _destination = value; }
        }

        public int emit()
        {
            HardwareRegister reg3 = _source3.Location as HardwareRegister;
            HardwareRegister reg2 = _source2.Location as HardwareRegister;
            HardwareRegister reg1 = _source1.Location as HardwareRegister;
            HardwareRegister dest = _destination.Location as HardwareRegister;


            switch (_opcode.Format)
            {
                case SpuInstructionFormat.None:
                    throw new Exception("Err.");
                case SpuInstructionFormat.RR2:
                case SpuInstructionFormat.RR1:
                case SpuInstructionFormat.RR:
                    if (reg1 != null && reg2 != null && dest != null)
                        return _opcode.OpCode | reg2.Register << 14 | reg1.Register << 7 | dest.Register;
                    else
                        throw new Exception("Err.");
                case SpuInstructionFormat.RRR:
                    if (reg1 != null && reg2 != null && reg3 != null && dest != null)
                        return _opcode.OpCode | dest.Register << 21 | reg2.Register << 14 | reg1.Register << 7 | reg3.Register;
                    else
                        throw new Exception("Err.");
                case SpuInstructionFormat.RR1DE:
                case SpuInstructionFormat.RR2DE:
                case SpuInstructionFormat.RI7:
                    if (reg1 != null && dest != null)
                        return _opcode.OpCode | _constant & 0x7F << 14 | reg1.Register << 7 | dest.Register;
                    else
                        throw new Exception("Err.");
                case SpuInstructionFormat.RI10: 
                    if (reg1 != null && dest != null)
                        return _opcode.OpCode | _constant & 0x3ff << 14 | reg1.Register << 7 | dest.Register;
                    else
                        throw new Exception("Err.");
                case SpuInstructionFormat.RI16:
                case SpuInstructionFormat.RI16x:
                    if (reg1 != null && dest != null)
                        return _opcode.OpCode | _constant & 0xffff << 7 | dest.Register;
                    else
                        throw new Exception("Err.");
                case SpuInstructionFormat.RI18:
                    if (reg1 != null && dest != null)
                        return _opcode.OpCode | _constant & 0x3ffff << 7 | dest.Register;
                    else
                        throw new Exception("Err.");
                case SpuInstructionFormat.RI8:
                    if (reg1 != null && dest != null)
                        return _opcode.OpCode | _constant & 0xff << 14 | reg1.Register << 7 | dest.Register;
                    else
                        throw new Exception("Err.");
				case SpuInstructionFormat.WEIRD:
            		return _opcode.OpCode | _constant;
            }
            return 0;
        }

		public static int[] emit(List<SpuInstruction> code)
		{
			List<int> bincode = new List<int>(code.Count);

			foreach (SpuInstruction inst in code)
			{
				bincode.Add(inst.emit());
			}

			return bincode.ToArray();
		}

        public String ToString()
        {
            return ""; //TODO
        }
    }
}