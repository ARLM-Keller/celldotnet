using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class DisassemblerTest
	{
		private delegate int SimpleDelegate();

		[Test]
		public void TestSimpleDisassembly()
		{
			SimpleDelegate del = delegate
			                     	{
			                     		int i = 34;
			                     		return i;
			                     	};

			CompileContext cc = new CompileContext(del.Method);
			cc.PerformProcessing(CompileContextState.S6AddressPatchingDone);

//			Disassembler.DisassembleToConsole(cc);
		}
	}
}