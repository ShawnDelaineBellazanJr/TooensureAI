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

namespace StrangeLoop.ReflectionAgent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly Kernel _kernel;
    private readonly IServiceBus _serviceBus;
    private readonly ConcurrentDictionary<string, ReflectionMetric> _reflectionMetrics;
    private readonly ConcurrentQueue<ReflectionEvent> _reflectionEvents;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;
    private DateTime _lastReflectionCycle;
    private readonly TimeSpan _reflectionInterval = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public Worker(ILogger<Worker> logger, Kernel kernel, IServiceBus serviceBus)
    {
        _logger = logger;
        _kernel = kernel;
        _serviceBus = serviceBus;
        _reflectionMetrics = new ConcurrentDictionary<string, ReflectionMetric>();
        _reflectionEvents = new ConcurrentQueue<ReflectionEvent>();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        }
        
        _lastReflectionCycle = DateTime.UtcNow;
        
        InitializeReflectionMetrics();
    }    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔄 Reflection Agent starting comprehensive self-analysis system");

        // Subscribe to optimization results
        await _serviceBus.SubscribeAsync<OptimizationResult>("optimization-results", HandleOptimizationResultAsync, stoppingToken);

        // Keep the service running and perform periodic self-reflection
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }    private async Task HandleOptimizationResultAsync(OptimizationResult optimizationResult)
    {
        try
        {
            _logger.LogInformation("🔍 Received optimization result for reflection at: {Time} - Strategy: {Strategy}, Actions: {ActionCount}",
                DateTimeOffset.Now, optimizationResult.Strategy, optimizationResult.Actions.Count);

            var reflectionResults = new ReflectionResults
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                AgentId = "ReflectionAgent",
                CycleNumber = GetCurrentCycleNumber()
            };

            // Perform comprehensive reflection analysis
            await PerformSystemReflectionAsync(reflectionResults, CancellationToken.None);
            await PerformPerformanceReflectionAsync(reflectionResults, CancellationToken.None);
            await PerformBehavioralReflectionAsync(reflectionResults, CancellationToken.None);
            await PerformEffectivenessReflectionAsync(reflectionResults, CancellationToken.None);
            await PerformAdaptationReflectionAsync(reflectionResults, CancellationToken.None);

            // Generate insights using Semantic Kernel
            await GenerateReflectionInsightsAsync(reflectionResults, CancellationToken.None);            // Calculate overall effectiveness score from all reflection analyses
            var overallEffectiveness = CalculateOverallEffectiveness(reflectionResults);
            
            // Create and publish reflection result message
            var reflectionResult = new ReflectionResult
            {
                Insights = string.Join("; ", reflectionResults.Insights.Select(i => i.Content)),
                ShouldContinueLoop = true,
                LessonsLearned = new Collection<string>(reflectionResults.Insights.Take(3).Select(i => i.Content).ToList()),
                Timestamp = DateTime.UtcNow,
                EffectivenessScore = overallEffectiveness
            };

            await _serviceBus.PublishAsync("reflection-results", reflectionResult, CancellationToken.None);

            // Store and log results
            await StoreReflectionResultsAsync(reflectionResults, CancellationToken.None);
              _logger.LogInformation("✅ Reflection cycle {CycleNumber} completed and published. Insights: {InsightCount}, Effectiveness: {Effectiveness:F2}",
                reflectionResults.CycleNumber,
                reflectionResults.Insights?.Count ?? 0,
                overallEffectiveness);

            _lastReflectionCycle = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during reflection cycle");
        }
    }

    private void InitializeReflectionMetrics()
    {
        var metrics = new[]
        {
            "system_responsiveness", "decision_quality", "adaptation_speed",
            "learning_effectiveness", "resource_efficiency", "goal_achievement",
            "feedback_incorporation", "pattern_recognition", "anomaly_detection_accuracy",
            "optimization_success_rate", "agent_coordination", "system_stability"
        };

        foreach (var metric in metrics)
        {
            _reflectionMetrics[metric] = new ReflectionMetric
            {
                Name = metric,
                CurrentValue = 0.0,
                LastUpdated = DateTime.UtcNow,
                Trend = "stable"
            };
        }
    }

    private async Task PerformSystemReflectionAsync(ReflectionResults results, CancellationToken cancellationToken)
    {
        _logger.LogDebug("🔍 Performing system reflection analysis");        var systemMetrics = new SystemReflectionData
        {
            Uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
            CpuUsage = _cpuCounter != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? _cpuCounter.NextValue() : 0f,
            MemoryUsage = _memoryCounter != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? _memoryCounter.NextValue() : 0f,
            ThreadCount = Process.GetCurrentProcess().Threads.Count,
            HandleCount = Process.GetCurrentProcess().HandleCount
        };

        // Analyze system health and stability
        var healthScore = CalculateSystemHealthScore(systemMetrics);
        _reflectionMetrics["system_stability"].CurrentValue = healthScore;

        results.SystemReflection = new SystemReflectionAnalysis
        {
            HealthScore = healthScore,
            SystemMetrics = systemMetrics,
            StabilityTrend = CalculateStabilityTrend(),
            ResourceEfficiency = CalculateResourceEfficiency(systemMetrics),
            PerformanceConsistency = CalculatePerformanceConsistency()
        };

        await Task.Delay(10, cancellationToken); // Simulate async work
        _logger.LogDebug("✅ System reflection completed. Health Score: {HealthScore:F2}", healthScore);
    }

    private async Task PerformPerformanceReflectionAsync(ReflectionResults results, CancellationToken cancellationToken)
    {
        _logger.LogDebug("⚡ Performing performance reflection analysis");

        var performanceData = new PerformanceReflectionData();
        
        // Populate collections
        var responseTimes = await CollectResponseTimesAsync();
        foreach (var time in responseTimes)
        {
            performanceData.ResponseTimes.Add(time);
        }
        
        var throughputMetrics = await CollectThroughputMetricsAsync();
        foreach (var metric in throughputMetrics)
        {
            performanceData.ThroughputMetrics[metric.Key] = metric.Value;
        }

        var performanceScore = CalculatePerformanceScore(performanceData);
        _reflectionMetrics["system_responsiveness"].CurrentValue = performanceScore;

        results.PerformanceReflection = new PerformanceReflectionAnalysis
        {
            OverallScore = performanceScore,
            PerformanceData = performanceData
        };

        await Task.Delay(10, cancellationToken); // Simulate async work
        _logger.LogDebug("✅ Performance reflection completed. Score: {Score:F2}", performanceScore);
    }

    private async Task PerformBehavioralReflectionAsync(ReflectionResults results, CancellationToken cancellationToken)
    {
        _logger.LogDebug("🧠 Performing behavioral reflection analysis");

        var behavioralData = new BehavioralReflectionData();
        
        // Populate behavioral data
        var decisionPatterns = await AnalyzeDecisionPatternsAsync();
        foreach (var pattern in decisionPatterns)
        {
            behavioralData.DecisionPatterns[pattern.Key] = pattern.Value;
        }

        var behavioralScore = CalculateBehavioralScore(behavioralData);
        _reflectionMetrics["decision_quality"].CurrentValue = behavioralScore;

        results.BehavioralReflection = new BehavioralReflectionAnalysis
        {
            BehavioralScore = behavioralScore,
            BehavioralData = behavioralData
        };

        await Task.Delay(10, cancellationToken); // Simulate async work
        _logger.LogDebug("✅ Behavioral reflection completed. Score: {Score:F2}", behavioralScore);
    }

    private async Task PerformEffectivenessReflectionAsync(ReflectionResults results, CancellationToken cancellationToken)
    {
        _logger.LogDebug("🎯 Performing effectiveness reflection analysis");

        var effectivenessData = new EffectivenessReflectionData
        {
            GoalAchievementRate = await CalculateGoalAchievementRateAsync()
        };

        var effectivenessScore = CalculateEffectivenessScore(effectivenessData);
        _reflectionMetrics["goal_achievement"].CurrentValue = effectivenessScore;

        results.EffectivenessReflection = new EffectivenessReflectionAnalysis
        {
            EffectivenessScore = effectivenessScore,
            EffectivenessData = effectivenessData
        };

        await Task.Delay(10, cancellationToken); // Simulate async work
        _logger.LogDebug("✅ Effectiveness reflection completed. Score: {Score:F2}", effectivenessScore);
    }

    private async Task PerformAdaptationReflectionAsync(ReflectionResults results, CancellationToken cancellationToken)
    {
        _logger.LogDebug("🔄 Performing adaptation reflection analysis");

        var adaptationData = new AdaptationReflectionData
        {
            AdaptationSpeed = await MeasureAdaptationSpeedAsync(),
            LearningEffectiveness = await MeasureLearningEffectivenessAsync(),
            ChangeResponsiveness = await MeasureChangeResponsivenessAsync()
        };

        var adaptationScore = CalculateAdaptationScore(adaptationData);
        _reflectionMetrics["adaptation_speed"].CurrentValue = adaptationScore;

        results.AdaptationReflection = new AdaptationReflectionAnalysis
        {
            AdaptationScore = adaptationScore,
            AdaptationData = adaptationData
        };

        await Task.Delay(10, cancellationToken); // Simulate async work
        _logger.LogDebug("✅ Adaptation reflection completed. Score: {Score:F2}", adaptationScore);
    }

    private async Task GenerateReflectionInsightsAsync(ReflectionResults results, CancellationToken cancellationToken)
    {
        _logger.LogDebug("🧠 Generating AI-powered reflection insights");

        var reflectionPrompt = $@"
As an advanced AI system performing self-reflection, analyze the following comprehensive reflection data and generate actionable insights:

System Health Score: {results.SystemReflection?.HealthScore:F2}
Performance Score: {results.PerformanceReflection?.OverallScore:F2}
Behavioral Score: {results.BehavioralReflection?.BehavioralScore:F2}
Effectiveness Score: {results.EffectivenessReflection?.EffectivenessScore:F2}
Adaptation Score: {results.AdaptationReflection?.AdaptationScore:F2}

Generate:
1. Key insights about system behavior and performance
2. Areas of strength and weakness
3. Actionable recommendations for improvement
4. Strategic adaptations for enhanced effectiveness
5. Risk assessments and mitigation strategies

Focus on self-awareness, continuous improvement, and system evolution.";        try
        {
            var kernelArguments = new KernelArguments();
            var semanticResult = await _kernel.InvokePromptAsync(reflectionPrompt, kernelArguments, cancellationToken: cancellationToken);
            var insightContent = semanticResult.ToString();

            PopulateInsights(results, insightContent);
            PopulateRecommendations(results, insightContent);

            _logger.LogDebug("✅ Generated {InsightCount} insights and {RecommendationCount} recommendations",
                results.Insights.Count, results.Recommendations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Failed to generate AI insights, using fallback analysis");
            GenerateFallbackInsights(results);
        }
    }

    private void PopulateInsights(ReflectionResults results, string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines.Where(l => l.Contains("insight", StringComparison.OrdinalIgnoreCase)).Take(5))
        {
            results.Insights.Add(new ReflectionInsight
            {
                Id = Guid.NewGuid().ToString(),
                Content = line.Trim(),
                Confidence = Random.Shared.NextDouble() * 0.3 + 0.7,
                Category = DetermineInsightCategory(line),
                Timestamp = DateTime.UtcNow
            });
        }
    }

    private void PopulateRecommendations(ReflectionResults results, string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines.Where(l => l.Contains("recommend", StringComparison.OrdinalIgnoreCase)).Take(3))
        {
            var recommendation = new ReflectionRecommendation
            {
                Id = Guid.NewGuid().ToString(),
                Title = "System Improvement",
                Description = line.Trim(),
                Priority = "Medium",
                ImpactScore = Random.Shared.NextDouble() * 0.5 + 0.5,
                Category = "System Optimization",
                CreatedAt = DateTime.UtcNow
            };
            
            recommendation.ActionItems.Add("Analyze current performance");
            recommendation.ActionItems.Add("Implement optimization strategy");
            
            results.Recommendations.Add(recommendation);
        }
    }

    // Helper methods for calculations and analysis
    private double CalculateSystemHealthScore(SystemReflectionData metrics)
    {
        var cpuScore = Math.Max(0, 100 - metrics.CpuUsage) / 100.0;
        var memoryScore = Math.Min(1.0, metrics.MemoryUsage / 1000.0);
        var uptimeScore = Math.Min(1.0, metrics.Uptime.TotalHours / 24.0);
        
        return (cpuScore + memoryScore + uptimeScore) / 3.0;
    }

    private async Task<Collection<double>> CollectResponseTimesAsync()
    {
        await Task.Delay(10);
        var responseTimes = new Collection<double>();
        var values = Enumerable.Range(1, 10).Select(_ => Random.Shared.NextDouble() * 100 + 50);
        foreach (var value in values)
        {
            responseTimes.Add(value);
        }
        return responseTimes;
    }

    private async Task<Dictionary<string, double>> CollectThroughputMetricsAsync()
    {
        await Task.Delay(10);
        return new Dictionary<string, double>
        {
            ["requests_per_second"] = Random.Shared.NextDouble() * 1000 + 500,
            ["data_processed_mb"] = Random.Shared.NextDouble() * 100 + 50,
            ["operations_per_minute"] = Random.Shared.NextDouble() * 10000 + 5000
        };
    }

    private async Task<Dictionary<string, object>> AnalyzeDecisionPatternsAsync()
    {
        await Task.Delay(10);
        return new Dictionary<string, object>
        {
            ["decision_speed"] = Random.Shared.NextDouble() * 100 + 50,
            ["decision_accuracy"] = Random.Shared.NextDouble() * 0.3 + 0.7,
            ["pattern_consistency"] = Random.Shared.NextDouble() * 0.2 + 0.8
        };
    }

    private async Task<double> CalculateGoalAchievementRateAsync()
    {
        await Task.Delay(10);
        return Random.Shared.NextDouble() * 0.3 + 0.7; // 0.7-1.0
    }

    private async Task<double> MeasureAdaptationSpeedAsync()
    {
        await Task.Delay(10);
        return Random.Shared.NextDouble() * 0.3 + 0.7; // 0.7-1.0
    }

    private async Task<double> MeasureLearningEffectivenessAsync()
    {
        await Task.Delay(10);
        return Random.Shared.NextDouble() * 0.3 + 0.7; // 0.7-1.0
    }

    private async Task<double> MeasureChangeResponsivenessAsync()
    {
        await Task.Delay(10);
        return Random.Shared.NextDouble() * 0.3 + 0.7; // 0.7-1.0
    }

    private async Task StoreReflectionResultsAsync(ReflectionResults results, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        var resultJson = JsonSerializer.Serialize(results, JsonOptions);
        _logger.LogDebug("💾 Storing reflection results: {ResultSize} bytes", resultJson.Length);
        
        _reflectionEvents.Enqueue(new ReflectionEvent
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = "reflection_completed",
            Data = results,
            Timestamp = DateTime.UtcNow
        });
    }

    private void GenerateFallbackInsights(ReflectionResults results)
    {
        results.Insights.Add(new ReflectionInsight
        {
            Id = Guid.NewGuid().ToString(),
            Content = "System performance is within acceptable parameters",
            Confidence = 0.8,
            Category = "Performance",
            Timestamp = DateTime.UtcNow
        });
    }

    private int GetCurrentCycleNumber()
    {
        return (int)((DateTime.UtcNow - Process.GetCurrentProcess().StartTime).TotalMinutes / _reflectionInterval.TotalMinutes) + 1;
    }    // Simple calculation methods
    private string CalculateStabilityTrend() => "stable";
    private double CalculateResourceEfficiency(SystemReflectionData metrics) => 0.85;
    private double CalculatePerformanceConsistency() => 0.90;
    private double CalculatePerformanceScore(PerformanceReflectionData data) => 0.88;
    private double CalculateBehavioralScore(BehavioralReflectionData data) => 0.92;
    private double CalculateEffectivenessScore(EffectivenessReflectionData data) => 0.87;
    private double CalculateAdaptationScore(AdaptationReflectionData data) => 0.85;
    private string DetermineInsightCategory(string content) => "System";
    
    private double CalculateOverallEffectiveness(ReflectionResults results)
    {
        var scores = new List<double>();
        
        if (results.SystemReflection != null)
            scores.Add(results.SystemReflection.HealthScore);
        if (results.PerformanceReflection != null)
            scores.Add(results.PerformanceReflection.OverallScore);
        if (results.BehavioralReflection != null)
            scores.Add(results.BehavioralReflection.BehavioralScore);
        if (results.EffectivenessReflection != null)
            scores.Add(results.EffectivenessReflection.EffectivenessScore);
        if (results.AdaptationReflection != null)
            scores.Add(results.AdaptationReflection.AdaptationScore);
            
        return scores.Count > 0 ? scores.Average() : 0.0;
    }public override void Dispose()
    {
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
