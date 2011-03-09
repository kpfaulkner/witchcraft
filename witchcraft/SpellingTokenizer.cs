using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;


namespace SpellingConsole
{
    public class Variation : IComparable<Variation>
    {
        private List<string> original = null;
        private List<string> Original
        {
            get { return original; }
            set { original = value; }
        }
        private List<Tuple<string, double>> s;
        public string S
        {
            get
            {
                string ss = "";
                foreach (var i in s)
                {
                    if (i == s.ElementAt(0))
                    {
                        ss += i.Item1;
                    }
                    else
                    {
                        ss += " " + i.Item1;
                    }
                }
                return ss;
            }
        }
        public string Sdebug
        {
            get
            {
                string ss = "";
                foreach (var i in s)
                {
                    string result = i.Item1 + "[" + i.Item2 + "]";
                    if (i == s.ElementAt(0))
                    {
                        ss += result;
                    }
                    else
                    {
                        ss += " " + result;
                    }
                }
                return ss;
            }
        }

        private double score;
        public double Score
        {
            get { return score; }
            //set { score = value; }
        }
        public Variation(List<Tuple<string, double>> s, List<string> original)
        {
            this.original = original;
            this.s = s;
            double n = 1;
            //double d = 0;
            for (int i = 0; i < s.Count(); i++)
            {
                var t = s.ElementAt(i);
                if (t.Item1 == original.ElementAt(i))
                {
                    n = n * Math.Pow(t.Item2, 0.25);
                }
                else
                {
                    n = n * t.Item2;
                }

            }
            this.score = n;
        }

        public int CompareTo(Variation other)
        {
            return this.Score.CompareTo(other.Score);
        }

    }

    public class ScoreComparer : IComparer<double>
    {
        public int Compare(double x, double y)
        {
            return x.CompareTo(y);
        }

    }

    class Band
    {
        public long min = 0;
        public long max = 0;
        public long band = 0;

    }


    public class SpellingTokenizer
    {

        private int maxQueryTerms = 10;
        private static GivronSpell givron = null;
        private static Ranker ranker = null;

        private List<string> tokens = null;
        delegate int comp(Variation v1, Variation v2);
        private int maxCombinationsPerToken = 100;
        private Dictionary<string, double> dictionary = new Dictionary<string, double>();

        // band information for initial ranks.
        private List<Band> bandInfo = new List<Band>();
        private long dictionaryCutOff = 1000;
       	
		private long dictionaryNormaliser = 0;
		private double bigramMultiplier = 0.0;
		
        public SpellingTokenizer()
        {
            ReadConfig();

            if (dictionary.Count == 0)
            {
                UpdateDictionary("dictionary.txt");
				
            }

            if (ranker == null)
            {
                ranker = new Ranker(dictionary);
            }

            if (givron == null)
            {

                // givron has access to dictionary.
                givron = new GivronSpell( dictionary, ranker );
                
            }

            
            

        }

        private long FindBandForRawRank(long rawRank)
        {


            // not sure this is working.
            //long res = bandInfo.Where( n => n.min >= rawRank ).Where( n => n.max >= rawRank).Select( n => n.band ).First();

            long res = 0;
            foreach (var i in bandInfo)
            {
                if (i.min <= rawRank && rawRank <= i.max)
                {
                    res = i.band;
                    break;
                }
            }


            return res;

        }

        // modified the existing dictionary member variable.
        private void UpdateDictionary(string filename)
        {

            using (FileStream fs = File.OpenRead(filename))
            {
                using (TextReader reader = new StreamReader(fs))
                {
                    var quit = false;

                    var cc = 0;

                    while (!quit)
                    {

                        cc++;

                        if (cc % 100000 == 0)
                        {
                            Console.WriteLine(cc);
                        }
                        var data = reader.ReadLine();
						
						//Console.WriteLine("data " + data );
						
                        try
                        {

                            if (data == null)
                            {

                                break;
                            }

							
							if (false )
							{
                            var idx = 0;
                            while (data[idx] == ' ')
                            {
                                ++idx;
                            }

                            var data2old = data.Substring(idx);
							}
							
							var data2 = data;
							
								
                            var indexOfSpaceAfterCount = data2.IndexOf('\t');
                            var count = Convert.ToInt64(data2.Substring(0, indexOfSpaceAfterCount));
							
							//Console.WriteLine("count is " + count.ToString() );
							
                            if (count > dictionaryCutOff)
                            {

                                //count = count / dictionaryNormaliser;

                                count = FindBandForRawRank(count);

                                // modify count by dividing by some divisor.
                                var rest = data2.Substring(indexOfSpaceAfterCount + 1);


                                // remove punctuation.
                                Regex rgx1 = new Regex("[-,.\"']");
                                var data3 = rgx1.Replace(rest, "");

                                // check for illegal chars in rest
                                // if no illegal chars, then store.
                                Regex rgx = new Regex("[^a-z ]");
                                var data4 = rgx.Replace(data3, "");
								
								//Console.WriteLine("data3 is " + data3 );
								
                                if (data3 == data4)
                                {
                                    dictionary[data3] = count;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            // ugly catch all, but just want to be able to skip a single line that causing us an issue.
                            Console.WriteLine("EX " + e.Message);
                        }
                    }


                }
				
            }
			
			var a = dictionary.getOrElse("lesson plan", 123.0);
			Console.WriteLine("XXX lesson plan test " + a.ToString() );
			
			
        }

        private void ReadConfig()
        {


            System.Configuration.AppSettingsReader aps = new System.Configuration.AppSettingsReader();


            maxCombinationsPerToken = (int)aps.GetValue("MaxCombinationsPerToken", typeof(System.Int32));
            dictionaryCutOff = (long)aps.GetValue("DictionaryCutOff", typeof(System.Int64));
            dictionaryNormaliser = (long)aps.GetValue("DictionaryNormaliser", typeof(System.Int64));
			
			bigramMultiplier = (double) aps.GetValue("BigramMultiplier", typeof( System.Double) );
			
				
            // read band info, for ranking purposes.
            var c = 1;
            while (true)
            {
                try
                {
                    var key = "band" + c.ToString();
                    var s = (string)aps.GetValue(key, typeof(System.String));
                    var sp = s.Split(':');
                    var b = new Band();

                    b.min = sp[0].GetLongElse(0);
                    b.max = sp[1].GetLongElse(0);
                    b.band = sp[2].GetLongElse(0);

                    bandInfo.Add(b);

                    c++;
                }
                catch (Exception e)
                {
                    // HACK!!!!
                    break;
                }


            }
        }

        // just generates the final query strings and combined rank.
        private List<ResultQuery> GenerateQueries(IEnumerable<List<Token>> candidates, int n)
        {
            Console.WriteLine("GenerateQueries start");

            var l = new List<ResultQuery>();

            var dict = new Dictionary<string, ResultQuery>();

            var count = 0;
            var sb = new StringBuilder();
            var comp = new ScoreComparer();

            var sortedListOfScores = new SortedList<double, string>(comp);
			
			var sortedListOfScores2 = new List< Tuple< string, double> >();
			
            var random = new Random();
			//Console.WriteLine("num candidates " + candidates.Count.ToString() );
			
            foreach (var c in candidates)
            {
				if ( count % 10000 == 0)
				{
                	Console.WriteLine("candidate " + (count++).ToString() );
				}
				sb.Clear();

                var score = 1.0;
                var previousWord = "";
				var bigramScoreAdjustment = 1.0;
				var bigramScore = 1.0;
				foreach (var t in c)
                {
					
				
					if ( previousWord == "")
					{
						previousWord = t.term;	
						bigramScore = 1.0;
					}
					else
					{
						var bigram = previousWord + " "+ t.term;
						
						previousWord = t.term;
						
						// bigram score.
						bigramScore = dictionary.getOrElse( bigram, 0);
						
						// totals of all bigrams added.
						bigramScoreAdjustment += bigramScore;
						
						
					}
					
                    if (t.term != "")
                    {
                        sb.Append(t.term);
                        sb.Append(" ");
                        score *= t.score;
						
						
					}

					

                }
				
				
				// just trying out ideas. A string with more acceptable bigrams gets a bigger score.
				score = score * bigramScoreAdjustment;
				
                var term = sb.ToString().Trim();



                // if sorted list is too long (greater than n). then trim.
                // unsure of perf issues here.
				
				if (true)
                {
                    // if sorted list not to max size, then just add.
                    if (sortedListOfScores2.Count < n)
                    {
						
						var t = new Tuple<string, double>( term, score);
						
						sortedListOfScores2.Add( t );
							
                    }
                    else
                    {
						
						// nth score.
						//var nthScore = sortedListOfScores2.
						if ( sortedListOfScores2.Count >=n )
						{
							//Console.WriteLine("going to check {0} against {1}", n.ToString(), sortedListOfScores2.Count.ToString() );
							
							var nthScore = 0.0;
							
							if ( sortedListOfScores2.Count > n)
							{
								nthScore = sortedListOfScores2[n].Item2;
							}
					
							//Console.WriteLine("done check");
							
							if ( score > nthScore )
							{
								// unsure if can modify list while iterating through it, so will just make note of the index.
								var indexToInsert = 0;
								
								// loop through linearly and find location for score.
								foreach( var i in sortedListOfScores2)
								{
									Console.WriteLine("looping through sorted list of scores");
									if ( score > i.Item2 )
									{
										break;					
									}
									
									indexToInsert++;
								}
								
								var t = new Tuple<string, double>( term, score);
								
								// indexToInsert is the location to insert new index.
								sortedListOfScores2.Insert( indexToInsert, t);
								
								Console.WriteLine( "inserted " + t.ToString() + " into " + indexToInsert.ToString() );
							}
						}
                    }
                }
				
				/*
                if (true)
                {
                    // if sorted list not to max size, then just add.
                    if (sortedListOfScores.Count < n)
                    {

                        // UTTER HACK... but just making sure 2 entries aren't the same.
                        var delta = 0.0001 * random.Next(1, 10000);
                        score = score - delta;
						var done = false;
						
						// HACK
						while (!done)
						{
							try
							{
								score -= 0.0001;
                        		sortedListOfScores.Add(score, term);
								done = true;
							
							}
							catch(System.ArgumentException ex )
							{
								
							}
						}
							
                    }
                    else
                    {
                        // check if greater than n-1th position.
                        var entry = sortedListOfScores.ElementAt(n - 1);
                        var existingScore = entry.Key;

                        if (score > existingScore)
                        {
                            var delta = 0.00000001 * random.Next(1, 10000);
                            score = score - delta;
							
							var done = false;
							
							// HACK
							while (!done)
							{
								try
								{
									score -= 0.0000001;
                            		sortedListOfScores.Add(score, term);
									done = true;
								
								}
								catch(System.ArgumentException ex )
								{
									
								}
							}
							
							
                            // remove entry at n.
                            // doing this all the time probably inefficient.
                            //sortedListOfScores.RemoveAt( n );
                        }
                    }
                }
                */

            }

            foreach (var i in sortedListOfScores2)
            {

                var rq = new ResultQuery();
                rq.query = i.Item1;
                rq.score = i.Item2;

                l.Add(rq);
            }

            Console.WriteLine("about to sort generated queries");

            // sort and get top n.
            l.Sort(delegate(ResultQuery a, ResultQuery b) { return a.score.CompareTo(b.score); });
            l.Reverse();

            Console.WriteLine("GenerateQueries end");

            return l.GetRange(0, Math.Min(n, l.Count));

        }


        public List<ResultQuery> BestN(string s, int n)
        {

            tokens = new List<string>(s.Split());
            List<List<Token>> chain = new List<List<Token>>();
            List<Variation> variations = new List<Variation>();

			// put each variant of the words in the chain list.
			// eg, convert "foo" to "foot", "foe" etc etc...
            foreach (var word in tokens)
            {
                List<Token> wordVariantList = givron.TopNCorrect(word, maxCombinationsPerToken);
				
				Console.WriteLine("term {0} generated {1}", word, wordVariantList.Count.ToString() );
				
                chain.Add(wordVariantList);
            }

            // get all combinations.
            // FIXME  STUPID STUPID LIMITATION OF MY LINQ-FOO.
            // Assume up to 10 terms per query maximum.
            // add extra empty lists to chain.
            var chainSize = chain.Count;
            for (int i = 0; i < maxQueryTerms - chainSize; ++i)
            {
                var dummyList = new List<Token>() { new Token("") };
                chain.Add(dummyList);
            }

            // general all variations.
            var results = from val0 in chain[0]
                          from val1 in chain[1]
                          from val2 in chain[2]
                          from val3 in chain[3]
                          from val4 in chain[4]
                          from val5 in chain[5]
                          from val6 in chain[6]
                          from val7 in chain[7]
                          from val8 in chain[8]
                          from val9 in chain[9]
                          select new List<Token>() { val0, val1, val2, val3, val4, val5, val6, val7, val8, val9 };

            Console.WriteLine("Generated all combinations ");

            // rank all the results....  this is where all the tuning will go.
            //ranker.Rank( results );

            // generate result strings for top n results 
            var finalQueries = GenerateQueries(results, n);
			
			// hack modification for " s" situation. ie, where apostrophies occur.
			ModifyBasedOnS( finalQueries) ;
			
			// only adjust ranks of EXISTING entries based on bigrams....  and NOT trying to use bigram entries 
			// on possible word combinations at an earlier stage.
			ModifyBasedOnBigrams( finalQueries );
			


            return finalQueries;

        }
		
		private void  ModifyBasedOnBigrams( List<ResultQuery> queryList )
		{
			// go through every pair of words and check for score adjustments.
			

			
			
		
		}

		// no idea on this one yet.
		private void  ModifyBasedOnS( List<ResultQuery> queryList )
		{
		
		}
		
        public static List<Tuple<string, string>> LoadTestData(string filename)
        {
            var l = new List<Tuple<string, string>>();

            using (FileStream fs = File.OpenRead(filename))
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
                        var sp = data.Split('\t');

                        var t = new Tuple<string, string>(sp[0].Trim(), sp[1].Trim());


                        l.Add(t);

                    }


                }
            }

            return l;
        }



        public static void Main(string[] args)
        {

            var a = args[0];
			
			//var a= "women s financial lesson plan";
            //var a = "receipt";

            var st = new SpellingTokenizer();


            if (a.StartsWith("test"))
            {
                List<Tuple<string, string>> l = null;

                if (a == "test")
                {
                    l = LoadTestData("trec.txt");
                }

                if (a == "test2")
                {
                    l = LoadTestData("trec-error.txt");
                }


                var sw = new Stopwatch();
                sw.Start();

                foreach (var i in l)
                {
                    var origQuery = i.Item1;
                    var expectedResult = i.Item2;

                    Console.WriteLine("testing " + origQuery);

                    var res = st.BestN(origQuery, 1);

                    if (res.Count == 0)
                    {
                        Console.WriteLine("NO RESULTS?!?!?!");
                    }
                    else
                        if (res[0].query != expectedResult)
                        {
                            Console.WriteLine("ERROR: !{0}! : !{1}! : !{2}!", origQuery, expectedResult, res[0].query);
                        }
                }
                sw.Stop();
                TimeSpan ts = sw.Elapsed;


                string elapsedTime = String.Format("took {0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds);
                Console.WriteLine(elapsedTime);

            }
            else
            {
                var sw = new Stopwatch();
                sw.Start();

                var res = st.BestN(a, 50);
                sw.Stop();

                foreach (var i in res)
                {
                    Console.WriteLine("XXX: " + i.query + " : " + i.score.ToString());
                }
                TimeSpan ts = sw.Elapsed;


                string elapsedTime = String.Format("took {0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds);
                Console.WriteLine(elapsedTime);


            }

        }

    }
}
