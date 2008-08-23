using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CellDotNet.Intermediate;

namespace CellDotNet.Cuda
{
	class PtxEmitter
	{
		private readonly StringWriter _methodPtx = new StringWriter();

		public void Emit(CudaMethod method)
		{
			Utilities.AssertArgument(method.State >= CudaMethodCompileState.InstructionSelectionDone, "method.State >= CudaMethodCompileState.InstructionSelectionDone");

			using (new CultureScope("en-US"))
			{
				Emit(method, _methodPtx);
			}
		}

		public String GetEmittedPtx()
		{
			var methodptx = _methodPtx.GetStringBuilder().ToString();
			return @"
	.version 1.2
	.target sm_11, map_f64_to_f32


" + methodptx;
		}

		void Emit(CudaMethod method, TextWriter ptx)
		{
			ptx.WriteLine(@"	.entry " + method.PtxName + @"
	{");
			foreach (GlobalVReg param in method.Parameters)
			{
				Utilities.DebugAssert(param.Storage == VRegStorage.Parameter, "vreg.Storage == VRegStorage.Parameter");

				ptx.WriteLine("\t.param " + GetPtxType(param.StackType, false) + " " + param.Name + ";");
			}

			NameAndDeclareLocals(method, ptx);
			EmitBlocks(method.Blocks, ptx);

			ptx.WriteLine(@"	} // " + method.PtxName);
			ptx.WriteLine();
		}

		private void EmitBlocks(List<BasicBlock> blocks, TextWriter ptx)
		{
			foreach (BasicBlock block in blocks)
			{
				ptx.WriteLine();
				ptx.WriteLine(block.Name + ":");

				foreach (ListInstruction inst in block.Instructions)
				{
					string opcodename = null;
					switch (inst.PtxCode)
					{
						case PtxCode.Add_F32: opcodename = "add.f32"; break;
//						case PtxCode.Add_S16: opcodename = "add.s16"; break;
						case PtxCode.Add_S32: opcodename = "add.s32"; break;
						case PtxCode.Div_F32: opcodename = "div.f32"; break;
						case PtxCode.Div_S32: opcodename = "div.s32"; break;
						case PtxCode.Div_U32: opcodename = "div.u32"; break;
						case PtxCode.Mul_F32: opcodename = "mul.f32"; break;
						case PtxCode.Mul_Lo_S32: opcodename = "mul.lo.s32"; break;
						case PtxCode.Sub_F32: opcodename = "sub.f32"; break;
						case PtxCode.Sub_S32: opcodename = "sub.s32"; break;
						case PtxCode.Ret: opcodename = "ret"; break;
						case PtxCode.Mov_S32: opcodename = "mov.s32"; break;
						case PtxCode.Mov_F32: opcodename = "mov.f32"; break;
						case PtxCode.Setp_Gt_F32: opcodename = "setp.gt.f32"; break;
						case PtxCode.Setp_Gt_S32: opcodename = "setp.gt.s32"; break;
						case PtxCode.Setp_Gtu_F32: opcodename = "setp.gtu.f32"; break;
						case PtxCode.Setp_Hi_U32: opcodename = "setp.hi.u32"; break;
						case PtxCode.Setp_Lt_F32: opcodename = "setp.lt.f32"; break;
						case PtxCode.Setp_Lt_S32: opcodename = "setp.lt.s32"; break;
						case PtxCode.Setp_Lo_U32: opcodename = "setp.lo.u32"; break;
						case PtxCode.Setp_Ltu_F32: opcodename = "setp.ltu.f32"; break;
						case PtxCode.Setp_Eq_S32: opcodename = "setp.eq.s32"; break;
						case PtxCode.Setp_Eq_F32: opcodename = "setp.eq.f32"; break;
						case PtxCode.Cvt_S32_U16: opcodename = "cvt.s32.u16"; break;
						case PtxCode.Bar_Sync: opcodename = "bar.sync"; break;

						case PtxCode.Ld_Global_F32: opcodename = "ld.global.f32"; goto case PtxCode.Ld_Global_S32;
//						case PtxCode.Ld_Global_S16: opcodename = "ld.global.s16"; goto case PtxCode.Ld_Global_S32;
						case PtxCode.Ld_Global_S32:
							if (opcodename == null)
								opcodename = "ld.global.s32";

							// Immediate addresses and offsets are not handled.
							ptx.WriteLine("\t{0} {1} {2}, [{3}];",
								GetPredicateInstructionPrefix(inst),
								opcodename,
								inst.Destination.Name,
								inst.Source1.Name);
							continue;
						case PtxCode.Ld_Param_F32: opcodename = "ld.param.f32"; goto case PtxCode.Ld_Param_S32;
//						case PtxCode.Ld_Param_S16: opcodename = "ld.param.s16"; goto case PtxCode.Ld_Param_S32;
						case PtxCode.Ld_Param_S32:
							if (opcodename == null)
								opcodename = "ld.param.s32";

							// Immediate addresses and offsets are not handled.
							ptx.WriteLine("\t{0} {1} {2}, [{3}];",
								GetPredicateInstructionPrefix(inst),
								opcodename,
								inst.Destination.Name,
								inst.OperandAsGlobalVRegNonNull.Name);
							continue;
						case PtxCode.St_Global_F32: opcodename = "st.global.f32"; goto case PtxCode.St_Global_S32;
//						case PtxCode.St_Global_S16: opcodename = "st.global.s16"; goto case PtxCode.St_Global_S32;
						case PtxCode.St_Global_S32:
								if (opcodename == null)
								opcodename = "st.global.s32";

							// Immediate addresses and offsets are not handled.
							ptx.WriteLine("\t{0} {1} [{2}], {3};",
								GetPredicateInstructionPrefix(inst),
								opcodename,
								inst.Source1.Name,
								inst.Source2.Name);
							continue;
						case PtxCode.Bra:
							ptx.WriteLine(GetPredicateInstructionPrefix(inst) + " bra " + ((BasicBlock)inst.Operand).Name + ";");
							continue;
//						case PtxCode.Call:
						default:
							throw new NotImplementedException("opcode: " + inst.PtxCode);
					}

					if (opcodename != null)
						EmitBasicRegisterOpcode(opcodename, inst, ptx);
				}
			}
		}

		private static void EmitBasicRegisterOpcode(string opcode, ListInstruction inst, TextWriter ptx)
		{
			string line = "\t" + GetPredicateInstructionPrefix(inst) + opcode;
			if (inst.Destination != null)
				line += " " + inst.Destination.GetAssemblyText();
			if (inst.Source1 != null)
			{
				if (inst.Destination != null)
					line += ", ";

				line += " " + inst.Source1.GetAssemblyText();
				if (inst.Source2 != null)
				{
					line += ", " + inst.Source2.GetAssemblyText();
					if (inst.Source3 != null)
						line += ", " + inst.Source3.GetAssemblyText();
				}				
			}
			ptx.WriteLine(line + ";");
		}

		private static string GetPredicateInstructionPrefix(ListInstruction inst)
		{
			if (inst.Predicate == null)
				return "";
			return "@" + (inst.PredicateNegation ? "!" : "") + inst.Predicate.Name + " ";
		}

		private static void NameAndDeclareLocals(CudaMethod method, TextWriter ptx)
		{
			// Count and assign names/indices to all register variables.
			var allregs = new HashSet<GlobalVReg>();
			foreach (ListInstruction inst in (from b in method.Blocks from li in b.Instructions select li))
			{

				if (inst.Destination != null)
					allregs.Add(inst.Destination);
				if (inst is MethodCallListInstruction)
				{
					foreach (GlobalVReg reg in ((MethodCallListInstruction) inst).Parameters)
						allregs.Add(reg);
				}
				else
				{
					if (inst.Source1 != null)
						allregs.Add(inst.Source1);
					if (inst.Source2 != null)
						allregs.Add(inst.Source2);
					if (inst.Source3 != null)
						allregs.Add(inst.Source3);
				}
			}

			foreach (var storagegroup in allregs
				.GroupBy(reg => new { reg.Storage, reg.StackType }))
			{
				string varprefix = "%";
				var stackType = storagegroup.Key.StackType;

				switch (storagegroup.Key.Storage)
				{
					case VRegStorage.Constant:
					case VRegStorage.Immediate:
					case VRegStorage.Parameter:
					case VRegStorage.SpecialRegister:
						continue;
					case VRegStorage.Texture:
						// Need to gather the textures.
						throw new NotSupportedException("Textures are not supported.");
					case VRegStorage.Global: varprefix += "g"; break;
					case VRegStorage.Local: varprefix += "l"; break;
					case VRegStorage.Register: varprefix += "r"; break;
					case VRegStorage.Shared: varprefix += "s"; break;
					default:
						throw new InvalidIRException("Bad vreg storage	: " + storagegroup.Key.Storage);
				}

				switch (stackType)
				{
					case StackType.Object: varprefix += "o"; break;
					case StackType.ManagedPointer: varprefix += "p"; break;
					case StackType.I4: varprefix += "i"; break;
					case StackType.I8: varprefix += "l"; break;
					case StackType.R4: varprefix += "f"; break;
					case StackType.R8: varprefix += "d"; break;
					case StackType.ValueType: varprefix += "pp"; break; // Below we'll check that it's really predicates.
					default: throw new InvalidIRException("Bad vreg stacktype: " + stackType);
				}

				int varcount = 0;
				foreach (GlobalVReg reg in storagegroup)
				{
					Utilities.DebugAssert(stackType != StackType.ValueType || reg.ReflectionType == typeof(PredicateValue));

					reg.Name = varprefix + varcount;
					varcount++;
				}

				{
					ptx.WriteLine(@"	{0} {1} {2}<{3}>;", GetStorageString(storagegroup.Key.Storage),
					              GetPtxType(stackType, true), varprefix, varcount);
				}
			}
		}

		private static string GetStorageString(VRegStorage storage)
		{
			switch (storage)
			{
				case VRegStorage.Constant: return ".constant";
				case VRegStorage.Immediate: throw new ArgumentOutOfRangeException("storage", "Can't do immediate.");
				case VRegStorage.Parameter: return ".param";
				case VRegStorage.SpecialRegister: throw new ArgumentOutOfRangeException("storage", "Can't do special register.");
				case VRegStorage.Texture: return ".tex";
				case VRegStorage.Global: return ".global";
				case VRegStorage.Local: return ".local";
				case VRegStorage.Register: return ".reg";
				case VRegStorage.Shared: return ".shared";
				default:
					throw new InvalidIRException("Bad vreg storage: " + storage);
			}
		}

		private static string GetPtxType(StackType type, bool isKnownToBePredicate)
		{
			switch (type)
			{
				case StackType.Object: return ".s32";
				case StackType.ManagedPointer: return ".s32";
				case StackType.ValueType: 
					if (isKnownToBePredicate)
						return ".pred";
					throw new NotSupportedException("Cannot emit PTX type for non-predicate value type.");
				case StackType.I4: return ".s32";
				case StackType.I8: return ".s64";
				case StackType.R4: return ".f32";
				case StackType.R8: return ".f64";
				default:
					throw new NotSupportedException("Cannot emit PTX type for '" + type + "'.");
			}
		}
	}

}
