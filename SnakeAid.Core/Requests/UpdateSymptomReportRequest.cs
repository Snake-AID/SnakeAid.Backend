using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAid.Core.Requests
{
    public class UpdateSymptomReportRequest
    {
        [Required(ErrorMessage = "Must have at least 1 symptom selected.")]
        public List<int> SymptomIdList { get; set; } = null!;
        public int? TimeSinceBiteMinutes { get; set; }
    }
}
