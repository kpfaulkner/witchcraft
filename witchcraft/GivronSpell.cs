using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SpellingConsole
{
	
	class Band
	{
		public long min = 0;
		public long max =0 ;
		public long band = 0;
		
	}
	
    class GivronSpell
    {

        // map of word to count of words.
        Dictionary<string, long > dictionary = new Dictionary<string, long>();


        // can find alternate.
        string alphabet = "abcdefghhijklmnopqrstuvwxyz";

        private long dictionaryCutOff = 1000;
		private long dictionaryNormaliser = 1000000;
		
        // ranker...
        private Ranker ranker = new Ranker();
		
		// band information for initial ranks.
		private List< Band > bandInfo = new List<Band>();
		
        public GivronSpell()
        {
            ReadConfig();

			//dictionary = LoadDictionaryORIG();
            UpdateDictionary("1gram-sorted");
            //UpdateDictionary("2gram");
        }

        


        private void ReadConfig()
        {


            System.Configuration.AppSettingsReader aps = new System.Configuration.AppSettingsReader();
            dictionaryCutOff = (long)aps.GetValue("DictionaryCutOff", typeof(System.Int64));
			dictionaryNormaliser = (long)aps.GetValue("DictionaryNormaliser", typeof(System.Int64));
			
			// read band info, for ranking purposes.
			var c = 1;
			while (true)
			{
				try
				{
					var key = "band"+ c.ToString();
					var s = (string)aps.GetValue( key , typeof(System.String));
					var sp = s.Split(':');
					var b = new Band();
					
					b.min = sp[0].GetLongElse( 0);
					b.max = sp[1].GetLongElse( 0);
					b.band = sp[2].GetLongElse(0);
					
					bandInfo.Add( b );
					
					c++;
				}
				catch( Exception e)
				{
					// HACK!!!!
					break;
				}
			
						
			}
			
			Console.WriteLine("bandinfo length "+ bandInfo.Count.ToString() );
        }
		
		private long FindBandForRawRank( long rawRank)
		{
			
			
			// not sure this is working.
			//long res = bandInfo.Where( n => n.min >= rawRank ).Where( n => n.max >= rawRank).Select( n => n.band ).First();
			
			long res = 0 ;
			foreach( var i in bandInfo)
			{
				if ( i.min <= rawRank && rawRank <= i.max)
				{
					res = i.band;
					break;
				}
			}
			
			
			return res;
				
		}
		
		// used to check single word.
		public bool WordExists( string word )
		{
		
			return dictionary.ContainsKey( word );
			
		}
		
		
        Dictionary<string, int> LoadDictionaryORIG()
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int>();

            using (FileStream fs = File.OpenRead("dictionary.txt"))
            {
                using (TextReader reader = new StreamReader(fs))
                {
                    var quit = false;

                    while (!quit)
                    {

                        var data = reader.ReadLine();

                        if (data == null)
                        {

                            break;
                        }
                        // split the line.
                        var sp = data.Split();
                        foreach (string s in sp)
                        {

                            int count = dictionary.getOrElse(s, 0);
                            count++;
                            dictionary[s] = count;

                        }

                    }


                }
            }

            return dictionary;
        }

        // modified the existing dictionary member variable.
        private void UpdateDictionary( string filename)
        {

            using (FileStream fs = File.OpenRead(filename ))
            {
                using (TextReader reader = new StreamReader(fs))
                {
                    var quit = false;
					
					var cc = 0;
					
                    while (!quit)
                    {
						
						cc++;
						
						if ( cc % 100000 == 0)
						{
							Console.WriteLine( cc );	
						}
                        var data = reader.ReadLine();

                        try
                        {

                            if (data == null)
                            {

                                break;
                            }


                            var idx = 0;
                            while (data[idx] == ' ')
                            {
                                ++idx;
                            }

                            var data2 = data.Substring(idx);

                            var indexOfSpaceAfterCount = data2.IndexOf(' ');
                            var count = Convert.ToInt64(data2.Substring(0, indexOfSpaceAfterCount));

                            if (count > dictionaryCutOff)
                            {
								
								//count = count / dictionaryNormaliser;
								
								count = FindBandForRawRank( count );
								
								// modify count by dividing by some divisor.
                                var rest = data2.Substring(indexOfSpaceAfterCount + 1);


                                // remove punctuation.
                                Regex rgx1 = new Regex("[-,.\"']");
                                var data3 = rgx1.Replace(rest, "");

                                // check for illegal chars in rest
                                // if no illegal chars, then store.
                                Regex rgx = new Regex("[^a-z ]");
                                var data4 = rgx.Replace(data3, "");
								
								if (data4 == "tax")
								{
									Console.WriteLine("TAX TAX TAX");
									
								}
                                if (data3 == data4)
                                {
                                    dictionary[data3] = count;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            // ugly catch all, but just want to be able to skip a single line that causing us an issue.
							Console.WriteLine("EX "+ e.Message);
                        }
                    }


                }
            }

        }


        // return enumerable of tuples.
        IEnumerable<Tuple<string, string>> split(string word)
        {
            foreach (int i in Enumerable.Range(1, word.Length))
            {
                yield return new Tuple<string, string>(word.Substring(0, i), word.Substring(i));
            }
        }



        HashSet<Token> Edits1(List<Token> tokenList)
        {

            var hs = new HashSet<Token>();

            // make sure original is added.
            foreach (var w in tokenList)
            {
                hs.Add(w);
            }

            // deletion.
            foreach (var w in tokenList)
            {
                //Console.WriteLine("word: " + w.term);
                foreach (Tuple<string, string> t in split( w.term  ))
                {
                    if (t.Item2 != null && t.Item2 != "")
                    {
                        var newWord = t.Item1 + t.Item2.Substring(1);
                        var token = new Token( newWord );
                        token.origTerm = w.origTerm;
						
						var mod = new Modification();
						mod.modType = ModificationType.Delete;
						mod.origChar = t.Item2[0];
						
						token.modifications = w.modifications.GetRange(0, w.modifications.Count);
						
						token.modifications.Add( mod );
                        hs.Add(token);
                    }
                }

            }

            // transposes
            foreach (var w in tokenList)
            {
                foreach (Tuple<string, string> t in split(w.term))
                {
                    if (t.Item2.Length > 1)
                    {
						
						// only if two letters aren't the same
						if ( t.Item2[0] != t.Item2[1] )
						{
	                        var newWord = t.Item1 + t.Item2[1] + t.Item2[0] + t.Item2.Substring(2);
	                        var token = new Token(newWord);
	                        token.origTerm = w.origTerm;
							
					        var mod = new Modification();
							mod.modType = ModificationType.Transpose;
							mod.origChar = t.Item2[0];
							mod.newChar = t.Item2[1];
							token.modifications = w.modifications.GetRange(0, w.modifications.Count);
							token.modifications.Add( mod );
							
	                        hs.Add(token);
						}
                    }
                }
            }


            // replacement.
            foreach (var w in tokenList)
            {
                foreach (Tuple<string, string> t in split(w.term))
                {
                    if (t.Item2 != "")
                    {
                        foreach (char c in alphabet)
                        {
                            // only replace if different char.
							if ( c != t.Item2[0] )
							{
	                            var newWord = t.Item1 + c + t.Item2.Substring(1);
	                            var token = new Token(newWord);
	                            token.origTerm = w.origTerm;
	                            
								var mod = new Modification();
								mod.modType = ModificationType.Replace;
								mod.origChar = t.Item2[0];
								mod.newChar = c;
								token.modifications = w.modifications.GetRange(0, w.modifications.Count);
								token.modifications.Add( mod );
								
	                            hs.Add(token);
							}
							
                        }
                    }

                }

            }

            // inserts.
            foreach (var w in tokenList)
            {
                foreach (Tuple<string, string> t in split(w.term))
                {
                    if (t.Item2 != "")
                    {
                        foreach (char c in alphabet)
                        {
                            var newWord = t.Item1 + c + t.Item2;
                            var token = new Token(newWord);
                            token.origTerm = w.origTerm;
                            //Console.WriteLine("orig term " + token.origTerm );
							//Console.WriteLine("new term " + token.term );
							
							
						    var mod = new Modification();
							mod.modType = ModificationType.Insert;
							mod.origChar = c;
							token.modifications = w.modifications.GetRange(0, w.modifications.Count);
							token.modifications.Add( mod );
                            hs.Add(token);
                       
                        }
                    }
                    else
                    {
                        // just appending to end... seem legit.
                        foreach (char c in alphabet)
                        {
                            var newWord = t.Item1 + c;
                            var token = new Token(newWord);
                            token.origTerm = w.origTerm;
						    var mod = new Modification();
							mod.modType = ModificationType.Insert;
							mod.origChar = c;
							//Console.WriteLine("orig term " + token.origTerm );
							//Console.WriteLine("new term " + token.term );
							
							token.modifications = w.modifications.GetRange(0, w.modifications.Count);
							token.modifications.Add( mod );
                            hs.Add(token);
                     
                        }

                    }

                }

            }


            return hs;

        }


        HashSet<Token> EditAgain(HashSet<Token> wordSet, int editNumber)
        {
            var hs = new HashSet<Token>();

            hs = Edits1(wordSet.ToList());

            return hs;
        }


        // v bad performance here...
        private List<Token> GetTokensForWord(IEnumerable<Token> tokens, string word)
        {
            var l = new List<Token>();

            foreach (var t in tokens)
            {
                if (t.term == word)
                {
                    l.Add(t);
                }
            }
            return l;
        }

        public List<Token> correct(string word)
        {
            var token = new Token( word );
            token.origTerm = word;
			var result = new List<Token>();
			
			Console.WriteLine("correct : " + word );
			
			// if its an number, just leave it alone.
			if (!word.IsInt() )
			{
				
            	var hs = Edits1(new List<Token>() { token });
			
	            // add original word
	            hs.Add(token);
	
	            var hs2 = EditAgain(hs, 2);
	            
				// cant just add them? tried union, but returns IEnumerable.
				foreach( var s in hs2)
				{
					hs.Add( s ) ;	
				}
				       
				var finalHS = hs;
			
	            //Console.WriteLine("dictionary size " + dictionary.Count.ToString());
				
				//Console.WriteLine("result size " + finalHS.Count.ToString() );
	       
	
	
	            var wordDict = new Dictionary<string, List<Token>>();
	
	            // old fashioned way
	            // get every single word that is legit.
	            // add to dictionary.
	            // determine "best" version of each word (ie, highest score for each of the tokens)
	            foreach (Token s in finalHS)
	            {
					//Console.WriteLine("testing {0}", s.term );
					
	                if (dictionary.ContainsKey(s.term ))
	                {
						//Console.WriteLine("{0} in dictionary", s.term );
						
	                    // initial score.
	                    s.score = dictionary.getOrElse(s.term, 1);
						
						//Console.WriteLine("term score :" + s.term + " : " + s.score.ToString() );
						
	                    // this will create a new list for every single call... v v v wasteful. Need to modify.
	                    var l = wordDict.getOrElseAssign(s.term, new List<Token>() );
	                    l.Add(s);
	                    
	                }
	            }
	
	            // now for each collection of tokens that all represent the same word, go and rank them, and only take "the best"
	            foreach (var k in wordDict.Keys)
	            {
	
	                // all same word.
	                var l = wordDict[k];
	
	                var t = ranker.RankWordTokens(l);
	
	                // should just be BEST version of the word.
	                result.Add(t);
	
	            }
			}
			else
			{
				token.score = 1.0;
				result.Add( token );	
			}
            return result.ToList<Token>();
        }


        // SCORE definition. (unsure if correct or not, but this is my current understanding).
        //
        // score member var will be probability a given word variation is the correct one we want.
        // eg, if input is "foo", and we manage to create 100 variations of "foo". 
        //     we're only interested in the top 10 variations (say).
        //     What we'll do is return the top 10, but adjust their scores so the "probability" is only based on these 10.
        //     so, if they all have the scores of 4,4,4,3,3,3,2,2,2,1  then the scores adjusted will be:
        //     total = 4+4+4+3+3+3+2+2+2+1 == 28
        //     each entry will then be 4/28, 4/28, 4/28, 3/28, 3/28.... 1/28
        //     ie. .142, .142, .142, .107,........ .035
        public List<Token> TopNCorrect(string word, int n)
        {
            var l = correct(word);
            List<Tuple<string,int>> l2 = new List<Tuple<string,int>>();

            // rank.
			//Console.WriteLine("before sort " + l.Count.ToString() );
            l.Sort(delegate(Token a, Token b) { return a.score.CompareTo( b.score ); });
			//Console.WriteLine("after sort " + l.Count.ToString() );

            l.Reverse();

            // top 5.?
			//Console.WriteLine("result reverse  " + l.Count.ToString() );
            double totalScore = 0;
			
			var len = l.Count;
			
            for (int i = 0; i < n; ++i)
            {
				
				if ( i < len )
				{
	                var s = l[i];
	
	                //Console.WriteLine("a: "+s.term + " : " +s.score.ToString() );
					
					foreach( var m in s.modifications)
					{
						//Console.WriteLine("mod " + m.modType +" : " +m.origChar +" : " + m.newChar);
					}
					totalScore += s.score;
				}
            }

            List<Tuple<Token,double>> l3 = new List<Tuple<Token, double>>();
            for (int i = 0; i < n; ++i)
            {
				if ( i < len )
				{
	                var s = l[i];
		
	                // score is the likelihood that this "version" of the word is the right one.
					s.score = s.score / totalScore;	
					Console.WriteLine("t2 " + s.term + " : " +s.score.ToString() );
	            
				}
			}
            return l.GetRange(0, Math.Min(n, l.Count()));

        }




    }

    class MainClass
    {
        public static void Main1(string[] args)
        {
            //var word = args[0];

            var word = "the";
            var givron = new GivronSpell();

            var sw = new Stopwatch();
            sw.Start();
            var l = givron.TopNCorrect(word, 100);
            sw.Stop();

            TimeSpan ts = sw.Elapsed;
			
			foreach( var i in l )
			{
				Console.WriteLine("res: " + i.term);
			}

            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds);
        }
    }
}

