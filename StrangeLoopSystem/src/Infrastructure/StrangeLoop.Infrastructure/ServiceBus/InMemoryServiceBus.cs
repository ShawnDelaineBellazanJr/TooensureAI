using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace StrangeLoop.Infrastructure.ServiceBus;

/// <summary>
/// High-performance in-memory implementation of IServiceBus for Strange Loop inter-agent communication.
/// Optimized for .NET 9.0 with C# 13 features and designed for low-latency message passing.
/// </summary>
public sealed class InMemoryServiceBus : IServiceBus, IDisposable
{
    private readonly ILogger<InMemoryServiceBus> _logger;
    private readonly ConcurrentDictionary<string, ConcurrentBag<ISubscription>> _subscriptions = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _pendingRequests = new();
    private readonly CancellationTokenSource _shutdownToken = new();
    private readonly Timer _metricsTimer;
    
    // Metrics tracking
    private long _totalMessagesPublished;
    private long _totalMessagesConsumed;
    private readonly ConcurrentQueue<double> _latencyMeasurements = new();
    private DateTime _lastActivity = DateTime.UtcNow;
    private string? _lastError;

    public InMemoryServiceBus(ILogger<InMemoryServiceBus> logger)
    {
        _logger = logger;
        
        // Set up metrics collection timer (every 30 seconds)
        _metricsTimer = new Timer(CollectMetrics, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        
        _logger.LogInformation("InMemoryServiceBus initialized with high-performance message routing");
    }

    /// <inheritdoc />
    public bool IsHealthy => !_shutdownToken.Token.IsCancellationRequested && string.IsNullOrEmpty(_lastError);

    /// <inheritdoc />
    public async Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(handler);

        var subscription = new TypedSubscription<T>(handler, cancellationToken);
        _subscriptions.AddOrUpdate(topic,
            _ => new ConcurrentBag<ISubscription> { subscription },
            (_, existing) => 
            {
                existing.Add(subscription);
                return existing;
            });

        _logger.LogDebug("Subscribed to topic '{Topic}' for message type {MessageType}", topic, typeof(T).Name);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(message);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (_subscriptions.TryGetValue(topic, out var subscriptions))
            {
                var tasks = new List<Task>();
                
                foreach (var subscription in subscriptions)
                {
                    if (subscription.CanHandle<T>() && !subscription.CancellationToken.IsCancellationRequested)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await subscription.HandleAsync(message);
                                Interlocked.Increment(ref _totalMessagesConsumed);
                            }
                            catch (Exception ex)
                            {
                                _lastError = ex.Message;
                                _logger.LogError(ex, "Error handling message on topic '{Topic}'", topic);
                            }
                        }, cancellationToken));
                    }
                }

                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks);
                }
            }

            Interlocked.Increment(ref _totalMessagesPublished);
            _lastActivity = DateTime.UtcNow;
            
            stopwatch.Stop();
            RecordLatency(stopwatch.Elapsed.TotalMilliseconds);
            
            _logger.LogTrace("Published message to topic '{Topic}' in {ElapsedMs}ms", topic, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            _logger.LogError(ex, "Failed to publish message to topic '{Topic}'", topic);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(
        string topic, 
        TRequest request, 
        TimeSpan timeout = default, 
        CancellationToken cancellationToken = default) 
        where TRequest : class 
        where TResponse : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(request);

        timeout = timeout == default ? TimeSpan.FromSeconds(30) : timeout;
        var requestId = Guid.NewGuid().ToString();
        var responseTopic = $"{topic}-response-{requestId}";
        
        var tcs = new TaskCompletionSource<object>();
        _pendingRequests[requestId] = tcs;

        try
        {
            // Set up response subscription first
            await SubscribeAsync<TResponse>(responseTopic, async response =>
            {
                if (_pendingRequests.TryRemove(requestId, out var pendingTcs))
                {
                    pendingTcs.SetResult(response);
                }
                await Task.CompletedTask;
            }, cancellationToken);

            // Send the request with response topic metadata
            var requestWrapper = new RequestMessage<TRequest>
            {
                Id = requestId,
                ResponseTopic = responseTopic,
                Payload = request
            };

            await PublishAsync(topic, requestWrapper, cancellationToken);

            // Wait for response with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            var completedTask = await Task.WhenAny(
                tcs.Task,
                Task.Delay(timeout, timeoutCts.Token)
            );

            if (completedTask == tcs.Task)
            {
                return (TResponse?)await tcs.Task;
            }
            else
            {
                _pendingRequests.TryRemove(requestId, out _);
                throw new TimeoutException($"Request to '{topic}' timed out after {timeout.TotalSeconds} seconds");
            }
        }
        finally
        {
            await UnsubscribeAsync<TResponse>(responseTopic, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task UnsubscribeAsync<T>(string topic, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        if (_subscriptions.TryGetValue(topic, out var subscriptions))
        {
            var filteredSubscriptions = new ConcurrentBag<ISubscription>();
            
            foreach (var subscription in subscriptions)
            {
                if (!subscription.CanHandle<T>())
                {
                    filteredSubscriptions.Add(subscription);
                }
            }

            if (filteredSubscriptions.IsEmpty)
            {
                _subscriptions.TryRemove(topic, out _);
            }
            else
            {
                _subscriptions[topic] = filteredSubscriptions;
            }
        }

        _logger.LogDebug("Unsubscribed from topic '{Topic}' for message type {MessageType}", topic, typeof(T).Name);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public ServiceBusMetrics GetMetrics()
    {
        var averageLatency = 0.0;
        var queuedMessages = 0;

        if (!_latencyMeasurements.IsEmpty)
        {
            var measurements = _latencyMeasurements.ToArray();
            averageLatency = measurements.Length > 0 ? measurements.Average() : 0.0;
        }

        var activeSubscriptions = _subscriptions.Values.Sum(bag => bag.Count);

        return new ServiceBusMetrics
        {
            TotalMessagesPublished = _totalMessagesPublished,
            TotalMessagesConsumed = _totalMessagesConsumed,
            ActiveSubscriptions = activeSubscriptions,
            AverageLatencyMs = averageLatency,
            QueuedMessages = queuedMessages,
            LastError = _lastError,
            LastActivity = _lastActivity
        };
    }

    private void RecordLatency(double latencyMs)
    {
        _latencyMeasurements.Enqueue(latencyMs);
        
        // Keep only last 1000 measurements for rolling average
        while (_latencyMeasurements.Count > 1000)
        {
            _latencyMeasurements.TryDequeue(out _);
        }
    }

    private void CollectMetrics(object? state)
    {
        try
        {
            var metrics = GetMetrics();
            _logger.LogDebug("ServiceBus Metrics - Published: {Published}, Consumed: {Consumed}, Active Subscriptions: {Subscriptions}, Avg Latency: {Latency}ms",
                metrics.TotalMessagesPublished,
                metrics.TotalMessagesConsumed,
                metrics.ActiveSubscriptions,
                metrics.AverageLatencyMs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect service bus metrics");
        }
    }

    public void Dispose()
    {
        _shutdownToken.Cancel();
        _metricsTimer?.Dispose();
        _shutdownToken.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Internal interface for type-erased subscription handling.
/// </summary>
internal interface ISubscription
{
    bool CanHandle<T>() where T : class;
    Task HandleAsync<T>(T message) where T : class;
    CancellationToken CancellationToken { get; }
}

/// <summary>
/// Strongly-typed subscription implementation.
/// </summary>
/// <typeparam name="T">The message type this subscription handles</typeparam>
internal sealed class TypedSubscription<T> : ISubscription where T : class
{
    private readonly Func<T, Task> _handler;

    public TypedSubscription(Func<T, Task> handler, CancellationToken cancellationToken)
    {
        _handler = handler;
        CancellationToken = cancellationToken;
    }

    public CancellationToken CancellationToken { get; }

    public bool CanHandle<TMessage>() where TMessage : class
    {
        return typeof(TMessage) == typeof(T);
    }

    public async Task HandleAsync<TMessage>(TMessage message) where TMessage : class
    {
        if (message is T typedMessage)
        {
            await _handler(typedMessage);
        }
    }
}

/// <summary>
/// Wrapper for request-response pattern messages.
/// </summary>
/// <typeparam name="T">The request payload type</typeparam>
internal sealed class RequestMessage<T> where T : class
{
    public required string Id { get; init; }
    public required string ResponseTopic { get; init; }
    public required T Payload { get; init; }
}
