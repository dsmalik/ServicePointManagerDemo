using System;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace ServicePointManagerApp
{
    public class ClientGetAsync
    {
        private static int totalFailed = 0;
        private static int totalSuccess = 0;

        public class threadStateInfo
        {
            public string strUrl;
            public ManualResetEvent evtReset;
            public int iReqNumber;
        }

        public static void ThreadProc(object stateInfo)
        {
            threadStateInfo tsInfo = stateInfo as threadStateInfo;

            try
            {
                HttpWebRequest aReq = WebRequest.Create(tsInfo.strUrl) as HttpWebRequest;
                aReq.Timeout = 4000;

                Console.WriteLine($"Begin Request {tsInfo.iReqNumber}");

                HttpWebResponse aResp = aReq.GetResponse() as HttpWebResponse;

                Thread.Sleep(500);

                aResp.Close();

                // Console.WriteLine($"End Request {tsInfo.iReqNumber}");

                totalSuccess++;
            }
            catch (WebException theEx)
            {
                // Console.WriteLine($"Exception for Request {tsInfo.iReqNumber}:{theEx.Message}");
                totalFailed++;
            }

            tsInfo.evtReset.Set();
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                showusage();
                return;
            }

            if (args.Length != 2)
            {
                ServicePointManager.DefaultConnectionLimit = 2;
            }
            else
            {
                ServicePointManager.DefaultConnectionLimit = Int32.Parse(args[1]);
            }

            Console.WriteLine($"Default Limit is {ServicePointManager.DefaultConnectionLimit}");

            Stopwatch sw = Stopwatch.StartNew();

            int numberRequests = 64;
            ManualResetEvent[] manualEvents = new ManualResetEvent[numberRequests];

            string httpSite = args[0];
            int minWorker, minIOC;

            ThreadPool.GetMinThreads(out minWorker, out minIOC);

            ThreadPool.SetMinThreads(numberRequests, minIOC);

            for (int i = 0; i < numberRequests; i++)
            {
                threadStateInfo theInfo = new threadStateInfo();

                manualEvents[i] = new ManualResetEvent(false);

                theInfo.evtReset = manualEvents[i];

                theInfo.iReqNumber = i;
                theInfo.strUrl = httpSite;

                ThreadPool.QueueUserWorkItem(ThreadProc, theInfo);
            }

            WaitHandle.WaitAll(manualEvents);

            Console.WriteLine($"Done in {sw.Elapsed.TotalMilliseconds}msec!");
            Console.WriteLine($"Failed - {totalFailed},   Success - {totalSuccess}");
        }

        public static void showusage()
        {
            Console.WriteLine("Attempt to GET a URL");
            Console.WriteLine("\r\nUsage");
            Console.WriteLine("     ClientGetAsync URL");
            Console.WriteLine("     Example:");
            Console.WriteLine("          ClientGetAsync http://www.contoso.com/");
        }
    }
}