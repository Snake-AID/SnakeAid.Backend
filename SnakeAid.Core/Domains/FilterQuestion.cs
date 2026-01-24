using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class FilterQuestion : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Question { get; set; }

        [Required]
        public bool IsActive { get; set; }

        public ICollection<FilterOption> FilterOptions { get; set; } = new List<FilterOption>();
    }
}