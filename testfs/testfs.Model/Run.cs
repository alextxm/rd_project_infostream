using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace testfs.Model
{
    public class Run
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long RunId { get; set; }

        [Required, MinLength(36), MaxLength(36)]
        public string CheckId { get; set; }

        [Required]
        public DateTime RunDate { get; set; }

        public virtual ICollection<File> Files { get; set; }
    }
}
