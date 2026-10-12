namespace Vas.Domain.Reporting;

// The text values the Gold columns hold. Assumed until the KPI Mapping arrives.

public static class ApplicationStatus
{
    public const string Pending = "Pending";
    public const string Processed = "Processed";
    public const string Closed = "Closed";
}

public static class ApplicationOutcome
{
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Withdrawn = "Withdrawn";
}

public static class ApplicationChannel
{
    public const string Digital = "Digital";
    public const string Centre = "Centre";
}

public static class ApplicationStageName
{
    public const string Submitted = "Submitted";
    public const string DocumentCheck = "DocumentCheck";
    public const string Biometrics = "Biometrics";
    public const string Review = "Review";
    public const string Decision = "Decision";

    public static IReadOnlyList<string> Ordered { get; } =
        [Submitted, DocumentCheck, Biometrics, Review, Decision];
}

public static class RefreshStatus
{
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
}
