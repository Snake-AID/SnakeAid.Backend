using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;

namespace SnakeAid.Core.Domains
{
    public class CommunityReport : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Point LocationCoordinates { get; set; }
        public string AdditionalDetails { get; set; }
    }
}