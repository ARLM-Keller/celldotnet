using System;
using CellDotNet;

namespace SciMarkCell
{
	class Class1
	{
		static public void Main(string[] args)
		{
//			Converter<int, uint> fun =
//				delegate(int input)
//					{
//						return 21;
////						return CellDotNet.SpuMath.Div_Un_DEBUG(14, 7);
////						return CellDotNet.SpuMath.Div_Un(14, 7);
//
//////						int result = 1;
////
//////						for (int i = 0; i < 32; i++)
//////						{
//////							bool q = input == 17;
//////							result = ((result << 1) | (q ? 1 : 0));
//////						}
////							return input + (input == 17 ? 0 : input);
//////						return result;
//					};
//
//			CompileContext cc = new CompileContext(fun.Method);
//
//			cc.PerformProcessing(CompileContextState.S8Complete);
//
//			object result = SpeContext.UnitTestRunProgram(cc, 17);

			new MonteCarloTest().TestMonteCarloSingle();
		}

	}
}
