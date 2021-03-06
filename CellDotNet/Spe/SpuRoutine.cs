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
using System.Collections.ObjectModel;
using System.Reflection;
using CellDotNet.Intermediate;

namespace CellDotNet.Spe
{
	abstract class SpuRoutine : ObjectWithAddress
	{
		private readonly bool hasSignature;
		private readonly StackTypeDescription _returnType;
		private readonly ReadOnlyCollection<MethodParameter> _parameters;

		protected SpuRoutine()
		{
		}

		protected SpuRoutine(string name) : this(name, null)
		{
			
		}

		protected SpuRoutine(string name, MethodInfo signature) : base(name)
		{
			if (signature != null)
			{
				hasSignature = true;
				TypeDeriver td = new TypeDeriver();
				_returnType = td.GetStackTypeDescription(signature.ReturnType);
				List<MethodParameter> plist = new List<MethodParameter>();
				foreach (ParameterInfo paraminfo in signature.GetParameters())
				{
					plist.Add(new MethodParameter(paraminfo, td.GetStackTypeDescription(paraminfo.ParameterType)));
				}
				_parameters = plist.AsReadOnly();
			}
		}

		public virtual ReadOnlyCollection<MethodParameter> Parameters
		{
			get
			{
				if (hasSignature)
					return _parameters;
				throw new InvalidOperationException();
			}
		}
		public virtual StackTypeDescription ReturnType
		{
			get
			{
				if (hasSignature)
					return _returnType;
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Implementations of this method should return the emitted binary code.
		/// </summary>
		/// <returns></returns>
		public abstract int[] Emit();

		/// <summary>
		/// Override this one in order to have addresses patched.
		/// <para>
		/// See <see cref="MethodCompiler"/> for an example.
		/// </para>
		/// </summary>
		public abstract void PerformAddressPatching();

		/// <summary>
		/// Returns an enumerator for the routine, if possible.
		/// <para>
		/// This is supposed to be used for disassembly, so it should only be called once
		/// the routine is done and has been patched.
		/// </para>
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">If the routine does not support this operation.</exception>
		public virtual IEnumerable<SpuInstruction> GetFinalInstructions()
		{
			throw new InvalidOperationException();
		}

		public virtual IEnumerable<SpuInstruction> GetInstructions()
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Replaces <see cref="SpuBasicBlock"/> and <see cref="ObjectWithAddress"/> operands 
		/// stored within the instructions with their numeric offsets.
		/// </summary>
		/// <param name="bblist">
		/// The entire list of basic blocks for the method/routine. It MUST contain
		/// all basic blocks, including the outer prolog and epilog; otherwise the offsets 
		/// will not be calculated correctly.
		/// </param>
		/// <param name="epilogStart">
		/// The first basic block of the epilog. 
		/// This one is also contained in the <paramref name="bblist"/> list.
		/// </param>
		protected void PerformAddressPatching(List<SpuBasicBlock> bblist, SpuBasicBlock epilogStart)
		{
			// All offsets are byte offset from start of method; 
			// that is, from the ObjectWithAddress.

			List<KeyValuePair<int, SpuInstruction>> branchlist = new List<KeyValuePair<int, SpuInstruction>>();
			int curroffset = 0;
			foreach (SpuBasicBlock bb in bblist)
			{
				bb.Offset = curroffset;
				if (bb.Head == null)
					continue;

				foreach (SpuInstruction inst in bb.Head.GetEnumerable())
				{
					if (inst.JumpTarget != null)
						branchlist.Add(new KeyValuePair<int, SpuInstruction>(curroffset, inst));
					else if (inst.OpCode == SpuOpCode.ret)
					{
						inst.OpCode = SpuOpCode.br;

						if (epilogStart == null)
						{
							// Make sure that we've got an epilog bb to branch to.
							throw new ArgumentException("epilogStart is null, but a ret opcode was encountered.");
						}
						inst.JumpTarget = epilogStart;
						branchlist.Add(new KeyValuePair<int, SpuInstruction>(curroffset, inst));
					}
					else if (inst.ObjectWithAddress != null)
					{
						Utilities.Assert(inst.ObjectWithAddress.Offset > 0, "Bad ObjectWithAddress offset: " + inst.ObjectWithAddress.Offset + ". Type: " + inst.ObjectWithAddress.GetType().Name);

						int bytediff = inst.ObjectWithAddress.Offset - (Offset + curroffset);

						Utilities.DebugAssert(bytediff % 4 == 0, "branch offset not multiple of four bytes: " + bytediff);
						Utilities.DebugAssert(inst.OpCode != SpuOpCode.brsl || (bytediff < 1024*127 || bytediff > -1024*127), "Branch offset for brsl is not whitin bounds " + -1024*127 + " and " + 1024*127 + ": " + bytediff);

						// Instructions and therefore branch offsets are 4-byte aligned, 
						// and the ISA uses that fact for relative loads, stores an branches.
						// Constant is assumed to be a quadwords ooffset.
						inst.Constant = inst.Constant + (bytediff >> 2);
					}

					curroffset += 4;
				}
			}

			// Insert offsets.
			foreach (KeyValuePair<int, SpuInstruction> branchpair in branchlist)
			{
				SpuBasicBlock targetbb = branchpair.Value.JumpTarget;

				int relativebranchbytes = targetbb.Offset - branchpair.Key;
				// Branch offset operands don't use the last two bytes, since all
				// instructions are 4-byte aligned.
				branchpair.Value.Constant = relativebranchbytes >> 2;
			}
		}
	}
}
