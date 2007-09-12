using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class ShellUtilitiesTest : UnitTest
	{
		[Test]
		public void TestExecuteCommand()
		{
			string output = ShellUtilities.ExecuteCommandAndGetOutput("hostname", null);
			AreNotEqual(null, output);
			AreNotEqual("", output);
		}

		[Test]
		public void TestExecuteShellScript()
		{
			if (!HasUnixShell)
				return;

			string script = @"
echo hej
";
			string output = ShellUtilities.ExecuteShellScript(script);
			AreEqual("hej\n", output);
		}
	}
}
