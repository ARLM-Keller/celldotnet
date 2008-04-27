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
using System.Diagnostics;
using JetBrains.Annotations;

namespace CellDotNet.Spe
{
    /// <summary>
    /// Represents an SPU instruction.
    /// </summary>
	[DebuggerDisplay("{DebuggerDisplay}")]
    class SpuInstruction
    {
		private static int SpuInstructionCount;

    	private object _jumpTargetOrObjectWithAddress;

    	private SpuOpCode _opcode;

    	internal string DebuggerDisplay
    	{
    		get { return OpCode.Name + " " + _spuInstructionNumber; }
    	}

    	/// <summary>
    	/// A number that can be used at will by transformations.
    	/// </summary>
    	public int Index { get; set; }

    	public int SpuInstructionNumber
    	{
    		get { return _spuInstructionNumber; }
    	}

    	private readonly int _spuInstructionNumber;

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

		[NotNull]
    	public SpuOpCode OpCode
        {
            get { return _opcode; }
            set
            {
				Utilities.AssertArgumentNotNull(value, "value");
            	_opcode = value;
            }
        }

    	public int Constant { get; set;}

		[CanBeNull]
		public VirtualRegister Ra { get; set; }

		[CanBeNull]
		public VirtualRegister Rb { get; set; }

		[CanBeNull]
		public VirtualRegister Rc { get; set; }

		[CanBeNull]
		public VirtualRegister Rt { get; set; }

    	[CanBeNull]
    	public SpuInstruction Next { get; set; }

    	[CanBeNull]
    	public SpuInstruction Prev { get; set; }

    	public VirtualRegister Def
    	{
    		get
			{
				if (!OpCode.RegisterRtNotWritten) return Rt;
				return null;
			}
    	}

		/// <summary>
		/// The set of virtual registers that this instruction uses. If you are going to use this alot,
		/// consider using <see cref="AppendUses"/> instead to avoid repeated list allocation.
		/// </summary>
    	public List<VirtualRegister> Use
    	{
    		get
    		{
    			List<VirtualRegister> uses = new List<VirtualRegister>();
    			AppendUses(uses);
    			return uses;
    		}
    	}

		/// <summary>
		/// Appends virtual registers that the instruction uses to the list. This avoids the list allocation
		/// that <see cref="Use"/> does.
		/// </summary>
		/// <param name="targetList"></param>
    	public void AppendUses(List<VirtualRegister> targetList)
    	{
    		if (Ra != null) targetList.Add(Ra);
    		if (Rb != null) targetList.Add(Rb);
    		if (Rc != null) targetList.Add(Rc);
    		if (Rt != null && OpCode.RegisterRtRead) targetList.Add(Rt);
    	}

    	/// <summary>
		/// A local branch target. This cannot be set while <see cref="ObjectWithAddress"/> is set.
		/// </summary>
		[CanBeNull]
		public SpuBasicBlock JumpTarget
    	{
			get
			{
				return _jumpTargetOrObjectWithAddress as SpuBasicBlock;
			}
			set
			{
				if (_jumpTargetOrObjectWithAddress != null)
					throw new InvalidOperationException("Setting jumptarget for the second time??");
				_jumpTargetOrObjectWithAddress = value;
			}
    	}

		/// <summary>
		/// A non-local object/method. This cannot be set while <see cref="JumpTarget"/> is set.
		/// </summary>
		[CanBeNull]
		public ObjectWithAddress ObjectWithAddress
		{
			get
			{
				return _jumpTargetOrObjectWithAddress as ObjectWithAddress;
			}
			set
			{
				if (_jumpTargetOrObjectWithAddress != null)
					throw new InvalidOperationException("Setting ObjectWithAddress for the second time??");
				_jumpTargetOrObjectWithAddress = value;
			}
		}

        public int Emit()
        {
			switch (_opcode.Format)
			{
				case SpuInstructionFormat.None:
					throw new InvalidOperationException("Err.");
				case SpuInstructionFormat.RR1:
					return _opcode.OpCode | ((int) Ra.Register << 7);
				case SpuInstructionFormat.RR2:
					return _opcode.OpCode | ((Constant & 0x7F) << 14) | ((int)Ra.Register << 7) | (int)Rt.Register;
				case SpuInstructionFormat.RR:
					return _opcode.OpCode | ((int) Rb.Register << 14) | ((int) Ra.Register << 7) | (int) Rt.Register;
				case SpuInstructionFormat.Rrr:
					return _opcode.OpCode | ((int) Rt.Register << 21) | ((int) Rb.Register << 14) | ((int) Ra.Register << 7) | (int) Rc.Register;
				case SpuInstructionFormat.RI7:
					return _opcode.OpCode | ((Constant & 0x7F) << 14) | ((int)Ra.Register << 7) | (int)Rt.Register;
				case SpuInstructionFormat.RI10:
						return _opcode.OpCode | ((Constant & 0x3ff) << 14) | ((int)Ra.Register << 7) | (int)Rt.Register;
				case SpuInstructionFormat.RI16:
					return _opcode.OpCode | ((Constant & 0xffff) << 7) | (int)Rt.Register;
				case SpuInstructionFormat.RI16NoRegs:
					return _opcode.OpCode | ((Constant & 0xffff) << 7) | 0;
				case SpuInstructionFormat.RI14:
					return _opcode.OpCode | (Constant & 0x3fff);
				case SpuInstructionFormat.RI18:
					return _opcode.OpCode | ((Constant & 0x3ffff) << 7) | (int)Rt.Register;
				case SpuInstructionFormat.RI8:
					return _opcode.OpCode | ((Constant & 0xff) << 14) | ((int)Ra.Register << 7) | (int)Rt.Register;
				case SpuInstructionFormat.Channel:
					return _opcode.OpCode | ((Constant & 0x3f) << 7) | (int)Rt.Register;
				case SpuInstructionFormat.Weird:
					return _opcode.OpCode | Constant;
				default:
					throw new BadSpuInstructionException(string.Format("Invalid SPU opcode instruction format '{0}'; instruction name '{1}'.", _opcode.Format, _opcode.Name));
			}
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