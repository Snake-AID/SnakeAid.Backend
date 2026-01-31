using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class FirstAidGuideline : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [Required]
        [Column(TypeName = "jsonb")]
        public string Content { get; set; }

        [MaxLength(500)]
        public string? Summary { get; set; }  // Tóm tắt ngắn

    }
}