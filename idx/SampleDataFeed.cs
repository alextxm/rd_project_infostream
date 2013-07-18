using System;
using System.Collections.Generic;
#if DATAFEED_TVC
using System.Data.Entity.Validation;
#endif
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;

using InfoStream.Metadata;

using Blackbird.Core;
using Blackbird.Core.Serialization;
using Blackbird.Core.Zib;


namespace ISClient
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

            Console.Write("Rebuilding data source feed; processing record ");
            int origRow = Console.CursorTop;
            int origCol = Console.CursorLeft;

            foreach (contenuti t in cl)
            {
                Blackbird.Core.ConsoleIO.ConsoleUtils.WriteAt(String.Format("{0} of {1} [{2}]     ", i, cl.Count, t.id), 0, 0, origRow, origCol);

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
            Console.WriteLine();

            Console.Write("Saving data source feed... ");
            using (Stream fs = File.Open(sourceFile, FileMode.Create))
            {
                using (GZipStream outStream = new GZipStream(fs, CompressionMode.Compress))
                {
                    serializer.SerializeToStream(theFeed, outStream);
                }
            }
            Console.WriteLine(" done.");
            Console.WriteLine();

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

    public static class StaticeDataFeedExtensions
    {
        public static IXDescriptor ToIXDescriptor(this StaticDataFeed dataItem)
        {
            return new IXDescriptor(
                    new IXDescriptorProperty("id",                dataItem.id.ToString(),                 null, FieldFlags.UNIQUEID, FieldStore.YES, FieldIndex.NOT_ANALYZED),
                    new IXDescriptorProperty("titolo",            dataItem.titolo,                        null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.ANALYZED),
                    new IXDescriptorProperty("titolo_orig",       dataItem.titolo_orig,                   null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.ANALYZED),
                    new IXDescriptorProperty("anno",              dataItem.anno.ToString(),               null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.NOT_ANALYZED),
                    new IXDescriptorProperty("attivo",            dataItem.attivo.ToString(),             null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.NOT_ANALYZED),
                    new IXDescriptorProperty("data_inserimento",  dataItem.data_inserimento.ToString(),   null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.NOT_ANALYZED),
                    new IXDescriptorProperty("artista",           dataItem.artista.ToString(),            null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.ANALYZED),
                    new IXDescriptorProperty("regista",           dataItem.regista.ToString(),            null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.ANALYZED),
                    new IXDescriptorProperty("sceneggiatore",     dataItem.sceneggiatore.ToString(),      null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.ANALYZED),
                    new IXDescriptorProperty("produttore",        dataItem.produttore.ToString(),         null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.ANALYZED),
                    new IXDescriptorProperty("generi",            dataItem.generi.ToString(),             null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.ANALYZED),
                    new IXDescriptorProperty("tags",              dataItem.tags.ToString(),               null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.ANALYZED),
                    new IXDescriptorProperty("data_rilascio",     dataItem.data_rilascio.ToString(),      null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.NOT_ANALYZED),
                    new IXDescriptorProperty("dettagli",          dataItem.dettagli.ToString(),           null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.ANALYZED),
                    new IXDescriptorProperty("libro",             dataItem.libro.ToString(),              null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.ANALYZED),
                    new IXDescriptorProperty("consigliato",       dataItem.consigliato.ToString(),        null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.ANALYZED),
                    new IXDescriptorProperty("recensione",        dataItem.recensione.ToString(),         null, FieldFlags.NONE,     FieldStore.YES, FieldIndex.ANALYZED)
               );
        }
    }
}
