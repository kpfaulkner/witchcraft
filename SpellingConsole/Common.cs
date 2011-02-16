using System;
using System.Collections.Generic;

namespace SpellingConsole
{
	public enum ModificationType {None, Insert, Delete, Transpose, Replace};
	
	public class Modification
	{
	
		public ModificationType modType = ModificationType.None;
		public char origChar = ' ';
		public char newChar = ' ';
	
	}
	
	
	public class ResultQuery
	{
		public string query;
		public double score;
	}
	
    public class Token
    {
        public Token(string t)
        {

            term = t;

        }

        // term user entered.
        public string origTerm { get; set; }

        // term we *think* it could be
        public string term { get; set; }

		// modifications that have happened.
		public List<Modification> modifications = new List<Modification>();
		
		// score!!!!
		public double score = 0.0;
		
    }
	
}

