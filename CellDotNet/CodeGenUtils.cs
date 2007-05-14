using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Mono.Cecil.Cil;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture, Ignore("Only for generating code.")]
	public class CodeGenUtils
	{
		[Test]
		public void GenerateFlowNextOpCodeSwitchCases()
		{
			TextWriter sw = Console.Out;
			FieldInfo[] fields = typeof (OpCodes).GetFields();

			foreach (FieldInfo fi in fields)
			{
				if (fi.FieldType != typeof(OpCode))
					continue;

				OpCode oc = (OpCode) fi.GetValue(null);
				if (oc.FlowControl != FlowControl.Next)
					continue;

				if (oc.OpCodeType == OpCodeType.Macro)
					continue;

				sw.Write("\t\t\tcase Code.{0}: // {1}\r\n", oc.Code, oc.Name);
			}
		}
	}
}
