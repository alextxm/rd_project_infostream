using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testfs.Model
{
    public class File
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long FileId { get; set; }

        [Required]
        public string FilePath { get; set; }

        // indexed
        [Required, MinLength(32), MaxLength(64)]
        public string FilePathHash { get; set; }

        [Required]
        public System.DateTime LastWrite { get; set; }

        public long ScanId { get; set; } // FK per Scan
        [ForeignKey("ScanId")]
        public virtual Scan Scan { get; set; }

        public long RunId { get; set; } // FK per Runs
        [ForeignKey("RunId")]
        public virtual Run Run { get; set; }
    }
}
