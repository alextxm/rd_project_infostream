//------------------------------------------------------------------------------
// <auto-generated>
//    Codice generato da un modello.
//
//    Le modifiche manuali a questo file potrebbero causare un comportamento imprevisto dell'applicazione.
//    Se il codice viene rigenerato, le modifiche manuali al file verranno sovrascritte.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ISClient
{
    using System;
    using System.Collections.Generic;
    
    public partial class contenuti
    {
        public long id { get; set; }
        public string titolo { get; set; }
        public string titolo_orig { get; set; }
        public double prezzo { get; set; }
        public double prezzo_st { get; set; }
        public Nullable<long> id_genere { get; set; }
        public Nullable<long> anno { get; set; }
        public Nullable<sbyte> attivo { get; set; }
        public long id_major { get; set; }
        public string codice_prodotto { get; set; }
        public string codicemw { get; set; }
        public string codicemw_st { get; set; }
        public System.DateTime data_inserimento { get; set; }
        public string codice_prezzo_major { get; set; }
        public string codice_prezzo_major_st { get; set; }
        public string artista { get; set; }
        public string regista { get; set; }
        public string sceneggiatore { get; set; }
        public string produttore { get; set; }
        public long durata { get; set; }
        public string generi { get; set; }
        public Nullable<decimal> voto { get; set; }
        public Nullable<int> num_voti { get; set; }
        public string titolo_search { get; set; }
        public string artista_search { get; set; }
        public string regista_search { get; set; }
        public string preview_url { get; set; }
        public int inricerca { get; set; }
        public int bundle { get; set; }
        public string tags { get; set; }
        public int cmgroupid { get; set; }
        public string targetdevice { get; set; }
    }
}