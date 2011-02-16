﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


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
		public int Compare( double x, double y)
		{
			return x.CompareTo( y );
		}

    }
	
    public class SpellingTokenizer
    {
		
		private int maxQueryTerms = 10;
        private static GivronSpell givron = null;
        private static Ranker ranker = null;

        private List<string> tokens = null;
        delegate int comp(Variation v1, Variation v2);
        private int maxCombinationsPerToken = 100;
		
		private Random random = new Random();
		
        public SpellingTokenizer()
        {
            if (givron == null)
            {
                givron = new GivronSpell();
            }

            if (ranker == null)
            {
                ranker = new Ranker();
            }

            ReadConfig();

        }

        private void ReadConfig()
        {


            System.Configuration.AppSettingsReader aps = new System.Configuration.AppSettingsReader();


            maxCombinationsPerToken = (int)aps.GetValue("MaxCombinationsPerToken", typeof(System.Int32));

        }

        // just generates the final query strings and combined rank.
		private List<ResultQuery> GenerateQueries( IEnumerable<List<Token>> candidates, int n )
		{
            Console.WriteLine("GenerateQueries start");

			var l = new List<ResultQuery>();

            var dict = new Dictionary<string, ResultQuery>();
			
			var count = 0;
			var sb = new StringBuilder();
			var comp = new ScoreComparer();
			
			var sortedListOfScores = new SortedList<double, string>( comp );
			var sortedListOfScores2 = new List< ResultQuery>();
			
            foreach (var c in candidates)
            {
				//Console.WriteLine("candidate " + (count++).ToString() );
				sb.Clear();
			    
                var score = 1.0;
                foreach (var t in c)
                {

                    if (t.term != "")
                    {
                        sb.Append(t.term);
                        sb.Append(" ");
                        score *= t.score;
                    }
                }

                var term = sb.ToString().Trim();
				
				
				
				// if sorted list is too long (greater than n). then trim.
				// unsure of perf issues here.
				
				// if sorted list not to max size, then just add.
				
				if ( sortedListOfScores.Count < n)
				{
					// HACK... to make sure 2 scores do NOT have the same score reduce by miniscule amount.
					
					var delta = 0.0000001 * random.Next( 1, 1000);
					score += delta;
					sortedListOfScores.Add( score, term );	
				
				} 
				else
				{
					// check if greater than n-1th position.
					var entry = sortedListOfScores.ElementAt( n-1 );
					var existingScore = entry.Key;
					
					if ( score > existingScore )
					{
						
						var delta = 0.0000001 * random.Next( 1, 1000);
						score += delta;
						sortedListOfScores.Add(score, term );	
						
						
						// remove entry at n.
						// doing this all the time probably inefficient.
						sortedListOfScores.RemoveAt( n );
					}
				}
				
            }
			
			foreach( var i in sortedListOfScores)
			{
			
				var rq = new ResultQuery();
				rq.query = i.Value;
				rq.score = i.Key;
				
				l.Add( rq );
			}
			
            Console.WriteLine("about to sort generated queries");
			
            // sort and get top n.
            l.Sort(delegate(ResultQuery a, ResultQuery b) { return a.score.CompareTo( b.score ); });
            l.Reverse();

            Console.WriteLine("GenerateQueries end");

            return l.GetRange(0, Math.Min(n, l.Count));
			
		}
		

        public List< ResultQuery > BestN(string s, int n)
        {

            tokens = new List<string>(s.Split());
            List<List<Token>> chain = new List<List<Token>>();
            List<Variation> variations = new List<Variation>();

			
            foreach (var word in tokens)
            {
                List<Token> tuples = givron.TopNCorrect(word, maxCombinationsPerToken );
                chain.Add(tuples);
                //Console.WriteLine("list length " + tuples.Count.ToString());


            }

            Console.WriteLine("created top N");

			// get all combinations.
			// FIXME  STUPID STUPID LIMITATION OF MY LINQ-FOO.
			// Assume up to 10 terms per query maximum.
			// add extra empty lists to chain.
            var chainSize = chain.Count;
            for (int i = 0; i < maxQueryTerms - chainSize; ++i)
			{
				var dummyList = new List<Token>(){ new Token("")};
				chain.Add( dummyList );
			}
            
			// general all variations.
			var results = 	from val0 in chain[0]
						from val1 in chain[1]
						from val2 in chain[2]
						from val3 in chain[3]
						from val4 in chain[4]
						from val5 in chain[5]
						from val6 in chain[6]
						from val7 in chain[7]
						from val8 in chain[8]
						from val9 in chain[9]
						select new List<Token>(){val0, val1, val2, val3, val4, val5,val6,val7,val8,val9};

            Console.WriteLine("Generated all combinations " );

			// rank all the results....  this is where all the tuning will go.
			//ranker.Rank( results );
			
			// generate result strings for top n results 
			var finalQueries = GenerateQueries( results, n );
			
			//var finalQueries = new List<ResultQuery>();
			
            return finalQueries;

        }
		
		public static List< Tuple<string, string>> LoadTestData()
		{
            var l = new List< Tuple<string, string> >();

            using (FileStream fs = File.OpenRead("trec.txt"))
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
						
						var t = new Tuple<string,string>(sp[0].Trim(), sp[1].Trim() );
						
						
                        l.Add( t );

                    }


                }
            }
			
			return l;
		}
		

		
        public static void Main(string[] args)
        {

            var a = args[0];

            //var a = "receipt";

            var st = new SpellingTokenizer();
			
			if ( a == "test")
			{
				var l = LoadTestData();
				
				foreach( var i in l )
				{
					var origQuery = i.Item1;
					var expectedResult = i.Item2;
					
					Console.WriteLine("testing " + origQuery );
					
					var res = st.BestN( origQuery, 1);
					
					if ( res.Count == 0)
					{
						Console.WriteLine("NO RESULTS?!?!?!");	
					}
					else
					if ( res[0].query != expectedResult )
					{
						Console.WriteLine("ERROR: !{0}! : !{1}!", origQuery, res[0].query );	
					}
				}
					
			}
			else
			{
				var res = st.BestN( a, 5);
				
				foreach ( var i in res )
				{
					Console.WriteLine("XXX: " + i.query + " : " + i.score.ToString() );	
				}
			}
			
        }

    }
}
