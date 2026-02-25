using System;
using System.Collections.Generic;

namespace SnakeAid.Core.Domains
{
    public interface IHasReportMedia
    {
        Guid Id { get; set; }
        ICollection<ReportMedia> Media { get; set; }
    }
}
