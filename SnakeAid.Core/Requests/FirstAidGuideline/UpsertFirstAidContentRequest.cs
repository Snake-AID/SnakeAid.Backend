using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Requests.FirstAidGuideline
{
    public class FirstAidContentRequest
    {
        public List<FirstAidStepRequest> Steps { get; set; } = new();
        public List<FirstAidStepRequest> Dos { get; set; } = new();
        public List<FirstAidStepRequest> Donts { get; set; } = new();
        public List<string> Notes { get; set; } = new();
    }

    public class FirstAidStepRequest
    {
        public string Text { get; set; } = string.Empty;
        public string? MediaUrl { get; set; }
        public Guid? MediaId { get; set; }
    }

}