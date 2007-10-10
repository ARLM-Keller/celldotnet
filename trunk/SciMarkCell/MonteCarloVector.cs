using CellDotNet;

/// <license>
/// This is a port of the SciMark2a Java Benchmark to C# by
/// Chris Re (cmr28@cornell.edu) and Werner Vogels (vogels@cs.cornell.edu)
/// 
/// For details on the original authors see http://math.nist.gov/scimark2
/// 
/// This software is likely to burn your processor, bitflip your memory chips
/// anihilate your screen and corrupt all your disks, so you it at your
/// own risk.
/// </license>
namespace SciMark2Cell
{
	/// <summary>Estimate Pi by approximating the area of a circle.
	/// How: generate N random numbers in the unit square, (0,0) to (1,1)
	/// and see how are within a radius of 1 or less, i.e.
	/// <pre>  
	/// sqrt(x^2 + y^2) < r
	/// </pre>
	/// since the radius is 1.0, we can square both sides
	/// and avoid a sqrt() computation:
	/// <pre>
	/// x^2 + y^2 <= 1.0
	/// </pre>
	/// this area under the curve is (Pi * r^2)/ 4.0,
	/// and the area of the unit of square is 1.0,
	/// so Pi can be approximated by 
	/// <pre>
	/// # points with x^2+y^2 < 1
	/// Pi =~ 		--------------------------  * 4.0
	/// total # points
	/// </pre>
	/// </summary>
	public class MonteCarloVector
	{
		internal const int SEED = 113;

		public static float num_flops(int Num_samples)
		{
			// 3 flops in x^2+y^2 and 1 flop in random routine

			// time 4 (Vectorization)

			return Num_samples*4.0f*4.0f;
		}

		public static float integrate(int Num_samples)
		{
			int iterations = Num_samples/4;

			RandomVector R = new RandomVector();

			R.initializeRandomCell(Int32Vector.Splat(SEED));

			Int32Vector under_curve = Int32Vector.Splat(0);

			Int32Vector _zerro = Int32Vector.Splat(0);
			Int32Vector _one = Int32Vector.Splat(1);
			Float32Vector unitVector = Float32Vector.Splat(1f);

			for (int count = 0; count < iterations; count++)
			{
				Float32Vector x = R.nextFloat();
				Float32Vector y = R.nextFloat();

				under_curve += SpuMath.CompareGreaterThanAndSelect(unitVector, x * x + y * y, _one, _zerro);
			}

			return ((float)(under_curve.E1 + under_curve.E2 + under_curve.E3 + under_curve.E4) / (float)(iterations*4)) * 4.0f;
		}
	}
}
