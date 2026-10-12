using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vas.Domain.Reporting;

namespace Vas.Infrastructure.Persistence.Configurations;

// This file is the one place that names the Gold objects and columns. The names are assumed
// (project-specs.md 3.1). When the real Gold layer arrives, change them here and record the
// mapping in docs/kpi-mapping.md.

internal static class GoldSchema
{
    public const string Name = "gold";
}

internal sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.HasNoKey().ToView("dim_country", GoldSchema.Name);
        builder.Property(x => x.CountryCode).HasColumnName("country_code");
        builder.Property(x => x.CountryName).HasColumnName("country_name");
        builder.Property(x => x.Region).HasColumnName("region");
    }
}

internal sealed class CentreConfiguration : IEntityTypeConfiguration<Centre>
{
    public void Configure(EntityTypeBuilder<Centre> builder)
    {
        builder.HasNoKey().ToView("dim_centre", GoldSchema.Name);
        builder.Property(x => x.CentreCode).HasColumnName("centre_code");
        builder.Property(x => x.CentreName).HasColumnName("centre_name");
        builder.Property(x => x.CountryCode).HasColumnName("country_code");
        builder.Property(x => x.City).HasColumnName("city");
        builder.Property(x => x.DailyCapacity).HasColumnName("daily_capacity");
    }
}

internal sealed class VisaTypeConfiguration : IEntityTypeConfiguration<VisaType>
{
    public void Configure(EntityTypeBuilder<VisaType> builder)
    {
        builder.HasNoKey().ToView("dim_visa_type", GoldSchema.Name);
        builder.Property(x => x.VisaTypeCode).HasColumnName("visa_type_code");
        builder.Property(x => x.VisaTypeName).HasColumnName("visa_type_name");
        builder.Property(x => x.Category).HasColumnName("category");
    }
}

internal sealed class VisaApplicationConfiguration : IEntityTypeConfiguration<VisaApplication>
{
    public void Configure(EntityTypeBuilder<VisaApplication> builder)
    {
        builder.HasNoKey().ToView("fact_visa_application", GoldSchema.Name);
        builder.Property(x => x.ApplicationId).HasColumnName("application_id");
        builder.Property(x => x.SubmittedDate).HasColumnName("submitted_date");
        builder.Property(x => x.CountryCode).HasColumnName("country_code");
        builder.Property(x => x.CentreCode).HasColumnName("centre_code");
        builder.Property(x => x.VisaTypeCode).HasColumnName("visa_type_code");
        builder.Property(x => x.Channel).HasColumnName("channel");
        builder.Property(x => x.CurrentStage).HasColumnName("current_stage");
        builder.Property(x => x.Status).HasColumnName("status");
        builder.Property(x => x.Outcome).HasColumnName("outcome");
        builder.Property(x => x.DecisionDate).HasColumnName("decision_date");
        builder.Property(x => x.TurnaroundDays).HasColumnName("turnaround_days");
    }
}

internal sealed class ApplicationStageConfiguration : IEntityTypeConfiguration<ApplicationStage>
{
    public void Configure(EntityTypeBuilder<ApplicationStage> builder)
    {
        builder.HasNoKey().ToView("fact_application_stage", GoldSchema.Name);
        builder.Property(x => x.ApplicationId).HasColumnName("application_id");
        builder.Property(x => x.Stage).HasColumnName("stage");
        builder.Property(x => x.EnteredAtUtc).HasColumnName("entered_at_utc");
        builder.Property(x => x.ExitedAtUtc).HasColumnName("exited_at_utc");
    }
}

internal sealed class VasRevenueTransactionConfiguration : IEntityTypeConfiguration<VasRevenueTransaction>
{
    public void Configure(EntityTypeBuilder<VasRevenueTransaction> builder)
    {
        builder.HasNoKey().ToView("fact_vas_revenue", GoldSchema.Name);
        builder.Property(x => x.TransactionId).HasColumnName("transaction_id");
        builder.Property(x => x.TransactionDate).HasColumnName("transaction_date");
        builder.Property(x => x.CountryCode).HasColumnName("country_code");
        builder.Property(x => x.CentreCode).HasColumnName("centre_code");
        builder.Property(x => x.VisaTypeCode).HasColumnName("visa_type_code");
        builder.Property(x => x.ServiceName).HasColumnName("service_name");
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasColumnName("currency");
    }
}

internal sealed class RefreshLogConfiguration : IEntityTypeConfiguration<RefreshLog>
{
    public void Configure(EntityTypeBuilder<RefreshLog> builder)
    {
        builder.HasNoKey().ToView("refresh_log", GoldSchema.Name);
        builder.Property(x => x.RefreshedAtUtc).HasColumnName("refreshed_at_utc");
        builder.Property(x => x.Status).HasColumnName("status");
    }
}
