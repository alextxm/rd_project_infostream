﻿//#define USE_PLAINEN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if USE_PLAINEN
using PlainElastic.Net;
using PlainElastic.Net.Serialization;
using PlainElastic.Net.Queries;
#else
using Nest;
using Nest.Extensions;
#endif

using utils;
using datatypes;
using dataesextensions;

namespace test_es1
{
    class Program
    {
        static string indexname = "istest";

        static void Usage()
        {
            Console.WriteLine("Usage: idx [options] [search string [fields]]");
            Console.WriteLine();
            Console.WriteLine(" -reset               reset the datafeed [WARNING! BE SURE TO BE ONLINE!]");
            Console.WriteLine(" -withscore           use results scoring");
            Console.WriteLine(" -index[=force]       repeat indexing of the datafeed");
        }

        static void Main(string[] args)
        {

            CommandLineArgs ca = new CommandLineArgs(args);

            if (ca.Arguments.Count < 1 && ca.Options.Count < 1)
            {
                Usage();
                Environment.Exit(1);
            }

#if USE_PLAINEN
            ElasticConnection connection = new ElasticConnection("localhost", 9200);
            JsonNetSerializer serializer = new JsonNetSerializer();
#else
            ConnectionSettings setting = new ConnectionSettings(new Uri("http://localhost:9200"), indexname);
            NestElasticClient client = null;

            try
            {
                client = new NestElasticClient(setting);
                IHealthResponse h = client.Health();
                if (h == null || String.IsNullOrEmpty(h.Status) || !h.ConnectionStatus.Success)
                    throw new Exception();
            }
            catch
            {
                Console.WriteLine("Unable to open connection to server");
                Environment.Exit(1);
            }
#endif

            bool reset = ca.Options.IsOptionEnabled("reset");
            bool index = ca.Options.IsOptionEnabled("index");
            bool forceindex = index && ca.Options["index"] == "force";
            bool updateindex = index && ca.Options["index"] == "update";
            bool usescoring = ca.Options.IsOptionEnabled("withscore");

            if (reset)
            {
                StaticDataFeedManager.BuildStaticDataFeed(false);
            }

            bool doReindex = reset || forceindex || updateindex;

            if (doReindex)
            {
                Console.Write("Data reindexing had been requested: ");
                int origRow = Console.CursorTop;
                int origCol = Console.CursorLeft;

                ConsoleUtils.WriteAt("loading data feed", 0, 0, origRow, origCol);
                List<StaticDataFeed> data = StaticDataFeedManager.ReadStaticDataFeed(false);

                if(data==null || data.Count<1)
                {
                    Console.WriteLine("error: no elements loaded");
                    Environment.Exit(1);
                }

                if (reset || forceindex)
                {
#if USE_PLAINEN
#else
                    if (client.IndexExists(indexname).Exists)
                        client.DeleteIndex(indexname);
                    
                    client.CreateIndex(indexname, new IndexSettings());
#endif
                }

#if !BOH
                RootObjectMapping map = client.GetMapping<StaticDataFeed>(indexname);
                if (map != null)
                    client.DeleteMapping<StaticDataFeed>(indexname);

                IElasticSearchTypeMapper mapper = new StaticDataFeedESMapper();
                mapper.MapToElasticSearch(indexname, client);
#endif

                // Convert result to typed index result object. 
                int i = 1;
                foreach (StaticDataFeed f in data)
                {
                    ConsoleUtils.WriteAt(String.Format("{1}indexing item {0}     ", i, (updateindex) ? "re" : String.Empty), 0, 0, origRow, origCol);

#if USE_PLAINEN
                    OperationResult res = connection.Post(new IndexCommand(indexname, "staticdatafeed"), serializer.ToJson(f));
#else
                    if (updateindex)
                    {
                        client.Update<StaticDataFeed>(u => u
                            .Index(indexname)
                            .Document(f)
                            .Type("staticdatafeed")
                            .DocAsUpsert()
                            .Id(f.id.ToString()));
                    }
                    else
                        client.Index<StaticDataFeed>(f, indexname, "staticdatafeed2");
#endif
                    //li.AddUpdateLuceneIndex(f.ToIXDescriptor());
                    i++;
                }

                data.Clear();
                data = null;
                ConsoleUtils.WriteAt(" completed.           ", 0, 0, origRow, origCol);
                Console.WriteLine();
            }

            if (ca.Arguments.Count > 0 && ca.Arguments[0] == "search")
            {
                RootObjectMapping ex = client.GetMapping(indexname, "staticdatafeed2");
                if (ex == null)
                {
                    Console.WriteLine("\nWARNING! missing mapping for staticdatafeed2\n");

                    //client.DeleteMapping<StaticDataFeed>(indexname, "staticdatafeed");

                    //RootObjectMapping map = client.GetMapping<StaticDataFeed>(indexname);
                    //if (map == null)
                    //    StaticDataFeed.MapToElasticSearch(indexname, client);
                }

                Console.WriteLine("[press a key to execute search]");
                Console.ReadLine();

#if USE_PLAINEN
                string query = new QueryBuilder<StaticDataFeed>()
                                    .Query(bq =>
                                            bq.QueryString(qs => qs.Query("")))
                                    .BuildBeautified();

                OperationResult result = connection.Post(Commands.Search(indexname, "itest"), query);
                Console.WriteLine("DEBUG {0}", result);
                //if(result.Result == "")
                foreach (StaticDataFeed r in serializer.ToSearchResult<StaticDataFeed>(result).Documents)
                    Console.WriteLine("Result matching{2}: {0} \"{1}\"", r.id, r.titolo, String.Empty); //, (usescoring) ? String.Format(" at {0:n2}%", r.Score) : String.Empty);

#else
                var tz0 = client.Count(new[] { indexname }, p => p.MatchAll());

                DateTime t1 = DateTime.Now;
                // query semplice
                //var tz = client.Search<StaticDataFeed>(
                //    p => p
                //        .AllIndices()
                //        .Query(q => q
                //                    .Text(m => m
                //                            .OnField(e => e.titolo)
                //                            .QueryString("casa")
                //                        )
                //              )
                //        );

                // query con OR
                //var tz = client.Search<StaticDataFeed>(
                //    p => p
                //        .AllIndices()
                //        .Query(q => 
                //                    (q.Text(m => m
                //                            .OnField(e => e.titolo)
                //                            .QueryString("casa"))) ||
                //                    (q.Text(m => m
                //                            .OnField(e => e.titolo)
                //                            .QueryString("dottor")) 
                //                        )
                //              )
                //        );

                // query semplice lucene-style (cosi'  ci siamo!!!)
                //"titolo:c?s* OR (titolo:dott*)^2.5")
                var tz = client.Search<StaticDataFeed>(
                    p => p
                        .Index(indexname)
                        .Type("staticdatafeed2")
                        .Query(q => q.QueryString(qs => qs
                            //.OnField("titolo")
                            //.MinimumShouldMatchPercentage(0)
                            .Query(ca.Arguments[1])
                            ))
                        );
                var tz2=tz.DocumentsWithMetaData.ToList();
                DateTime t2 = DateTime.Now;
                Console.WriteLine("query duration: {0}ms vs {1}ms", tz.ElapsedMilliseconds, (t2-t1).TotalMilliseconds);

                foreach (IHit<StaticDataFeed> r in tz2)
                {
                    Console.WriteLine("Result matching{2}: {0} \"{1}\"", r.Source.id, r.Source.titolo, (usescoring) ? String.Format(" at {0:n2}", r.Score) : String.Empty);
                }
#endif

                //IXRequest req = new IXRequest(ca.Arguments[1], 0, -1, (usescoring) ? IXRequestFlags.None : IXRequestFlags.UseScore);

                //DateTime t1 = DateTime.Now;
                //IXQueryCollection reply = li.Search(req);
                //DateTime t2 = DateTime.Now;

                //if (reply.Status == IXQueryStatus.Success)
                //{
                //    foreach (IXQuery r in reply.Results)
                //        Console.WriteLine("Result matching{2}: {0} \"{1}\"", r["id"].String, r["titolo"].String, (usescoring) ? String.Format(" at {0:n2}%", r.Score) : String.Empty);
                //}

                //Console.WriteLine("Search stats: scope:{0} items status:{2} time:{1}ms results:{3} items", li.IndexSize, Convert.ToInt32((t2 - t1).TotalMilliseconds), reply.Status, reply.Count);
            }
        }
    }
}
