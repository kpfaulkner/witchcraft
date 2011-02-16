using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.IO;
using System.Globalization;

namespace SpellingConsole
{
    class Program
    {
        static void Main1(string[] args)
        {
            bool serviceIsUp = true;
            try
            {
                // register the URL manually by running the following as administrator:
                // > netsh http add urlacl url=http://+:8080/spellchecker0 user=DOMAIN\user
                WebServiceHost host = new WebServiceHost(
                    new SpellerServiceEntry(),
                    new Uri("http://localhost:8080/spellchecker0")
                );
                host.Open();
                
                Console.WriteLine("Press Any Key to Exit");
                Console.ReadKey();
                host.Close();
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
                serviceIsUp = false;
                Console.ReadKey();
            }            
        }
    }

    [ServiceContract]
    public interface IMSSpellerChallengeService
    {
        [OperationContract]
        [WebGet(UriTemplate = "?runID={runID}&q={q}")]
        Message Speller(string runID, string q);

        [OperationContract]
        [WebGet(UriTemplate = "")]
        Message Default();
    }


    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SpellerServiceEntry : IMSSpellerChallengeService
    {
        TextWriter writer;

        public SpellerServiceEntry()
        {
            writer = new StreamWriter(File.OpenWrite("log" + DateTime.Now.ToString("yyyyMMddTHHmmss", DateTimeFormatInfo.InvariantInfo) + ".txt"));
        }

/*        ~SpellerServiceEntry() {
            writer.Flush();
            writer.Close();
        }
 */

        public Message Default()
        {
            //StringBuilder response = new StringBuilder("It is now " + DateTime.Now.ToString("s"));
            StringBuilder response = new StringBuilder();
            //return WebOperationContext.Current.CreateTextResponse("", "text/plain", System.Text.Encoding.UTF8);
			
			return null;
        }

        public Message Speller(string runID, string q)
        {
            DateTime dt = DateTime.Now;
            string url = "";
            string dt_s = dt.ToString("s", DateTimeFormatInfo.InvariantInfo);
            writer.WriteLine("[{0}]\t{1}:\t{2}\t{3}", dt_s, url, runID, q);
            writer.Flush();
            

            StringBuilder response = new StringBuilder();
            //response.Append("foobar");
            //response.Append("\t");
            //response.Append("0.65");
            //response.Append("\n");

            // foreach (var answer in Correct(runID, q)) {
            //      response.Append(answer.correctedString);
            //      response.Append('\t');
            //      response.Append(answer.score.ToString());
            //      response.Append('\n');
            // }
            var tokens = new SpellingTokenizer();
            
            foreach (var answer in tokens.BestN(q, 5))
            {
#if DEBUG
                response.Append(answer.query + "\t" + answer.score + "\n");
#else
                response.Append(answer.S + "\t" + answer.Score.ToString("0.##") + "\n");
                
                
                
#endif
            }
            
            
            /*return WebOperationContext.Current.CreateTextResponse(
                response.ToString(), "text/plain", System.Text.Encoding.UTF8);*/

			return null;
			
        }
    }

}