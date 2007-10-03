using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// Represents a routine which must execute on the PPE.
	/// </summary>
	class PpeMethod : SpuRoutine
	{
		private MethodInfo _method;


		public MethodInfo Method
		{
			get { return _method; }
		}

		public PpeMethod(MethodInfo method) : base(method.Name, method)
		{
			_method = method;
		}

		public override int Size
		{
			get { throw new InvalidOperationException(); }
		}
	}
}
