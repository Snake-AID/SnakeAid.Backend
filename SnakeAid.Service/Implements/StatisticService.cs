using Microsoft.EntityFrameworkCore;
using SnakeAid.Core.Domains;
using SnakeAid.Core.Exceptions;
using SnakeAid.Core.Requests.Statistic;
using SnakeAid.Core.Responses.Statistic;
using SnakeAid.Repository.Data;
using SnakeAid.Repository.Interfaces;
using SnakeAid.Service.Interfaces;

namespace SnakeAid.Service.Implements;

public class StatisticService : IStatisticService
{
    private readonly IUnitOfWork<SnakeAidDbContext> _unitOfWork;

    public StatisticService(IUnitOfWork<SnakeAidDbContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RevenueAnalyticsResponse> GetRevenueAsync(RevenueAnalyticsQueryRequest request, CancellationToken cancellationToken = default)
    {
        var period = ParsePeriod(request.Period);
        var flowFilter = ParseFlow(request.Flow);
        ValidateDateRange(request.From, request.To);

        var transactions = await GetTransactionsInRangeAsync(request.From, request.To, RelevantRevenueTypes, cancellationToken);
        var labels = BuildLabels(request.From, request.To, period);
        var buckets = CreateFlowBuckets(labels);

        foreach (var tx in transactions)
        {
            var flow = MapRevenueFlow(tx.TransactionType);
            if (flow is null)
            {
                continue;
            }

            if (flowFilter.HasValue && flow != flowFilter.Value)
            {
                continue;
            }

            var label = GetLabel(tx.CreatedAt, period);
            if (!buckets.TryGetValue(label, out var flowValues))
            {
                continue;
            }

            var signedAmount = tx.TransactionType == TransactionType.ConsultationRefund ? -tx.Amount : tx.Amount;
            flowValues[flow.Value] += signedAmount;
        }

        var byFlow = BuildByFlow(buckets.Values, flowFilter);
        var timeline = BuildRevenueTimeline(labels, buckets, flowFilter);

        return new RevenueAnalyticsResponse
        {
            From = request.From.ToString("yyyy-MM-dd"),
            To = request.To.ToString("yyyy-MM-dd"),
            Period = request.Period.Trim().ToLowerInvariant(),
            Currency = "VND",
            Total = byFlow.Values.Sum(),
            ByFlow = byFlow,
            Timeline = timeline
        };
    }

    public async Task<CommissionAnalyticsResponse> GetCommissionAsync(CommissionAnalyticsQueryRequest request, CancellationToken cancellationToken = default)
    {
        var period = ParsePeriod(request.Period);
        ValidateDateRange(request.From, request.To);

        var transactions = await GetTransactionsInRangeAsync(request.From, request.To, RelevantCommissionTypes, cancellationToken);
        var labels = BuildLabels(request.From, request.To, period);

        var timelineBuckets = labels.ToDictionary(
            l => l,
            _ => new CommissionBucket());

        foreach (var tx in transactions)
        {
            var label = GetLabel(tx.CreatedAt, period);
            if (!timelineBuckets.TryGetValue(label, out var bucket))
            {
                continue;
            }

            switch (tx.TransactionType)
            {
                case TransactionType.ConsultationPayment:
                    bucket.Revenue += tx.Amount;
                    break;
                case TransactionType.ExpertPayout:
                    bucket.ExpertPayout += tx.Amount;
                    break;
                case TransactionType.ConsultationRefund:
                    bucket.Refund += tx.Amount;
                    break;
            }
        }

        var timeline = labels.Select(label =>
        {
            var bucket = timelineBuckets[label];
            var commission = bucket.Revenue - bucket.ExpertPayout - bucket.Refund;
            return new CommissionTimelinePoint
            {
                Label = label,
                Revenue = bucket.Revenue,
                ExpertPayout = bucket.ExpertPayout,
                Refund = bucket.Refund,
                Commission = commission
            };
        }).ToList();

        return new CommissionAnalyticsResponse
        {
            From = request.From.ToString("yyyy-MM-dd"),
            To = request.To.ToString("yyyy-MM-dd"),
            Period = request.Period.Trim().ToLowerInvariant(),
            Currency = "VND",
            TotalCommission = timeline.Sum(x => x.Commission),
            RateNote = "Hoa hồng = ConsultationPayment - ExpertPayout - ConsultationRefund",
            Timeline = timeline
        };
    }

    public async Task<ProfitAnalyticsResponse> GetProfitAsync(ProfitAnalyticsQueryRequest request, CancellationToken cancellationToken = default)
    {
        var period = ParsePeriod(request.Period);
        var flowFilter = ParseFlow(request.Flow);
        ValidateDateRange(request.From, request.To);

        var transactions = await GetTransactionsInRangeAsync(request.From, request.To, RelevantProfitTypes, cancellationToken);
        var labels = BuildLabels(request.From, request.To, period);
        var buckets = CreateFlowBuckets(labels);

        foreach (var tx in transactions)
        {
            var flow = MapProfitFlow(tx.TransactionType);
            if (flow is null)
            {
                continue;
            }

            if (flowFilter.HasValue && flow != flowFilter.Value)
            {
                continue;
            }

            var label = GetLabel(tx.CreatedAt, period);
            if (!buckets.TryGetValue(label, out var flowValues))
            {
                continue;
            }

            flowValues[flow.Value] += GetProfitSignedAmount(tx);
        }

        var byFlow = BuildByFlow(buckets.Values, flowFilter);
        var timeline = BuildProfitTimeline(labels, buckets, flowFilter);

        return new ProfitAnalyticsResponse
        {
            From = request.From.ToString("yyyy-MM-dd"),
            To = request.To.ToString("yyyy-MM-dd"),
            Period = request.Period.Trim().ToLowerInvariant(),
            Currency = "VND",
            TotalProfit = byFlow.Values.Sum(),
            ByFlow = byFlow,
            Timeline = timeline
        };
    }

    public async Task<UserAnalyticsResponse> GetUsersAsync(UserAnalyticsQueryRequest request, CancellationToken cancellationToken = default)
    {
        var period = ParsePeriod(request.Period);
        ValidateDateRange(request.From, request.To);

        var labels = BuildLabels(request.From, request.To, period);
        var countsByLabel = labels.ToDictionary(x => x, _ => 0);

        var fromDate = DateTime.SpecifyKind(request.From.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toExclusive = DateTime.SpecifyKind(request.To.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc).AddDays(1);

        var users = await _unitOfWork
            .GetRepository<Account>()
            .CreateBaseQuery(true)
            .Where(x => x.CreatedAt >= fromDate && x.CreatedAt < toExclusive)
            .Select(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var createdAt in users)
        {
            var label = GetLabel(createdAt, period);
            if (countsByLabel.ContainsKey(label))
            {
                countsByLabel[label] += 1;
            }
        }

        return new UserAnalyticsResponse
        {
            From = request.From.ToString("yyyy-MM-dd"),
            To = request.To.ToString("yyyy-MM-dd"),
            Period = request.Period.Trim().ToLowerInvariant(),
            TotalUsers = countsByLabel.Values.Sum(),
            Timeline = labels.Select(label => new UserTimelinePoint
            {
                Label = label,
                TotalUsers = countsByLabel[label]
            }).ToList()
        };
    }

    public async Task<CaseAnalyticsResponse> GetCasesAsync(CaseAnalyticsQueryRequest request, CancellationToken cancellationToken = default)
    {
        var period = ParsePeriod(request.Period);
        ValidateDateRange(request.From, request.To);

        var labels = BuildLabels(request.From, request.To, period);
        var timelineBuckets = labels.ToDictionary(x => x, _ => new CaseBucket());

        var fromDate = DateTime.SpecifyKind(request.From.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toExclusive = DateTime.SpecifyKind(request.To.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc).AddDays(1);

        var rescueCreatedAt = await _unitOfWork
            .GetRepository<SnakebiteIncident>()
            .CreateBaseQuery(true)
            .Where(x => x.CreatedAt >= fromDate && x.CreatedAt < toExclusive)
            .Select(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var catchingCreatedAt = await _unitOfWork
            .GetRepository<SnakeCatchingRequest>()
            .CreateBaseQuery(true)
            .Where(x => x.CreatedAt >= fromDate && x.CreatedAt < toExclusive)
            .Select(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        IncrementCaseBuckets(timelineBuckets, rescueCreatedAt, period, CaseFlow.Snakebite);
        IncrementCaseBuckets(timelineBuckets, catchingCreatedAt, period, CaseFlow.Catching);

        var timeline = labels.Select(label =>
        {
            var bucket = timelineBuckets[label];
            var snakebiteCases = bucket.Snakebite;
            var snakeCatchingCases = bucket.Catching;

            return new CaseTimelinePoint
            {
                Label = label,
                TotalCases = snakebiteCases + snakeCatchingCases,
                SnakebiteCases = snakebiteCases,
                SnakeCatchingCases = snakeCatchingCases
            };
        }).ToList();

        var snakebiteTotal = timeline.Sum(x => x.SnakebiteCases);
        var snakeCatchingTotal = timeline.Sum(x => x.SnakeCatchingCases);

        return new CaseAnalyticsResponse
        {
            From = request.From.ToString("yyyy-MM-dd"),
            To = request.To.ToString("yyyy-MM-dd"),
            Period = request.Period.Trim().ToLowerInvariant(),
            TotalCases = snakebiteTotal + snakeCatchingTotal,
            SnakebiteCases = snakebiteTotal,
            SnakeCatchingCases = snakeCatchingTotal,
            Timeline = timeline
        };
    }

    public async Task<RescuerTodayStatisticsResponse> GetRescuerStatisticsAsync(Guid rescuerId, string period, CancellationToken cancellationToken = default)
    {
        var rolePeriod = ParseRolePeriod(period);
        var (startUtc, endUtc) = GetRolePeriodRangeUtc(rolePeriod);

        var snakebiteRequests = await _unitOfWork
            .GetRepository<SnakebiteIncident>()
            .CountAsync(x => x.AssignedRescuerId == rescuerId
                             && x.AssignedAt.HasValue
                             && x.AssignedAt.Value >= startUtc
                             && x.AssignedAt.Value < endUtc,
                cancellationToken);

        var snakeCatchingRequests = await _unitOfWork
            .GetRepository<SnakeCatchingRequest>()
            .CountAsync(x => x.AssignedRescuerId == rescuerId
                             && x.AssignedAt.HasValue
                             && x.AssignedAt.Value >= startUtc
                             && x.AssignedAt.Value < endUtc,
                cancellationToken);

        var snakebiteCompleted = await _unitOfWork
            .GetRepository<RescueMission>()
            .CountAsync(x => x.RescuerId == rescuerId
                             && x.Status == RescueMissionStatus.MissionCompleted
                             && x.CompletedAt.HasValue
                             && x.CompletedAt.Value >= startUtc
                             && x.CompletedAt.Value < endUtc,
                cancellationToken);

        var snakeCatchingCompleted = await _unitOfWork
            .GetRepository<SnakeCatchingMission>()
            .CountAsync(x => x.RescuerId == rescuerId
                             && x.Status == CatchingMissionStatus.MissionCompleted
                             && x.CompletedAt.HasValue
                             && x.CompletedAt.Value >= startUtc
                             && x.CompletedAt.Value < endUtc,
                cancellationToken);

        var rescuerIncome = await _unitOfWork
            .GetRepository<Transaction>()
            .CreateBaseQuery(true)
            .Where(x => x.UserId == rescuerId
                        && x.CreatedAt.HasValue
                        && x.CreatedAt.Value >= startUtc
                        && x.CreatedAt.Value < endUtc
                        && (x.TransactionType == TransactionType.CatcherPayout
                            || x.TransactionType == TransactionType.RescuerReward))
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        return new RescuerTodayStatisticsResponse
        {
            Period = ToRolePeriodName(rolePeriod),
            From = startUtc.ToString("yyyy-MM-dd"),
            To = endUtc.AddDays(-1).ToString("yyyy-MM-dd"),
            SnakebiteRequests = snakebiteRequests,
            SnakeCatchingRequests = snakeCatchingRequests,
            TotalRequests = snakebiteRequests + snakeCatchingRequests,
            SnakebiteCompleted = snakebiteCompleted,
            SnakeCatchingCompleted = snakeCatchingCompleted,
            TotalCompleted = snakebiteCompleted + snakeCatchingCompleted,
            TotalIncome = rescuerIncome,
            Currency = "VND"
        };
    }

    public async Task<ExpertTodayStatisticsResponse> GetExpertStatisticsAsync(Guid expertId, string period, CancellationToken cancellationToken = default)
    {
        var rolePeriod = ParseRolePeriod(period);
        var (startUtc, endUtc) = GetRolePeriodRangeUtc(rolePeriod);

        var scheduledRequests = await _unitOfWork
            .GetRepository<ConsultationBooking>()
            .CountAsync(x => x.ExpertId == expertId
                             && x.CreatedAt >= startUtc
                             && x.CreatedAt < endUtc,
                cancellationToken);

        var emergencyRequests = await _unitOfWork
            .GetRepository<ConsultationPingRequest>()
            .CountAsync(x => x.ExpertId == expertId
                             && x.RequestedAt >= startUtc
                             && x.RequestedAt < endUtc,
                cancellationToken);

        var completedConsultations = await _unitOfWork
            .GetRepository<Consultation>()
            .CountAsync(x => x.CalleeId == expertId
                             && x.Status == ConsultationStatus.Completed
                             && x.EndTime.HasValue
                             && x.EndTime.Value >= startUtc
                             && x.EndTime.Value < endUtc,
                cancellationToken);

        var expertIncome = await _unitOfWork
            .GetRepository<Transaction>()
            .CreateBaseQuery(true)
            .Where(x => x.UserId == expertId
                        && x.CreatedAt.HasValue
                        && x.CreatedAt.Value >= startUtc
                        && x.CreatedAt.Value < endUtc
                        && x.TransactionType == TransactionType.ExpertPayout)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        return new ExpertTodayStatisticsResponse
        {
            Period = ToRolePeriodName(rolePeriod),
            From = startUtc.ToString("yyyy-MM-dd"),
            To = endUtc.AddDays(-1).ToString("yyyy-MM-dd"),
            ConsultationRequests = scheduledRequests + emergencyRequests,
            CompletedConsultations = completedConsultations,
            TotalIncome = expertIncome,
            Currency = "VND"
        };
    }

    private async Task<List<TransactionLite>> GetTransactionsInRangeAsync(
        DateOnly from,
        DateOnly to,
        HashSet<TransactionType> types,
        CancellationToken cancellationToken)
    {
        var fromDate = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toExclusive = DateTime.SpecifyKind(to.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc).AddDays(1);

        return await _unitOfWork
            .GetRepository<Transaction>()
            .CreateBaseQuery(true)
            .Where(t => t.CreatedAt.HasValue
                        && t.CreatedAt.Value >= fromDate
                        && t.CreatedAt.Value < toExclusive
                        && types.Contains(t.TransactionType))
            .Select(t => new TransactionLite
            {
                TransactionType = t.TransactionType,
                Amount = t.Amount,
                CreatedAt = t.CreatedAt!.Value
            })
            .ToListAsync(cancellationToken);
    }

    private static Dictionary<string, decimal> BuildByFlow(IEnumerable<FlowBucket> buckets, AnalyticsFlow? flowFilter)
    {
        var consultation = buckets.Sum(b => b[AnalyticsFlow.Consultation]);
        var catching = buckets.Sum(b => b[AnalyticsFlow.Catching]);
        var snakebite = buckets.Sum(b => b[AnalyticsFlow.Snakebite]);

        if (flowFilter.HasValue)
        {
            return new Dictionary<string, decimal>
            {
                [ToFlowName(flowFilter.Value)] = flowFilter.Value switch
                {
                    AnalyticsFlow.Consultation => consultation,
                    AnalyticsFlow.Catching => catching,
                    AnalyticsFlow.Snakebite => snakebite,
                    _ => 0m
                }
            };
        }

        return new Dictionary<string, decimal>
        {
            ["consultation"] = consultation,
            ["catching"] = catching,
            ["snakebite"] = snakebite
        };
    }

    private static List<Dictionary<string, object>> BuildRevenueTimeline(
        IEnumerable<string> labels,
        Dictionary<string, FlowBucket> buckets,
        AnalyticsFlow? flowFilter)
    {
        var timeline = new List<Dictionary<string, object>>();

        foreach (var label in labels)
        {
            var bucket = buckets[label];
            var consultation = bucket[AnalyticsFlow.Consultation];
            var catching = bucket[AnalyticsFlow.Catching];
            var snakebite = bucket[AnalyticsFlow.Snakebite];

            var row = new Dictionary<string, object>
            {
                ["label"] = label,
                ["total"] = consultation + catching + snakebite
            };

            if (flowFilter.HasValue)
            {
                row[ToFlowName(flowFilter.Value)] = bucket[flowFilter.Value];
            }
            else
            {
                row["consultation"] = consultation;
                row["catching"] = catching;
                row["snakebite"] = snakebite;
            }

            timeline.Add(row);
        }

        return timeline;
    }

    private static List<Dictionary<string, object>> BuildProfitTimeline(
        IEnumerable<string> labels,
        Dictionary<string, FlowBucket> buckets,
        AnalyticsFlow? flowFilter)
    {
        var timeline = new List<Dictionary<string, object>>();

        foreach (var label in labels)
        {
            var bucket = buckets[label];
            var consultation = bucket[AnalyticsFlow.Consultation];
            var catching = bucket[AnalyticsFlow.Catching];
            var snakebite = bucket[AnalyticsFlow.Snakebite];

            var row = new Dictionary<string, object>
            {
                ["label"] = label,
                ["totalProfit"] = consultation + catching + snakebite
            };

            if (flowFilter.HasValue)
            {
                row[ToFlowName(flowFilter.Value)] = bucket[flowFilter.Value];
            }
            else
            {
                row["consultation"] = consultation;
                row["catching"] = catching;
                row["snakebite"] = snakebite;
            }

            timeline.Add(row);
        }

        return timeline;
    }

    private static Dictionary<string, FlowBucket> CreateFlowBuckets(IEnumerable<string> labels)
    {
        return labels.ToDictionary(
            l => l,
            _ => new FlowBucket());
    }

    private static (DateTime startUtc, DateTime endUtc) GetRolePeriodRangeUtc(RolePeriod period)
    {
        var now = DateTime.UtcNow;

        return period switch
        {
            RolePeriod.Today => (now.Date, now.Date.AddDays(1)),
            RolePeriod.Month =>
            (
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)
            ),
            RolePeriod.Year =>
            (
                new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(now.Year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            ),
            _ => throw new BadRequestException("Invalid period. Supported values: today, month, year.")
        };
    }

    private static RolePeriod ParseRolePeriod(string? period)
    {
        var normalized = (period ?? string.Empty).Trim().ToLowerInvariant();

        return normalized switch
        {
            "today" or "day" => RolePeriod.Today,
            "month" => RolePeriod.Month,
            "year" => RolePeriod.Year,
            _ => throw new BadRequestException("Invalid period. Supported values: today, month, year.")
        };
    }

    private static string ToRolePeriodName(RolePeriod period)
    {
        return period switch
        {
            RolePeriod.Today => "today",
            RolePeriod.Month => "month",
            RolePeriod.Year => "year",
            _ => "today"
        };
    }

    private static void IncrementCaseBuckets(
        Dictionary<string, CaseBucket> timelineBuckets,
        IEnumerable<DateTime> timestamps,
        AnalyticsPeriod period,
        CaseFlow flow)
    {
        foreach (var ts in timestamps)
        {
            var label = GetLabel(ts, period);
            if (!timelineBuckets.TryGetValue(label, out var bucket))
            {
                continue;
            }

            switch (flow)
            {
                case CaseFlow.Snakebite:
                    bucket.Snakebite += 1;
                    break;
                case CaseFlow.Catching:
                    bucket.Catching += 1;
                    break;
            }
        }
    }

    private static decimal GetProfitSignedAmount(TransactionLite tx)
    {
        return tx.TransactionType switch
        {
            TransactionType.ExpertPayout => -tx.Amount,
            TransactionType.ConsultationRefund => -tx.Amount,
            _ => tx.Amount
        };
    }

    private static AnalyticsFlow? MapRevenueFlow(TransactionType type)
    {
        return type switch
        {
            TransactionType.ConsultationPayment => AnalyticsFlow.Consultation,
            TransactionType.ConsultationRefund => AnalyticsFlow.Consultation,
            TransactionType.CatchingPayment => AnalyticsFlow.Catching,
            TransactionType.CatchingDeposit => AnalyticsFlow.Catching,
            TransactionType.SnakebiteIncidentPayment => AnalyticsFlow.Snakebite,
            _ => null
        };
    }

    private static AnalyticsFlow? MapProfitFlow(TransactionType type)
    {
        return type switch
        {
            TransactionType.ConsultationPayment => AnalyticsFlow.Consultation,
            TransactionType.ExpertPayout => AnalyticsFlow.Consultation,
            TransactionType.ConsultationRefund => AnalyticsFlow.Consultation,
            TransactionType.CatchingPayment => AnalyticsFlow.Catching,
            TransactionType.CatchingDeposit => AnalyticsFlow.Catching,
            TransactionType.SnakebiteIncidentPayment => AnalyticsFlow.Snakebite,
            _ => null
        };
    }

    private static List<string> BuildLabels(DateOnly from, DateOnly to, AnalyticsPeriod period)
    {
        if (from > to)
        {
            return new List<string>();
        }

        return period switch
        {
            AnalyticsPeriod.Day => BuildDayLabels(from, to),
            AnalyticsPeriod.Month => BuildMonthLabels(from, to),
            AnalyticsPeriod.Year => BuildYearLabels(from, to),
            _ => new List<string>()
        };
    }

    private static List<string> BuildDayLabels(DateOnly from, DateOnly to)
    {
        var labels = new List<string>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            labels.Add(date.ToString("yyyy-MM-dd"));
        }

        return labels;
    }

    private static List<string> BuildMonthLabels(DateOnly from, DateOnly to)
    {
        var labels = new List<string>();
        var cursor = new DateOnly(from.Year, from.Month, 1);
        var limit = new DateOnly(to.Year, to.Month, 1);

        while (cursor <= limit)
        {
            labels.Add($"{cursor.Year:D4}-{cursor.Month:D2}");
            cursor = cursor.AddMonths(1);
        }

        return labels;
    }

    private static List<string> BuildYearLabels(DateOnly from, DateOnly to)
    {
        var labels = new List<string>();

        for (var year = from.Year; year <= to.Year; year++)
        {
            labels.Add(year.ToString());
        }

        return labels;
    }

    private static string GetLabel(DateTime createdAt, AnalyticsPeriod period)
    {
        return period switch
        {
            AnalyticsPeriod.Day => createdAt.ToString("yyyy-MM-dd"),
            AnalyticsPeriod.Month => createdAt.ToString("yyyy-MM"),
            AnalyticsPeriod.Year => createdAt.ToString("yyyy"),
            _ => throw new BadRequestException("Unsupported period. Allowed values: day, month, year.")
        };
    }

    private static AnalyticsPeriod ParsePeriod(string period)
    {
        var normalized = (period ?? string.Empty).Trim().ToLowerInvariant();

        return normalized switch
        {
            "day" => AnalyticsPeriod.Day,
            "month" => AnalyticsPeriod.Month,
            "year" => AnalyticsPeriod.Year,
            _ => throw new BadRequestException("Invalid period. Supported values: day, month, year.")
        };
    }

    private static AnalyticsFlow? ParseFlow(string? flow)
    {
        if (string.IsNullOrWhiteSpace(flow))
        {
            return null;
        }

        var normalized = flow.Trim().ToLowerInvariant();

        return normalized switch
        {
            "consultation" => AnalyticsFlow.Consultation,
            "catching" => AnalyticsFlow.Catching,
            "snakebite" => AnalyticsFlow.Snakebite,
            _ => throw new BadRequestException("Invalid flow. Supported values: consultation, catching, snakebite.")
        };
    }

    private static string ToFlowName(AnalyticsFlow flow)
    {
        return flow switch
        {
            AnalyticsFlow.Consultation => "consultation",
            AnalyticsFlow.Catching => "catching",
            AnalyticsFlow.Snakebite => "snakebite",
            _ => throw new BadRequestException("Unsupported flow.")
        };
    }

    private static void ValidateDateRange(DateOnly from, DateOnly to)
    {
        if (from == default || to == default)
        {
            throw new BadRequestException("Invalid date format. Please use a valid date with format YYYY-MM-DD.");
        }

        if (from > to)
        {
            throw new BadRequestException("Invalid date range. 'from' must be less than or equal to 'to'.");
        }
    }

    private static readonly HashSet<TransactionType> RelevantRevenueTypes =
    [
        TransactionType.ConsultationPayment,
        TransactionType.ConsultationRefund,
        TransactionType.CatchingPayment,
        TransactionType.CatchingDeposit,
        TransactionType.SnakebiteIncidentPayment
    ];

    private static readonly HashSet<TransactionType> RelevantCommissionTypes =
    [
        TransactionType.ConsultationPayment,
        TransactionType.ExpertPayout,
        TransactionType.ConsultationRefund
    ];

    private static readonly HashSet<TransactionType> RelevantProfitTypes =
    [
        TransactionType.ConsultationPayment,
        TransactionType.ExpertPayout,
        TransactionType.ConsultationRefund,
        TransactionType.CatchingPayment,
        TransactionType.CatchingDeposit,
        TransactionType.SnakebiteIncidentPayment
    ];

    private sealed class TransactionLite
    {
        public TransactionType TransactionType { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class CommissionBucket
    {
        public decimal Revenue { get; set; }
        public decimal ExpertPayout { get; set; }
        public decimal Refund { get; set; }
    }

    private sealed class FlowBucket
    {
        private readonly Dictionary<AnalyticsFlow, decimal> _values = new()
        {
            [AnalyticsFlow.Consultation] = 0m,
            [AnalyticsFlow.Catching] = 0m,
            [AnalyticsFlow.Snakebite] = 0m
        };

        public decimal this[AnalyticsFlow flow]
        {
            get => _values[flow];
            set => _values[flow] = value;
        }
    }

    private enum AnalyticsPeriod
    {
        Day,
        Month,
        Year
    }

    private enum AnalyticsFlow
    {
        Consultation,
        Catching,
        Snakebite
    }

    private sealed class CaseBucket
    {
        public int Snakebite { get; set; }
        public int Catching { get; set; }
    }

    private enum CaseFlow
    {
        Snakebite,
        Catching
    }

    private enum RolePeriod
    {
        Today,
        Month,
        Year
    }
}
