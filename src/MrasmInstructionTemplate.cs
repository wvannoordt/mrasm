using System;
using System.IO;
using System.Collections.Generic;

namespace Mrasm
{
	public class MrasmInstructionTemplate
	{
		private int arg_pitch;
		private int base_word;
		private bool needs_arg;
		private string name;
		public string Name {get {return name;}}
		const int NUMDATA = 4;
		const string SPECIALS = "`\"\':;~!@#$%^&*()-=_+<>,./?\\|{}[]";
		const string WHITES = " \t";
		const char CONSTDATAPASS = '<';
		private bool accepts_input_data;
		private EnvironmentDefinitions environment_defs;
		public EnvironmentDefinitions EnvDefs {set{environment_defs = value;}}
		public MrasmInstructionTemplate(string template_line)
		{
			string[] line_array = template_line.Split(',');
			if (line_array.Length != NUMDATA) throw_error("could not parse the following line: " + template_line + " (not enough or too much data)");
			if (!int.TryParse(line_array[2], out arg_pitch)) throw_error("could not parse the following line: " + template_line + " (argument offset)");
			if (!parse_base_word(line_array[1], out base_word)) throw_error("could not parse the following line: " + template_line + " (bad specifier)");
			if (!parse_name(line_array[0], out name)) throw_error("could not parse following line: " + template_line + " (bad name)");
			if (!parse_accepts_input_data(line_array[3], out accepts_input_data)) throw_error("could not parse following line: " + template_line + " (bad input data specifier)");
			needs_arg = (arg_pitch>0);
		}

		public bool CheckNameMatch(string line)
		{
			string working = remove_whites(line.Trim().ToLower());
			return working.StartsWith(name);
		}

		public bool CheckSyntaxMatch(string line, out string control_word, out string specific_error)
		{
			control_word = "";
			specific_error = "name mismatch";
			string argument_string;
			int passed_arg_int = -1;
			string working = remove_whites(line.Trim().ToLower());
			if (!working.StartsWith(name)) return false;
			if (!no_junk_after(working))
			{
				specific_error = "unexpected junk here";
				return false;
			}
			if (!parse_arg_syntax(working, out specific_error, out argument_string)) return false;
			if (needs_arg)
			{
				if (!parse_arg(argument_string, out specific_error, out passed_arg_int)) return false;
			}
			control_word = build_control_word(passed_arg_int);
			string passed_constdata;
			if (has_passed_const_data(line, out passed_constdata))
			{
				if (!accepts_input_data)
				{
					specific_error = "command \"" + name + "\" does not accept data input";
				}
				int passeddata;
				try
				{
					if (!parse_environment_const_int(passed_constdata, out passeddata))
					{
						specific_error = "could not evaluate data expression to const int";
						return false;
					}
				}
				catch (RecursionLimitException e)
				{
					specific_error = e.Message;
					return false;
				}
				control_word = modify_control_word_with_passed_data(control_word, passeddata);
			}
			return true;
		}

		private bool no_junk_after(string line)
		{
			string afterstr = "";
			int start_pos = name.Length;
			if (line.Contains("]")) start_pos = line.LastIndexOf("]")+1;
			for (int i = start_pos; i < line.Length; i++) afterstr += line[i];
			afterstr = remove_whites(afterstr);
			if (!accepts_input_data || !line.Contains(CONSTDATAPASS)) return afterstr.Length == 0;
			return true;
		}

		private string modify_control_word_with_passed_data(string control_word, int passeddata)
		{
			return control_word + " < " + Convert.ToString(passeddata, 2).PadLeft(8, '0');
		}

		private bool parse_accepts_input_data(string line, out bool accepts)
		{
			accepts = false;
			if (!(line.Trim() == "1" || line.Trim() == "0")) return false;
			accepts = (line.Trim() == "1");
			return true;
		}

		private bool has_passed_const_data(string line, out string passed_constdata)
		{
			passed_constdata = "";
			string[] ar = line.Split(CONSTDATAPASS);
			if (ar.Length > 1)
			{
				passed_constdata = ar[1].Trim();
				return true;
			}
			return false;
		}

		private string build_control_word(int argument_if_passed)
		{
			int control_int = needs_arg ? (base_word + arg_pitch*argument_if_passed) : base_word;
			return Convert.ToString(control_int, 2);
		}

		private bool parse_arg(string input, out string error, out int const_int_expr)
		{
			error = "none";
			if (!parse_environment_const_int(input, out const_int_expr))
			{
				error = "could not evaluate index expression to const int";
				return false;
			}
			return true;
		}

		private bool parse_environment_const_int(string input, out int input_integer)
		{
			if (int.TryParse(input, out input_integer)) return true;
			string resolved_symbol;
			if (environment_defs.TryResolveSymbol(input, out resolved_symbol))
			{
				if (int.TryParse(resolved_symbol, out input_integer)) return true;
			}
			input_integer = -213123;
			return false;
		}

		private bool parse_arg_syntax(string input, out string error, out string argument_string)
		{
			error = "none";
			argument_string = "none";
			if (!needs_arg)
			{
				if (input.Contains("[") || input.Contains("]"))
				{
					error = "command \"" + name + "\" does not accept address parameter";
					return false;
				}
				return true;
			}
			else
			{
				int bracket_level;
				if (!bracket_consistency(input, out argument_string, out bracket_level))
				{
					error = "indexer bracket mismatch";
					return false;
				}
				if (bracket_level == 0)
				{
					error = "expecting indexer bracket pair";
					return false;
				}
				if (bracket_level != 1)
				{
					error = "multi-level indexer evaluation not supported";
					return false;
				}
				return true;
			}
		}

		private bool bracket_consistency(string input, out string first_level_argument, out int bracket_level)
		{
			bracket_level = 0;
			first_level_argument = null;
			string first_arg_out = "";
			int current_consistency = 0;
			bool addingmode = false;
			foreach (char t in input)
			{
				if (t=='[')
				{
					current_consistency++;
					bracket_level++;
				}
				if (t==']') current_consistency--;
				addingmode = current_consistency > 0 && t!='[';
				if (current_consistency < 0) return false;
				if (addingmode) first_arg_out += t;
			}
			if (current_consistency != 0) return false;
			first_level_argument = first_arg_out;
			return true;
		}

		private string remove_whites(string p)
		{
			string output  = "";
			foreach (char t in p)
			{
				if (!WHITES.Contains(t)) output += t;
			}
			return output;
		}

		private bool parse_base_word(string input, out int output)
		{
			output = -1;
			string working = input.ToLower().Trim();
			try
			{
				output = int.Parse(working, System.Globalization.NumberStyles.HexNumber);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private bool parse_name(string input, out string output)
		{
			output = null;
			string working = input.ToLower().Trim();
			foreach(char t in working)
			{
				if (SPECIALS.Contains(t)) return false;
			}
			output = working;
			return true;
		}

		private void throw_error(string p)
		{
			throw new Exception("[MrasmInstructionTemplate] Error: " + p);
		}

		public override string ToString()
		{
			return name;
		}
	}
}
