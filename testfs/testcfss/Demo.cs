using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testcfss
{
    public class Demo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long DemoId { get; set; }
        
        [Required]
        public string Info { get; set; }

        [MaxLength(100)]
        public string AuxInfo1 { get; set; }
    }
}
