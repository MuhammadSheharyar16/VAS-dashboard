namespace Vas.Application.Common.Reporting;

/// <summary>A half-open range of days: <see cref="From"/> is included, <see cref="ToExclusive"/> is not.</summary>
public sealed record DateRange(DateOnly From, DateOnly ToExclusive)
{
    public const int DefaultDays = 30;
    public const int MaxMonths = 24;

    public int Days => ToExclusive.DayNumber - From.DayNumber;

    /// <summary>The range of the same length that ends where this one starts.</summary>
    public DateRange Previous() => new(From.AddDays(-Days), From);

    /// <summary>Returns the reason the range is not acceptable, or null when it is.</summary>
    public static string? Validate(DateOnly from, DateOnly toExclusive)
    {
        if (from >= toExclusive)
        {
            return "The start date must be before the end date.";
        }

        return from.AddMonths(MaxMonths) < toExclusive
            ? $"The date range cannot be longer than {MaxMonths} months."
            : null;
    }
}
