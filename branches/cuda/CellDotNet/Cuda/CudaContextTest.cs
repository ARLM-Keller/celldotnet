using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

#if UNITTEST
namespace CellDotNet.Cuda
{
	[TestFixture]
	public class CudaContextTest : UnitTest
	{
		[Test]
		public void TestAttach()
		{
			using (var c1 = CudaContext.GetCurrentOrNew())
			using (var c2 = CudaContext.GetCurrentOrNew())
			{
				AreEqual(c1.CudaHandle, c2.CudaHandle);
			}
		}

		[Test]
		public void TestDispose1()
		{
			using (var c1 = CudaContext.GetCurrentOrNew())
			using (var c2 = CudaContext.GetCurrentOrNew())
			{
				AreEqual(c1.CudaHandle, c2.CudaHandle);

				c1.Dispose();
				var mem = c2.AllocateLinear<int>(100);
				AreNotEqual(0, ((IGlobalMemory) mem).GetDeviceAddress());
			}
		}

		[Test]
		public void TestDispose2()
		{
			using (var c1 = CudaContext.GetCurrentOrNew())
			using (var c2 = CudaContext.GetCurrentOrNew())
			{
				AreEqual(c1.CudaHandle, c2.CudaHandle);

				c2.Dispose();
				var mem = c1.AllocateLinear<int>(100);
				AreNotEqual(0, ((IGlobalMemory)mem).GetDeviceAddress());
			}
		}

		[Test]
		public void TestDisposeDouble()
		{
			using (var c1 = CudaContext.GetCurrentOrNew())
			{
				c1.Dispose();
			}
		}

		[Test]
		public void TestAllocMemCopy()
		{
			using (var ctx = CudaContext.GetCurrentOrNew())
			{
				var memory = ctx.AllocateLinear<float>(200);
				AreEqual(200, memory.Length);

				var arr1 = new float[200];
				for (int i = 0; i < arr1.Length; i++)
					arr1[i] = i;
				var arr2 = new float[200];

				ctx.CopyHostToDevice(arr1, 0, memory, 0, 200);
				ctx.CopyDeviceToHost(memory, 0, arr2, 0, 200);

				AreEqual(arr1, arr2);
			}
		}

	}
}
#endif // UNITTEST