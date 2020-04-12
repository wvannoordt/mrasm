using System;
using System.IO;
using System.Collections.Generic;

namespace Mrasm
{
	public static class Utilities
	{
		public static bool StringArrContains(string[] arr, string invalue)
		{
			foreach (string i in arr)
			{
				if (invalue == i) return true;
			}
			return false;
		}
	}
}
