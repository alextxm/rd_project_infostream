using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

using InfoStream.Core;
using InfoStream.Metadata;

namespace CoreService
{
    // NOTA: è possibile utilizzare il comando "Rinomina" del menu "Refactoring" per modificare il nome di classe "Service1" nel codice, nel file svc e nel file di configurazione contemporaneamente.
    // NOTA: per avviare il client di prova WCF per testare il servizio, selezionare Service1.svc o Service1.svc.cs in Esplora soluzioni e avviare il debug.
    //[ServiceBehavior(InstanceContextMode.Single)]
    public class Service1 : IService1
    {
        private ISIndexer indexer = null;
        public Service1()
        {
            indexer = CacheManager.GetDataCache();
        }

        public IXQueryCollection SearchData(IXRequest request)
        {
            return indexer.Search(request);   
        }

        //public bool AddUpdateData(IEnumerable<InterchangeDocument> documents)
        //{
        //   return li.AddUpdateLuceneIndex(documents, true);
        //}

        //public bool RemoveData(IEnumerable<InterchangeDocument> documents)
        //{
        //    return li.DeleteFromIndex(documents.ElementAt(0));
        //}
    }
}
