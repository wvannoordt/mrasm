using System;
using System.IO;
using System.Collections.Generic;

namespace Mrasm
{
	public class UserInput
	{
		string target, output_exe;
		public string Target {get{return target;} set {target = value;}}
		public string OutputExecutableName {get{return output_exe;} set {output_exe = value;}}
		public string[] CompilerFlags {get{return compiler_flags;} set {compiler_flags = value;}}
		public string[] PreprocessorDefs {get{return definition_flag_names;} set {definition_flag_names = value;}}
		public string[] PreprocessorVals {get{return definition_flag_values;} set {definition_flag_values = value;}}

		private string[] compiler_flags;
		private string[] definition_flag_names;
		private string[] definition_flag_values;

		const string FILE_PERMISSIBLE = "abcdefghijklmnopqrstuvwxyz1234567890-_.";
		const string FILE_START_FORBIDDEN = "-_";
		const string TARGETEXTENSION = ".mra";


		public UserInput(){}

		public bool HasFlag(string flag)
		{
			return Utilities.StringArrContains(compiler_flags, flag);
		}

		public static bool TryParse(string[] args, out UserInput input, out string error)
		{
			bool[] isjunkflag = new bool[args.Length];
			for (int i = 0; i < isjunkflag.Length; i++) isjunkflag[i] = true;
			input = new UserInput();
			error = "none";
			if (args.Length == 0)
			{
				error = "not enough input arguents";
				return false;
			}
			string in_target, in_exe;

			if (!parse_target(args, out error, out in_target, isjunkflag)) return false;
			input.Target = in_target;

			if (!parse_output_exe(args, out error, out in_exe, isjunkflag)) return false;
			input.OutputExecutableName = in_exe;

			string[] givenflags;
			if (!parse_flags(args, out error, out givenflags, isjunkflag)) return false;
			input.CompilerFlags = givenflags;

			string[] given_defs, given_vals;
			if (!parse_precompiler_values(args, out given_defs, out given_vals, out error, isjunkflag)) return false;
			input.PreprocessorDefs = given_defs;
			input.PreprocessorVals = given_vals;

			if (!parse_any_junk(args, isjunkflag, input, out error)) return false;

			return true;
		}

		private static bool parse_precompiler_values(string[] args, out string[] given_defs, out string[] given_vals, out string error, bool[] junks)
		{
			given_defs = new string[0];
			given_vals = new string[0];
			error = "none";
			List<string> found_defs = new List<string>();
			List<string> found_vals = new List<string>();
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i].StartsWith("-D"))
				{
					string assignment = args[i].Substring(2);
					junks[i] = false;
					string[] spt = assignment.Split('=');
					if (spt.Length > 2)
					{
						error = "cannot parse precompiler assignment \"" + assignment + "\"";
						return false;
					}
					if (string.IsNullOrEmpty(assignment))
					{
						error = "empty precompiler assignment";
						return false;
					}
					string vname = spt[0];
					string vval = "";

					if (spt.Length > 1)
					{
						vval = spt[1];
					}
					if (!IsValidPreprocessorName(vname, out error)) return false;

					found_defs.Add(vname);
					found_vals.Add(vval);
				}
			}
			given_defs = found_defs.ToArray();
			given_vals = found_vals.ToArray();
			return true;
		}

		public static bool IsValidPreprocessorName(string vname, out string error)
		{
			error = "none";
			foreach (char t in vname)
			{
				if (!CompilationContext.PREPROCESS_PERMISSIBLE.Contains(char.ToLower(t)))
				{
					error = "forbidden character value in preprocessor variable \"" + vname + "\"";
					return false;
				}
			}

			foreach (char t in CompilationContext.PREPROCESS_START_FORBIDDEN)
			{
				if (vname.StartsWith(t))
				{
					error = "invalid starting character in preprocessor variable \"" + vname + "\"";
					return false;
				}
			}
			return true;
		}

		private static bool parse_any_junk(string[] args, bool[] junks, UserInput input, out string error)
		{
			error = "none";
			for (int i = 0; i < junks.Length; i++)
			{
				if (junks[i])
				{
					if (input.HasFlag("relaxinput"))
					{
						Console.WriteLine("Warning: ignoring input \"" + args[i] + "\"");
					}
					else
					{
						error = "unknown junk input \"" + args[i] + "\"";
						return false;
					}
				}
			}
			return true;
		}

		private static bool parse_flags(string[] args, out string error, out string[] givenflags, bool[] isjunkflags)
		{
			error = "none";
			givenflags = new string[0];
			List<string> found = new List<string>();
			string[] known_flags = ResourceManager.GetResource("lang/mrasm.flags");
			for (int i = 0; i < args.Length; i++)
			{
				string current = args[i];
				if (current.StartsWith("-f"))
				{
					string flagname=current.Substring(2);
					isjunkflags[i] = false;
					found.Add(flagname);
					if (!Utilities.StringArrContains(known_flags, flagname))
					{
						error = "unknown flag \"" + flagname + "\".";
						return false;
					}
				}
			}
			givenflags = found.ToArray();
			return true;
		}

		private static bool parse_output_exe(string[] args, out string error, out string output_exe_name, bool[] isjunkflags)
		{
			error = "none";
			output_exe_name = "a.out";
			bool alreadyfound = false;
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i] == "-o")
				{
					if (alreadyfound)
					{
						error = "found too many output specifications";
						return false;
					}
					alreadyfound = true;
					if (i == (args.Length - 1))
					{
						error = "\"-o\" specified with no suitable output file name";
						return false;
					}
					if (alreadyfound)
					{
						output_exe_name = args[i+1];
						isjunkflags[i] = false;
						isjunkflags[i+1] = false;
						foreach (char t in output_exe_name)
						{
							if (!FILE_PERMISSIBLE.Contains(t))
							{
								error = "invalid character \'" + t + "\' found in output file name";
								return false;
							}
						}
						foreach (char t in FILE_START_FORBIDDEN)
						{
							if (output_exe_name.StartsWith(t))
							{
								error = "output file name starts with bad character \'" + t + "\'";
								return false;
							}
						}
					}
				}
			}
			return true;
		}

		private static bool parse_target(string[] args, out string error, out string targetname, bool[] isjunkflags)
		{
			error = "none";
			targetname = "NOTARGETNAME";
			bool alreadyfound = false;
			for (int i = 0; i < args.Length; i++)
			{
				if (!args[i].StartsWith("-"))
				{
					bool is_output_exe = i > 0;
					if (is_output_exe) is_output_exe = (args[i-1]=="-o");
					if (!is_output_exe)
					{
						if (alreadyfound)
						{
							error = "too many targets specified";
							return false;
						}
						targetname = args[i];
						isjunkflags[i] = false;
						alreadyfound = true;
						if (!targetname.EndsWith(TARGETEXTENSION))
						{
							error = "target \"" + targetname + "\" does not have extension " + TARGETEXTENSION;
							return false;
						}
					}
				}
			}
			if (!alreadyfound)
			{
				error = "no suitable target found";
				return false;
			}
			return true;
		}
	}
}
