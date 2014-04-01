using System;
using System.Collections.Generic;
#if DATAFEED_TVC
using System.Data.Entity.Validation;
#endif
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;

using utils;
using Ionic.Zlib;

namespace datatypes
{
    using datatypes.netmovie;

    public static class StaticDataFeedManager
    {
        public static bool BuildStaticDataFeed(bool compressed = false, string dataFile = "data.feed")
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
                ConsoleUtils.WriteAt(String.Format("{0} of {1} [{2}]     ", i, cl.Count, t.id), 0, 0, origRow, origCol);

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
            DataContractSerialization<IEnumerable<StaticDataFeed>> serializer = new DataContractSerialization<IEnumerable<StaticDataFeed>>();

            using (Stream fs = File.Open(dataFile, FileMode.Create))
            {
                Stream s = (compressed) ? new GZipStream(fs, CompressionMode.Compress) : fs;
                serializer.SerializeToStream(theFeed, s);
                if (compressed)
                    s.Dispose();
            }

            Console.WriteLine(" done.");
            Console.WriteLine();

            //ctx.SaveChanges();
            return true;
#else
            return true;
#endif
        }

        public static List<StaticDataFeed> ReadStaticDataFeed(bool compressed = false, string dataFile = "data.feed", int bufferSize=4096)
        {
            DataContractSerialization<IEnumerable<StaticDataFeed>> serializer = new DataContractSerialization<IEnumerable<StaticDataFeed>>();

            if (String.IsNullOrEmpty(dataFile) || !File.Exists(dataFile))
                return new List<StaticDataFeed>();

            if (bufferSize < 1024)
                bufferSize = 1024;

            IEnumerable<StaticDataFeed> data = null;
            using (MemoryStream ms = new MemoryStream())
            {
                using (Stream fs = File.Open(dataFile, FileMode.Open))
                {
                    Stream s = (compressed) ? new GZipStream(fs, CompressionMode.Decompress) : fs;

                    int size = bufferSize;
                    byte[] writeData = new byte[bufferSize];

                    while (true)
                    {
                        size = s.Read(writeData, 0, size);

                        if (size > 0)
                            ms.Write(writeData, 0, size);
                        else
                            break;
                    }

                    if (compressed)
                        s.Dispose();
                }

                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);

                data = serializer.DeserializeFromStream(ms);
            }

            return data.ToList();
        }
    }
}
