using Microsoft.Extensions.DependencyInjection;
using Vas.Application;
using Vas.Application.Common.Interfaces;
using Vas.Infrastructure;

namespace Vas.ApplicationTests;

public interface ISampleReportService;

public sealed class SampleReportService : ISampleReportService;

public sealed class UnpairedService;

public abstract class AbstractReportService : ISampleReportService;

public class ServiceRegistrationTests
{
    [Fact]
    public void Scan_registers_a_service_against_its_matching_interface_as_scoped()
    {
        var services = new ServiceCollection()
            .AddServicesFromAssembly(typeof(ServiceRegistrationTests).Assembly);

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(ISampleReportService));
        Assert.Equal(typeof(SampleReportService), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Scan_skips_services_without_a_matching_interface_and_abstract_classes()
    {
        var services = new ServiceCollection()
            .AddServicesFromAssembly(typeof(ServiceRegistrationTests).Assembly);

        Assert.DoesNotContain(services, d => d.ImplementationType == typeof(UnpairedService));
        Assert.DoesNotContain(services, d => d.ImplementationType == typeof(AbstractReportService));
    }

    [Fact]
    public void Infrastructure_provides_a_utc_clock()
    {
        using var provider = new ServiceCollection().AddInfrastructure().BuildServiceProvider();

        var clock = provider.GetRequiredService<IDateTimeProvider>();

        Assert.Equal(DateTimeKind.Utc, clock.UtcNow.Kind);
    }
}
