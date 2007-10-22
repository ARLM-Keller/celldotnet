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
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class MfcTest : UnitTest
	{
		[Test]
		public void TestGetQueueDepth()
		{
			Func<int> del = delegate { return Mfc.GetAvailableQueueEntries(); };
			Func<int> del2 = SpeDelegateRunner.CreateSpeDelegate(del);

			SpeDelegateRunner runner = (SpeDelegateRunner) del2.Target;
			AreEqual(1, runner.CompileContext.Methods.Count);

			if (!SpeContext.HasSpeHardware)
				return;

			int depth = del2();
			AreEqual(16, depth);
		}

		[Test]
		public void TestDma_GetIntArray()
		{
			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(4))
			{
				// Create elements whose sum is 26.
				for (int i = mem.ArraySegment.Offset, j = 5; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++, j++)
				{
					mem.ArraySegment.Array[i] = j;
				}

				Converter<MainStorageArea, int> del =
					delegate(MainStorageArea input)
						{
							int[] arr = new int[4];

							uint tag = 1;
							uint tagmask = (uint) 1 << 31;
							Mfc.Get(arr, input, 4, tag);
							Mfc.WaitForDmaCompletion(tagmask);

							int sum = 0;
							for (int i = 0; i < 4; i++)
								sum += arr[i];

							return sum;
						};

				CompileContext cc = new CompileContext(del.Method);
				cc.PerformProcessing(CompileContextState.S8Complete);

				object rv = SpeContext.UnitTestRunProgram(cc, mem.GetArea());
				AreEqual(26, (int) rv);
			}
		}

		[Test]
		public void TestDma_GetFloatArray()
		{
			using (AlignedMemory<float> mem = SpeContext.AllocateAlignedFloat(4))
			{
				// Create elements whose sum is 26.
				for (int i = mem.ArraySegment.Offset, j = 1; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++, j++)
				{
					mem.ArraySegment.Array[i] = j*1.3f;
				}

				Converter<MainStorageArea, float> del =
					delegate(MainStorageArea input)
					{
						float[] arr = new float[4];

						uint tag = 1;
						uint tagmask = (uint)1 << 31;
						Mfc.Get(arr, input, 4, tag);
						Mfc.WaitForDmaCompletion(tagmask);

						float sum = 0;
						for (int i = 0; i < 4; i++)
							sum += arr[i];

						return sum;
					};

				CompileContext cc = new CompileContext(del.Method);
				cc.PerformProcessing(CompileContextState.S8Complete);

				object rv = SpeContext.UnitTestRunProgram(cc, mem.GetArea());
				Utilities.AssertWithinLimits((float)rv, 13f, 0.0001f, "");
//				AreEqual(13f, (float)rv);
			}
		}

		[Test]
		public void TestDma_WrappedGetIntArray()
		{
			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(4))
			{
				// Create elements whose sum is twenty.
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
				{
					mem.ArraySegment.Array[i] = 5;
				}

				Converter<MainStorageArea, int> del =
					delegate(MainStorageArea input)
						{
							int[] arr = new int[4];

							Mfc.Get(arr, input);

							int sum = 0;
							for (int i = 0; i < 4; i++)
								sum += arr[i];

							return sum;
						};

				CompileContext cc = new CompileContext(del.Method);
				cc.PerformProcessing(CompileContextState.S8Complete);

				object rv = SpeContext.UnitTestRunProgram(cc, mem.GetArea());
				AreEqual(20, (int) rv);
			}
		}

		[Test]
		public void TestDma_WrappedPutIntArray()
		{
			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(4))
			{
				Action<MainStorageArea> del =
					delegate(MainStorageArea input)
						{
							int[] arr = new int[4];
							for (int i = 0; i < 4; i++)
								arr[i] = i;

							Mfc.Put(arr, input);
						};

				CompileContext cc = new CompileContext(del.Method);
				cc.PerformProcessing(CompileContextState.S8Complete);

				// Run locally.
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
				{
					mem.ArraySegment.Array[i] = 0;
				}

				// Run on spu.
				object rv = SpeContext.UnitTestRunProgram(cc, mem.GetArea());

				IsNull(rv);
			}
		}


		[Test]
		public void TestDma_PutIntArray()
		{
			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(4))
			{
				Action<MainStorageArea> del =
					delegate(MainStorageArea input)
						{
							int[] arr = new int[4];
							for (int i = 0; i < 4; i++)
								arr[i] = i;

							uint tag = 1;
							uint tagMask = (uint) 1 << 31;
							Mfc.Put(arr, input, 4, tag);
							Mfc.WaitForDmaCompletion(tagMask);
						};

				CompileContext cc = new CompileContext(del.Method);
				cc.PerformProcessing(CompileContextState.S8Complete);
//				Disassembler.DisassembleToConsole(cc);
//				cc.WriteAssemblyToFile("TestDma_PutIntArray_asm.s", mem.GetArea());

				// Run locally.
//				del(mem.GetArea());
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
				{
//					AreEqual(5, mem.ArraySegment.Array[i]);
					mem.ArraySegment.Array[i] = 0;
				}

				// Run on spu.
				object rv = SpeContext.UnitTestRunProgram(cc, mem.GetArea());

//				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
//					Console.WriteLine("mem.ArraySegment.Array[{0}] = {1}", i, mem.ArraySegment.Array[i]);

				IsNull(rv);

//				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
//					AreEqual(i - mem.ArraySegment.Offset, mem.ArraySegment.Array[i]);
			}
		}
	}
}
