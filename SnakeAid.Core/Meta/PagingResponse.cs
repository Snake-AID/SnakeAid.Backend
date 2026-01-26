using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SnakeAid.Core.Meta
{
    public class PagingResponse<T>
    {
        [JsonPropertyName("items")]
        public IEnumerable<T> Items { get; set; }

        [JsonPropertyName("meta")]
        public PaginationMeta Meta { get; set; }
    }
}
