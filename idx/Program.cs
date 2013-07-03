﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Blackbird.Core.ConsoleIO;

using corelib;

namespace idx
{
    class Program
    {
        static void Usage()
        {
            Console.WriteLine("Usage: idx [options] [search string fields]");
            Console.WriteLine();
            Console.WriteLine(" -reset               reset the datafeed [WARNING! BE SURE TO BE ONLINE!]");
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

            if(reset)
            {    
                fd.BuildStaticDataFeed();
            }

            List<StaticDataFeed> data = fd.ReadStaticDataFeed();
            Console.WriteLine("data: {0}", data.Count);

            IndexerStorageMode mode = IndexerStorageMode.FS;
            if (ca.Options.IsOptionEnabled("mode"))
            {
                if (ca.Options["mode"] == "ram")
                    mode = IndexerStorageMode.RAM;
                else if (ca.Options["mode"] == "fsram")
                    mode = IndexerStorageMode.FSRAM;
            }

            Console.WriteLine("Mode: {0}", mode);
            IndexerInterop<StaticDataFeed> li = new IndexerInterop<StaticDataFeed>(
                mode,
                IndexerAnalyzer.StandardAnalyzer,
                new StaticeDataFeedLI(delegate(int id) { return data.FirstOrDefault(p => p.id == id); }));

            if (reset || (mode==IndexerStorageMode.RAM) || (mode!=IndexerStorageMode.RAM && forceindex))
            {
                Console.Write("Indexing item with identifier:");
                int origRow = Console.CursorTop;
                int origCol = Console.CursorLeft;

                foreach (StaticDataFeed f in data)
                {
                    ConsoleUtils.WriteAt(f.id.ToString() + "... ", 0, 0, origRow, origCol);
                    bool res = li.AddUpdateLuceneIndex(f);
                    ConsoleUtils.WriteAt((res==true ? "ok" : "fail") + "          ", 0, 0, Console.CursorTop, Console.CursorLeft);
                }
                Console.WriteLine();
            }

            if (mode == IndexerStorageMode.FSRAM && ca.Options.IsOptionEnabled("simfsramrefresh"))
                Console.WriteLine("Simulate index refresh: {0}", li.UpdateMemoryIndexFromFS());

            if (ca.Arguments.Count > 0 && ca.Arguments[0] == "search")
            {
                string what = ca.Arguments[1];
                IEnumerable<string> where = (ca.Arguments.Count > 2) ? ca.Arguments[2].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries) : new string[]{};

                DateTime t1 = DateTime.Now;
                List<StaticDataFeed> found = li.Search(what, where).ToList(); // li.GetAllIndexRecords().ToList();
                DateTime t2 = DateTime.Now;

                foreach (StaticDataFeed f in found)
                    Console.WriteLine("Result matching: {0}", f.id);

                Console.WriteLine("search on {0} items required {1}ms and produced {2} results", li.IndexSize, Convert.ToInt32((t2 - t1).TotalMilliseconds), found.Count);
            }
        }
    }
}
