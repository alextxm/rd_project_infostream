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
    public class Service1 : IService1
    {
        private IndexerInterop<InterchangeDocument> li = null;
        public Service1()
        {
            //IndexerInterop<InterchangeDocument> li = new IndexerInterop<InterchangeDocument>(
            //        IndexerStorageMode.FSRAM,
            //        IndexerAnalyzer.StandardAnalyzer,
            //        new StaticeDataFeedLI(delegate(int id) { return data.FirstOrDefault(p => p.id == id); }));
        }

        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }
    }
}
