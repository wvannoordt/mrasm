using System;
using System.IO;
using System.Collections.Generic;

namespace Mrasm
{
	public static class Program
	{
		public static int Main(string[] args)
		{
			string error;
			UserInput input;

			if (!UserInput.TryParse(args, out input, out error))
			{
				Console.WriteLine("Fatal error: " + error);
				return 2;
			}
			CompilationContext context = new CompilationContext(input);
			return context.Compile();
		}
	}
}
