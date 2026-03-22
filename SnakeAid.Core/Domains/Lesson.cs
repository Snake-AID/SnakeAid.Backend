using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class Lesson : BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public Category Category { get; set; }

        [Required]
        public bool IsPublished { get; set; } = true;

    }

    public enum Category
    {
        FirstAid = 1,
        Catching = 2,
        Safety = 3,
    }
}