using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class ElfLibraryTest : UnitTest
	{
		[Test]
		public void TestParseElfSections()
		{
			string input =
				@".text 00000008 00000000 00000038
.data 00000000 00000000 00000040
.bss 00000000 00000000 00000040
.comment 00000012 00000003 00000040
";
			List<ElfLibrary.ElfSectionInfo> sections = ElfLibrary.ParseElfSectionInfo(input);

			AreEqual(4, sections.Count);

			AreEqual(".text", sections[0].Name);
			AreEqual(0x8, sections[0].Size);
			AreEqual(0, sections[0].VirtualAddress);
			AreEqual(0x38, sections[0].FileOffset);

			AreEqual(".comment", sections[3].Name);
			AreEqual(0x12, sections[3].Size);
			AreEqual(0x3, sections[3].VirtualAddress);
			AreEqual(0x40, sections[3].FileOffset);
		}

		[Test]
		public void TestParseElfSymbolsInfo()
		{
			string input =
				@"000000f0 T TestRoutine1
000000e0 T TestRoutine2
";
			List<ElfLibrary.ElfSymbolInfo> sections = ElfLibrary.ParseElfSymbolsInfo(input);

			AreEqual(2, sections.Count);

			AreEqual("TestRoutine1", sections[0].Name);
			AreEqual(0xf0, sections[0].VirtualAddress);

			AreEqual("TestRoutine2", sections[1].Name);
			AreEqual(0xe0, sections[1].VirtualAddress);
		}

	}
}
