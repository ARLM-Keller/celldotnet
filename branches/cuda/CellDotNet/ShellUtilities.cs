// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CellDotNet
{
	static class ShellUtilities
	{
		internal static string ExecuteShellScript(string scriptText)
		{
			string scripttempfile = null;
			try
			{
				scripttempfile = Path.GetTempFileName();

				File.WriteAllText(scripttempfile, scriptText.Replace("\r\n", "\n"));

				return ExecuteCommandAndGetOutput("sh", scripttempfile);
			}
			finally
			{
				if (scripttempfile != null && File.Exists(scripttempfile))
					File.Delete(scripttempfile);
			}
		}

		internal static string ExecuteCommandAndGetOutput(string program, string arguments)
		{
			using (Process p = new Process())
			{
				p.StartInfo.FileName = program;
				p.StartInfo.CreateNoWindow = true;
				p.StartInfo.UseShellExecute = false;
				if (!string.IsNullOrEmpty(arguments))
					p.StartInfo.Arguments = arguments;

				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.RedirectStandardError = true;
				p.Start();
				StringBuilder sb = new StringBuilder();

				while (!p.HasExited)
				{
					sb.AppendLine(p.StandardOutput.ReadToEnd());

					if (p.StandardError.Peek() != -1)
					{
						string alloutput = p.StandardError.ReadToEnd();
						throw new ShellExecutionException(string.Format("The program wrote {0} characters to standard output:\r\n{1}",
							alloutput.Length, alloutput));
					}
				}

				return sb.ToString();
			}
		}
	}
}
