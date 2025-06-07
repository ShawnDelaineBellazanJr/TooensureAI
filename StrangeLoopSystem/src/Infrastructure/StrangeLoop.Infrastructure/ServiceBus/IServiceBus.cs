using System.ComponentModel;

namespace StrangeLoop.Infrastructure.ServiceBus;

/// <summary>
/// Defines the contract for inter-agent communication through a service bus pattern.
/// Enables publish-subscribe messaging between Strange Loop agents for coordinated execution.
/// </summary>
public interface IServiceBus
{
    /// <summary>
    /// Subscribes to messages of type T on the specified topic.
    /// </summary>
    /// <typeparam name="T">The message type to subscribe to</typeparam>
    /// <param name="topic">The topic name to subscribe to</param>
    /// <param name="handler">The handler function to process received messages</param>
    /// <param name="cancellationToken">Cancellation token for stopping the subscription</param>
    /// <returns>A task representing the subscription operation</returns>
    Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Publishes a message of type T to the specified topic.
    /// </summary>
    /// <typeparam name="T">The message type to publish</typeparam>
    /// <param name="topic">The topic name to publish to</param>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">Cancellation token for the publish operation</param>
    /// <returns>A task representing the publish operation</returns>
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Publishes a message and waits for a response (request-response pattern).
    /// </summary>
    /// <typeparam name="TRequest">The request message type</typeparam>
    /// <typeparam name="TResponse">The response message type</typeparam>
    /// <param name="topic">The topic name to send the request to</param>
    /// <param name="request">The request message</param>
    /// <param name="timeout">Maximum time to wait for a response</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The response message</returns>
    Task<TResponse?> RequestAsync<TRequest, TResponse>(
        string topic, 
        TRequest request, 
        TimeSpan timeout = default, 
        CancellationToken cancellationToken = default) 
        where TRequest : class 
        where TResponse : class;

    /// <summary>
    /// Unsubscribes from messages of type T on the specified topic.
    /// </summary>
    /// <typeparam name="T">The message type to unsubscribe from</typeparam>
    /// <param name="topic">The topic name to unsubscribe from</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task representing the unsubscribe operation</returns>
    Task UnsubscribeAsync<T>(string topic, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets the current health status of the service bus.
    /// </summary>
    /// <returns>True if the service bus is healthy and operational</returns>
    bool IsHealthy { get; }

    /// <summary>
    /// Gets metrics about the service bus operation.
    /// </summary>
    /// <returns>Service bus metrics including message counts, latency, etc.</returns>
    ServiceBusMetrics GetMetrics();
}

/// <summary>
/// Contains metrics and operational data about the service bus.
/// </summary>
public sealed class ServiceBusMetrics
{
    /// <summary>
    /// Total number of messages published since startup.
    /// </summary>
    public long TotalMessagesPublished { get; init; }

    /// <summary>
    /// Total number of messages consumed since startup.
    /// </summary>
    public long TotalMessagesConsumed { get; init; }

    /// <summary>
    /// Number of active subscriptions.
    /// </summary>
    public int ActiveSubscriptions { get; init; }

    /// <summary>
    /// Average message processing latency in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; init; }

    /// <summary>
    /// Number of messages currently in queue.
    /// </summary>
    public int QueuedMessages { get; init; }

    /// <summary>
    /// Last recorded error, if any.
    /// </summary>
    public string? LastError { get; init; }

    /// <summary>
    /// Timestamp of the last activity.
    /// </summary>
    public DateTime LastActivity { get; init; }
}
