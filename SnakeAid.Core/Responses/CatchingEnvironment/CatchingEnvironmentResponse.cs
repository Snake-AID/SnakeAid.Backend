using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.CatchingEnvironment
{
    public class CatchingEnvironmentResponse
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal Price { get; set; }

        [StringLength(5)]
        public string Currency { get; set; }
    }
}
