using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	/// <summary>
	/// For simple programs. Test focused on arrays and object should go into <see cref="ObjectModelTest"/>.
	/// </summary>
	[TestFixture]
	public class SimpleProgramsTest : UnitTest
	{
		private delegate int IntReturnDelegate();
		private delegate float FloatReturnDelegate();

		[Test]
		public void TestLoop_SumInt()
		{
			IntReturnDelegate del =
				delegate
					{
						const int count = 5;
						int sum = 0;
						for (int i = 0; i < count; i++)
							sum += i*i;
						return sum;
					};

			int correctVal = del();
			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(correctVal, SpeDelegateRunner.CreateSpeDelegate(del)());
		}

		[Test]
		public void TestLoop_SumFloat()
		{
			FloatReturnDelegate del =
				delegate
				{
					const int count = 5;
					float sum = 0;
					for (int i = 0; i < count; i++)
						sum += (float) i * (float) i;
					return sum;
				};

			float correctVal = del();
			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(correctVal, SpeDelegateRunner.CreateSpeDelegate(del)());
		}

		[Test]
		public void TestLoop_SumFloat2()
		{
			FloatReturnDelegate del =
				delegate
				{
					const int count = 5;
					float sum = 0;
					int[] arr = new int[count];

					for (int i = 0; i < count; i++)
						arr[i] = i;
					for (int i = 0; i < count; i++)
						sum += arr[i];

					return sum;
				};

			float correctVal = del();
			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(correctVal, SpeDelegateRunner.CreateSpeDelegate(del)());
		}

		// **************************************************
		// TestRecursiveSummation_Int
		// **************************************************

		[Test]
		public void TestRecursiveSummation_Int()
		{
			Converter<int, int> del = RecursiveSummation_Int;

			const int arg = 15;
			int correctval = del(arg);

			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(correctval, SpeDelegateRunner.CreateSpeDelegate(del)(arg));
		}

		/// <summary>
		/// This one just does some weird recursion.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		static int RecursiveSummation_Int(int level)
		{
			if (level == 0)
				return 0;
			else if ((level & 2) == 1)
				return level + RecursiveSummation_Int(level - 1);
			else
				return RecursiveSummation_Int(level - 1);
		}

		// **************************************************
		// TestRecursiveSummation_Float
		// **************************************************

		[Test]
		public void TestRecursiveSummation_Float()
		{
			Converter<int, float> del = RecursiveSummation_Float;

			const int arg = 15;
			float correctval = del(arg);

			if (!SpeContext.HasSpeHardware)
				return;

			AreEqual(correctval, SpeDelegateRunner.CreateSpeDelegate(del)(arg));
		}

		/// <summary>
		/// This one just does some weird recursion.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		static float RecursiveSummation_Float(int level)
		{
			if (level == 0)
				return 0;
			else if ((level & 2) == 1)
				return level + RecursiveSummation_Int(level - 1);
			else
				return RecursiveSummation_Int(level - 1);
		}
	}
}
