using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class TemporaryFileTest : UnitTest
	{
		[Test]
		public void Test()
		{
			string path;
			using (var tf = new TemporaryFile())
			{
				IsTrue(File.Exists(tf.Path));
				path = tf.Path;
			}
			IsFalse(File.Exists(path));
		}
	}
}
