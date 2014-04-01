using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testfs.Model
{
    public class Scan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ScanId { get; set; }

        [Required]
        public string ScanPath { get; set; }

        [Required, MinLength(32), MaxLength(64)]
        public string ScanPathHash { get; set; }

        [Required]
        public string HashAlg { get; set; }

        public virtual ICollection<File> Files { get; set; }
    }
}
