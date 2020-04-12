using System;
using System.IO;
using System.Collections.Generic;

namespace Mrasm
{
	public static class ResourceManager
	{
		public static string[] GetResource(string filename)
		{
			string absfile = get_abs_file_name(filename);
			if (!File.Exists(absfile)) throw new Exception("[ResourceManager] Cannot find file " + absfile + ".");
			return File.ReadAllLines(absfile);
		}
		private static string get_abs_file_name(string filename)
		{
			if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MRASMPATH")))
			{
				Console.WriteLine("Cannot find mrasm resources. Please set MRASMPATH environment variable.");
				throw new Exception("Exception thrown from ResourceManager.");
			}
			return Path.Combine(Environment.GetEnvironmentVariable("MRASMPATH"), filename);
		}
	}
}
