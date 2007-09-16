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

		[Test, Ignore()]
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
						Mfc.Get(arr, input, 4, tag);
						Mfc.WaitForDmaCompletion(tag);

						int sum = 0;
						for (int i = 0; i < 4; i++)
							sum += arr[i];

						return sum;
					};

				CompileContext cc = new CompileContext(del.Method);
				cc.PerformProcessing(CompileContextState.S8Complete);
//				Disassembler.DisassembleToConsole(cc);
//				cc.WriteAssemblyToFile("dma.s", mem.GetArea());

				object rv = SpeContext.UnitTestRunProgram(cc, mem.GetArea());
				int correctVal = del(mem.GetArea());
				AreEqual(20, correctVal);
				AreEqual(20, (int)rv);
			}
		}

		[Test, Ignore()]
		public void TestDma_GetIntArray_DEBUG()
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
						Mfc.Get(arr, input, 4, tag);
						Mfc.WaitForDmaCompletion(tag);

						int sum = 0;
						for (int i = 0; i < 4; i++)
							sum += arr[i];

						return sum;
					};

				CompileContext cc = new CompileContext(del.Method);

				cc.PerformProcessing(CompileContextState.S3InstructionSelectionDone);

//				Disassembler.DisassembleUnconditional(cc, Console.Out);
				
				cc.PerformProcessing(CompileContextState.S8Complete);

//				Disassembler.DisassembleToConsole(cc);

				object rv = SpeContext.UnitTestRunProgram(cc, mem.GetArea());
				int correctVal = del(mem.GetArea());
				AreEqual(20, correctVal);
				AreEqual(20, (int)rv);
			}
		}

		[Test, Ignore()]
		public void TestDma_PutIntArray()
		{
			using (AlignedMemory<int> mem = SpeContext.AllocateAlignedInt32(4))
			{
				Action<MainStorageArea> del =
					delegate(MainStorageArea input)
					{
						int[] arr = new int[4];
						for (int i = 0; i < 4; i++)
							arr[i] = 5;

						uint tag = 1;
						Mfc.Put(arr, input, 4, tag);
						Mfc.WaitForDmaCompletion(tag);
					};

				CompileContext cc = new CompileContext(del.Method);
				cc.PerformProcessing(CompileContextState.S8Complete);
//				Disassembler.DisassembleToConsole(cc);
				cc.WriteAssemblyToFile("dma.s", mem.GetArea());

				// Run locally.
				del(mem.GetArea());
				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
				{
					AreEqual(5, mem.ArraySegment.Array[i]);
					mem.ArraySegment.Array[i] = 0;
				}

				// Run on spu.
				object rv = SpeContext.UnitTestRunProgram(cc, mem.GetArea());
				IsNull(rv);

				for (int i = mem.ArraySegment.Offset; i < mem.ArraySegment.Offset + mem.ArraySegment.Count; i++)
					AreEqual(5, mem.ArraySegment.Array[i]);
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
		internal uint EffectiveAddress
		{
			[IntrinsicMethod(SpuIntrinsicMethod.MainStorageArea_get_EffectiveAddress)]
			get { return _effectiveAddress; }
		}

		internal MainStorageArea(IntPtr effectiveAddress)
		{
			_effectiveAddress = (uint) effectiveAddress;
		}
	}
}
