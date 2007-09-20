using System;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class MfcTest : UnitTest
	{
		private delegate int IntDelegate();

		[Test]
		public void TestGetQueueDepth()
		{
			IntDelegate del = delegate { return Mfc.GetAvailableQueueEntries(); };
			IntDelegate del2 = SpeDelegateRunner.CreateSpeDelegate(del);

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
				// Create elements whose sum is twenty.
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
				{
					mem.ArraySegment.Array[i] = 5;
				}

				Converter<MainStorageArea, int> del =
					delegate(MainStorageArea input)
					{
						int[] arr = new int[4];

						uint tag = 1;
						uint tagmask = (uint)1<<31;
						Mfc.Get(arr, input, 4, tag);
						Mfc.WaitForDmaCompletion(tagmask);

						int sum = 0;
						for (int i = 0; i < 4; i++)
							sum += arr[i];

						return sum;
					};

				CompileContext cc = new CompileContext(del.Method);
				cc.PerformProcessing(CompileContextState.S8Complete);

//				Disassembler.DisassembleToConsole(cc);
				cc.WriteAssemblyToFile("TestDma_GetIntArray_asm.s", mem.GetArea());

				object rv = SpeContext.UnitTestRunProgram(cc, mem.GetArea());
//				int correctVal = del(mem.GetArea());
//				AreEqual(20, correctVal);
				AreEqual(20, (int)rv);
			}
		}

		[Test, Ignore()]
		public unsafe void TestDma_GetIntArray_DEBUG()
		{
			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(4))
			{
				// Create elements whose sum is twenty.
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
				{
					mem.ArraySegment.Array[i] = 10;
				}

				Converter<MainStorageArea, int> del =
					delegate(MainStorageArea input)
					{
						int[] arr = new int[4];

						Mfc.Get_DEBUG(arr, input, 4, 1);
						Mfc.WaitForDmaCompletion(0xffffffff);

						int sum = 0;
						for (int i = 0; i < 4; i++)
							sum += arr[i];

						return sum;
					};

				CompileContext cc = new CompileContext(del.Method);

				cc.PerformProcessing(CompileContextState.S3InstructionSelectionDone);

				Disassembler.DisassembleUnconditional(cc, Console.Out);
				
				cc.PerformProcessing(CompileContextState.S8Complete);

				Disassembler.DisassembleToConsole(cc);

				cc.WriteAssemblyToFile("TestDma_GetIntArray_DEBUG_asm.s", mem.GetArea());

				if(!SpeContext.HasSpeHardware)
					return;

				object rv = new SpeContext().RunProgram(cc, mem.GetArea());

				Console.WriteLine("Result: {0:x}", MainStorageArea.GetEffectiveAddress(mem.GetArea()));

				Console.WriteLine("Result: {0:x}", (int)rv);

//				int correctVal = del(mem.GetArea());
//				AreEqual(20, correctVal);
//				AreEqual(20, (int)rv);
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
						uint tagMask = (uint)1<<31;
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

				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
					Console.WriteLine("mem.ArraySegment.Array[{0}] = {1}", i, mem.ArraySegment.Array[i]);

				IsNull(rv);

				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
					AreEqual(i-mem.ArraySegment.Offset, mem.ArraySegment.Array[i]);
			}
		}
	}

	/// <summary>
	/// Represent an area in main storage that can be used for dma transfers.
	/// </summary>
	/// <remarks>
	/// This struct will be laid out so that the effective address will be in the preferred slot.
	/// </remarks>
	public struct MainStorageArea
	{
		private uint _effectiveAddress;

		internal MainStorageArea(IntPtr effectiveAddress)
		{
			_effectiveAddress = (uint) effectiveAddress;
		}

		[IntrinsicMethod(SpuIntrinsicMethod.MainStorageArea_get_EffectiveAddress)]
		public static uint GetEffectiveAddress(MainStorageArea ma)
		{
			return ma._effectiveAddress;
		}

	}
}
