using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StrangeLoop.Infrastructure.ServiceBus;

/// <summary>
/// Extension methods for registering ServiceBus infrastructure in dependency injection.
/// </summary>
public static class ServiceBusExtensions
{
    /// <summary>
    /// Adds the Strange Loop ServiceBus infrastructure to the service collection.
    /// </summary>
    /// <param name="builder">The host application builder</param>
    /// <returns>The builder for chaining</returns>
    public static IHostApplicationBuilder AddStrangeLoopServiceBus(this IHostApplicationBuilder builder)
    {
        // Register the ServiceBus as a singleton for high performance
        builder.Services.AddSingleton<IServiceBus, InMemoryServiceBus>();
        
        // Add health check for ServiceBus
        builder.Services.AddHealthChecks()
            .AddCheck<ServiceBusHealthCheck>("servicebus");

        builder.Services.AddLogging();
        
        return builder;
    }

    /// <summary>
    /// Adds the Strange Loop ServiceBus infrastructure to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The services for chaining</returns>
    public static IServiceCollection AddStrangeLoopServiceBus(this IServiceCollection services)
    {
        // Register the ServiceBus as a singleton for high performance
        services.AddSingleton<IServiceBus, InMemoryServiceBus>();
        
        // Add health check for ServiceBus
        services.AddHealthChecks()
            .AddCheck<ServiceBusHealthCheck>("servicebus");

        services.AddLogging();
        
        return services;
    }
}

/// <summary>
/// Health check for the ServiceBus to monitor its operational status.
/// </summary>
public sealed class ServiceBusHealthCheck : IHealthCheck
{
    private readonly IServiceBus _serviceBus;

    public ServiceBusHealthCheck(IServiceBus serviceBus)
    {
        _serviceBus = serviceBus;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_serviceBus.IsHealthy)
            {
                var metrics = _serviceBus.GetMetrics();
                var data = new Dictionary<string, object>
                {
                    ["messages_published"] = metrics.TotalMessagesPublished,
                    ["messages_consumed"] = metrics.TotalMessagesConsumed,
                    ["active_subscriptions"] = metrics.ActiveSubscriptions,
                    ["average_latency_ms"] = metrics.AverageLatencyMs,
                    ["queued_messages"] = metrics.QueuedMessages,
                    ["last_activity"] = metrics.LastActivity
                };

                return Task.FromResult(HealthCheckResult.Healthy("ServiceBus is operational", data));
            }
            else
            {
                var metrics = _serviceBus.GetMetrics();
                return Task.FromResult(HealthCheckResult.Unhealthy($"ServiceBus is not healthy: {metrics.LastError}"));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"ServiceBus health check failed: {ex.Message}"));
        }
    }
}
