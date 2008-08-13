using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class DelegateWrapperTest : UnitTest
	{
		[Test]
		public void TestDelegateWrapper_ActionBasic()
		{
			Action<int> target = i => Fail();
			bool gotcalled = false;
			object[] args = null;
			Action<object[]> interceptor = arr =>
			                               	{
			                               		gotcalled = true;
			                               		args = arr;
			                               	};

			Action<int> wrapper = DelegateWrapper.CreateWrapper(target, interceptor);
			wrapper(100);

			IsTrue(gotcalled, "gotcalled");
			AreEqual(1, args.Length);
			AreEqual(100, (int) args[0]);
		}

		[Test]
		public void TestDelegateWrapper_ActionReferenceType()
		{
			Action<int, string> target = (i, s) => Fail();
			bool gotcalled = false;
			object[] args = null;
			Action<object[]> interceptor = arr =>
			                               	{
			                               		gotcalled = true;
			                               		args = arr;
			                               	};

			Action<int, string> wrapper = DelegateWrapper.CreateWrapper(target, interceptor);
			wrapper(100, "xx");

			IsTrue(gotcalled, "gotcalled");
			AreEqual(2, args.Length);
			AreEqual(100, (int) args[0]);
			AreEqual("xx", (string) args[0]);
		}
	}
}
