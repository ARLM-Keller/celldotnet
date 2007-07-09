using System;
using System.Collections.Generic;
using System.Reflection;

namespace CellDotNet
{
	class MethodCallInstruction : TreeInstruction
	{
		/// <summary>
		/// The Operand casted as a method.
		/// </summary>
		public MethodBase Method
		{
			get { return (MethodBase) Operand; }
		}

		public MethodCallInstruction(MethodBase _method, IROpCode _opcode)
		{
			Operand = _method;
			Opcode = _opcode;
		}

		private List<TreeInstruction> _parameters = new List<TreeInstruction>();
		public List<TreeInstruction> Parameters
		{
			get { return _parameters; }
		}


		public override void BuildPreorder(List<TreeInstruction> list)
		{
			list.Add(this);
			foreach (TreeInstruction param in Parameters)
			{
				param.BuildPreorder(list);
			}
//			base.BuildPreorder(list);
		}
	}
}
