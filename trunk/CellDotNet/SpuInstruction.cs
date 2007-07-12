using System;
using System.Collections.Generic;
using System.Diagnostics;

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

        private VirtualRegister _ra;

        public VirtualRegister Ra
        {
            get { return _ra; }
            set { _ra = value; }
        }

        private VirtualRegister _rb;

        public VirtualRegister Rb
        {
            get { return _rb; }
            set { _rb = value; }
        }

        private VirtualRegister _rc;

        public VirtualRegister Rc
        {
            get { return _rc; }
            set { _rc = value; }
        }

        // Maby a little over kill, but result in more nice/better programming.
        public ICollection<VirtualRegister> Sources
        {
            get
            {
                ICollection<VirtualRegister> s = new LinkedList<VirtualRegister>();
                if (_ra != null) s.Add(_ra);
                if (_rb != null) s.Add(_rb);
                if (_rc != null) s.Add(_rc);
				if (_rt != null && OpCode.NoRegisterWrite) s.Add(_rt);
                return s;
            }
        }

        private VirtualRegister _rt;

        public VirtualRegister Rt
        {
			//Selvom rt ikke bruges til at returnere værdier, så bruges det stadig af f.eks. stqd
//			get { return (OpCode.NoRegisterWrite)? null : _rt; }
			get { return _rt; }
			set { _rt = value; }
        }

    	private SpuInstruction _next;
    	public SpuInstruction Next
    	{
			get { return _next; }
			set { _next = value; }
    	}

		private SpuInstruction _prev;
		public SpuInstruction Prev
		{
			get { return _prev; }
			set { _prev = value;}
		}

    	public VirtualRegister Def
    	{
    		get
			{
				if (!OpCode.NoRegisterWrite) return Rt;
				return null;
			}
    	}

    	public List<VirtualRegister> Use
    	{
    		get
    		{
    			List<VirtualRegister> use = new List<VirtualRegister>();
				if (Ra != null) use.Add(_ra);
				if (Rb != null) use.Add(_rb);
				if (Rc != null) use.Add(_rc);
				if (Rt != null && OpCode.NoRegisterWrite ) use.Add(_rt);
				return use;
    		}
    	}

    	private SpuBasicBlock _jumpTarget;
		public SpuBasicBlock JumpTarget
    	{
			get
			{
				throw new Exception("Use of JumpTarget set is not implemented.");
				return _jumpTarget;
			}
			set { _jumpTarget = value; }
    	}

        public int emit()
        {
			HardwareRegister reg3 = (_rc != null) ? _rc.Location as HardwareRegister : null;
			HardwareRegister reg2 = (_rb != null) ? _rb.Location as HardwareRegister : null;
			HardwareRegister reg1 = (_ra != null) ? _ra.Location as HardwareRegister : null;
			HardwareRegister dest = (_rt != null) ? _rt.Location as HardwareRegister : null;


            switch (_opcode.Format)
            {
                case SpuInstructionFormat.None:
                    throw new Exception("Err.");
                case SpuInstructionFormat.RR2:
                case SpuInstructionFormat.RR1:
                case SpuInstructionFormat.RR:
                    if (reg1 != null && reg2 != null && dest != null)
                        return _opcode.OpCode | (reg2.Register << 14) | (reg1.Register << 7) | dest.Register;
                    else
                        throw new Exception("Err.");
                case SpuInstructionFormat.RRR:
                    if (reg1 != null && reg2 != null && reg3 != null && dest != null)
                        return _opcode.OpCode | (dest.Register << 21) | (reg2.Register << 14) | (reg1.Register << 7) | reg3.Register;
                    else
                        throw new Exception("Err.");
                case SpuInstructionFormat.RI7:
                    if (reg1 != null && dest != null)
                        return _opcode.OpCode | ((_constant & 0x7F) << 14) | (reg1.Register << 7) | dest.Register;
                    else
                        throw new Exception("Err.");
                case SpuInstructionFormat.RI10: 
                    if (reg1 != null && dest != null)
                        return _opcode.OpCode | ((_constant & 0x3ff) << 14) | (reg1.Register << 7) | dest.Register;
                    else
                        throw new Exception("Err.");
                case SpuInstructionFormat.RI16:
					if (dest != null)
						return _opcode.OpCode | ((_constant & 0xffff) << 7) | dest.Register;
					else
						throw new Exception("Err.");
				case SpuInstructionFormat.RI16NoRegs:
                        return _opcode.OpCode | ((_constant & 0xffff) << 7) | 0;
                case SpuInstructionFormat.RI18:
                    if (reg1 != null && dest != null)
                        return _opcode.OpCode | ((_constant & 0x3ffff) << 7) | dest.Register;
                    else
                        throw new Exception("Err.");
                case SpuInstructionFormat.RI8:
                    if (reg1 != null && dest != null)
                        return _opcode.OpCode | ((_constant & 0xff) << 14) | (reg1.Register << 7) | dest.Register;
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
    }
}