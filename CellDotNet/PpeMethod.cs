using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// Represents a routine which must execute on the PPE.
	/// </summary>
	class PpeMethod : SpuRoutine
	{
		public override ReadOnlyCollection<MethodParameter> Parameters
		{
			get { throw new NotImplementedException(); }
		}

		public override StackTypeDescription ReturnType
		{
			get { throw new NotImplementedException(); }
		}

		public override int Size
		{
			get { throw new InvalidOperationException(); }
		}
	}
}
