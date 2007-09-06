using System;
using System.Collections.Generic;
using System.Security.Permissions;
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
				SecurityPermission perm = new SecurityPermission(SecurityPermissionFlag.AllFlags);
				perm.Assert();
				perm.Demand();

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
//						Mfc.Get(arr, input, 4);
						Mfc.Get(arr, input, 4, tag);
						Mfc.WaitForCompletion(tag);

						int sum = 0;
						for (int i = 0; i < 4; i++)
							sum += arr[i];

						return sum;
					};

				CompileContext cc = new CompileContext(del.Method);
				cc.PerformProcessing(CompileContextState.S8Complete);

				using (SpeContext sc = new SpeContext())
				{
					object rv = sc.RunProgram(cc, mem.GetArea());

					AreEqual(20, (int) rv);
				}
			}

		}
	}

	/// <summary>
	/// Represent an area in main storage that can be used for dma transfers.
	/// </summary>
	public struct MainStorageArea
	{
		private int _effectiveAddress;
		public int EffectiveAddress
		{
			get { return _effectiveAddress; }
		}

		internal MainStorageArea(IntPtr effectiveAddress)
		{
			_effectiveAddress = (int) effectiveAddress;
		}
	}
}
