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
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using CellDotNet.Intermediate;
using CellDotNet.Spe;

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

		private static unsafe void RunRasmus()
		{
			new ILOpCodeExecutionTest().Test_Ldc_R8();

//			double d = 4324534.523226;
//			long l = (long) *((double*) &d);
//			Console.WriteLine("double hex: " +  hexencode(BitConverter.GetBytes(d)));
//			Console.WriteLine("long reinterpreted hex: " +  hexencode(BitConverter.GetBytes(l)));

//			new ILOpCodeExecutionTest().Test_Rem_Un_I4();
//			Func<double, double, double> f = (d1, d2) => d1 % d2;
//			Console.WriteLine(f(3,5));
		}
	}
}
