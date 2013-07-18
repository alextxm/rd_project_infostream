using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Blackbird.Core.ConsoleIO;

using InfoStream.Core;
using InfoStream.Metadata;

namespace ISClient
{
    class Program
    {
        static void Usage()
        {
            Console.WriteLine("Usage: idx [options] [search string [fields]]");
            Console.WriteLine();
            Console.WriteLine(" -reset               reset the datafeed [WARNING! BE SURE TO BE ONLINE!]");
            Console.WriteLine(" -withscore           use results scoring");
            Console.WriteLine(" -index               repeat indexing of the datafeed");
            Console.WriteLine(" -mode=value          index mode: fs,fsram,ram");
            Console.WriteLine(" -simfsramrefresh     simulate a refresh for fsram-based index");
        }

        static void Main(string[] args)
        {
            CommandLineArgs ca = new CommandLineArgs(args);

            if (ca.Arguments.Count < 1 && ca.Options.Count < 1)
            {
                Usage();
                Environment.Exit(1);
            }
            
            SampleDataFeed fd = new SampleDataFeed();

            bool reset = ca.Options.IsOptionEnabled("reset");
            bool index = ca.Options.IsOptionEnabled("index");
            bool forceindex = index && ca.Options["index"]=="force";
            bool updateindex = index && ca.Options["index"]=="update";
            bool usescoring = ca.Options.IsOptionEnabled("withscore");

            if(reset)
            {    
                fd.BuildStaticDataFeed();
            }

            IndexerStorageMode mode = IndexerStorageMode.FS;
            if (ca.Options.IsOptionEnabled("mode"))
            {
                if (ca.Options["mode"] == "ram")
                    mode = IndexerStorageMode.RAM;
                else if (ca.Options["mode"] == "fsram")
                    mode = IndexerStorageMode.FSRAM;
            }

            bool doReindex = reset || (mode==IndexerStorageMode.RAM) || (mode!=IndexerStorageMode.RAM && forceindex) || updateindex;

            Console.Write("Loading indexer in {0} mode... ", mode);           
            long m0 = GC.GetTotalMemory(false);
            ISIndexer li = new ISIndexer(mode, IndexerAnalyzer.StandardAnalyzer);
            Console.WriteLine(" done.");

            if (doReindex)
            {
                Console.Write("Data reindexing had been requested: ");
                int origRow = Console.CursorTop;
                int origCol = Console.CursorLeft;
                
                ConsoleUtils.WriteAt("loading data feed",0,0,origRow,origCol);
                List<StaticDataFeed> data = fd.ReadStaticDataFeed();

                if (reset || forceindex)
                    li.ClearAllIndex();

                int i = 1;
                foreach (StaticDataFeed f in data)
                {
                    ConsoleUtils.WriteAt(String.Format("indexing item {0}     ", i), 0, 0, origRow, origCol);
                    li.AddUpdateLuceneIndex(f.ToIXDescriptor());
                    i++;
                }

                li.UpdateMemoryIndexFromFS();
                data.Clear();
                data = null;
                GC.Collect();
                ConsoleUtils.WriteAt(" completed.           ", 0, 0, origRow, origCol);
                Console.WriteLine();
            }

            long m1 = GC.GetTotalMemory(false);
            Console.WriteLine("Index is using {0} kB of RAM", (m1 - m0) > 0 ? (m1 - m0) / (long)1024 : 0);

            if (mode == IndexerStorageMode.FSRAM && ca.Options.IsOptionEnabled("simfsramrefresh"))
                Console.WriteLine("Simulate index refresh: {0}", li.UpdateMemoryIndexFromFS());

            if (ca.Arguments.Count > 0 && ca.Arguments[0] == "search")
            {
                Console.WriteLine("[press a key to execute search]");
                Console.ReadLine();

                IXRequest req = new IXRequest(ca.Arguments[1], 0, -1, (usescoring) ? IXRequestFlags.None : IXRequestFlags.UseScore);

                DateTime t1 = DateTime.Now;
                IXQueryCollection reply = li.Search(req);
                DateTime t2 = DateTime.Now;

                if (reply.Status == IXQueryStatus.Success)
                {
                    foreach (IXQuery r in reply.Results)
                        Console.WriteLine("Result matching{2}: {0} \"{1}\"", r["id"].String, r["titolo"].String, (usescoring) ? String.Format(" at {0:n2}%", r.Score) : String.Empty);
                }

                Console.WriteLine("Search stats: scope:{0} items status:{2} time:{1}ms results:{3} items", li.IndexSize, Convert.ToInt32((t2 - t1).TotalMilliseconds), reply.Status, reply.Count);
            }
        }
    }
}
