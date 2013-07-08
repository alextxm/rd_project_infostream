using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

using corelib.Interchange;

namespace CoreService
{
    // NOTA: è possibile utilizzare il comando "Rinomina" del menu "Refactoring" per modificare il nome di interfaccia "IService1" nel codice e nel file di configurazione contemporaneamente.
    [ServiceContract]
    public interface IService1
    {
        [OperationContract]
        InterchangeDocumentsCollection SearchData(string query, int skip, int take, bool useScoring, IEnumerable<string> filteredFields);
    }
}
