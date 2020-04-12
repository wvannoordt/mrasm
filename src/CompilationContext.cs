using System;
using System.IO;
using System.Collections.Generic;

namespace Mrasm
{
	public class CompilationContext
	{
		private string[] lines;
		private bool[] is_preprocessor_line;
		private MrasmInstructionTemplate[] language;
		private EnvironmentDefinitions variable_defs;
		UserInput input;

		public const string PREPROCESS_PERMISSIBLE = "abcdefghijklmnopqrstuvwxyz1234567890_";
		public const string PREPROCESS_START_FORBIDDEN = "1234567890";

		enum ErrorCode
		{
			Success,
			Preprocessor,
			Compiler
		}
		private string comment_specifier = "//";

		public CompilationContext(UserInput input_in)
		{
			input = input_in;
			language = get_instructions("lang/mrasm.lang");
			lines = File.ReadAllLines(input.Target);
			is_preprocessor_line = new bool[lines.Length];
		}

		public int Compile()
		{
			string preprocess_error, er_line;
			int er_line_num;
			if (!try_pre_process(out preprocess_error, out er_line, out er_line_num))
			{
				write_compile_error(preprocess_error, er_line, er_line_num, input.Target);
				return (int)ErrorCode.Preprocessor;
			}
			List<string> output = new List<string>();
			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];
				bool iscomment = (line.Trim().StartsWith(comment_specifier));
				bool isempty = string.IsNullOrEmpty(line.Trim());
				if (!iscomment && !isempty)
				{
					bool found_template = false;
					MrasmInstructionTemplate match = null;
					foreach (MrasmInstructionTemplate template in language)
					{
						if (template.CheckNameMatch(line))
						{
							found_template = true;
							match = template;
						}
					}

					if (!found_template)
					{
						write_compile_error("found no matching command", line, i+1, input.Target);
						return (int)ErrorCode.Compiler;
					}

					string control_word;
					string compile_error;
					if (!match.CheckSyntaxMatch(line, out control_word, out compile_error))
					{
						write_compile_error(compile_error, line, i, input.Target);
						return (int)ErrorCode.Compiler;
					}
					output.Add(control_word.PadLeft(8, '0'));
				}
			}
			File.WriteAllLines(input.OutputExecutableName, output.ToArray());
			return (int)ErrorCode.Success;
		}

		private bool try_pre_process(out string error, out string error_line, out int error_line_num)
		{
			error = "none";
			error_line = "none";
			error_line_num = -1;
			return true;
		}

		private MrasmInstructionTemplate[] get_instructions(string filename)
		{

			string[] lines = ResourceManager.GetResource(filename);
			MrasmInstructionTemplate[] output = new MrasmInstructionTemplate[lines.Length];
			for (int i = 0; i < output.Length; i++) output[i] = new MrasmInstructionTemplate(lines[i]);
			return output;
		}


		private void write_compile_error(string message, string line, int line_num, string filename)
		{
			Console.WriteLine("Error in " + filename + ", line " + line_num + ": " + message);
			Console.WriteLine(" >>> " + line);
		}
	}
}
