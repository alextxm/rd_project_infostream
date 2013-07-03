using System;
using System.Collections.Generic;

#if USE_EF_DATAFEED
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#endif

using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace idx
{
    //[DataContract(Name = "StaticDataFeed")]
    public class StaticDataFeed
    {
        #if USE_EF_DATAFEED
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        #endif
        //[DataMember]
        public int id { get; set; }

        #if USE_EF_DATAFEED
        [MaxLength(256)]
        #endif
        //[DataMember]
        public string titolo { get; set; }
        
        #if USE_EF_DATAFEED
        [MaxLength(256)]
        #endif
        //[DataMember]
        public string titolo_orig { get; set; }

        //[DataMember]
        public int anno { get; set; }

        //[DataMember]
        public bool attivo { get; set; }

        //[DataMember]
        public long id_major { get; set; }

        #if USE_EF_DATAFEED
        [MaxLength(16)]
        #endif
        //[DataMember]
        public string codice_prodotto { get; set; }

        #if USE_EF_DATAFEED
        [MaxLength(16)]
        #endif
        //[DataMember]
        public string codicemw { get; set; }

        #if USE_EF_DATAFEED
        [MaxLength(16)]
        #endif
        //[DataMember]
        public string codicemw_st { get; set; }

        //[DataMember]
        public System.DateTime data_inserimento { get; set; }

        #if USE_EF_DATAFEED
        [MaxLength(256)]
        #endif
        //[DataMember]
        public string artista { get; set; }
        
        #if USE_EF_DATAFEED
        [MaxLength(256)]
        #endif
        //[DataMember]
        public string regista { get; set; }
        
        #if USE_EF_DATAFEED
        [MaxLength(256)]
        #endif
        //[DataMember]
        public string sceneggiatore { get; set; }
        
        #if USE_EF_DATAFEED
        [MaxLength(256)]
        #endif
        //[DataMember]
        public string produttore { get; set; }

        //[DataMember]
        public long durata { get; set; }
        
        #if USE_EF_DATAFEED
        [MaxLength(256)]
        #endif
        //[DataMember]
        public string generi { get; set; }
        
        #if USE_EF_DATAFEED
        [MaxLength(256)]
        #endif
        //[DataMember]
        public string tags { get; set; }

        //[DataMember]
        public System.DateTime data_rilascio { get; set; }
        
        #if USE_EF_DATAFEED
        [MaxLength(1000)]
        #endif
        //[DataMember]
        public string dettagli { get; set; }
        
        #if USE_EF_DATAFEED
        [MaxLength(256)]
        #endif
        //[DataMember]
        public string libro { get; set; }
        
        #if USE_EF_DATAFEED
        [MaxLength(32)]
        #endif
        //[DataMember]
        public string consigliato { get; set; }

        #if USE_EF_DATAFEED
        [MaxLength(4000)]
        #endif
        //[DataMember]
        public string recensione { get; set; }
    }
}
