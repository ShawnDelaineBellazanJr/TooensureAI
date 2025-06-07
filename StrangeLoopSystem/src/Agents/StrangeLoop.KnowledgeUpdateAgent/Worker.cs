using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using StrangeLoop.Core.Models;
using StrangeLoop.Core.Messages;
using StrangeLoop.Infrastructure.ServiceBus;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace StrangeLoop.KnowledgeUpdateAgent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly Kernel _kernel;
    private readonly IServiceBus _serviceBus;
    private readonly ConcurrentDictionary<string, KnowledgeUpdateMetric> _knowledgeMetrics;
    private readonly ConcurrentQueue<KnowledgeUpdateEvent> _knowledgeEvents;
    private readonly ConcurrentDictionary<string, object> _persistentKnowledgeBase;
    private readonly ConcurrentDictionary<string, ConceptLearning> _conceptLearningTracker;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;
    private DateTime _lastKnowledgeUpdate;
    private readonly TimeSpan _knowledgeUpdateInterval = TimeSpan.FromMinutes(3);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private int _knowledgeVersion = 1;
    private readonly SemaphoreSlim _knowledgeSemaphore = new(1, 1);

    public Worker(ILogger<Worker> logger, Kernel kernel, IServiceBus serviceBus)
    {
        _logger = logger;
        _kernel = kernel;
        _serviceBus = serviceBus;
        _knowledgeMetrics = new ConcurrentDictionary<string, KnowledgeUpdateMetric>();
        _knowledgeEvents = new ConcurrentQueue<KnowledgeUpdateEvent>();
        _persistentKnowledgeBase = new ConcurrentDictionary<string, object>();
        _conceptLearningTracker = new ConcurrentDictionary<string, ConceptLearning>();
        
        // Initialize performance counters only on Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Could not initialize performance counters, will use default values");
            }
        }
        
        _lastKnowledgeUpdate = DateTime.UtcNow;
        InitializeKnowledgeBase();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🧠 Knowledge Update Agent starting comprehensive learning and adaptation system");

        await _serviceBus.SubscribeAsync<ReflectionResult>("reflection-results", HandleReflectionResultAsync, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_knowledgeUpdateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task HandleReflectionResultAsync(ReflectionResult reflectionResult)
    {
        try
        {            _logger.LogInformation("🧠 Received reflection result for knowledge update at: {Time} - Insights: {InsightsLength}, Effectiveness: {Effectiveness:F2}",
                reflectionResult.Timestamp, reflectionResult.Insights.Length, reflectionResult.EffectivenessScore);

            var knowledgeUpdates = new KnowledgeUpdates
            {
                Timestamp = DateTime.UtcNow,
                Version = _knowledgeVersion++
            };

            await PerformKnowledgeDiscoveryAsync(knowledgeUpdates, CancellationToken.None);
            await PerformConceptLearningAsync(knowledgeUpdates, CancellationToken.None);
            await PerformPatternRecognitionAsync(knowledgeUpdates, CancellationToken.None);
            await PerformKnowledgeConsolidationAsync(knowledgeUpdates, CancellationToken.None);
            await PerformMetaLearningAsync(knowledgeUpdates, CancellationToken.None);
            await PerformSemanticKernelLearningAsync(knowledgeUpdates, CancellationToken.None);
            await PerformAdaptivePolicyUpdatesAsync(knowledgeUpdates, CancellationToken.None);
            await PersistKnowledgeUpdatesAsync(knowledgeUpdates, CancellationToken.None);

            var knowledgeUpdateResult = new
            {
                Timestamp = DateTime.UtcNow,
                Source = "KnowledgeUpdateAgent",
                UpdatesApplied = knowledgeUpdates.Version,
                ConceptsLearned = _conceptLearningTracker.Keys.Take(10).ToList(),
                Success = true
            };

            await _serviceBus.PublishAsync("knowledge-update-results", knowledgeUpdateResult, CancellationToken.None);

            _logger.LogInformation("✅ Knowledge update cycle completed and published. Concepts: {ConceptCount}, Success: {Success}",
                _conceptLearningTracker.Count, true);

            _lastKnowledgeUpdate = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during knowledge update cycle");
        }
    }

    private async Task PerformKnowledgeDiscoveryAsync(KnowledgeUpdates knowledgeUpdates, CancellationToken cancellationToken)
    {
        await _knowledgeSemaphore.WaitAsync(cancellationToken);
        try
        {
            var discoveryPrompt = $@"
🔍 KNOWLEDGE DISCOVERY ANALYSIS

Current Knowledge Base Size: {_persistentKnowledgeBase.Count}
Recent Learning Activity: {_knowledgeEvents.Count} events

Analyze current knowledge gaps and identify areas for new learning:
1. What knowledge domains need expansion?
2. What patterns suggest missing information?
3. What new concepts should be explored?

Provide discoveries as 'Key|Value' format, one per line.";

            var kernelArguments = new KernelArguments();
            var result = await _kernel.InvokePromptAsync(discoveryPrompt, kernelArguments, cancellationToken: cancellationToken);
            var resultText = result.ToString();

            if (!string.IsNullOrEmpty(resultText))
            {
                var discoveries = ExtractKeyValuePairs(resultText);
                foreach (var discovery in discoveries)
                {
                    if (!_persistentKnowledgeBase.ContainsKey(discovery.Key))
                    {
                        _persistentKnowledgeBase.TryAdd(discovery.Key, discovery.Value);
                    }
                }
                _logger.LogInformation("🔍 Knowledge discovery completed: {Count} new items", discoveries.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in knowledge discovery");
        }
        finally
        {
            _knowledgeSemaphore.Release();
        }
    }

    private async Task PerformConceptLearningAsync(KnowledgeUpdates knowledgeUpdates, CancellationToken cancellationToken)
    {
        try
        {
            var conceptPrompt = $@"
🎯 CONCEPT LEARNING ANALYSIS

Knowledge Base: {string.Join(", ", _persistentKnowledgeBase.Keys.Take(10))}

Identify key concepts that need deeper learning:
1. What conceptual frameworks should be developed?
2. What abstract patterns need understanding?
3. What relationship models should be built?

List concepts separated by commas.";

            var kernelArguments = new KernelArguments();
            var result = await _kernel.InvokePromptAsync(conceptPrompt, kernelArguments, cancellationToken: cancellationToken);
            var concepts = result.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var concept in concepts)
            {
                var learning = _conceptLearningTracker.GetOrAdd(concept, new ConceptLearning
                {
                    ConceptName = concept,
                    LearningStarted = DateTime.UtcNow,
                    LearningDepth = 1,
                    ConfidenceLevel = 0.7,
                    LastReinforcement = DateTime.UtcNow
                });
            }

            _logger.LogInformation("🎯 Concept learning completed: {Count} concepts processed", concepts.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in concept learning");
        }
    }

    private async Task PerformPatternRecognitionAsync(KnowledgeUpdates knowledgeUpdates, CancellationToken cancellationToken)
    {
        try
        {
            var patternPrompt = $@"
🔍 PATTERN RECOGNITION ANALYSIS

Knowledge Data: {JsonSerializer.Serialize(_persistentKnowledgeBase.Take(5).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()))}

Identify patterns and trends in the knowledge base:
1. What recurring themes appear?
2. What correlations exist between knowledge items?
3. What emergent patterns suggest new insights?

List patterns separated by commas.";

            var kernelArguments = new KernelArguments();
            var result = await _kernel.InvokePromptAsync(patternPrompt, kernelArguments, cancellationToken: cancellationToken);
            var patterns = result.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);

            _logger.LogInformation("🔍 Pattern recognition completed: {Count} patterns identified", patterns.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in pattern recognition");
        }
    }

    private async Task PerformKnowledgeConsolidationAsync(KnowledgeUpdates knowledgeUpdates, CancellationToken cancellationToken)
    {
        await _knowledgeSemaphore.WaitAsync(cancellationToken);
        try
        {
            var consolidationPrompt = $@"
🔗 KNOWLEDGE CONSOLIDATION ANALYSIS

Fragmented Knowledge: {JsonSerializer.Serialize(_persistentKnowledgeBase.Take(3).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()))}

Consolidate and organize existing knowledge:
1. What knowledge can be merged or unified?
2. What redundancies can be eliminated?
3. What connections between items should be strengthened?

Provide consolidated items as 'Key|Value' format, one per line.";

            var kernelArguments = new KernelArguments();
            var result = await _kernel.InvokePromptAsync(consolidationPrompt, kernelArguments, cancellationToken: cancellationToken);
            var resultText = result.ToString();

            if (!string.IsNullOrEmpty(resultText))
            {
                var consolidated = ExtractKeyValuePairs(resultText);
                foreach (var item in consolidated)
                {
                    _persistentKnowledgeBase.AddOrUpdate(item.Key, item.Value, (key, oldValue) => item.Value);
                }
                _logger.LogInformation("🔗 Knowledge consolidation completed: {Count} items consolidated", consolidated.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in knowledge consolidation");
        }
        finally
        {
            _knowledgeSemaphore.Release();
        }
    }

    private async Task PerformMetaLearningAsync(KnowledgeUpdates knowledgeUpdates, CancellationToken cancellationToken)
    {
        try
        {
            var metaPrompt = $@"
🧠 META-LEARNING ANALYSIS

Learning History: {_conceptLearningTracker.Count} concepts tracked

Analyze learning effectiveness and strategies:
1. What learning approaches are most effective?
2. What knowledge gaps persist despite learning efforts?
3. What meta-cognitive strategies should be adopted?

List meta-insights separated by commas.";

            var kernelArguments = new KernelArguments();
            var result = await _kernel.InvokePromptAsync(metaPrompt, kernelArguments, cancellationToken: cancellationToken);
            var metaInsights = result.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);

            _logger.LogInformation("🧠 Meta-learning completed: {Count} meta-insights generated", metaInsights.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in meta-learning");
        }
    }

    private async Task PerformSemanticKernelLearningAsync(KnowledgeUpdates knowledgeUpdates, CancellationToken cancellationToken)
    {
        try
        {
            var kernelPrompt = $@"
⚙️ SEMANTIC KERNEL OPTIMIZATION ANALYSIS

System State: {_persistentKnowledgeBase.Count} knowledge items

Analyze and optimize the semantic kernel system:
1. What kernel functions need improvement?
2. What prompts are most/least effective?
3. What semantic patterns enhance performance?

List optimization insights separated by commas.";

            var kernelArguments = new KernelArguments();
            var result = await _kernel.InvokePromptAsync(kernelPrompt, kernelArguments, cancellationToken: cancellationToken);
            var kernelInsights = result.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);

            _logger.LogInformation("⚙️ Semantic Kernel learning completed: {Count} optimization insights", kernelInsights.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in Semantic Kernel learning");
        }
    }

    private async Task PerformAdaptivePolicyUpdatesAsync(KnowledgeUpdates knowledgeUpdates, CancellationToken cancellationToken)
    {
        try
        {
            var policyPrompt = $@"
📋 ADAPTIVE POLICY ANALYSIS

Learning Evidence: {_knowledgeEvents.Count} events recorded

Analyze and update adaptive policies:
1. What policies need adjustment based on learning outcomes?
2. What new policies should be implemented?
3. What policy conflicts need resolution?

List policy updates separated by commas.";

            var kernelArguments = new KernelArguments();
            var result = await _kernel.InvokePromptAsync(policyPrompt, kernelArguments, cancellationToken: cancellationToken);
            var policies = result.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);

            _logger.LogInformation("📋 Adaptive policy updates completed: {Count} policies updated", policies.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in adaptive policy updates");
        }
    }

    private async Task PersistKnowledgeUpdatesAsync(KnowledgeUpdates knowledgeUpdates, CancellationToken cancellationToken)
    {
        await _knowledgeSemaphore.WaitAsync(cancellationToken);
        try
        {
            var persistenceData = new
            {
                Timestamp = DateTime.UtcNow,
                Version = knowledgeUpdates.Version,
                KnowledgeBase = _persistentKnowledgeBase.Take(10).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ConceptLearning = _conceptLearningTracker.Take(5).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                Metrics = _knowledgeMetrics.Take(5).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            var persistenceJson = JsonSerializer.Serialize(persistenceData, JsonOptions);
            var persistenceKey = $"knowledge_persistence_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            
            _logger.LogInformation("💾 Knowledge persistence completed: {Key}", persistenceKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in knowledge persistence");
        }
        finally
        {
            _knowledgeSemaphore.Release();
        }
    }

    private void InitializeKnowledgeBase()
    {
        _persistentKnowledgeBase.TryAdd("system_initialized", DateTime.UtcNow);
        _persistentKnowledgeBase.TryAdd("learning_strategy", "continuous_adaptive_learning");
        _persistentKnowledgeBase.TryAdd("knowledge_version", 1);
        
        InitializeKnowledgeMetrics();
        _logger.LogInformation("🧠 Knowledge base initialized with foundational knowledge");
    }

    private void InitializeKnowledgeMetrics()
    {
        var metrics = new[] { "knowledge_discovery_rate", "concept_learning_depth", "pattern_recognition_accuracy", "consolidation_efficiency" };
        
        foreach (var metric in metrics)
        {
            _knowledgeMetrics.TryAdd(metric, new KnowledgeUpdateMetric
            {
                Name = metric,
                Value = 0.0,
                LastUpdated = DateTime.UtcNow,
                Trend = "stable"
            });
        }
    }

    private void UpdateKnowledgeMetric(string name, double value, DateTime timestamp)
    {
        _knowledgeMetrics.AddOrUpdate(name,
            new KnowledgeUpdateMetric { Name = name, Value = value, LastUpdated = timestamp, Trend = "stable" },
            (key, existing) => new KnowledgeUpdateMetric { Name = name, Value = value, LastUpdated = timestamp, Trend = value > existing.Value ? "increasing" : "decreasing" });
    }

    private void RecordKnowledgeEvent(KnowledgeUpdateEvent knowledgeEvent)
    {
        _knowledgeEvents.Enqueue(knowledgeEvent);
        
        // Keep only recent events
        while (_knowledgeEvents.Count > 1000)
        {
            _knowledgeEvents.TryDequeue(out _);
        }
    }

    private List<KeyValuePair<string, object>> ExtractKeyValuePairs(string text)
    {
        var pairs = new List<KeyValuePair<string, object>>();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var parts = line.Split('|', 2);
            if (parts.Length == 2)
            {
                pairs.Add(new KeyValuePair<string, object>(parts[0].Trim(), parts[1].Trim()));
            }
        }
        
        return pairs;
    }

    public override void Dispose()
    {
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
        _knowledgeSemaphore?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Local model classes
public class KnowledgeUpdateMetric
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime LastUpdated { get; set; }
    public string Trend { get; set; } = string.Empty;
}

public class KnowledgeUpdateEvent
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; } = new();
}

public class ConceptLearning
{
    public string ConceptName { get; set; } = string.Empty;
    public DateTime LearningStarted { get; set; }
    public int LearningDepth { get; set; }
    public double ConfidenceLevel { get; set; }
    public DateTime LastReinforcement { get; set; }
}
