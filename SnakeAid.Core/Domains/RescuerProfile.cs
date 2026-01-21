using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SnakeAid.Core.Domains
{
    public class RescuerProfile : Account
    {
        public bool IsOnline { get; set; }
        public float Rating { get; set; }
        public int RatingCount { get; set; }
    }

    public enum RescuerType
    {
        Emergency = 0,
        Catching = 1,
        Both = 2,
    }
}