using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CellDotNet
{
    /// <summary>
    /// Represents an SPU instruction.
    /// </summary>
	[DebuggerDisplay("{OpCode.Name} {_spuInstructionNumber}")]
    class SpuInstruction
    {
		private static int SpuInstructionCount = 0;

    	private int _constant;

    	private VirtualRegister _ra;

    	private VirtualRegister _rb;

    	private VirtualRegister _rc;

    	private VirtualRegister _rt;

    	private SpuInstruction _next;

    	private SpuInstruction _prev;

    	private object _jumpTargetOrObjectWithAddress;

    	private SpuOpCode _opcode;

    	private int _index;

		/// <summary>
		/// A number that can be used at will by transformations.
		/// </summary>
    	public int Index
    	{
    		get { return _index; }
    		set { _index = value; }
    	}

    	public int SpuInstructionNumber
    	{
    		get { return _spuInstructionNumber; }
    	}

    	private int _spuInstructionNumber;

        public SpuInstruction(SpuOpCode opcode)
        {
			Utilities.AssertArgumentNotNull(opcode, "opcode");
			_spuInstructionNumber = ++SpuInstructionCount;
			_opcode = opcode;
        }

		public override string ToString()
		{
			return "#" + _spuInstructionNumber;
		}

    	public SpuOpCode OpCode
        {
            get { return _opcode; }
            set { _opcode = value; }
        }

    	public int Constant
        {
            get { return _constant; }
            set { _constant = value; }
        }

    	public VirtualRegister Ra
        {
            get { return _ra; }
            set { _ra = value; }
        }

    	public VirtualRegister Rb
        {
            get { return _rb; }
            set { _rb = value; }
        }

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

    	public VirtualRegister Rt
        {
			//Selvom rt ikke bruges til at returnere værdier, så bruges det stadig af f.eks. stqd
//			get { return (OpCode.NoRegisterWrite)? null : _rt; }
			get { return _rt; }
			set { _rt = value; }
        }

    	public SpuInstruction Next
    	{
			get { return _next; }
			set { _next = value; }
    	}

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

		public bool IsCall()
		{
			return _opcode == SpuOpCode.brsl;
		}

    	public List<VirtualRegister> Use
    	{
    		get
    		{
//				if (_opcode == SpuOpCode.brsl)
//					return new List<VirtualRegister>(HardwareRegister.CallerSavesRegisters);

    			List<VirtualRegister> use = new List<VirtualRegister>();
				if (Ra != null) use.Add(_ra);
				if (Rb != null) use.Add(_rb);
				if (Rc != null) use.Add(_rc);
				if (Rt != null && OpCode.NoRegisterWrite ) use.Add(_rt);
				return use;
    		}
    	}

    	/// <summary>
		/// A local branch target. This cannot be set while <see cref="ObjectWithAddress"/> is set.
		/// </summary>
		public SpuBasicBlock JumpTarget
    	{
			get
			{
				return _jumpTargetOrObjectWithAddress as SpuBasicBlock;
			}
			set
			{
				if (_jumpTargetOrObjectWithAddress != null)
					throw new Exception("Setting jumptarget for the second time??");
				_jumpTargetOrObjectWithAddress = value;
			}
    	}

		/// <summary>
		/// A non-local object/method. This cannot be set while <see cref="JumpTarget"/> is set.
		/// </summary>
		public ObjectWithAddress ObjectWithAddress
		{
			get
			{
				return _jumpTargetOrObjectWithAddress as ObjectWithAddress;
			}
			set
			{
				if (_jumpTargetOrObjectWithAddress != null)
					throw new Exception("Setting ObjectWithAddress for the second time??");
				_jumpTargetOrObjectWithAddress = value;
			}
		}

        public int Emit()
        {
			switch (_opcode.Format)
			{
				case SpuInstructionFormat.None:
					throw new Exception("Err.");
				case SpuInstructionFormat.RR1:
					return _opcode.OpCode | ((int) _ra.Register << 7);
				case SpuInstructionFormat.RR2:
					return _opcode.OpCode | ((_constant & 0x7F) << 14) | ((int)_ra.Register << 7) | (int)_rt.Register;
				case SpuInstructionFormat.RR:
					return _opcode.OpCode | ((int) _rb.Register << 14) | ((int) _ra.Register << 7) | (int) _rt.Register;
				case SpuInstructionFormat.RRR:
					return _opcode.OpCode | ((int) _rt.Register << 21) | ((int) _rb.Register << 14) | ((int) _ra.Register << 7) | (int) _rc.Register;
				case SpuInstructionFormat.RI7:
					return _opcode.OpCode | ((_constant & 0x7F) << 14) | ((int)_ra.Register << 7) | (int)_rt.Register;
				case SpuInstructionFormat.RI10:
					return _opcode.OpCode | ((_constant & 0x3ff) << 14) | ((int)_ra.Register << 7) | (int)_rt.Register;
				case SpuInstructionFormat.RI16:
					return _opcode.OpCode | ((_constant & 0xffff) << 7) | (int)_rt.Register;
				case SpuInstructionFormat.RI16NoRegs:
					return _opcode.OpCode | ((_constant & 0xffff) << 7) | 0;
				case SpuInstructionFormat.RI18:
					return _opcode.OpCode | ((_constant & 0x3ffff) << 7) | (int)_rt.Register;
				case SpuInstructionFormat.RI8:
					return _opcode.OpCode | ((_constant & 0xff) << 14) | ((int)_ra.Register << 7) | (int)_rt.Register;
				case SpuInstructionFormat.Channel:
					return _opcode.OpCode | ((_constant & 0x3f) << 7) | (int)_rt.Register;
				case SpuInstructionFormat.WEIRD:
					return _opcode.OpCode | _constant;
				default:
					throw new BadSpuInstructionException(string.Format("Invalid SPU opcode instruction format '{0}'; instruction name '{1}'.", _opcode.Format, _opcode.Name));
			}
        }

		private BadSpuInstructionException CreateEmitException()
		{
			if (JumpTarget != null)
				return new BadSpuInstructionException("JumpTarget is not null, so the instruction has not been patched.");
			else if (ObjectWithAddress != null)
				return new BadSpuInstructionException("ObjectWithAddress is not null, so the instruction has not been patched.");
			else
				return new BadSpuInstructionException(this);
		}

		public static int[] Emit(List<SpuInstruction> code)
		{
			List<int> bincode = new List<int>(code.Count);

			int instnum = 0;

			foreach (SpuInstruction inst in code)
			{
				try
				{
					bincode.Add(inst.Emit());
				}
				catch (InvalidOperationException e)
				{
					throw new InvalidOperationException(
						"An error occurred while emitting instruction no. " + instnum + " (" + inst.OpCode.Name + "): " + e.Message, e);
				}
				instnum++;
			}

			return bincode.ToArray();
		}

    	public IEnumerable<SpuInstruction> GetEnumerable()
    	{
    		SpuInstruction current = this;
    		do
    		{
    			yield return current;
    			current = current.Next;
    		} while (current != null);
    	}
    }
}