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
		private EnvironmentDefinitions environment_defs;
		UserInput input;

		public const string PREPROCESS_PERMISSIBLE = "abcdefghijklmnopqrstuvwxyz1234567890_";
		public const string PREPROCESS_START_FORBIDDEN = "1234567890";

		public const string PREPROCESSORSPECIFIER = "#";

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
			for (int i = 0; i < is_preprocessor_line.Length; i++) is_preprocessor_line[i] = false;
			environment_defs = new EnvironmentDefinitions();
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
				if (!iscomment && !isempty && !is_preprocessor_line[i])
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
					match.EnvDefs = environment_defs;
					if (!match.CheckSyntaxMatch(line, out control_word, out compile_error))
					{
						write_compile_error(compile_error, line, i+1, input.Target);
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
			if (!environment_defs.TryIngestInput(input, out error)) return false;

			string[] lines_preprocessor;
			int[] line_nums_preprocessor;
			determine_preprocessor_lines(out lines_preprocessor, out line_nums_preprocessor);
			for (int i = 0; i < lines_preprocessor.Length; i++)
			{

				string line = lines_preprocessor[i];
				error_line = line;
				int display_line_number = line_nums_preprocessor[i];
				error_line_num = display_line_number;
				string working_line = line.Substring(PREPROCESSORSPECIFIER.Length).Trim();

				if (has_preprocessor_definition(working_line))
				{
					if (!parse_preprocessor_definition(working_line, out error)) return false;
				}
			}
			return true;
		}

		private bool has_preprocessor_definition(string workingline)
		{
			return workingline.StartsWith("define");
		}

		private bool parse_preprocessor_definition(string line, out string error)
		{
			error = "none";
			string[] spt = line.Split(' ');
			List<string> nonempty = new List<string>();
			foreach (string p in spt)
			{
				if (!string.IsNullOrEmpty(p)) nonempty.Add(p);
			}
			string[] nonemptyar = nonempty.ToArray();
			if (nonemptyar.Length < 3)
			{
				error = "unable to parse preprocessor variable";
				return false;
			}
			if (nonemptyar[0].Trim() != "define")
			{
				error = "unknown definition keyword \"" + nonemptyar[0] + "\"";
				return false;
			}
			if (!UserInput.IsValidPreprocessorName(nonemptyar[1], out error)) return false;
			if (environment_defs.IsDefined(nonemptyar[1]))
			{
				error = "duplicate definition of preprocessor variable \"" + nonemptyar[1] + "\"";
				return false;
			}
			environment_defs.AddDefinition(nonemptyar[1], nonemptyar[2]);
			return true;
		}

		private void determine_preprocessor_lines(out string[] preprocessor_lines, out int[] proprocessor_line_numbers)
		{
			//This can be made much more sophisticated
			List<string> prelines = new List<string>();
			List<int> prenums = new List<int>();
			for (int i = 0; i < lines.Length; i++)
			{
				is_preprocessor_line[i] = lines[i].StartsWith(PREPROCESSORSPECIFIER);
				if (is_preprocessor_line[i])
				{
					prelines.Add(lines[i]);
					prenums.Add(i+1);
				}
			}
			preprocessor_lines = prelines.ToArray();
			proprocessor_line_numbers = prenums.ToArray();
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
			if (line_num < 0)
			{
				Console.WriteLine("Error when compiling " + filename + ": " + message);
			}
			else
			{
				Console.WriteLine("Error in " + filename + ", line " + line_num + ": " + message);
				Console.WriteLine(" >>> " + line);
			}
		}
	}
}
