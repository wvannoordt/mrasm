using System;
using System.IO;
using System.Collections.Generic;

namespace Mrasm
{
	public class EnvironmentDefinitions
	{
		private Dictionary<string, string> var_defs;
		const int MAXRECURSIONLEVEL = 32;
		public EnvironmentDefinitions()
		{
			var_defs = new Dictionary<string, string>();
		}
		public bool TryIngestInput(UserInput input, out string error)
		{
			error = "none";
			for (int i = 0; i < input.PreprocessorDefs.Length; i++)
			{
				if (var_defs.ContainsKey(input.PreprocessorDefs[i]))
				{
					error = "preprocessor variable \"" + input.PreprocessorDefs[i] + "\" has multiple definitions";
					return false;
				}
				var_defs.Add(input.PreprocessorDefs[i], input.PreprocessorVals[i]);
			}
			return true;
		}
		public bool TryResolveSymbol(string symbol, out string valueout)
		{
			return try_resolve_recursive(symbol, out valueout, 0);
		}
		private bool try_resolve_recursive(string symbol, out string valueout, int recusionlevel)
		{
			valueout = "";
			if (recusionlevel > MAXRECURSIONLEVEL)
			{
				throw new RecursionLimitException("max definition recursion level met when evaluating preprocessor variable \"" + symbol + "\". Possible circular definition.");
			}
			string currentvalue;
			if (!IsDefined(symbol, out currentvalue)) return false;
			if (IsDefined(currentvalue))
			{
				return try_resolve_recursive(currentvalue, out valueout, recusionlevel+1);
			}
			else
			{
				valueout = currentvalue;
				return true;
			}
		}
		public void AddDefinition(string defvar, string defval)
		{
			var_defs.Add(defvar, defval);
		}
		public bool IsDefined(string variable)
		{
			return var_defs.ContainsKey(variable);
		}

		public bool IsDefined(string variable, out string val)
		{
			return var_defs.TryGetValue(variable, out val);
		}
	}
}
