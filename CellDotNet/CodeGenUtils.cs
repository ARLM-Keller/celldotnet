using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture, Ignore("Only for generating code.")]
	public class CodeGenUtils
	{
		/// <summary>
		/// Returns all IL opcodes.
		/// </summary>
		/// <returns></returns>
		private List<IROpCode> GetILOpcodes()
		{
			List<IROpCode> list=  new List<IROpCode>();
			FieldInfo[] fields = typeof(IROpCodes).GetFields();

			foreach (FieldInfo fi in fields)
			{
				if (fi.FieldType != typeof(IROpCode))
					continue;

				IROpCode oc = (IROpCode)fi.GetValue(null);
				list.Add(oc);
			}

			return list;
		}

		[Test]
		public void GenerateILFlowNextOpCodeSwitchCases()
		{
			TextWriter sw = Console.Out;

			foreach (IROpCode oc in GetILOpcodes())
			{
				if (oc.FlowControl != FlowControl.Next)
					continue;

//				if (oc.OpCodeType == OpCodeType.Macro)
//					continue;

				sw.WriteLine("\t\t\tcase IRCode.{0}: // {1}", oc.IRCode, oc.Name);
			}
		}

		/// <summary>
		/// Generates enumeration values 
		/// </summary>
		[Test]
		public void GenerateILEnumValues()
		{
			StringWriter sw = new StringWriter();

			foreach (IROpCode oc in GetILOpcodes())
			{
//				if (oc.OpCodeType == OpCodeType.Macro)
//					sw.WriteLine("\t\t// {0} = {1}, // {2}", oc.Code, (int) oc.Code, oc.OpCodeType);
//				else
					sw.WriteLine("\t\t{0} = {1},", oc.IRCode, (int) oc.IRCode);
			}

			Console.Write(sw.GetStringBuilder().ToString());
		}


		/// <summary>
		/// Returns the SPU opcodes that are defined and checks that their field names are the same as the name that is given to the constructor.
		/// </summary>
		/// <returns></returns>
		private static List<SpuOpCode> GetSpuOpCodes()
		{
			FieldInfo[] fields = typeof (SpuOpCode).GetFields(BindingFlags.Static | BindingFlags.Public);

			List<SpuOpCode> opcodes = new List<SpuOpCode>();

			foreach (FieldInfo field in fields)
			{
				if (field.FieldType != typeof(SpuOpCode))
					continue;

				SpuOpCode oc = (SpuOpCode) field.GetValue(null);

				if (oc.Name != field.Name)
					throw new Exception(string.Format("Name of opcode field {0} is not the same as the opcode name ({1}).", field.Name, oc.Name));

				opcodes.Add(oc);
			}

			return opcodes;
		}

//		[Test]
//		public void RhDump()
//		{
//			foreach (SpuOpCode code in GetSpuOpCodes())
//			{
//				Console.WriteLine(code.Name + ": reg: " + code.RegisterUsage + "; nowrite: " + code.NoRegisterWrite);
//			}
//		}

		/// <summary>
		/// Returns the qualified name of the static field that contains the field. 
		/// Used for generating the instruction writer methods.
		/// </summary>
		static string GetQualifiedOpcodeFieldName(SpuOpCode opcode)
		{
			return typeof (SpuOpCode).Name + "." + opcode.Name;
		}

		[Test]
		public void GenerateSpuInstructionWriterMethods()
		{
			StringWriter tw = new StringWriter();

			tw.Write(@"
	// THIS CLASS IS GENERATED BY {0}.{1} - DO NO EDIT. 
	partial class {2}
	{{
", GetType().FullName, "GenerateSpuInstructionWriterMethods()", typeof(SpuInstructionWriter).Name);

			List<SpuOpCode> list = GetSpuOpCodes();
			foreach (SpuOpCode opcode in list)
			{
				if (opcode.Format == SpuInstructionFormat.Custom)
					continue;

				// capitalized name.
				string ocname = opcode.Name[0].ToString().ToUpper() + opcode.Name.Substring(1);

				List<string> regnames = new List<string>();
				if ((opcode.RegisterUsage & SpuOpCodeRegisterUsage.Rt) != SpuOpCodeRegisterUsage.None)
					regnames.Add("rt");
				if ((opcode.RegisterUsage & SpuOpCodeRegisterUsage.Ra) != SpuOpCodeRegisterUsage.None)
					regnames.Add("ra");
				if ((opcode.RegisterUsage & SpuOpCodeRegisterUsage.Rb) != SpuOpCodeRegisterUsage.None)
					regnames.Add("rb");
				if ((opcode.RegisterUsage & SpuOpCodeRegisterUsage.Rc) != SpuOpCodeRegisterUsage.None)
					regnames.Add("rc");


				StringBuilder declnewdest = new StringBuilder();
				StringBuilder declolddest = new StringBuilder();
				StringBuilder bodynewdest = new StringBuilder();
				StringBuilder bodyolddest = new StringBuilder();


				// Declaration.
				foreach (string name in regnames)
				{
					declolddest.Append((declolddest.Length != 0 ? ", " : "") + "VirtualRegister " + name);
					if (name != "rt" || opcode.NoRegisterWrite)
						declnewdest.Append((declnewdest.Length != 0 ? ", " : "") + "VirtualRegister " + name);
				}

				if (opcode.HasImmediate)
				{
					declnewdest.Append((declnewdest.Length != 0 ? ", " : "") + "int immediate");
					declolddest.Append((declolddest.Length != 0 ? ", " : "") + "int immediate");
				}

				// Body.
				if ((opcode.RegisterUsage & SpuOpCodeRegisterUsage.Rt) != SpuOpCodeRegisterUsage.None)
				{
					if (opcode.NoRegisterWrite)
					{
						bodynewdest.AppendLine("AssertRegisterNotNull(rt, \"rt\");");
						bodynewdest.AppendLine("inst.Rt = rt;");
						bodyolddest.AppendLine("AssertRegisterNotNull(rt, \"rt\");");
						bodyolddest.AppendLine("inst.Rt = rt;");
					}
					else
					{
						bodynewdest.AppendLine("inst.Rt = NextRegister();");
						bodyolddest.AppendLine("AssertRegisterNotNull(rt, \"rt\");");
						bodyolddest.AppendLine("inst.Rt = rt;");
					}
				}
				if ((opcode.RegisterUsage & SpuOpCodeRegisterUsage.Ra) != SpuOpCodeRegisterUsage.None)
				{
					bodynewdest.AppendLine("inst.Ra = ra;");
					bodynewdest.AppendLine("AssertRegisterNotNull(ra, \"ra\");");
					bodyolddest.AppendLine("inst.Ra = ra;");
					bodyolddest.AppendLine("AssertRegisterNotNull(ra, \"ra\");");
				}
				if ((opcode.RegisterUsage & SpuOpCodeRegisterUsage.Rb) != SpuOpCodeRegisterUsage.None)
				{
					bodynewdest.AppendLine("inst.Rb = rb;");
					bodynewdest.AppendLine("AssertRegisterNotNull(rb, \"rb\");");
					bodyolddest.AppendLine("inst.Rb = rb;");
					bodyolddest.AppendLine("AssertRegisterNotNull(rb, \"rb\");");
				}
				if ((opcode.RegisterUsage & SpuOpCodeRegisterUsage.Rc) != SpuOpCodeRegisterUsage.None)
				{
					bodynewdest.AppendLine("inst.Rc = rc;");
					bodynewdest.AppendLine("AssertRegisterNotNull(rc, \"rc\");");
					bodyolddest.AppendLine("inst.Rc = rc;");
					bodyolddest.AppendLine("AssertRegisterNotNull(rc, \"rc\");");
				}
				if (opcode.HasImmediate)
				{
					bodynewdest.AppendLine("inst.Constant = immediate;");
					bodyolddest.AppendLine("inst.Constant = immediate;");
				}
				bodynewdest.AppendLine("AddInstruction(inst);");
				bodyolddest.AppendLine("AddInstruction(inst);");
				bodynewdest.AppendLine("return inst.Rt;");

				// Put it together.

				string methodformat = @"
		/// <summary>
		/// {0}
		/// </summary>
		public {3} Write{1}({2})
		{{
			SpuInstruction inst = new SpuInstruction({4});
			{5}
		}}
";
				// GetQualifiedOpcodeFieldName(opcode)
				tw.Write(methodformat, opcode.Title, ocname, declnewdest, "VirtualRegister", GetQualifiedOpcodeFieldName(opcode), bodynewdest);
				if (declolddest.Length != declnewdest.Length)
					tw.Write(methodformat, opcode.Title, ocname, declolddest, "void", GetQualifiedOpcodeFieldName(opcode), bodyolddest);
			}

			tw.Write(@"
	}
");

			Console.Write(tw.GetStringBuilder().ToString());
		}

		[Test, Explicit]
		public void GenerateSpuOpCodeEnum()
		{
			StringWriter sw = new StringWriter();

			sw.Write(@"
	// This enumeration is generated by {0}. DO NOT EDIT.
	enum SpuOpCodeEnum
	{{
		None,", GetType().FullName);

			foreach (SpuOpCode code in GetSpuOpCodes())
			{
				sw.Write(@"
		/// <summary>
		/// {1}
		/// </summary>
		{0},", CultureInfo.InvariantCulture.TextInfo.ToTitleCase(code.Name), code.Title);
			}
			sw.Write(@"
	}
");
			Console.Write(sw.GetStringBuilder());
		}

		[Test, Explicit]
		public void GenerateIROpcodes()
		{
			StringBuilder enumcode = new StringBuilder();
			enumcode.AppendFormat(@"
		// These IR opcode enum values are generated by {0}. DO NOT EDIT.
", GetType().FullName);

			StringBuilder opcodewriter = new StringBuilder();
			opcodewriter.AppendFormat(@"
	// This class is generated by {0}. DO NOT EDIT.
	partial class IROpCodes
	{{
", GetType().FullName);

			FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
			foreach (FieldInfo fi in fields)
			{
				OpCode oc = (OpCode)fi.GetValue(null);

				if (oc.OpCodeType == OpCodeType.Macro)
				{
					//					continue;
					// We generally don't want macros, but these are okay...
					if (oc != OpCodes.Blt && oc != OpCodes.Ble &&
						oc != OpCodes.Blt_Un && oc != OpCodes.Ble_Un &&
						oc != OpCodes.Beq && oc != OpCodes.Bne_Un &&
						oc != OpCodes.Bge && oc != OpCodes.Bgt &&
						oc != OpCodes.Bge_Un && oc != OpCodes.Bgt_Un)
					{
						Console.WriteLine("Skipping opcode: {0}.", oc.Name);
						continue;
					}
				}

				enumcode.AppendFormat("		{0},\r\n", fi.Name);
				opcodewriter.AppendFormat(
					"		public static readonly IROpCode {0} = new IROpCode(\"{1}\", IRCode.{0}, " +
					"FlowControl.{2}, OpCodes.{3});\r\n",
					fi.Name, oc.Name, oc.FlowControl, fi.Name);
			}


			enumcode.Append(@"
		// End of generated opcodes.
");
			opcodewriter.Append(@"
	}
");

			Console.Write(enumcode.ToString());
			Console.Write(opcodewriter.ToString());
		}
	}
}
