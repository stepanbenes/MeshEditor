using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Wintellect.PowerCollections;

namespace MeshEditor.Data
{
	public class PropertyCommand
	{

		public enum CommandType
		{
			ndofn,
			bocon,
			dof_coupl,
			
			nod_tfunc,
			nod_crsec,
			nod_spring,
			nod_lcs,
			nod_load,
			nod_tdload,
			nod_inicond,
			nod_temper,

			el_type,
			el_mat,
			el_crsec,
			el_lcs,
			el_load,
			el_tfunc,

			edge_load,
			surf_load,
			volume_load
		}

		//public enum Keywords
		//{
		//    propid, num_bc, dir, cond, ndir, tfunc_id, type, type_id, num_mat, dim, basevec, lc_id, slc_id, load_comp, ini_cd_type, nval, temperature, strastrestate, ncomp, func_type, coord_sys
		//}

		public struct Parameter
		{
			string text;
			public string Text
			{
				get { return text; }
				set { text = value; }
			}
			public Parameter(string text)
			{
				this.text = text;
			}
		}
		
		private CommandType type;
		private Dictionary<string, string> variableValueMap;

		public Dictionary<string, string> VariableValueMap
		{
			get { return variableValueMap; }
		}

		public CommandType Type
		{
			get { return type; }
			set { type = value; }
		}

		public Property? GetPropertyValue()
		{
			string value;
			int number;
			if (variableValueMap.TryGetValue(PropertyNumberVariableName, out value) && int.TryParse(value, out number))
				return new Property(number);
			return null;
		}

		public PropertyCommand Clone()
		{
			PropertyCommand copy = new PropertyCommand(this.GetPropertyValue() ?? Property.Zero, this.type);
			foreach (string var in this.variableValueMap.Keys)
				copy.variableValueMap[var] = this.variableValueMap[var];
			return copy;
		}

		public PropertyCommand(CommandType type)
			: this(Property.Zero, type)
		{ }

		public PropertyCommand(Property property, CommandType type)
		{
			this.type = type;

			variableValueMap = new Dictionary<string, string>();

			if (property != Property.Zero)
				variableValueMap[PropertyNumberVariableName] = property.ToString();
		}

		#region Static members

		private static Dictionary<CommandType, string> patterns;

		public static Dictionary<CommandType, string> Patterns
		{
			get { return PropertyCommand.patterns; }
		}

		public static readonly string PropertyNumberVariableName;

		static PropertyCommand()
		{
			patterns = new Dictionary<CommandType, string>();

			PropertyNumberVariableName = "prop";

			patterns[CommandType.ndofn] = "%ndof propid %prop";
			patterns[CommandType.bocon] = "propid %prop num_bc %nbc {dir %d cond %val}*nbc";
			patterns[CommandType.dof_coupl] = "propid %prop ndir %nd {dir %d}*nd";
			patterns[CommandType.nod_tfunc] = "propid %prop ndir %nd {dir %d tfunc_id %id}*nd";
			patterns[CommandType.nod_crsec] = "propid %prop type %t type_id %id";
			patterns[CommandType.nod_spring] = "propid %prop dir %d num_mat %nm {type %t type_id %id}*nm";
			patterns[CommandType.nod_lcs] = "propid %prop dim %d {basevec {%comp}*d}*d";
			patterns[CommandType.nod_load] = "propid %prop lc_id %nlc [slc_id %slc] load_comp {%v}*ndof";
			patterns[CommandType.nod_tdload] = "propid %prop lc_id %nlc load_comp {%v}*ndof";
			patterns[CommandType.nod_inicond] = "propid %prop lc_id %nlc cond ini_cd_type %ict nval %nv {%v}*nv";
			patterns[CommandType.nod_temper] = "propid %prop lc_id %nlc [slc_id %slc] temperature %t";
			patterns[CommandType.el_type] = "propid %prop %t [strastrestate %s]";
			patterns[CommandType.el_mat] = "propid %prop num_mat %nm {type %t type_id %id}*nm";
			patterns[CommandType.el_crsec] = "propid %prop type %t type_id %id";
			patterns[CommandType.el_lcs] = "propid %prop dim %d {basevec {%comp}*d}*d";
			patterns[CommandType.el_load] = "propid %prop %loadel";
			patterns[CommandType.edge_load] = "propid %prop lc_id %nlc [slc_id %slc] ncomp %nc func_type %ft coord_sys %c load_comp {%v}*nc";
			patterns[CommandType.surf_load] = "propid %prop lc_id %nlc [slc_id %slc] ncomp %nc func_type %ft coord_sys %c load_comp {%v}*nc";
			patterns[CommandType.volume_load] = "propid %prop lc_id %nlc [slc_id %slc] ncomp %nc func_type %ft coord_sys %c load_comp {%v}*nc";
			patterns[CommandType.el_tfunc] = "propid %prop tfunc_id %id";
		}

		public static PropertyCommand CreateFromString(string inputLine)
		{
			if (string.IsNullOrEmpty(inputLine))
				return null;

			char[] separators = new char[] { ' ', '\t', '[', ']', '{', '}' };

			string[] inputWords = inputLine.Trim().Split(separators, StringSplitOptions.RemoveEmptyEntries);

			if (inputWords.Length <= 2)
				return null;

			int inputIndex = 0;

			CommandType commandType = CommandType.ndofn;

			//try // parse entity type
			//{
			//    entityType = (EntityType)Enum.Parse(typeof(EntityType), inputWords[inputIndex], /*ignoreCase: */ true);
			//    inputIndex++; // increment only if entity type was successfuly parsed
			//}
			//catch (ArgumentException)
			//{
			//    entityType = null; // if can not parse entity type, it is considered to be skipped, so continue with command type
			//}
			try // parse command type
			{
				commandType = (CommandType)Enum.Parse(typeof(CommandType), inputWords[inputIndex++], /*ignoreCase: */ true);
			}
			catch (ArgumentException)
			{
				throw; // wrong file format - command type expected
			}
			
			string[] patternWords = getPatternString(commandType).Trim().Split(separators, StringSplitOptions.RemoveEmptyEntries);

			PropertyCommand command = new PropertyCommand(commandType);
			command.Type = commandType;

			Dictionary<string, int> patternKeywords = new Dictionary<string, int>();
			for (int i = 0; i < patternWords.Length; i++)
			{
				if (!wordIsVariable(patternWords[i]) && !wordIsRepeatVariable(patternWords[i]))
					patternKeywords[patternWords[i]] = i;
			}
			Dictionary<string, int> repeatVariablesCount = new Dictionary<string, int>();
			string lastVariable = null;
			for (int patternIndex = 0; patternIndex < patternWords.Length; patternIndex++)
			{
				//string inputWord = inputWords[inputIndex];
				string patternWord = patternWords[patternIndex];
				if (wordIsVariable(patternWord)) // variable
				{
					lastVariable = patternWord.Substring(1);

					int count = 0;
					{
						if (!repeatVariablesCount.TryGetValue(lastVariable, out count))
							count = 0;
						if (count > 0 && command.VariableValueMap.ContainsKey(lastVariable))
						{
							string value = command.VariableValueMap[lastVariable];
							command.VariableValueMap.Remove(lastVariable);
							command.VariableValueMap[lastVariable + count] = value;
						}
					}
					repeatVariablesCount[lastVariable] = count + 1;
					string variableName = (count == 0) ? lastVariable : lastVariable + (count + 1);
					command.VariableValueMap[variableName] = inputWords[inputIndex++]; // save variable value
				}
				else // keyword or repeat-variable
				{
					int keywordIndex = 0;
					while (inputIndex < inputWords.Length && !patternKeywords.TryGetValue(inputWords[inputIndex++], out keywordIndex))
					{
						int count = 0;
						{
							if (!repeatVariablesCount.TryGetValue(lastVariable, out count))
								count = 0;
							if (count > 0 && command.VariableValueMap.ContainsKey(lastVariable))
							{
								string value = command.VariableValueMap[lastVariable];
								command.VariableValueMap.Remove(lastVariable);
								command.VariableValueMap[lastVariable + count] = value;
							}
						}
						repeatVariablesCount[lastVariable] = count + 1;
						string variableName = (count == 0) ? lastVariable : lastVariable + (count + 1);
						command.VariableValueMap[variableName] = inputWords[inputIndex - 1]; // save variable value
					}

					if (wordIsRepeatVariable(patternWord))
					{
						string repeatVariable = patternWord.Substring(1);
						int count = 0;
						if (!command.VariableValueMap.ContainsKey(repeatVariable) && repeatVariablesCount.TryGetValue(lastVariable, out count))
							command.VariableValueMap[repeatVariable] = count.ToString();
					}

					if (inputIndex >= inputWords.Length) // end of input
						break;
					patternIndex = keywordIndex;
				}
			}

			return command;
		}

		private static bool wordIsVariable(string word)
		{
			if (string.IsNullOrEmpty(word))
				return false;
			return word[0] == '%';
		}

		private static bool wordIsRepeatVariable(string word)
		{
			if (string.IsNullOrEmpty(word))
				return false;
			return word[0] == '*';
		}

		#endregion

		public string GetCommandPattern()
		{
			return getCommandString(this.type);
		}

		private static string getCommandString(CommandType type)
		{
			return type.ToString() + " " + getPatternString(type);
		}

		private static string getPatternString(CommandType type)
		{
			string pattern = null;
			patterns.TryGetValue(type, out pattern);
			return pattern;
		}

		private static IEnumerable<string> findDistinctVariablesInText(string text)
		{
			string[] parts = text.Split(new char[] { ' ', '\t', '[', ']', '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
			Set<string> variables = new Set<string>();
			foreach (string part in parts)
			{
				if (!string.IsNullOrEmpty(part) && part[0] == '%')
					variables.Add(part);
			}
			return variables;
		}

		private static IEnumerable<string> findDuplicitVariablesInText(string text)
		{
			string[] parts = text.Split(new char[] { ' ', '\t', '[', ']', '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
			Set<string> result = new Set<string>();
			Set<string> variables = new Set<string>();
			foreach (string part in parts)
			{
				if (!string.IsNullOrEmpty(part) && part[0] == '%')
				{
					if (variables.Add(part)) // if already cantained in variables set
						result.Add(part);
				}
			}
			return result;
		}

		public string[] GetAllVariables()
		{
			List<string> allVariables;
			string text = fillPattern(out allVariables); // adds
			return allVariables.ToArray();
		}

		public string FillPattern()
		{
			List<string> allVariablesInText;
			return fillPattern(out allVariablesInText);
		}

		public string fillPattern(out List<string> allVariablesInText)
		{
			Debug.Assert(variableValueMap != null);

			allVariablesInText = new List<string>();

			string text = GetCommandPattern();

			string[] parts = Regex.Split(text, @"(?=[\ \[\]\{\}])");

			Stack<StringBuilder> blocks = new Stack<StringBuilder>();
			blocks.Push(new StringBuilder());

			foreach (string part in parts)
			{
				string partTrimmed = part.Trim();
				if (string.IsNullOrEmpty(partTrimmed))
					continue;
				switch (partTrimmed[0])
				{
					case '[':
						{
							string s = partTrimmed.Substring(1); // remove starting '['
							if (s.StartsWith("%"))
								allVariablesInText.Add(s.Substring(1)); // remove starting '%'
							blocks.Push(new StringBuilder(s + " "));
						}
						break;
					case ']':
						Debug.Assert(blocks.Count > 0);
						string optionalBlock = blocks.Pop().ToString();
						bool allPresent = true;
						foreach (string var in findDistinctVariablesInText(optionalBlock))
						{
							string varName = var.Substring(1); // remove starting '%'
							if (!variableValueMap.ContainsKey(varName) || string.IsNullOrEmpty(variableValueMap[varName]))
							{
								allPresent = false;
								// allVariablesInText.Add(varName); // this is done in "default" case
							}
						}
						if (allPresent)
							blocks.Peek().Append(optionalBlock); // insert back to string
						break;
					case '{':
						{
							string s = partTrimmed.Substring(1); // remove starting '{'
							if (s.StartsWith("%"))
								allVariablesInText.Add(s.Substring(1));  // remove starting '%'
							blocks.Push(new StringBuilder(s + " "));							
						}
						break;
					case '}':
						Debug.Assert(blocks.Count > 0);
						string repeatVar = partTrimmed.Substring(2); // remove starting "}*"
						string repeatBlock = blocks.Pop().ToString();
						int repeatCount;
						string repeatCountString;
						if (variableValueMap.TryGetValue(repeatVar, out repeatCountString) && int.TryParse(repeatCountString, out repeatCount))
						{
							for (int i = 0; i < repeatCount; i++)
							{
								blocks.Peek().Append(repeatBlock);
							}
							if (repeatCount <= 0)
							{
								// remove all variables in block from allVariablesInText
								foreach (string var in findDistinctVariablesInText(repeatBlock))
								{
									string varName = var.Substring(1); // remove starting '%'
									allVariablesInText.Remove(varName);
								}
							}
						}
						else
						{
							if (!allVariablesInText.Contains(repeatVar))
								allVariablesInText.Add(repeatVar);
						}
						break;
					default:
						{
							if (partTrimmed.StartsWith("%"))
								allVariablesInText.Add(partTrimmed.Substring(1)); // remove starting '%'
							blocks.Peek().Append(partTrimmed + " ");
						}
						break;
				}
			}

			Debug.Assert(blocks.Count == 1);

			string result = blocks.Pop().ToString().Trim();

			result = addNumericSuffixesToDuplicitVariables(result, allVariablesInText);

			return result;
		}

		private string addNumericSuffixesToDuplicitVariables(string text, List<string> allVariablesInText)
		{
			string result = text;
			foreach (string variable in findDuplicitVariablesInText(text))
			{
				allVariablesInText.Remove(variable.Substring(1));
				int count = 0;
				int index = 0;
				while ((index = result.IndexOf(variable, index)) >= 0)
				{
					++count;
					index += variable.Length;
					result = result.Insert(index, count.ToString());

					allVariablesInText.Add(variable.Substring(1) + count);
				}
			}
			return result;
		}
		
		public override string ToString()
		{
			string result = FillPattern();
			// replace %variables with their values in variableValueMap
			foreach (KeyValuePair<string, string> pair in variableValueMap)
			{
				result = replaceVariableWithValue(result, "%" + pair.Key, pair.Value);
			}
			return result;
		}

		private string replaceVariableWithValue(string text, string variable, string value)
		{
			//return text.Replace(variable, value);
			int index = 0;
			string result = text;
			while ((index = result.IndexOf(variable, index)) >= 0)
			{
				if ((index + variable.Length) < (result.Length - 1) && char.IsLetterOrDigit(result[index + variable.Length]))
				{
					index += variable.Length;
					continue;
				}
				result = result.Remove(index, variable.Length).Insert(index, value);
			}
			return result;
		}

	}
}
