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

#if UNITTEST

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

			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(4))
			{
				// Create elements whose sum is 26.
				for (int i = mem.ArraySegment.Offset, j = 5; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++, j++)
				{
					mem.ArraySegment.Array[i] = j;
				}

				if (!SpeContext.HasSpeHardware)
					return;

				object rv = SpeContext.UnitTestRunProgram(cc, mem.GetArea());
				AreEqual(26, (int) rv);
			}
		}

		[Test]
		public void TestDma_GetFloatArray()
		{
			Converter<MainStorageArea, float> del =
				delegate(MainStorageArea input)
					{
						float[] arr = new float[4];

						uint tag = 1;
						uint tagmask = (uint) 1 << 31;
						Mfc.Get(arr, input, 4, tag);
						Mfc.WaitForDmaCompletion(tagmask);

						float sum = 0;
						for (int i = 0; i < 4; i++)
							sum += arr[i];

						return sum;
					};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S8Complete);

			using (AlignedMemory<float> mem = SpeContext.AllocateAlignedFloat(4))
			{
				// Create elements whose sum is 26.
				for (int i = mem.ArraySegment.Offset, j = 1; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++, j++)
				{
					mem.ArraySegment.Array[i] = j*1.3f;
				}

				if (!SpeContext.HasSpeHardware)
					return;

				object rv = SpeContext.UnitTestRunProgram(cc, mem.GetArea());
				Utilities.AssertWithinLimits((float) rv, 13f, 0.0001f, "");
			}
		}

		[Test]
		public void TestDma_GetLargeIntArray()
		{
			const int elementcount = 16*1024;
			const int magicnumber = 0xfa794d;

			Converter<MainStorageArea, int> del =
				delegate(MainStorageArea input)
					{
						int[] arr = new int[elementcount];
						for (int i = 0; i < arr.Length; i++)
							arr[i] = 0;

						Mfc.Get(arr, input);

						int result = 0;
						for (int i = 0; i < arr.Length; i++)
							result += SpuMath.CompareEqualsAndSelect(arr[i], magicnumber, 1, 0);

						return result;
					};

			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(elementcount))
			{
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
					mem.ArraySegment.Array[i] = magicnumber;

				if (!SpeContext.HasSpeHardware)
					return;

				object rv = SpeContext.UnitTestRunProgram(del, mem.GetArea());
				AreEqual(elementcount, (int) rv);
			}
		}

		[Test]
		public void TestDma_GetLargeIntVectorArray()
		{
			const int elementcount = 4*1024;
			const int magicnumber = 0xfa794d;

			Converter<MainStorageArea, int> del =
				delegate(MainStorageArea input)
					{
						Int32Vector zerrovector = Int32Vector.Splat(0);
						Int32Vector unitvector = Int32Vector.Splat(1);
						Int32Vector magicvector = Int32Vector.Splat(magicnumber);

						Int32Vector[] arr = new Int32Vector[elementcount];
						for (int i = 0; i < arr.Length; i++)
							arr[i] = zerrovector;

						Mfc.Get(arr, input);

						Int32Vector result = zerrovector;
						for (int i = 0; i < arr.Length; i++)
							result += SpuMath.CompareEqualsAndSelect(arr[i], magicvector, unitvector, zerrovector);

						return result.E1 + result.E2 + result.E3 + result.E4;
					};

			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(elementcount*4))
			{
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
					mem.ArraySegment.Array[i] = magicnumber;

				if (!SpeContext.HasSpeHardware)
					return;

				object rv = SpeContext.UnitTestRunProgram(del, mem.GetArea());
				AreEqual(elementcount*4, (int) rv);
			}
		}

		[Test]
		public void TestDma_GetLargeFloatVectorArray()
		{
			const int elementcount = 4*1024;
			const float magicnumber = 1234.5f;

			Converter<MainStorageArea, int> del =
				delegate(MainStorageArea input)
					{
						Int32Vector zerrovector = Int32Vector.Splat(0);
						Int32Vector unitvector = Int32Vector.Splat(1);

						Float32Vector zerrofloatvector = Float32Vector.Splat(0f);
						Float32Vector magicvector = Float32Vector.Splat(magicnumber);

						Float32Vector[] arr = new Float32Vector[elementcount];
						for (int i = 0; i < arr.Length; i++)
							arr[i] = zerrofloatvector;

						Mfc.Get(arr, input);

						Int32Vector result = zerrovector;
						for (int i = 0; i < arr.Length; i++)
						{
							result += SpuMath.CompareGreaterThanAndSelect(arr[i], magicvector, zerrovector, unitvector);
							result += SpuMath.CompareGreaterThanAndSelect(magicvector, arr[i], zerrovector, unitvector);
						}

						return result.E1 + result.E2 + result.E3 + result.E4;
					};

			using (AlignedMemory<float> mem = SpeContext.AllocateAlignedFloat(elementcount*4))
			{
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
					mem.ArraySegment.Array[i] = magicnumber;

				if (!SpeContext.HasSpeHardware)
					return;

				object rv = SpeContext.UnitTestRunProgram(del, mem.GetArea());
				AreEqual(elementcount*4*2, (int) rv);
			}
		}

		[Test]
		public void TestDma_GetLargeFloatArray()
		{
			const int elementcount = 16 * 1024;
			const float magicnumber = 1234.5f;

			Converter<MainStorageArea, int> del =
				delegate(MainStorageArea input)
					{
						float[] arr = new float[elementcount];

						for (int i = 0; i < arr.Length; i++)
							arr[i] = 0;

						Mfc.Get(arr, input);

						int result = 0;

						for (int i = 0; i < arr.Length; i++)
							if (arr[i] == magicnumber)
								result++;

						return result;
					};

			using (AlignedMemory<float> mem = SpeContext.AllocateAlignedFloat(elementcount))
			{
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
					mem.ArraySegment.Array[i] = magicnumber;

				if (!SpeContext.HasSpeHardware)
					return;

				object rv = SpeContext.UnitTestRunProgram(del, mem.GetArea());
				AreEqual(elementcount, (int) rv);
			}
		}

		[Test]
		public void TestDma_WrappedGetIntArray()
		{
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

			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(4))
			{
				// Create elements whose sum is twenty.
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
				{
					mem.ArraySegment.Array[i] = 5;
				}

				if (!SpeContext.HasSpeHardware)
					return;

				object rv = SpeContext.UnitTestRunProgram(cc, mem.GetArea());
				AreEqual(20, (int) rv);
			}
		}

		[Test]
		public void TestDma_WrappedPutIntArray()
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

			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(4))
			{
				// Run locally.
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
				{
					mem.ArraySegment.Array[i] = 0;
				}

				if (!SpeContext.HasSpeHardware)
					return;

				// Run on spu.
				object rv = SpeContext.UnitTestRunProgram(cc, mem.GetArea());

				IsNull(rv);
			}
		}


		[Test]
		public void TestDma_PutIntArray()
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

			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(4))
			{
				// Run locally.
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
				{
					mem.ArraySegment.Array[i] = 0;
				}

				if (!SpeContext.HasSpeHardware)
					return;

				// Run on spu.
				object rv = SpeContext.UnitTestRunProgram(cc, mem.GetArea());

				IsNull(rv);
			}
		}

		[Test]
		public void TestDma_PutLargeIntArray()
		{
			const int elementcount = 16 * 1024;
			const int magicnumber = 0xfa794d;

			Action<MainStorageArea> del =
				delegate(MainStorageArea input)
					{
						int[] arr = new int[elementcount];
						for (int i = 0; i < arr.Length; i++)
							arr[i] = magicnumber;

						Mfc.Put(arr, input);
					};

			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(elementcount))
			{
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
					mem.ArraySegment.Array[i] = 0;

				if (!SpeContext.HasSpeHardware)
					return;

				SpeContext.UnitTestRunProgram(del, mem.GetArea());

				int result = 0;
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
					if (mem.ArraySegment.Array[i] == magicnumber)
						result++;


				AreEqual(elementcount, result);
			}
		}

		[Test]
		public void TestDma_PutLargeIntVectorArray()
		{
			const int elementcount = 4*1024;
			const int magicnumber = 0xfa794d;

			Converter<MainStorageArea, int> del =
				delegate(MainStorageArea input)
					{
						Int32Vector zerrovector = Int32Vector.Splat(0);
						Int32Vector unitvector = Int32Vector.Splat(1);
						Int32Vector magicvector = Int32Vector.Splat(magicnumber);

						Int32Vector[] arr = new Int32Vector[elementcount];
						for (int i = 0; i < arr.Length; i++)
							arr[i] = zerrovector;

						Mfc.Get(arr, input);

						Int32Vector result = zerrovector;
						for (int i = 0; i < arr.Length; i++)
							result += SpuMath.CompareEqualsAndSelect(arr[i], magicvector, unitvector, zerrovector);

						return result.E1 + result.E2 + result.E3 + result.E4;
					};

			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(elementcount*4))
			{
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
					mem.ArraySegment.Array[i] = magicnumber;

				if (!SpeContext.HasSpeHardware)
					return;


				object rv = SpeContext.UnitTestRunProgram(del, mem.GetArea());
				AreEqual(elementcount*4, (int) rv);
			}
		}

		[Test]
		public void TestDma_PutLargeFloatVectorArray()
		{
			const int elementcount = 4*1024;
			const float magicnumber = 1234.5f;

			Converter<MainStorageArea, int> del =
				delegate(MainStorageArea input)
					{
						Int32Vector zerrovector = Int32Vector.Splat(0);
						Int32Vector unitvector = Int32Vector.Splat(1);

						Float32Vector zerrofloatvector = Float32Vector.Splat(0f);
						Float32Vector magicvector = Float32Vector.Splat(magicnumber);

						Float32Vector[] arr = new Float32Vector[elementcount];
						for (int i = 0; i < arr.Length; i++)
							arr[i] = zerrofloatvector;

						Mfc.Get(arr, input);

						Int32Vector result = zerrovector;
						for (int i = 0; i < arr.Length; i++)
						{
							result += SpuMath.CompareGreaterThanAndSelect(arr[i], magicvector, zerrovector, unitvector);
							result += SpuMath.CompareGreaterThanAndSelect(magicvector, arr[i], zerrovector, unitvector);
						}

						return result.E1 + result.E2 + result.E3 + result.E4;
					};

			using (AlignedMemory<float> mem = SpeContext.AllocateAlignedFloat(elementcount*4))
			{
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
					mem.ArraySegment.Array[i] = magicnumber;

				if (!SpeContext.HasSpeHardware)
					return;

				object rv = SpeContext.UnitTestRunProgram(del, mem.GetArea());
				AreEqual(elementcount*4*2, (int) rv);
			}
		}

		[Test]
		public void TestDma_PutLargeFloatArray()
		{
			const int elementcount = 16*1024;
			const float magicnumber = 1234.5f;

			Converter<MainStorageArea, int> del =
				delegate(MainStorageArea input)
					{
						float[] arr = new float[elementcount];

						for (int i = 0; i < arr.Length; i++)
							arr[i] = 0;

						Mfc.Get(arr, input);

						int result = 0;

						for (int i = 0; i < arr.Length; i++)
							if (arr[i] == magicnumber)
								result++;

						return result;
					};

			using (AlignedMemory<float> mem = SpeContext.AllocateAlignedFloat(elementcount))
			{
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
					mem.ArraySegment.Array[i] = magicnumber;

				if (!SpeContext.HasSpeHardware)
					return;

				object rv = SpeContext.UnitTestRunProgram(del, mem.GetArea());
				AreEqual(elementcount, (int) rv);
			}
		}



	}
}

#endif