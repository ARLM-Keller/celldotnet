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
using System.Collections.Generic;
using NUnit.Framework;



namespace CellDotNet
{
	[TestFixture]
	public class ShellUtilitiesTest : UnitTest
	{
		[Test, Ignore("Isn't reliable...")]
		public void TestExecuteCommand()
		{
			string output = ShellUtilities.ExecuteCommandAndGetOutput("hostname", null);
			AreNotEqual(null, output);
			AreNotEqual("", output);
		}

		[Test, Ignore("Isn't reliable...")]
		public void TestExecuteShellScript()
		{
			if (!HasUnixShell)
				return;

			string script = @"
echo -e hey\\nhey2
";
			string output = ShellUtilities.ExecuteShellScript(script);
			AreEqual("hey\nhey2\n\n", output);
		}
	}
}


#endif