using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Nest;
//using Nest.Extensions;

using datatypes;
using datainterfaces;
using dataesextensions;
using utils;

// -resetindex "C:\Users\alessandro.pisani\Documents\File di Outlook\test.pst"
namespace fluenttest
{
    class Program
    {
        static ElasticClient client = null;
        static string indexname = "mailtestindex";
        //static TypesStoreManager typesManager = new TypesStoreManager();

#if DEMOCLASS_TEST
        static List<DemoClass> classes = new List<DemoClass>()
                {
                    new DemoClass() { Value = 10001, FieldA = "a", FieldB = "a2" },
                    new DemoClass() { Value = 20001, FieldA = "b", FieldB = "b2" },
                    new DemoClass() { Value = 10002, FieldA = "c", FieldB = "c2" },
                    new DemoClass() { Value = 20002, FieldA = "d", FieldB = "d2" },
                    new DemoClass() { Value = 10003, FieldA = "e", FieldB = "e2" },
                    new DemoClass() { Value = 20003, FieldA = "f", FieldB = "f2" }
                };

        static void ManageIndex(IElasticClient client)
        {
            if (client.IndexExists(indexname).Exists)
                client.DeleteIndex(indexname);

            typesManager.AddType(typeof(DemoClass), "mydemoclass");

            // Okay, il trucco e' qua: (11/12/2013 - 18:20)
            // 1) crea indice
            // 2) cerca il mapping
            // 3) se lo trovi DISTRUGGILO
            // 4) a questo punto usa il MapFluent per fare l'override
            // verifica su: http://localhost:9200/indexname/typename/_mapping?pretty
            client.CreateIndex(indexname, new IndexSettings());

            RootObjectMapping map = client.GetMapping<DemoClass>(indexname);
            if (map != null)
                client.DeleteMapping<DemoClass>(indexname);

            IElasticSearchTypeMapper mapper = new DemoClassESMapper();
            mapper.MapToElasticSearch(indexname, client);
        }
#endif

        static void ManageIndex(IElasticClient client, ConnectionSettings settings, bool forceIndexRecreation)
        {
            //typesManager.AddType(typeof(EmailMessage), "emailmessage");
            ElasticInferrer inferrer = new ElasticInferrer(settings);

            Nest.Resolvers.TypeNameMarker marker = typeof(EmailMessage);
            if (marker == null || String.IsNullOrEmpty(inferrer.TypeName(marker)))
                client.Map<EmailMessage>(p => p.Index(indexname));

            if (forceIndexRecreation)
            {

                if (client.IndexExists(f => f.Index(indexname)).Exists)
                    client.DeleteIndex(f => f.Index(indexname));

                client.Refresh();

                // Okay, il trucco e' qua: (11/12/2013 - 18:20)
                // 1) crea indice
                // 2) cerca il mapping
                // 3) se lo trovi DISTRUGGILO
                // 4) a questo punto usa il MapFluent per fare l'override
                // verifica su: http://localhost:9200/indexname/typename/_mapping?pretty
                client.CreateIndex(indexname); //, new IndexSettings());
            }

            //RootObjectMapping map = client.GetMapping<EmailMessage>(p => p.Index(indexname)).Mapping;
            //if (map != null)
            //    client.DeleteMapping<EmailMessage>(p => p.Index(indexname));

            IElasticSearchTypeMapper mapper = new EmailMessageESMapper();
            mapper.MapToElasticSearch(indexname, client);

            RootObjectMapping map = client.GetMapping<EmailMessage>(p => p.Index(indexname)).Mapping;

            client.Refresh();
        }

        static void MailConverter(IElasticClient client, string file, bool showStatus)
        {
            mailconvlib.Convert<EmailMessage>(file, MailConverterDelegate);
        }

        static bool MailConverterDelegate(IEmailMessage msg)
        {
            Console.Write("{0}\\{1} ... ", msg.FullPathName, (String.IsNullOrEmpty(msg.Filename)) ? msg.MessageID : msg.Filename);
            bool ret = client.Index<EmailMessage>(new EmailMessage(msg), p => p.Index(indexname)).Created; // .Type("emailmessage")
            Console.WriteLine((ret) ? "done." : "failed.");
            return ret;
        }

        static void Main(string[] args)
        {
            CommandLineArgs cmdline = new CommandLineArgs(args);

            bool resetIndex = cmdline.Options.IsOptionEnabled("resetindex");

            if (resetIndex)
            {
                if (cmdline.Arguments.Count < 1 || !System.IO.File.Exists(cmdline.Arguments[0]))
                {
                    Console.WriteLine("Unable to find file");
                    Environment.Exit(1);
                }
            }
            else
            {
                if(cmdline.Arguments.Count < 1)
                {
                    Console.WriteLine("wrong query");
                    Environment.Exit(1);
                }
            }



            ConnectionSettings settings = new ConnectionSettings(new Uri("http://localhost:9200"), "default")
                                                    .SetDefaultTypeNameInferrer(f => f.Name.ToLowerInvariant())
                //.MapDefaultTypeNames(p => p.Add(typeof(EmailMessage), "emailmessage"))
                //.SetDefaultTypeNameInferrer(f => typesManager.InferTypeName(f))
                //.SetDefaultIndex(indexname)
                //.MapDefaultTypeNames(p => p.Add(typeof(DemoClass), "mydemoclass"))
                                                    ;
            //NestElasticClient client = new NestElasticClient(settings);
            client = new ElasticClient(settings);

            // wrapper eccezioni di collegamento
            if (!client.Raw.CatMaster(p => p.V(true)).Success)
            {
                Console.WriteLine("Error: CONNECTION failed");
                Environment.Exit(1);
            }

            ManageIndex(client, settings, resetIndex);

            if (resetIndex)
            {
                MailConverter(client, cmdline.Arguments[0], true);
            }
            else
            {
                var h = client.Health(p => p.Index(indexname));

                bool usescoring = false;
                DateTime t1 = DateTime.Now;
                var tz = client.Search<EmailMessage>(
                    p => p
                        .Index(indexname)
                        .Query(q => q.QueryString(qs => qs
                            //.OnField("titolo")
                            //.MinimumShouldMatchPercentage(0)
                            .Query(cmdline.Arguments[0])
                            ))
                        );
                var tz2 = tz.Hits.ToList();
                DateTime t2 = DateTime.Now;
                Console.WriteLine("query duration: {0}ms vs {1}ms", tz.ElapsedMilliseconds, (t2 - t1).TotalMilliseconds);

                foreach (IHit<EmailMessage> r in tz2)
                {
                    Console.WriteLine("Result matching{2}: {0} \"{1}\"", r.Source.MessageID, r.Source.Subject, (usescoring) ? String.Format(" at {0:n2}", r.Score) : String.Empty);
                }
            }

#if DEMOCLASS_TEST
            foreach (DemoClass d in classes)
                client.Index<DemoClass>(d, indexname, "mydemoclass", d.Value);

            var x1 = client.GetMapping(indexname, "mydemoclass");
            var x2 = client.GetMapping<DemoClass>(indexname);
            System.Diagnostics.Debug.Assert(x1 != null && x2 != null, "ES GetMapping FAILED");

            var tz = client.Search<DemoClass>(
                    p => p
                        .Index(indexname)
                        .Type("mydemoclass")
                        .Query(q => q.QueryString(qs => qs.Query("fieldB:b*")))
                        );

            var y = client.Validate(p => p.Index(indexname).QueryString(q => q.Query("fieldB:b*")));

            foreach (IHit<DemoClass> r in tz.DocumentsWithMetaData)
            {
                Console.WriteLine("Result matching: {0}", r.Source.Value);
            }
#endif
        }
    }

}
