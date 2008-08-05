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

using System;
using System.Collections.Generic;
using System.Text;
using CellDotNet.Spe;
using System.Linq;

namespace CellDotNet
{
	internal class Class1
	{
		static public void Main(string[] args)
		{
			RunRasmus();
		}

		public static string hexencode(byte[] arr)
		{
			char[] hexchars = new[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'};
			StringBuilder sb = new StringBuilder(arr.Length*2);
			foreach (var b in arr)
			{
				sb.Append(hexchars[(b & 0xf) >> 4]);
				sb.Append(hexchars[b >> 4]);
			}
			return sb.ToString();
		}

		private static void RunRasmus()
		{
			new SpeContextTest().SpeSimpleDMATest();
			return;

			CodeGenUtils.GeneratePatchCode(@"\\10.0.3.13\linux\codetests\dp.s", @"C:\Temp\pstext\out.cs", new HashSet<string>(StringComparer.Ordinal)
                         	{
                         		"cosd2", "sind2", "tand2",
                         		"acosd2", "asind2", "atand2", "atan2d2",
                         		"cosf4", "sinf4", "tanf4",
                         		"acosf4", "asinf4", "atanf4", "atan2f4",
                         		"divd2", "sqrtf4", "sqrtd2", "logf4", "logd2"
                         	});
		}
		

	}
}
