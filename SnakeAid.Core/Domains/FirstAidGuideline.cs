using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class FirstAidGuideline : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public FirstAidContent Content { get; set; }

    }

    public class FirstAidContent
    {
        public List<string> DoList { get; set; } = new();

        public List<string> DontList { get; set; } = new();

        public List<string> ImageExamples { get; set; } = new();  // URLs or base64 strings
    }
}