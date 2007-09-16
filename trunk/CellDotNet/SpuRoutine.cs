using System.Collections.ObjectModel;

namespace CellDotNet
{
	abstract class SpuRoutine : ObjectWithAddress
	{
		protected SpuRoutine()
		{
		}

		public SpuRoutine(string name)
			: base(name)
		{
		}

		public abstract ReadOnlyCollection<MethodParameter> Parameters { get; }
		public abstract StackTypeDescription ReturnType { get; }
	}
}
