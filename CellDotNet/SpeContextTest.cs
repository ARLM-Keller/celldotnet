using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace CellDotNet
{
	class SpeContextTest
	{
		[Test]
		public void SpeSimpleDMATest()
		{
			SpeContext ctxt = new SpeContext();
			ctxt.LoadProgram(new int[] { 13 });

			int[] lsa = ctxt.GetCopyOffLocalStorage();

			if (lsa[0] != 13)
				Assert.Fail("DMA error.");
		}
	}
}
