using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Responses.SnakebiteIncident
{
    public class UpdateSymptomReportResponse
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        [Column(TypeName = "jsonb")]
        public string? SymptomsReport { get; set; }

        public int? SeverityLevel { get; set; }
    }
}
