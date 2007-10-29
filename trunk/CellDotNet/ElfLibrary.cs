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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CellDotNet.Spe
{
	class ElfLibrary : Library
	{
		private string _fullpath;
		private List<ElfSectionInfo> _sections;
		private List<ElfSymbolInfo> _symbols;
		private int _size;

		public ElfLibrary(string fullpath)
		{
			if (!File.Exists(fullpath))
				throw new ArgumentException("File " + fullpath + " does not exist.");

			_size = (int) new FileInfo(fullpath).Length;
			_fullpath = fullpath;
			_sections = GetElfSections(fullpath);
			_symbols = GetElfSymbols(fullpath);
		}

		public override LibraryMethod ResolveMethod(MethodInfo dllImportMethod)
		{
			Utilities.AssertArgumentNotNull(dllImportMethod, "dllImportMethod");
			DllImportAttribute att = (DllImportAttribute) dllImportMethod.GetCustomAttributes(typeof (DllImportAttribute), false)[0];

			// Find the symbol.
			string symbolname;
			if (!string.IsNullOrEmpty(att.EntryPoint))
				symbolname = att.EntryPoint;
			else
				symbolname = dllImportMethod.Name;

			ElfSymbolInfo elfsymbol = _symbols.Find(delegate(ElfSymbolInfo obj) { return obj.Name == symbolname; });
			if (elfsymbol == null)
				throw new DllNotFoundException("The symbol '" + symbolname + "' could not be found in the file '" + _fullpath + "'.");


			// Find the file offset based on the virtual (load) address and the section load info.
			ElfSectionInfo section = _sections.Find(delegate(ElfSectionInfo sec)
			                                        	{
			                                        		return elfsymbol.VirtualAddress >= sec.VirtualAddress &&
			                                        		       elfsymbol.VirtualAddress <= (sec.VirtualAddress + sec.Size);
			                                        	});
			if (section == null)
				throw new DllNotFoundException(
					string.Format("Could not find ELF section for virtual address 0x{0:x}.", elfsymbol.VirtualAddress));

			int fileoffset = section.FileOffset + (elfsymbol.VirtualAddress - section.VirtualAddress);

			Trace.WriteLine(string.Format("Resolved ELF symbol '{0}' to file offset 0x{1:x} and virtual address 0x{2:x}.", 
				symbolname, fileoffset, elfsymbol.VirtualAddress));

			return new LibraryMethod(symbolname, this, fileoffset, dllImportMethod);
		}

		public override byte[] GetContents()
		{
			return File.ReadAllBytes(_fullpath);
		}



		public override int Size
		{
			get { return _size; }
		}

		private List<ElfSymbolInfo> GetElfSymbols(string fullpath)
		{
			string shellScript = string.Format(@"
spu-nm {0} | awk '/^[0-9]+/ {{ print $1, $2, $3 }}'
", fullpath);
			string output = ShellUtilities.ExecuteShellScript(shellScript);

			return ParseElfSymbolsInfo(output);
		}

		private List<ElfSectionInfo> GetElfSections(string fullpath)
		{
			string shellScript = string.Format(@"
spu-objdump -h {0} | awk '/^ +[0-9]/ {{ print $2,$3,$4,$6}}'
", fullpath);
			string output = ShellUtilities.ExecuteShellScript(shellScript);

			return ParseElfSectionInfo(output);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// <para>Sample input:</para>
		/// <pre>
		/// .text 00000008 00000000 00000038<br />
		/// .data 00000000 00000000 00000040<br />
		/// .bss 00000000 00000000 00000040<br />
		/// .comment 00000012 00000000 00000040<br />
		/// </pre>
		/// <list type="">
		/// <item>Col1: Section name</item>
		/// <item>Col1: Section size</item>
		/// <item>Col1: Section virtual address</item>
		/// <item>Col1: Section file offset</item>
		/// </list>
		/// </remarks>
		/// <param name="objDumpOutput"></param>
		/// <returns></returns>
		public static List<ElfSectionInfo> ParseElfSectionInfo(string objDumpOutput)
		{
			string[] lines = objDumpOutput.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			List<ElfSectionInfo> list = new List<ElfSectionInfo>(lines.Length);

			foreach (string line in lines)
			{
				string[] arr = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				Utilities.Assert(arr.Length == 4, "Not four elements.");

				string name = arr[0];
				int size = Convert.ToInt32(arr[1], 16);
				int virtualAddress = Convert.ToInt32(arr[2], 16);
				int fileOffset = Convert.ToInt32(arr[3], 16);

				list.Add(new ElfSectionInfo(name, fileOffset, virtualAddress, size));
			}

			return list;
		}

		/// <summary>
		/// Parses the output of nm.
		/// </summary>
		/// <remarks>
		/// Sample record:
		/// <pre>
		/// 00000000 T TestRoutine1
		/// </pre>
		/// <list type="">
		///	<item>Col1: Hex virtual address.</item>
		///	<item>Col2: Symbol type. 'T' is what we're looking for.</item>
		///	<item>Col3: Symbol name.</item>
		/// </list>
		/// </remarks>
		/// <param name="nmDumpOutput"></param>
		/// <returns></returns>
		public static List<ElfSymbolInfo> ParseElfSymbolsInfo(string nmDumpOutput)
		{
			string[] lines = nmDumpOutput.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			List<ElfSymbolInfo> list = new List<ElfSymbolInfo>(lines.Length);

			foreach (string line in lines)
			{
				string[] arr = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (arr.Length == 2)
				{
					// Probably a weak symbol without value.
					if (arr[0] == "w")
						continue;
					else
						throw new FormatException("Bad output line.");
				}

				Utilities.Assert(arr.Length == 3, "Not three elements in line. nm output:\n" + nmDumpOutput);

				int virtualAddress = Convert.ToInt32(arr[0], 16);
				char symbolType = arr[1][0];
				string name = arr[2];

				if (symbolType != 'T')
					continue;

				list.Add(new ElfSymbolInfo(name, virtualAddress));
			}

			return list;
		}


		public class ElfSymbolInfo
		{
			private string _name;
			private int _virtualAddress;

			public ElfSymbolInfo(string name, int virtualAddress)
			{
				_name = name;
				_virtualAddress = virtualAddress;
			}

			public string Name
			{
				get { return _name; }
			}

			public int VirtualAddress
			{
				get { return _virtualAddress; }
			}
		}

		public class ElfSectionInfo
		{
			private string _name;
			private int _fileOffset;
			private int _virtualAddress;
			private int _size;


			public ElfSectionInfo(string name, int fileOffset, int virtualAddress, int size)
			{
				_name = name;
				_fileOffset = fileOffset;
				_virtualAddress = virtualAddress;
				_size = size;
			}

			public string Name
			{
				get { return _name; }
			}

			public int FileOffset
			{
				get { return _fileOffset; }
			}

			public int VirtualAddress
			{
				get { return _virtualAddress; }
			}

			public int Size
			{
				get { return _size; }
			}
		}

	}
}
