using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class FilterQuestion : BaseEntity
    {
        public int Id { get; set; }
        public string Question { get; set; }
        public bool IsActive { get; set; }

        public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
    }
}