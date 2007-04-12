using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;

namespace CellDotNet
{
	internal class Class1
	{
		static public void Main(string[] args)
		{
			TestBuildTree();
			return;

			Action<int> a = MyMethod;
			MethodDefinition method;

			method = GetMethod(a);
			method.Body.Simplify();
			Console.WriteLine("Variables: " + method.Body.Variables.Count);
			foreach (VariableDefinition var in method.Body.Variables)
			{
				Console.WriteLine(var.Name + ": " + var.VariableType.Name);
			}
			Console.WriteLine("Max stack: " + method.Body.MaxStack);
			Console.WriteLine();
			

			foreach (Instruction inst in method.Body.Instructions)
			{
				Console.Write("Inst {0:x2}: {1}", inst.Offset, inst.OpCode.Code);
				if (inst.Operand != null)
				{
					if (inst.Operand.GetType() == typeof (VariableDefinition))
					{
						VariableDefinition var = (VariableDefinition) inst.Operand;
						Console.Write("; var type: {0} ({1})", var.VariableType.Name, var.Name);
					}
					else if (inst.OpCode.FlowControl == FlowControl.Branch || inst.OpCode.FlowControl == FlowControl.Cond_Branch)
					{
						if (inst.OpCode == OpCodes.Switch)
						{
							foreach (Instruction target in (System.Collections.IEnumerable) inst.Operand)
							{
								Console.Write("; target: {0:x2}", target.Offset);
							}
						}
						else
						{
							Instruction target = (Instruction) inst.Operand;
							Console.Write("; target: {0:x2}", target.Offset);
						}
					}
					else if (inst.OpCode.FlowControl == FlowControl.Call)
					{
						MethodReference cmet = (MethodReference) inst.Operand;
						Console.Write("; target: " + cmet);
					}
					else
					{
						Console.Write("; operand: {0} ({1})", inst.Operand, inst.Operand.GetType().Name);
					}
				}

				Console.WriteLine();
			}
		}

		static void DrawTree(MethodDefinition method, TreeInstruction inst, int level)
		{
			for (int i = 0; i < level; i++)
				Console.Write("  ");
			Console.Write(inst.Offset.ToString("x4") + " " + inst.Opcode.Name);

			if (inst.Operand != null)
			{
				if (inst.Operand is TreeInstruction)
					Console.Write(" " + ((TreeInstruction)inst.Operand).Offset.ToString("x4"));
				else
					Console.Write(" " + inst.Operand);
			}
			Console.WriteLine();

			if (inst.Left != null)
				DrawTree(method, inst.Left, level + 1);
			if (inst.Right != null)
				DrawTree(method, inst.Right, level + 1);
		}

		static void DrawTree(MethodDefinition method, BasicBlock block)
		{
			foreach (TreeInstruction root in block.Roots)
			{
				DrawTree(method, root, 0);
			}
		}

		private static void TestBuildTree()
		{
			Action<int> del = delegate(int i)
			                  	{
			                  		int j = 8 + (i*5);
									if (i > 5)
										j++;
									while (j < 0)
									{
										j--;
									}
			                  	};
			MethodDefinition method = GetMethod(del);
			CompileInfo ci = new CompileInfo(method);


			foreach (BasicBlock block in ci.Blocks)
			{
				Console.WriteLine(" - Basic Block:");
				DrawTree(method, block);
			}
		}

		private static MethodDefinition GetMethod(Delegate a)
		{
			AssemblyDefinition ass = AssemblyFactory.GetAssembly(a.Method.DeclaringType.Assembly.Location);
			MetadataToken token = new MetadataToken(a.Method.DeclaringType.MetadataToken);
			foreach (TypeDefinition type in ass.MainModule.Types)
			{
				if (type.MetadataToken != token)
					continue;
				foreach (MethodDefinition meth in type.Methods)
				{
					if (meth.MetadataToken == new MetadataToken(a.Method.MetadataToken))
						return meth;
				}
			}

			throw new ArgumentException("Can't find the type or method");
		}

		struct MyStruct
		{
			private int i;

			public void Method()
			{
				
			}
		}


		static void IfMethod(int arg)
		{
			MyStruct s = new MyStruct();
			s.Method();

			if (arg == 3)
				Console.WriteLine();
			else
				arg = 345;
		}

		static private void MyMethod(int intArg)
		{
//			XmlReader xr = XmlReader.Create(new StringReader("<root />"));
//			xr.Read();
//			xr.ReadOuterXml();
			Dictionary<int, string> dict = new Dictionary<int, string>();
		}
	}
}
