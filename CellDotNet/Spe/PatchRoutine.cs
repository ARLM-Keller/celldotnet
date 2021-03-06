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
using System.Linq;
using System.Reflection;
using System.Text;
using CellDotNet.Intermediate;
using JetBrains.Annotations;

namespace CellDotNet.Spe
{
	/// <summary>
	/// A routine which essentially consists of a blob, but also needs a bit of address patching.
	/// </summary>
	sealed class PatchRoutine : SpuRoutine
	{
		readonly int[] _code;
		readonly List<KeyValuePair<int, int>> _offsetsAndCounts;
		private int _totalWrittenInstructionCount;

		private readonly ReadOnlyCollection<MethodParameter> _parameters;
		private readonly StackTypeDescription _returnType;

		public PatchRoutine([NotNull] string name, [NotNull] MethodInfo methodinfo, [NotNull] byte[] codeInBigEndian) : base(name)
		{
			_code = new int[codeInBigEndian.Length / 4];
			Buffer.BlockCopy(codeInBigEndian, 0, _code, 0, codeInBigEndian.Length);

			Utilities.BigEndianToHost(_code);

			var i = 0;
			List<MethodParameter> parlist = new List<MethodParameter>();
			foreach (ParameterInfo pi in methodinfo.GetParameters())
			{
				Utilities.Assert(pi.Position == i - ((methodinfo.CallingConvention & CallingConventions.HasThis) != 0 ? 1 : 0), "pi.Index == i");
				i++;

				parlist.Add(new MethodParameter(pi, new TypeDeriver().GetStackTypeDescription(pi.ParameterType)));
			}
			_parameters = new ReadOnlyCollection<MethodParameter>(parlist);
			_returnType = new TypeDeriver().GetStackTypeDescription(methodinfo.ReturnType);


			Writer = new SpuInstructionWriter();
			Writer.BeginNewBasicBlock();

			_offsetsAndCounts = new List<KeyValuePair<int, int>> { new KeyValuePair<int, int>(0, 0) };
		}

		public PatchRoutine([NotNull]string name, [NotNull] int[] rawCode) : base(name)
		{
			Utilities.AssertArgumentNotNull(rawCode, "rawCode");
			_code = (int[]) rawCode.Clone();


			Writer = new SpuInstructionWriter();
			Writer.BeginNewBasicBlock();

			_offsetsAndCounts = new List<KeyValuePair<int, int>> {new KeyValuePair<int, int>(0, 0)};
		}

		public SpuInstructionWriter Writer { get; private set; }

		public override int Size
		{
			get { return _code.Length*4; }
		}

		public override int[] Emit()
		{
			return _code;
		}

		public override void PerformAddressPatching()
		{
			if (Writer.BasicBlocks.Count > 1)
				throw new InvalidOperationException("Only one block must be written.");

			UpdatePreviousOffsetInstructionWriteCount();

			if (Writer.CurrentBlock.Head == null && _offsetsAndCounts.Count == 1 && _offsetsAndCounts[0].Value == 0)
				return;

			IEnumerator<SpuInstruction> enumerator = Writer.CurrentBlock.Head.GetEnumerable().GetEnumerator();
			foreach (KeyValuePair<int, int> offsetAndCount in _offsetsAndCounts)
			{
				for (int instnum = 0; instnum < offsetAndCount.Value; instnum++)
				{
					Utilities.Assert(enumerator.MoveNext(), "enumerator.MoveNext()");
					SpuInstruction inst = enumerator.Current;

					if (inst.ObjectWithAddress != null)
					{
						int bytediff = inst.ObjectWithAddress.Offset - (Offset + offsetAndCount.Key + instnum*4);
						inst.Constant = inst.Constant + (bytediff >> 2);
					}

					_code[offsetAndCount.Key/4 + instnum] = inst.Emit();
				}
			}
			Utilities.Assert(!enumerator.MoveNext(), "!enumerator.MoveNext()");
		}

		public void Seek(int bytePosition)
		{
			if (!Utilities.IsWordAligned(bytePosition))
				throw new ArgumentException("Not word aligned: " + bytePosition);
			if (Writer.BasicBlocks.Count > 1)
				throw new InvalidOperationException("Only one block must be written.");

			UpdatePreviousOffsetInstructionWriteCount();

			_offsetsAndCounts.Add(new KeyValuePair<int, int>(bytePosition, 0));
		}

		private void UpdatePreviousOffsetInstructionWriteCount()
		{
			int currentInstCount = Writer.CurrentBlock.GetInstructionCount();

			// Record number of insts written at old offset.
			int prevOffsetInstCount;
			if (_offsetsAndCounts.Count == 1)
				prevOffsetInstCount = currentInstCount;
			else
				prevOffsetInstCount = currentInstCount - _totalWrittenInstructionCount;
//				previnstcount = currentInstCount - _offsetsAndCounts[_offsetsAndCounts.Count - 2].Value;

			_offsetsAndCounts[_offsetsAndCounts.Count - 1] = new KeyValuePair<int, int>(_offsetsAndCounts[_offsetsAndCounts.Count - 1].Key, prevOffsetInstCount);
			_totalWrittenInstructionCount = currentInstCount;
		}

		public override ReadOnlyCollection<MethodParameter> Parameters
		{
			get { return _parameters; }
		}

		public override StackTypeDescription ReturnType
		{
			get { return _returnType; }
		}
	}

}
