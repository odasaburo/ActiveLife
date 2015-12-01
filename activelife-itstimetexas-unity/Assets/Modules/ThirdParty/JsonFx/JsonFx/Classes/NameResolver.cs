using System;
using System.Collections.Generic;

namespace JsonFx.Json
{
	public abstract class NameResolver
	{
		public static string JsonToCSharp (string name)
		{
			string[] words = SplitWords (name);
			for (int i = 0; i < words.Length; i++)
			{
				string word = words[i];
				if (word.Length <= 1)
				{
					words[i] = word.ToUpper();
				}
				else
				{
					words[i] = Char.ToUpper(word[0]) + word.Substring(1).ToLower();
				}
			}
			return String.Join("", words);
		}

		public static string CSharpToJson (string name)
		{
			string[] words = SplitWords (name);
			for (int i = 0; i < words.Length; i++)
			{
				string word = words [i];
				words [i] = word.ToLower ();
			}
			return String.Join("_", words);
		}

		private static string[] SplitWords(string multiword)
		{
			if (String.IsNullOrEmpty(multiword))
			{
				return new string[0];
			}
			
			List<string> words = new List<string>(5);
			
			// treat as capitalized (even if not)
			bool prevWasUpper = true;
			
			int start = 0,
			length = multiword.Length;
			
			for (int i=0; i<length; i++)
			{
				if (!Char.IsLetterOrDigit(multiword, i))
				{
					// found split on symbol char
					if (i > start)
					{
						words.Add(multiword.Substring(start, i-start));
					}
					start = i+1;
					prevWasUpper = true;
					continue;
				}
				
				bool isLower = Char.IsLower(multiword, i);
				
				if (prevWasUpper)
				{
					if (isLower && (start < i-1))
					{
						// found split
						words.Add(multiword.Substring(start, i-start-1));
						start = i-1;
					}
				}
				else if (!isLower)
				{
					// found split
					words.Add(multiword.Substring(start, i-start));
					start = i;
				}
				
				prevWasUpper = !isLower;
			}
			
			int remaining = length-start;
			if (((remaining == 1) && Char.IsLetterOrDigit(multiword[start])) || (remaining > 1))
			{
				words.Add(multiword.Substring(start));
			}
			
			return words.ToArray();
		}
	}
}

