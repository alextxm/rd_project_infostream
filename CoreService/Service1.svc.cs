using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

using corelib;
using corelib.Interchange;

namespace CoreService
{
    // NOTA: è possibile utilizzare il comando "Rinomina" del menu "Refactoring" per modificare il nome di classe "Service1" nel codice, nel file svc e nel file di configurazione contemporaneamente.
    // NOTA: per avviare il client di prova WCF per testare il servizio, selezionare Service1.svc o Service1.svc.cs in Esplora soluzioni e avviare il debug.
    //[ServiceBehavior(InstanceContextMode.Single)]
    public class Service1 : IService1
    {
        private IndexerInterop<InterchangeDocument> li = null;
        public Service1()
        {
            li = CacheManager.GetDataCache();
        }

        public InterchangeDocumentsCollection SearchData(string query, int skip, int take, bool useScoring, IEnumerable<string> filteredFields)
        {
            InterchangeDocumentsCollection coll = new InterchangeDocumentsCollection() { Result=false, Start=0, Take=0, InterchangeDocuments=null, Count=0 };

            if (take <= 0)
                take = 10;

            if (skip <= 0)
                skip = 0;

            if(String.IsNullOrEmpty(query))
                return coll;

            coll.InterchangeDocuments = li.Search(query, String.Empty, skip, take, filteredFields).ScoreDocs.Select(p => new InterchangeDocumentInfo() { Score = p.Score, Element = p.Element });
            return coll;
        }
    }
}
