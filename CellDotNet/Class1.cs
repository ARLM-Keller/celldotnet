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
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
			GeneratePatchCode(@"\\10.0.3.13\linux\codetests\dp.s", @"C:\Temp\pstext\out.cs");
		}
		

		static void GeneratePatchCode(string fileWithDisassembly, string outputFile)
		{
			Dictionary<int, QuadWord> constants = new Dictionary<int, QuadWord>();

			var instRegex = new Regex(@"^\s+([0-9a-f]+):\s*(\w\w \w\w \w\w \w\w)");

			Console.WriteLine("Reading constants...");
			{
				uint i1 = 0, i2 = 0, i3 = 0;
				int qwStartAddress = 0;
				int linenum = 0;
				foreach (string line in File.ReadAllLines(fileWithDisassembly))
				{
					linenum++;
					var match = instRegex.Match(line);
					if (!match.Success)
						continue;

					int address = Convert.ToInt32(match.Groups[1].Value, 16);
					uint hex = Convert.ToUInt32(match.Groups[2].Value.Replace(" ", ""), 16);

					switch (address % 16)
					{
						case 0:
							i1 = hex;
							qwStartAddress = address;
							break;
						case 4:
							i2 = hex;
							break;
						case 8:
							i3 = hex;
							break;
						case 12:
							if (address == qwStartAddress + 12)
							{
								uint i4 = hex;
								QuadWord qw = new QuadWord(i1, i2, i3, i4);
								constants.Add(qwStartAddress, qw);
							}
							break;
						default:
							throw new Exception();
					}
					
				}
				
			}

			var desiredFunctions = new HashSet<string>(StringComparer.Ordinal)
			                       	{
			                       		"cosd2", "sind2", "tand2",
			                       		"acosd2", "asind2", "atand2", "atan2d2",
			                       		"cosf4", "sinf4", "tanf4",
			                       		"acosf4", "asinf4", "atanf4", "atan2f4",
										"divd2"
			                       	};

			Console.WriteLine("Reading disassembly...");
			{
				var headerRegex = new Regex(@"^([0-9a-f]{8}) <(\w+)>:");
				var lqrRegex = new Regex(@"^\s+([0-9a-f]+):.+\slqr\s\$(\d+).+#\s*([0-9a-f]+)");
				int lineno = 0;

				string currentfunctionname = null;
				int? currentfunctionaddress = null;
				File.Delete(outputFile);

				var sw = new StringWriter();
				List<int> currentfunctionbody = null;

				foreach (string line in File.ReadAllLines(fileWithDisassembly))
				{
					lineno++;

					if (line == "")
						continue;

					var h = headerRegex.Match(line);
					var inst = lqrRegex.Match(line);

					if (h.Success)
					{
						if (currentfunctionbody != null && !string.IsNullOrEmpty(currentfunctionname))
						{
							int[] arr = currentfunctionbody.ToArray();
							Utilities.HostToBigEndian(arr);
							byte[] codebytes = new byte[arr.Length * 4];
							Buffer.BlockCopy(arr, 0, codebytes, 0, codebytes.Length);
							File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(outputFile), currentfunctionname + ".bin"), codebytes);
						}

						currentfunctionname = h.Groups[2].Value;
						currentfunctionaddress = Convert.ToInt32(h.Groups[1].Value, 16);
						string outputline = string.Format(@"
					// {0} ", currentfunctionname);

						sw.WriteLine();
						sw.Write(outputline);

						if (desiredFunctions.Contains(currentfunctionname))
							currentfunctionbody = new List<int>();
						else
							currentfunctionbody = null;
					}
					else if (inst.Success)
					{
						if (string.IsNullOrEmpty(currentfunctionname) || currentfunctionaddress == null)
							throw new Exception("xxasdf");

						if (!desiredFunctions.Contains(currentfunctionname))
							continue;

						int instaddress = Convert.ToInt32(inst.Groups[1].Value, 16);
						int regnum = Convert.ToInt32(inst.Groups[2].Value);
						int constaddress = Convert.ToInt32(inst.Groups[3].Value, 16);
						QuadWord fs = constants[constaddress];

						int instoffset = instaddress - currentfunctionaddress.Value;
						string outputline = string.Format(@"
					{0}.Seek(0x{1:x});
					{0}.Writer.WriteLoad(HardwareRegister.GetHardwareRegister({2}), RegisterConstant({3})); // {4}", 
							currentfunctionname, instoffset, regnum, 
							"0x" + fs.I1.ToString("x") + ", 0x" + fs.I2.ToString("x") + ", 0x" + fs.I3.ToString("x") + ", 0x" + fs.I4.ToString("x"), 
							line);

						sw.WriteLine();
						sw.Write(outputline);
					}

					var match2 = instRegex.Match(line);
					if (match2.Success && currentfunctionbody != null)
					{
						uint hex = Convert.ToUInt32(match2.Groups[2].Value.Replace(" ", ""), 16);
						currentfunctionbody.Add((int) hex);
					}
				}

				File.WriteAllText(outputFile, sw.GetStringBuilder().ToString());
			}
		}
	}
}
