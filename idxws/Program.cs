using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Blackbird.Core.WCF;
using Blackbird.Core.ConsoleIO;

using InfoStream.Metadata;

namespace idxws
{
    class Program
    {
        static WCFServiceReferenceInfo wsConfiguration = new WCFServiceReferenceInfo()
                                                                    {
                                                                        EndpointAddress = "http://localhost:51043/InfoStreamService.svc",
                                                                        ServiceReferenceName = "IInfoStreamService",
                                                                        Info = new WCFBindingInfo()
                                                                          {
                                                                              BindingType = WCFBindingType.BasicHttp,
                                                                              TransferMode = WCFTransferMode.Buffered,
                                                                              TransferLimit = 1048576,
                                                                              ItemsLimit = 1048576
                                                                          }
                                                                    };

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
                //WCFConnectionManager<InfoStream.Metadata.IInfoStreamService> zk = new WCFConnectionManager<IInfoStreamService>()
                //WCFServiceReferenceInfo zz = new WCFServiceReferenceInfo(
                //WCFSimpleConnectionManager<InfoStream.Metadata.IInfoStreamService> px = new WCFSimpleConnectionManager<IInfoStreamService>()
                //    new WCFServiceReferenceInfo(
                //        new WCFBindingInfo(
                //    {
                //                          BindingType = WCFBindingType.BasicHttp,
                //                          CloseTimeout = new TimeSpan(0, 1, 0),
                //                          OpenTimeout = new TimeSpan(0, 1, 0),
                //                          SendTimeout = new TimeSpan(0, 1, 0),
                //                          ReceiveTimeout = new TimeSpan(0, 1, 0),
                //                          SecurityMode = WCFServiceSecurityMode.None,
                //                          TransferMode = System.ServiceModel.TransferMode.Buffered,
                //                          Protocol = WCFProtocolType.HTTP
                //                      },
                //        new Endpoint
                //        ServiceReferenceName = "is"
                //    });

                IXQueryCollection found = null;

                //CoreService.InfoStreamServiceClient cli = new CoreService.InfoStreamServiceClient();
                WCFConnectionManager<IInfoStreamService> cm = new WCFConnectionManager<IInfoStreamService>(wsConfiguration);

                Console.WriteLine("Press a key to execute the query");
                Console.ReadLine();
                string what = ca.Arguments[1];

                IXRequest req = new IXRequest() { Query = ca.Arguments[1], Fields = new string[] { "id", "titolo" }, Skip = 0, Take = 1000, Flags = IXRequestFlags.UseScore };

                DateTime t1 = DateTime.MinValue;
                DateTime t2 = DateTime.MinValue;

                using (WCFGenericProxy<IInfoStreamService> cli = cm.CreateProxyClient())
                {
                    t1 = DateTime.Now;
                    found = cli.WsInterface.SearchData(req); //cli.SearchData(req); 
                    t2 = DateTime.Now;
                }

                foreach (IXQuery q in found.Results)
                    Console.WriteLine("Result matching{2}: {0} \"{1}\"", q.Records.First(f => f.Name == "id").String, q.Records.First(f => f.Name == "titolo").String, (usescoring) ? String.Format(" at {0:n2}%", q.Score) : String.Empty);

                Console.WriteLine("search required {0}ms and produced {1} results", Convert.ToInt32((t2 - t1).TotalMilliseconds), found.Count);

                cm.Dispose();
            }
        }
    }
}
