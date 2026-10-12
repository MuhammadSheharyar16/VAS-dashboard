using Vas.Domain.Reporting;
using Vas.Infrastructure.Persistence.Mock;

namespace Vas.MockDataGenerator;

/// <summary>
/// Builds the synthetic Gold data set. Everything comes from one fixed seed and fixed dates, so
/// every run produces the same rows. No real applicant data is involved.
/// </summary>
public static class MockGoldGenerator
{
    public const int Seed = 20261012;
    public const string Currency = "AED";

    /// <summary>First and last day with applications: 18 months.</summary>
    public static readonly DateOnly FirstDay = new(2025, 4, 12);
    public static readonly DateOnly LastDay = new(2026, 10, 11);

    /// <summary>Days deliberately left without data, so empty states and chart gaps can be tested.</summary>
    public static readonly IReadOnlyList<DateOnly> NoDataDays =
        [new(2025, 12, 25), new(2026, 3, 20), new(2026, 9, 27)];

    /// <summary>The centre generated close to its daily capacity.</summary>
    public const string NearCapacityCentreCode = "AE-DXB";

    private const double BaseApplicationsPerDay = 38.7;
    private const double StuckShare = 0.03;
    private const double WithdrawnShare = 0.04;
    private const int DaysUntilClosed = 7;

    private sealed record CountrySeed(string Code, string Name, string Region, double Weight);

    private sealed record CentreSeed(string Code, string Name, string CountryCode, string City, int DailyCapacity, double Weight);

    private sealed record VisaTypeSeed(
        string Code, string Name, string Category, double Weight,
        int BaseTurnaroundDays, double ApprovalShare, double DigitalShare, decimal ServiceFee);

    private sealed record AddOnSeed(string ServiceName, decimal Amount, double Probability);

    private static readonly CountrySeed[] CountrySeeds =
    [
        new("AE", "United Arab Emirates", "Middle East", 0.28),
        new("SA", "Saudi Arabia", "Middle East", 0.18),
        new("IN", "India", "South Asia", 0.22),
        new("PK", "Pakistan", "South Asia", 0.12),
        new("GB", "United Kingdom", "Europe", 0.10),
        new("EG", "Egypt", "Africa", 0.10),
    ];

    private static readonly CentreSeed[] CentreSeeds =
    [
        new(NearCapacityCentreCode, "Dubai Centre", "AE", "Dubai", 6, 0.55),
        new("AE-AUH", "Abu Dhabi Centre", "AE", "Abu Dhabi", 8, 0.30),
        new("AE-SHJ", "Sharjah Centre", "AE", "Sharjah", 5, 0.15),
        new("SA-RUH", "Riyadh Centre", "SA", "Riyadh", 8, 0.50),
        new("SA-JED", "Jeddah Centre", "SA", "Jeddah", 6, 0.30),
        new("SA-DMM", "Dammam Centre", "SA", "Dammam", 4, 0.20),
        new("IN-BOM", "Mumbai Centre", "IN", "Mumbai", 8, 0.40),
        new("IN-DEL", "New Delhi Centre", "IN", "New Delhi", 7, 0.40),
        new("IN-BLR", "Bengaluru Centre", "IN", "Bengaluru", 5, 0.20),
        new("PK-KHI", "Karachi Centre", "PK", "Karachi", 6, 0.60),
        new("PK-LHE", "Lahore Centre", "PK", "Lahore", 5, 0.40),
        new("GB-LON", "London Centre", "GB", "London", 6, 0.70),
        new("GB-MAN", "Manchester Centre", "GB", "Manchester", 4, 0.30),
        new("EG-CAI", "Cairo Centre", "EG", "Cairo", 6, 0.70),
        new("EG-ALX", "Alexandria Centre", "EG", "Alexandria", 4, 0.30),
    ];

    private static readonly VisaTypeSeed[] VisaTypeSeeds =
    [
        new("TOUR", "Tourist", "Short stay", 0.30, 5, 0.82, 0.65, 350m),
        new("BUS", "Business", "Short stay", 0.15, 6, 0.85, 0.60, 450m),
        new("STU", "Student", "Long stay", 0.08, 15, 0.72, 0.50, 600m),
        new("WORK", "Employment", "Long stay", 0.12, 20, 0.70, 0.45, 750m),
        new("FAM", "Family Visit", "Short stay", 0.15, 8, 0.80, 0.55, 300m),
        new("TRAN", "Transit", "Short stay", 0.08, 2, 0.92, 0.80, 150m),
        new("MED", "Medical", "Short stay", 0.05, 4, 0.88, 0.50, 400m),
        new("RES", "Residence", "Long stay", 0.07, 25, 0.65, 0.40, 900m),
    ];

    private static readonly AddOnSeed[] AddOnSeeds =
    [
        new("Premium Lounge", 250m, 0.12),
        new("Courier Return", 60m, 0.15),
        new("SMS Updates", 15m, 0.10),
        new("Form Filling Assistance", 80m, 0.06),
    ];

    /// <summary>The share of an application's turnaround spent in each stage, in stage order.</summary>
    private static readonly double[] StageShares = [0.05, 0.25, 0.20, 0.40, 0.10];

    public static MockGoldDataSet Generate()
    {
        var random = new Random(Seed);
        var applications = new List<VisaApplication>();
        var stages = new List<ApplicationStage>();
        var revenue = new List<VasRevenueTransaction>();

        for (var day = FirstDay; day <= LastDay; day = day.AddDays(1))
        {
            if (NoDataDays.Contains(day))
            {
                continue;
            }

            var count = ApplicationsOn(day, random);
            for (var i = 0; i < count; i++)
            {
                AddApplication(day, random, applications, stages, revenue);
            }
        }

        return new MockGoldDataSet(
            CountrySeeds.Select(c => new Country { CountryCode = c.Code, CountryName = c.Name, Region = c.Region }).ToList(),
            CentreSeeds.Select(c => new Centre
            {
                CentreCode = c.Code,
                CentreName = c.Name,
                CountryCode = c.CountryCode,
                City = c.City,
                DailyCapacity = c.DailyCapacity,
            }).ToList(),
            VisaTypeSeeds.Select(v => new VisaType { VisaTypeCode = v.Code, VisaTypeName = v.Name, Category = v.Category }).ToList(),
            applications,
            stages,
            revenue,
            BuildRefreshLog());
    }

    private static int ApplicationsOn(DateOnly day, Random random)
    {
        var season = 1 + 0.25 * Math.Sin(2 * Math.PI * (day.DayOfYear - 80) / 365.0);
        var weekday = day.DayOfWeek switch
        {
            DayOfWeek.Saturday => 0.55,
            DayOfWeek.Sunday => 0.65,
            _ => 1.08,
        };
        var elapsed = (day.DayNumber - FirstDay.DayNumber) / (double)(LastDay.DayNumber - FirstDay.DayNumber);
        var growth = 0.85 + 0.30 * elapsed;
        var noise = 0.85 + 0.30 * random.NextDouble();

        return (int)Math.Round(BaseApplicationsPerDay * season * weekday * growth * noise);
    }

    private static void AddApplication(
        DateOnly submitted,
        Random random,
        List<VisaApplication> applications,
        List<ApplicationStage> stages,
        List<VasRevenueTransaction> revenue)
    {
        var country = Pick(CountrySeeds, c => c.Weight, random);
        var centre = Pick(CentreSeeds.Where(c => c.CountryCode == country.Code).ToArray(), c => c.Weight, random);
        var visaType = Pick(VisaTypeSeeds, v => v.Weight, random);
        var isDigital = random.NextDouble() < visaType.DigitalShare;

        var pace = (isDigital ? 0.85 : 1.0) * (centre.Code == NearCapacityCentreCode ? 1.25 : 1.0);
        var plannedDays = Math.Max(1, (int)Math.Round(visaType.BaseTurnaroundDays * pace * (0.6 + 0.9 * random.NextDouble())));
        var isStuck = random.NextDouble() < StuckShare;
        var outcomeRoll = random.NextDouble();
        var stuckProgress = random.NextDouble() * 0.7;

        var decisionDate = submitted.AddDays(plannedDays);
        var isDecided = !isStuck && decisionDate <= LastDay;

        var progress = isDecided
            ? 1.0
            : isStuck
                ? stuckProgress
                : Math.Min(0.99, (LastDay.DayNumber - submitted.DayNumber) / (double)plannedDays);

        var applicationId = $"APP-{applications.Count + 1:D6}";
        var currentStage = AddStages(applicationId, submitted, plannedDays, progress, isDecided, stages);

        applications.Add(new VisaApplication
        {
            ApplicationId = applicationId,
            SubmittedDate = submitted,
            CountryCode = country.Code,
            CentreCode = centre.Code,
            VisaTypeCode = visaType.Code,
            Channel = isDigital ? ApplicationChannel.Digital : ApplicationChannel.Centre,
            CurrentStage = currentStage,
            Status = !isDecided
                ? ApplicationStatus.Pending
                : decisionDate.AddDays(DaysUntilClosed) <= LastDay ? ApplicationStatus.Closed : ApplicationStatus.Processed,
            Outcome = !isDecided
                ? null
                : outcomeRoll < WithdrawnShare ? ApplicationOutcome.Withdrawn
                : outcomeRoll < WithdrawnShare + (1 - WithdrawnShare) * visaType.ApprovalShare ? ApplicationOutcome.Approved
                : ApplicationOutcome.Rejected,
            DecisionDate = isDecided ? decisionDate : null,
            TurnaroundDays = isDecided ? plannedDays : null,
        });

        AddRevenue(submitted, country.Code, centre.Code, visaType, random, revenue);
    }

    /// <summary>Adds the stage rows an application has reached and returns the name of its current stage.</summary>
    private static string AddStages(
        string applicationId,
        DateOnly submitted,
        int plannedDays,
        double progress,
        bool isDecided,
        List<ApplicationStage> stages)
    {
        var names = ApplicationStageName.Ordered;
        var enteredAt = submitted.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc);
        var totalMinutes = plannedDays * 24 * 60;
        var completedShare = 0.0;

        for (var i = 0; i < names.Count; i++)
        {
            var isLast = i == names.Count - 1;
            completedShare += StageShares[i];

            // A stage is finished when the application has moved past it; a decided application finished all of them.
            var isFinished = isDecided || (!isLast && progress >= completedShare);
            var exitedAt = isLast
                ? submitted.AddDays(plannedDays).ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc)
                : enteredAt.AddMinutes(Math.Round(totalMinutes * StageShares[i]));

            stages.Add(new ApplicationStage
            {
                ApplicationId = applicationId,
                Stage = names[i],
                EnteredAtUtc = enteredAt,
                ExitedAtUtc = isFinished ? exitedAt : null,
            });

            if (!isFinished)
            {
                return names[i];
            }

            enteredAt = exitedAt;
        }

        return ApplicationStageName.Decision;
    }

    private static void AddRevenue(
        DateOnly date,
        string countryCode,
        string centreCode,
        VisaTypeSeed visaType,
        Random random,
        List<VasRevenueTransaction> revenue)
    {
        void Add(string serviceName, decimal amount) => revenue.Add(new VasRevenueTransaction
        {
            TransactionId = $"TXN-{revenue.Count + 1:D7}",
            TransactionDate = date,
            CountryCode = countryCode,
            CentreCode = centreCode,
            VisaTypeCode = visaType.Code,
            ServiceName = serviceName,
            Amount = amount,
            Currency = Currency,
        });

        Add("Service Fee", visaType.ServiceFee);

        foreach (var addOn in AddOnSeeds)
        {
            if (random.NextDouble() < addOn.Probability)
            {
                Add(addOn.ServiceName, addOn.Amount);
            }
        }
    }

    /// <summary>A nightly refresh at 02:00 UTC for the last 30 nights, one of which failed.</summary>
    private static List<RefreshLog> BuildRefreshLog()
    {
        var failedOn = LastDay.AddDays(-6);

        return Enumerable.Range(0, 30)
            .Select(offset => LastDay.AddDays(1 - 29 + offset))
            .Select(day => new RefreshLog
            {
                RefreshedAtUtc = day.ToDateTime(new TimeOnly(2, 0), DateTimeKind.Utc),
                Status = day == failedOn ? RefreshStatus.Failed : RefreshStatus.Succeeded,
            })
            .ToList();
    }

    private static T Pick<T>(IReadOnlyList<T> items, Func<T, double> weight, Random random)
    {
        var roll = random.NextDouble() * items.Sum(weight);

        foreach (var item in items)
        {
            roll -= weight(item);
            if (roll < 0)
            {
                return item;
            }
        }

        return items[^1];
    }
}
