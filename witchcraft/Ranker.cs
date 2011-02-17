using System;
using System.Linq;
using System.Collections.Generic;


namespace SpellingConsole
{
    class Ranker
    {

        private double maxTermsInQuery = 10.0;
        private double multiplierReductionPerChange = 0.95;
        private double multiplierReductionPerDeletion = 0.95;
        private double multiplierReductionPerReplace = 0.95;
        private double multiplierReductionPerTransposition = 0.95;
        private double multiplierReductionPerInsertion = 0.95;
        private double multiplierIncreaseForCorrectSpelling = 1.05;

        // map of word to count of words.
        private Dictionary<string, double > dictionary = null;

		
		public Ranker( Dictionary<string, double> d  )
		{
			ReadConfig();
			
            dictionary = d;
		}
		
        private void ReadConfig()
        {


            System.Configuration.AppSettingsReader aps = new System.Configuration.AppSettingsReader();

            
            maxTermsInQuery = (double)aps.GetValue("MaxTermsInQuery", typeof(System.Double));
            multiplierReductionPerChange = (double)aps.GetValue("MultiplierReductionPerChange", typeof(System.Double));
            multiplierReductionPerDeletion = (double)aps.GetValue("MultiplierReductionPerDeletion", typeof(System.Double));
            multiplierReductionPerReplace = (double)aps.GetValue("MultiplierReductionPerReplace", typeof(System.Double));
            multiplierReductionPerTransposition = (double)aps.GetValue("MultiplierReductionPerTransposition", typeof(System.Double));
            multiplierReductionPerInsertion = (double)aps.GetValue("MultiplierReductionPerInsertion", typeof(System.Double));
            multiplierIncreaseForCorrectSpelling = (double)aps.GetValue("MultiplierIncreaseForCorrectSpelling", typeof(System.Double));
        }

        private int GetModificationTypeCount(Token t, ModificationType modType )
        {
            int c = 0;
            foreach (var m in t.modifications)
            {
                if (m.modType == modType)
                {
                    ++c;
                }
            }

            return c;

        }

        // all input tokens are of the same word.
        // but they might have been generated using a different method.
        // need to rank each of these versions and just return
        // the one that will be used....  ie, the one that will have the highest score.
        public Token RankWordTokens(IEnumerable<Token> wordVariationList)
        {

            Token t = null;

            List<Token> tokenList = new List<Token>();

            foreach (var token in wordVariationList)
            {

                // if tokens term is "" then its just a dummy. ugly but required atm.
                if (token.term != "")
                {
					
					//Console.WriteLine("term " + token.term );
					//Console.WriteLine("orig score " + token.score.ToString() );
					

                    // if term is same as original term, then increase percentage
                    if (token.term == token.origTerm)
                    {
                        //token.score = 1.0;  // FIXME: utter bollocks but needs to rank fairly high unless another high ranking tuple effects this?
                        // UNSURE.
                        //token.score = System.UInt64.MaxValue / 2;
						token.score = token.score * multiplierIncreaseForCorrectSpelling;
                        
                    }
                    else
                    {
                        // orig score is just based off dictionary popularity.
                        var origScore = token.score;

                        // get the score for the NEW word that was created.
                        var newScore = dictionary.getOrElse(token.term, 1.0);

                        // adjustment based on # modifications.
                        // should adjustment be % of existing or totals added/removed?
						
						if (token.modifications.Count > 0)
						{
                        	newScore = newScore *  Math.Pow(multiplierReductionPerChange , token.modifications.Count);
						}
						
                        // adjustment based on deletions
						var deleteCount = GetModificationTypeCount(token, ModificationType.Delete);
						if (deleteCount > 0)
						{
                        	newScore = newScore *  Math.Pow(multiplierReductionPerDeletion , deleteCount );
						}
						
						
                        // adjustment based on insertions
						var insertCount = GetModificationTypeCount(token, ModificationType.Insert);
						if (insertCount > 0)
						{
                        	newScore = newScore *  Math.Pow(multiplierReductionPerInsertion , insertCount);
						}
						
                        // adjustment based on transposition
						var transposeCount = GetModificationTypeCount(token, ModificationType.Transpose);
						
						if (transposeCount > 0)
						{
                        	newScore = newScore *   Math.Pow(multiplierReductionPerTransposition , transposeCount);
						}
						
                        // adjustment based on replace
						var replaceCount = GetModificationTypeCount(token, ModificationType.Replace);
						if (replaceCount > 0)
						{
                        	newScore = newScore *  Math.Pow(multiplierReductionPerReplace , replaceCount);
						}
						
                        // just simple for now.
                        token.score = newScore;

                    }
					//Console.WriteLine("new score " + token.score.ToString() );
				
                    tokenList.Add(token);

                }
            }

            tokenList.Sort(delegate(Token a, Token b) { return a.score.CompareTo(b.score); });
            tokenList.Reverse();

            // just get first.
            t = tokenList[0];

            return t;
        }


    }
}