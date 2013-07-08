using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Blackbird.Core.ConsoleIO;

using corelib.Interchange;
using System.Diagnostics;

namespace idxws
{
    class Program
    {
        static void Usage()
        {
            Console.WriteLine("Usage: idxws [options] [search string [fields]]");
            Console.WriteLine();
            Console.WriteLine("NONATTIVO -reset               reset the datafeed [WARNING! BE SURE TO BE ONLINE!]");
            Console.WriteLine(" -withscore           use results scoring");
            Console.WriteLine("NONATTIVO  -index               repeat indexing of the datafeed");
            Console.WriteLine("NONATTIVO  -mode=value          index mode: fs,fsram,ram");
            Console.WriteLine("NONATTIVO  -simfsramrefresh     simulate a refresh for fsram-based index");
        }

        static void Main(string[] args)
        {
            CommandLineArgs ca = new CommandLineArgs(args);

            if (ca.Arguments.Count < 1 && ca.Options.Count < 1)
            {
                Usage();
                Environment.Exit(1);
            }

            //SampleDataFeed fd = new SampleDataFeed();
            //
            //bool reset = ca.Options.IsOptionEnabled("reset");
            //bool index = ca.Options.IsOptionEnabled("index");
            //bool forceindex = index && ca.Options["index"] == "force";
            bool usescoring = ca.Options.IsOptionEnabled("withscore");

            //if (reset)
            //{
            //    fd.BuildStaticDataFeed();
            //}
            //
            //long m0 = GC.GetTotalMemory(false);
            //List<StaticDataFeed> data = fd.ReadStaticDataFeed();
            //long m1 = GC.GetTotalMemory(false);
            //long ms1 = (m1 - m0) > 0 ? (m1 - m0) / (long)1024 : 0;
            //Console.WriteLine("Data feed: {0} items [mem: {1} kB]", data.Count, ms1);
            //
            //IndexerStorageMode mode = IndexerStorageMode.FS;
            //if (ca.Options.IsOptionEnabled("mode"))
            //{
            //    if (ca.Options["mode"] == "ram")
            //        mode = IndexerStorageMode.RAM;
            //    else if (ca.Options["mode"] == "fsram")
            //        mode = IndexerStorageMode.FSRAM;
            //}
            //
            //Console.WriteLine("Index mode: {0}", mode);
            //IndexerInterop<StaticDataFeed> li = new IndexerInterop<StaticDataFeed>(
            //    mode,
            //    IndexerAnalyzer.StandardAnalyzer,
            //    new StaticeDataFeedLI(delegate(int id) { return data.FirstOrDefault(p => p.id == id); }));
            //
            //long m2 = GC.GetTotalMemory(false);
            //long ms2 = (m2 - m1) > 0 ? (m2 - m1) / (long)1024 : 0;
            //long msA = (m2 - m0) > 0 ? (m2 - m0) / (long)1024 : 0;
            //Console.WriteLine("Index data: {0} items [mem: {1} kB - total so far: {2} kB]", li.IndexSize, ms2, msA);
            //if (reset || (mode == IndexerStorageMode.RAM) || (mode != IndexerStorageMode.RAM && forceindex))
            //{
            //    Console.Write("Indexing item with identifier:");
            //    int origRow = Console.CursorTop;
            //    int origCol = Console.CursorLeft;
            //
            //    foreach (StaticDataFeed f in data)
            //    {
            //        ConsoleUtils.WriteAt(f.id.ToString() + "... ", 0, 0, origRow, origCol);
            //        bool res = li.AddUpdateLuceneIndex(f);
            //        ConsoleUtils.WriteAt((res == true ? "ok" : "fail") + "          ", 0, 0, Console.CursorTop, Console.CursorLeft);
            //    }
            //    Console.WriteLine();
            //}

            //if (mode == IndexerStorageMode.FSRAM && ca.Options.IsOptionEnabled("simfsramrefresh"))
            //    Console.WriteLine("Simulate index refresh: {0}", li.UpdateMemoryIndexFromFS());

            if (ca.Arguments.Count > 0 && ca.Arguments[0] == "search")
            {
                CoreService.Service1Client cli = new CoreService.Service1Client();

                Console.ReadLine();
                string what = ca.Arguments[1];
               
                DateTime t1 = DateTime.Now;
                InterchangeDocumentsCollection found = cli.SearchData(what, 0, 1000, (usescoring), new List<string>() { "id", "titolo" });
                DateTime t2 = DateTime.Now;

                foreach (InterchangeDocumentInfo f in found.InterchangeDocuments)
                    Console.WriteLine("Result matching{2}: {0} \"{1}\"", f.Element.Get("id"), f.Element.Get("titolo"), (usescoring) ? String.Format(" at {0:n2}%", f.Score) : String.Empty);

                Console.WriteLine("search required {0}ms and produced {1} results", Convert.ToInt32((t2 - t1).TotalMilliseconds), found.Count);
            }
        }
    }
}
