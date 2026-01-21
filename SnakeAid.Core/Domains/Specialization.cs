using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class Specialization : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}