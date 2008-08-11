using System;
using System.Reflection;

namespace CellDotNet.Cuda
{
	public class CudaKernel<T> where T : class
	{
		private MethodInfo _kernelMethod;
		private readonly T _kernelWrapperDelegate;

		public CudaKernel(T kerneldelegate)
		{
			if (!(kerneldelegate is Delegate))
				throw new ArgumentException("Type argument must be a delegate type.");

			// TODO Generate LCG delegate wrapper.
			this._kernelWrapperDelegate = kerneldelegate;
		}

		public T Start
		{
			get { return _kernelWrapperDelegate; }
		}

		public void StartTmp(object[] args)
		{
//			 BuildIRTree(_kernelMethod);
			CudaMethod cm = new CudaMethod(_kernelMethod);

			/// 1: Compile to PTX
			/// 2: Compile to cubin
			/// 3: Load cubin
			/// 4: Set up arguments/data.
			/// 5: Start kernel.
		}

		public void SetBlockSize(int x, int y)
		{
			throw new NotImplementedException();
		}

		public void SetGridSize(int x, int y)
		{
			throw new NotImplementedException();
		}
	}
}