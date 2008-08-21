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
	}
}
#endif // UNITTEST