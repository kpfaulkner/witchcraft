using System;
using System.Collections.Generic;

namespace SpellingConsole
{
	
	public static class DictionaryHelpers
    {

        public static T2 getOrElse<T1, T2>(this Dictionary<T1, T2> d, T1 key, T2 defaultVal)
        {

            T2 v = defaultVal;

            if (d.ContainsKey(key))
            {
                v = d[key];
            }
            else
            {
                v = defaultVal;

            }

            return v;
        }


        public static T2 getOrElseAssign<T1, T2>(this Dictionary<T1, T2> d, T1 key, T2 defaultVal)
        {

            T2 v = defaultVal;

            v = d.getOrElse(key, defaultVal);
            d[key] = v;

            return v;
        }


    }
	
	public static class StringHelpers
	{
		public static bool IsInt( this string s )
		{
			var res = 0;
			
			var isInt = int.TryParse( s, out res );
			
			return isInt;
		}
		
		public static long GetLongElse( this string s, long d )
		{
			long res = 0;
			
			var isInt = long.TryParse( s, out res );
			
			if ( !isInt )
			{
				res = d;	
			}
			
			return res;
		}
		
	}
}

