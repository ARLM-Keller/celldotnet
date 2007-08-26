using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class ObjectModelTest : UnitTest
	{
		private delegate int IntReturnDelegate();
		private delegate void SimpleDelegate();

		[Test]
		public void TestArray_Create()
		{
			SimpleDelegate del =
				delegate
				{
					int[] arr = new int[5];
					Utilities.PretendVariableIsUsed(arr);
				};

			SimpleDelegate del2 = SpeDelegateRunner<SimpleDelegate>.CreateSpeDelegate(del);
			if (!SpeContext.HasSpeHardware)
				return;

			del2();
		}

		[Test]
		public void TestArray_Length()
		{
			IntReturnDelegate del = 
				delegate
					{
						int[] arr = new int[5];
						return arr.Length;
					};

			IntReturnDelegate del2 = SpeDelegateRunner<IntReturnDelegate>.CreateSpeDelegate(del);
			if (!SpeContext.HasSpeHardware)
				return;

			int val = del2();
			AreEqual(5, val);
		}

		[Test]
		public void TestArray_IntElements()
		{
			const int MagicNumber = 0xbababa;
			IntReturnDelegate del = 
				delegate
					{
						int[] arr = new int[10];
						arr[0] = MagicNumber;
						arr[1] = 20;
						return arr[0];
					};

			IntReturnDelegate del2 = SpeDelegateRunner<IntReturnDelegate>.CreateSpeDelegate(del);

			if (!SpeContext.HasSpeHardware)
				return;

			int retval = del2();
			AreEqual(MagicNumber, retval);
		}
	}
}
