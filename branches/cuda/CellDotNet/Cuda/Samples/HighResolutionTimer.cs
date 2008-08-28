using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace CellDotNet.Cuda.Samples
{
	[Obsolete]
	public class HighResolutionTimer
	{
		[DllImport("Kernel32.dll")]
		private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

		[DllImport("Kernel32.dll")]
		private static extern bool QueryPerformanceFrequency(out long lpFrequency);

		private long startTime;
		private long stopTime;
		private long freq;

		/// <summary>
		/// ctor
		/// </summary>
		public HighResolutionTimer()
		{
			startTime = 0;
			stopTime = 0;
			freq = 0;
			if (QueryPerformanceFrequency(out freq) == false)
			{
				throw new NotSupportedException("No high resolution timer was found.");
			}
		}

		/// <summary>
		/// Start the timer.
		/// </summary>
		/// <returns>tick count</returns>
		public long Start()
		{
			QueryPerformanceCounter(out startTime);
			return startTime;
		}

		/// <summary>
		/// Stop timer.
		/// </summary>
		/// <returns>tick count</returns>
		public long Stop()
		{
			QueryPerformanceCounter(out stopTime);
			return stopTime;
		}

		/// <summary>
		/// Return the duration of the timer (in seconds).
		/// </summary>
		/// <returns>duration</returns>
		public double Seconds
		{
			get { return (stopTime - startTime) / (double)freq; }
		}

		/// <summary>
		/// Frequency of timer (no counts in one second on this machine).
		/// </summary>
		///<returns>Frequency</returns>
		public long Frequency
		{
			get
			{
				QueryPerformanceFrequency(out freq);
				return freq;
			}
		}
	}
}
