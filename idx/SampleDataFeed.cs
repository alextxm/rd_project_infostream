using System;
using System.Collections.Generic;
#if DATAFEED_TVC
using System.Data.Entity.Validation;
#endif
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;

using corelib;
using corelib.Interchange;

using Blackbird.Core;
using Blackbird.Core.Serialization;
using Blackbird.Core.Zib;


namespace idx
{
    class SampleDataFeed
    {
        private readonly string sourceFile = "data.feed";
        private DataContractSerialization<IEnumerable<StaticDataFeed>> serializer = new DataContractSerialization<IEnumerable<StaticDataFeed>>();
        

        public bool BuildStaticDataFeed()
        {
#if DATAFEED_TVC
            netmovie2Entities db = new netmovie2Entities();
            //DataFeedContext ctx = new DataFeedContext();

            //try
            //{
            //    if (ctx.DataFeed.Count() > 0)
            //    {
            //        foreach (var t in ctx.DataFeed.Where(p => p.id > 0).OrderByDescending(p => p.id))
            //            ctx.DataFeed.Remove(t);

            //        ctx.SaveChanges();
            //        Console.WriteLine("SDF Clear complete");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("Error clearing SDF: {0} {1}", ex.Message, (ex.InnerException==null) ? String.Empty : ex.InnerException.Message);
            //}

            List<contenuti> cl = db.contenuti.Where(p => p.id > 0).ToList();
            List<contenuti_dettagli> cld = db.contenuti_dettagli.Where(p => p.id > 0).ToList();

            int i = 1;
            List<StaticDataFeed> theFeed = new List<StaticDataFeed>();

            foreach (contenuti t in cl)
            {
                Console.WriteLine("Processing record {0} of {1} [{2}]", i, cl.Count, t.id);
                contenuti_dettagli td = cld.FirstOrDefault(p => p.id == t.id);

                //byte[] compressed = null;
                //if (td != null && !String.IsNullOrEmpty(td.recensione))
                //{
                //    using (MemoryStream outputMemStream = new MemoryStream())
                //    {
                //        GZipOutputStream gs = new GZipOutputStream(outputMemStream);
                //        byte[] data = System.Text.ASCIIEncoding.UTF8.GetBytes(td.recensione);
                //        gs.Write(data, 0, data.Length);
                //        gs.Flush();
                //        gs.Close();
                //        compressed = outputMemStream.ToArray();
                //    }
                //}
                //else
                //{
                //    if (td == null)
                //        Console.WriteLine("WARNING: record {0} [{1}] is malformed.", i, t.id);
                //}

                //string hex = (compressed == null) ? String.Empty : compressed.ByteArrayToHexString();
                ////Console.WriteLine("DBG: {0} ", hex.Length);

                //if(hex.Length > 4000)
                //    Console.WriteLine("WARNING: record {0} [{1}] has field >4000bytes {2}", i, t.id, hex.Length);

                StaticDataFeed feed = new StaticDataFeed()
                {
                    id = (int)t.id,
                    titolo = t.titolo,
                    titolo_orig = t.titolo_orig,
                    anno = (t.anno == null) ? 1900 : (int)t.anno,
                    attivo = (t.attivo == null) ? false : Convert.ToBoolean(t.attivo),
                    id_major = t.id_major,
                    codice_prodotto = t.codice_prodotto,
                    codicemw = t.codicemw,
                    codicemw_st = t.codicemw_st,
                    data_inserimento = t.data_inserimento,
                    artista = t.artista,
                    regista = t.regista,
                    sceneggiatore = t.sceneggiatore,
                    produttore = t.produttore,
                    durata = t.durata,
                    generi = t.generi,
                    tags = t.tags,
                    data_rilascio = (td == null) ? new DateTime(1900, 1, 1) : td.data_rilascio,
                    dettagli = (td == null) ? String.Empty : td.dettagli,
                    libro = (td == null) ? String.Empty : td.libro,
                    consigliato = (td == null) ? String.Empty : td.consigliato,
                    recensione = (td == null) ? String.Empty : td.recensione
                };

                theFeed.Add(feed);
                //ctx.DataFeed.Add(feed);
                    
                //try
                //{
                //    ctx.SaveChanges();
                //}
                //catch (DbEntityValidationException dex)
                //{
                //    Console.WriteLine("Error updating SDF: {0}", dex.Message);
                //    foreach(DbEntityValidationResult rx in dex.EntityValidationErrors)
                //    {
                //        foreach (var error in rx.ValidationErrors.Select(p => new { p.PropertyName, p.ErrorMessage }))
                //            Console.WriteLine("\t {0} {1}", error.PropertyName, error.ErrorMessage);
                //    }

                //    break;
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine("Error updating SDF: {0} {1}", ex.Message, (ex.InnerException==null) ? String.Empty : ex.InnerException.Message);
                //    break;
                //}

                i++;
            } // end foreach

            using (Stream fs = File.Open(sourceFile, FileMode.Create))
            {
                using (GZipOutputStream outStream = new GZipOutputStream(fs))
                {
                    serializer.SerializeToStream(theFeed, outStream);
                }
            }

            //ctx.SaveChanges();
            return true;
#else
            return true;
#endif
        }

        public List<StaticDataFeed> ReadStaticDataFeed(string source = null)
        {
            string file = (source == null) ? sourceFile : source;

            if (!File.Exists(file))
                return new List<StaticDataFeed>();

            IEnumerable<StaticDataFeed> data = null;
            using (MemoryStream ms = new MemoryStream())
            {
                using (Stream fs = File.Open(sourceFile, FileMode.Open))
                {
                    using (GZipStream inStream = new GZipStream(fs, CompressionMode.Decompress))
                    {
                        int size = 2048;
                        byte[] writeData = new byte[2048];

                        while (true)
                        {
                            size = inStream.Read(writeData, 0, size);

                            if (size > 0)
                                ms.Write(writeData, 0, size);
                            else
                                break;
                        }                       
                    }
                }

                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);

                data = serializer.DeserializeFromStream(ms);
            }

            return data.ToList();
        }
    }

    public delegate StaticDataFeed StaticDataFeedGetByIdDelegate(int id);

    /// <summary>
    /// implementazione SPECIFICA del LIOH
    /// </summary>
    class StaticeDataFeedLI : IndexableObjectHandler<StaticDataFeed>
    {
        private StaticDataFeedGetByIdDelegate getDelegate = null;

        public StaticeDataFeedLI(StaticDataFeedGetByIdDelegate getDelegate)
        {
            this.getDelegate = getDelegate;
        }

        public override string DataItemUniqueIdentifierField
        {
            get { return "id"; }
        }

        //Lucene.Net.Documents.
        public override StaticDataFeed BuildDataItem(InterchangeDocument doc)
        {
            if (doc == null)
                return null;

            int id = id = Convert.ToInt32(doc.Get("id"));
            return getDelegate(id);

            //StaticDataFeed feed = new StaticDataFeed()
            //{
            //    id = Convert.ToInt32(doc.Get("id")),
            //    titolo = doc.Get("titolo"),
            //    titolo_orig = doc.Get("titolo_orig"),
            //    anno = Convert.ToInt32(doc.Get("anno")),
            //    attivo = Convert.ToBoolean(doc.Get("attivo")),
            //    id_major = Convert.ToInt32(doc.Get("id_major")),
            //    codice_prodotto = doc.Get("codice_prodotto"),
            //    codicemw = doc.Get("codicemw"),
            //    codicemw_st = doc.Get("codicemw_st"),
            //    data_inserimento = Convert.ToDateTime(doc.Get("data_inserimento")),
            //    artista = doc.Get("artista"),
            //    regista = doc.Get("regista"),
            //    sceneggiatore = doc.Get("sceneggiatore"),
            //    produttore = doc.Get("produttore"),
            //    durata = Convert.ToInt64(doc.Get("durata")),
            //    generi = doc.Get("generi"),
            //    tags = doc.Get("tags"),
            //    data_rilascio = Convert.ToDateTime(doc.Get("data_rilascio")),
            //    dettagli = doc.Get("dettagli"),
            //    libro = doc.Get("libro"),
            //    consigliato = doc.Get("consigliato"),
            //    recensione = doc.Get("recensione")
            //};

            //return feed;
        }

        public override IEnumerable<InterchangeField> FieldsParseFromDataItem(StaticDataFeed dataItem)
        {
            return new InterchangeField[]
            {
                new InterchangeField("id",                dataItem.id.ToString(),                 FieldStore.YES, FieldIndex.NOT_ANALYZED),
                new InterchangeField("titolo",            dataItem.titolo,                        FieldStore.YES, FieldIndex.ANALYZED),
                new InterchangeField("titolo_orig",       dataItem.titolo_orig,                   FieldStore.YES, FieldIndex.ANALYZED),
                new InterchangeField("anno",              dataItem.anno.ToString(),               FieldStore.YES, FieldIndex.NOT_ANALYZED),
                new InterchangeField("attivo",            dataItem.attivo.ToString(),             FieldStore.YES, FieldIndex.NOT_ANALYZED),
                new InterchangeField("data_inserimento",  dataItem.data_inserimento.ToString(),   FieldStore.YES, FieldIndex.NOT_ANALYZED),
                new InterchangeField("artista",           dataItem.artista.ToString(),            FieldStore.YES, FieldIndex.ANALYZED),
                new InterchangeField("regista",           dataItem.regista.ToString(),            FieldStore.YES, FieldIndex.ANALYZED),
                new InterchangeField("sceneggiatore",     dataItem.sceneggiatore.ToString(),      FieldStore.YES, FieldIndex.ANALYZED),
                new InterchangeField("produttore",        dataItem.produttore.ToString(),         FieldStore.YES, FieldIndex.ANALYZED),
                new InterchangeField("generi",            dataItem.generi.ToString(),             FieldStore.YES, FieldIndex.ANALYZED),
                new InterchangeField("tags",              dataItem.tags.ToString(),               FieldStore.YES, FieldIndex.ANALYZED),
                new InterchangeField("data_rilascio",     dataItem.data_rilascio.ToString(),      FieldStore.YES, FieldIndex.NOT_ANALYZED),
                new InterchangeField("dettagli",          dataItem.dettagli.ToString(),           FieldStore.YES, FieldIndex.ANALYZED),
                new InterchangeField("libro",             dataItem.libro.ToString(),              FieldStore.YES, FieldIndex.ANALYZED),
                new InterchangeField("consigliato",       dataItem.consigliato.ToString(),        FieldStore.YES, FieldIndex.ANALYZED),
                new InterchangeField("recensione",        dataItem.recensione.ToString(),         FieldStore.YES, FieldIndex.ANALYZED),
           };
        }

        public override object DataItemUniqueIdentifierValue(StaticDataFeed dataItem)
        {
            return dataItem.id;
        }
    }
}
