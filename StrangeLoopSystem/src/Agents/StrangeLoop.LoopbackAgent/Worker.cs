using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using StrangeLoop.Core.Models;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace StrangeLoop.LoopbackAgent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly Kernel _kernel;
    private readonly ConcurrentDictionary<string, LoopbackMetric> _loopbackMetrics;
    private readonly ConcurrentQueue<LoopbackEvent> _loopbackEvents;
    private readonly ConcurrentDictionary<string, LoopCycleState> _cycleStates;
    private readonly ConcurrentDictionary<string, LoopPerformanceData> _performanceHistory;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;
    private DateTime _lastLoopbackCycle;
    private readonly TimeSpan _loopbackInterval = TimeSpan.FromMinutes(15); // Complete full cycle interval
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private int _currentLoopCycle = 1;
    private readonly SemaphoreSlim _loopbackSemaphore = new(1, 1);

    public Worker(ILogger<Worker> logger, Kernel kernel)
    {
        _logger = logger;
        _kernel = kernel;
        _loopbackMetrics = new ConcurrentDictionary<string, LoopbackMetric>();
        _loopbackEvents = new ConcurrentQueue<LoopbackEvent>();
        _cycleStates = new ConcurrentDictionary<string, LoopCycleState>();
        _performanceHistory = new ConcurrentDictionary<string, LoopPerformanceData>();
        
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
        
        _lastLoopbackCycle = DateTime.UtcNow;
        
        InitializeLoopbackSystem();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🔄 Loopback Agent starting comprehensive loop management and cycle orchestration");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var loopbackResults = new LoopbackResults
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow,
                    AgentId = "LoopbackAgent",
                    CycleNumber = _currentLoopCycle
                };

                // Core Loopback Operations
                await AnalyzeSystemConvergenceAsync(loopbackResults, stoppingToken);
                await EvaluateLoopEffectivenessAsync(loopbackResults, stoppingToken);
                await DetermineLoopContinuationAsync(loopbackResults, stoppingToken);
                await TriggerNewCycleIfNeededAsync(loopbackResults, stoppingToken);
                await RecordCycleHistoryAsync(loopbackResults, stoppingToken);

                // Advanced Loop Management Operations
                await PerformMetaLoopAnalysisAsync(loopbackResults, stoppingToken);
                await OptimizeLoopParametersAsync(loopbackResults, stoppingToken);
                await ManageSystemAdaptationAsync(loopbackResults, stoppingToken);
                await CoordinateInterAgentCommunicationAsync(loopbackResults, stoppingToken);

                // Loop Monitoring and Control
                await MonitorLoopHealthAsync(loopbackResults, stoppingToken);
                await PerformLoopDiagnosticsAsync(loopbackResults, stoppingToken);

                // Performance Monitoring
                await MonitorLoopbackPerformanceAsync(stoppingToken);
                
                var serializedResults = JsonSerializer.Serialize(loopbackResults, JsonOptions);
                _logger.LogInformation("🔄 Loopback cycle completed: {Results}", serializedResults);

                _lastLoopbackCycle = DateTime.UtcNow;
                await Task.Delay(_loopbackInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("🔄 Loopback Agent stopping...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in Loopback Agent cycle");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task AnalyzeSystemConvergenceAsync(LoopbackResults loopbackResults, CancellationToken cancellationToken)
    {
        try
        {
            var convergenceFunction = _kernel.Plugins.GetFunction("LoopbackPlugin", "AnalyzeSystemConvergence");
            var result = await _kernel.InvokeAsync(convergenceFunction, new()
            {
                ["systemState"] = JsonSerializer.Serialize(_cycleStates),
                ["performanceHistory"] = JsonSerializer.Serialize(_performanceHistory),
                ["convergenceCriteria"] = "stability,improvement_rate,error_reduction,goal_achievement"
            }, cancellationToken);

            var convergenceJson = result.GetValue<string>() ?? "{}";
            var convergence = JsonSerializer.Deserialize<Dictionary<string, double>>(convergenceJson) ?? new();

            loopbackResults.SystemConvergence.Clear();
            foreach (var kvp in convergence)
            {
                loopbackResults.SystemConvergence[kvp.Key] = kvp.Value;
            }
            loopbackResults.ConvergenceScore = convergence.Values.Average();

            RecordLoopbackEvent(new LoopbackEvent
            {
                Type = "ConvergenceAnalysis",
                Description = $"System convergence analyzed, score: {loopbackResults.ConvergenceScore:F3}",
                Timestamp = DateTime.UtcNow,
                CycleNumber = _currentLoopCycle,
                Confidence = 0.9
            });

            UpdateLoopbackMetric("ConvergenceScore", loopbackResults.ConvergenceScore, DateTime.UtcNow);
            _logger.LogInformation("📊 System convergence analysis completed: Score={Score:F3}", loopbackResults.ConvergenceScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in system convergence analysis");
        }
    }

    private async Task EvaluateLoopEffectivenessAsync(LoopbackResults loopbackResults, CancellationToken cancellationToken)
    {
        try
        {
            var effectivenessFunction = _kernel.Plugins.GetFunction("LoopbackPlugin", "EvaluateLoopEffectiveness");
            var result = await _kernel.InvokeAsync(effectivenessFunction, new()
            {
                ["loopHistory"] = JsonSerializer.Serialize(_loopbackEvents.ToArray()),
                ["performanceMetrics"] = JsonSerializer.Serialize(_loopbackMetrics),
                ["effectivenessFactors"] = "learning_rate,adaptation_speed,error_correction,goal_alignment"
            }, cancellationToken);

            var effectivenessJson = result.GetValue<string>() ?? "{}";
            var effectiveness = JsonSerializer.Deserialize<Dictionary<string, double>>(effectivenessJson) ?? new();

            loopbackResults.LoopEffectiveness.Clear();
            foreach (var kvp in effectiveness)
            {
                loopbackResults.LoopEffectiveness[kvp.Key] = kvp.Value;
            }
            loopbackResults.EffectivenessScore = effectiveness.Values.Average();

            RecordLoopbackEvent(new LoopbackEvent
            {
                Type = "EffectivenessEvaluation",
                Description = $"Loop effectiveness evaluated, score: {loopbackResults.EffectivenessScore:F3}",
                Timestamp = DateTime.UtcNow,
                CycleNumber = _currentLoopCycle,
                Confidence = 0.95
            });

            UpdateLoopbackMetric("EffectivenessScore", loopbackResults.EffectivenessScore, DateTime.UtcNow);
            _logger.LogInformation("⚡ Loop effectiveness evaluation completed: Score={Score:F3}", loopbackResults.EffectivenessScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in loop effectiveness evaluation");
        }
    }

    private async Task DetermineLoopContinuationAsync(LoopbackResults loopbackResults, CancellationToken cancellationToken)
    {
        try
        {
            var continuationFunction = _kernel.Plugins.GetFunction("LoopbackPlugin", "DetermineLoopContinuation");
            var result = await _kernel.InvokeAsync(continuationFunction, new()
            {
                ["convergenceData"] = JsonSerializer.Serialize(loopbackResults.SystemConvergence),
                ["effectivenessData"] = JsonSerializer.Serialize(loopbackResults.LoopEffectiveness),
                ["continuationCriteria"] = "improvement_potential,resource_availability,goal_completion,time_constraints"
            }, cancellationToken);

            var continuationDecision = result.GetValue<string>() ?? "continue";
            var shouldContinue = continuationDecision.Equals("continue", StringComparison.OrdinalIgnoreCase);

            loopbackResults.ShouldContinue = shouldContinue;
            loopbackResults.ContinuationReason = continuationDecision;

            RecordLoopbackEvent(new LoopbackEvent
            {
                Type = "ContinuationDecision",
                Description = $"Loop continuation decision: {continuationDecision}",
                Timestamp = DateTime.UtcNow,
                CycleNumber = _currentLoopCycle,
                Confidence = 0.9
            });

            UpdateLoopbackMetric("ContinuationDecisions", shouldContinue ? 1 : 0, DateTime.UtcNow);
            _logger.LogInformation("🎯 Loop continuation determined: {Decision} ({Reason})", shouldContinue ? "Continue" : "Stop", continuationDecision);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in loop continuation determination");
            loopbackResults.ShouldContinue = true; // Default to continue on error
        }
    }

    private async Task TriggerNewCycleIfNeededAsync(LoopbackResults loopbackResults, CancellationToken cancellationToken)
    {
        try
        {
            if (loopbackResults.ShouldContinue)
            {
                var triggerFunction = _kernel.Plugins.GetFunction("LoopbackPlugin", "TriggerNewCycle");
                var result = await _kernel.InvokeAsync(triggerFunction, new()
                {
                    ["currentCycle"] = _currentLoopCycle.ToString(),
                    ["loopResults"] = JsonSerializer.Serialize(loopbackResults),
                    ["triggerParameters"] = "reset_agents,initialize_monitoring,start_new_cycle"
                }, cancellationToken);

                var newCycleData = result.GetValue<string>() ?? "{}";
                var cycleInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(newCycleData) ?? new();

                _currentLoopCycle++;
                
                var newCycleState = new LoopCycleState
                {
                    CycleNumber = _currentLoopCycle,
                    StartTime = DateTime.UtcNow,
                    TriggerReason = loopbackResults.ContinuationReason,
                    PreviousCycleResults = loopbackResults
                };

                _cycleStates.TryAdd($"cycle_{_currentLoopCycle}", newCycleState);

                RecordLoopbackEvent(new LoopbackEvent
                {
                    Type = "NewCycleTriggered",
                    Description = $"New cycle {_currentLoopCycle} triggered based on: {loopbackResults.ContinuationReason}",
                    Timestamp = DateTime.UtcNow,
                    CycleNumber = _currentLoopCycle,
                    Confidence = 1.0
                });

                UpdateLoopbackMetric("TriggeredCycles", _currentLoopCycle, DateTime.UtcNow);
                _logger.LogInformation("🚀 New cycle triggered: Cycle #{Cycle} started", _currentLoopCycle);
            }
            else
            {
                _logger.LogInformation("⏸️ Loop cycle paused due to: {Reason}", loopbackResults.ContinuationReason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in new cycle triggering");
        }
    }

    private async Task RecordCycleHistoryAsync(LoopbackResults loopbackResults, CancellationToken cancellationToken)
    {
        await _loopbackSemaphore.WaitAsync(cancellationToken);
        try
        {
            try
            {
                var performanceData = new LoopPerformanceData
                {
                    CycleNumber = _currentLoopCycle,
                    Timestamp = DateTime.UtcNow,
                    ConvergenceScore = loopbackResults.ConvergenceScore,
                    EffectivenessScore = loopbackResults.EffectivenessScore,
                    Duration = DateTime.UtcNow - _lastLoopbackCycle,
                    EventCount = _loopbackEvents.Count
                };

                _performanceHistory.TryAdd($"cycle_{_currentLoopCycle}", performanceData);

                // Keep only recent history to prevent memory bloat
                if (_performanceHistory.Count > 100)
                {
                    var oldestKey = _performanceHistory.Keys.OrderBy(k => k).FirstOrDefault();
                    if (oldestKey != null)
                    {
                        _performanceHistory.TryRemove(oldestKey, out _);
                    }
                }

                UpdateLoopbackMetric("CycleHistorySize", _performanceHistory.Count, DateTime.UtcNow);
                _logger.LogInformation("📝 Cycle history recorded: Cycle #{Cycle}, Performance: C={Convergence:F3}, E={Effectiveness:F3}", 
                    _currentLoopCycle, loopbackResults.ConvergenceScore, loopbackResults.EffectivenessScore);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in cycle history recording");
            }
        }
        finally
        {
            _loopbackSemaphore.Release();
        }
    }

    private async Task PerformMetaLoopAnalysisAsync(LoopbackResults loopbackResults, CancellationToken cancellationToken)
    {
        try
        {
            var metaAnalysisFunction = _kernel.Plugins.GetFunction("LoopbackPlugin", "PerformMetaLoopAnalysis");
            var result = await _kernel.InvokeAsync(metaAnalysisFunction, new()
            {
                ["multiCycleData"] = JsonSerializer.Serialize(_performanceHistory),
                ["loopPatterns"] = JsonSerializer.Serialize(_cycleStates),
                ["analysisTypes"] = "pattern_detection,trend_analysis,anomaly_detection,optimization_opportunities"
            }, cancellationToken);

            var metaInsightsJson = result.GetValue<string>() ?? "[]";
            var metaInsights = JsonSerializer.Deserialize<string[]>(metaInsightsJson) ?? Array.Empty<string>();            loopbackResults.MetaLoopInsights.Clear();
            foreach (var metaInsight in metaInsights)
            {
                loopbackResults.MetaLoopInsights.Add(metaInsight);
            }

            foreach (var metaInsight in metaInsights)
            {
                RecordLoopbackEvent(new LoopbackEvent
                {
                    Type = "MetaAnalysis",
                    Description = $"Meta-insight: {metaInsight}",
                    Timestamp = DateTime.UtcNow,
                    CycleNumber = _currentLoopCycle,
                    Confidence = 0.85
                });
            }

            UpdateLoopbackMetric("MetaInsights", metaInsights.Length, DateTime.UtcNow);
            _logger.LogInformation("🔬 Meta-loop analysis completed: {Count} insights generated", metaInsights.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in meta-loop analysis");
        }
    }

    private async Task OptimizeLoopParametersAsync(LoopbackResults loopbackResults, CancellationToken cancellationToken)
    {
        try
        {
            var optimizationFunction = _kernel.Plugins.GetFunction("LoopbackPlugin", "OptimizeLoopParameters");
            var result = await _kernel.InvokeAsync(optimizationFunction, new()
            {
                ["currentParameters"] = JsonSerializer.Serialize(_loopbackMetrics),
                ["performanceData"] = JsonSerializer.Serialize(_performanceHistory),
                ["optimizationTargets"] = "convergence_speed,effectiveness_improvement,resource_efficiency"
            }, cancellationToken);

            var optimizationsJson = result.GetValue<string>() ?? "{}";
            var optimizations = JsonSerializer.Deserialize<Dictionary<string, object>>(optimizationsJson) ?? new();

            loopbackResults.ParameterOptimizations.Clear();
            foreach (var kvp in optimizations)
            {
                loopbackResults.ParameterOptimizations[kvp.Key] = kvp.Value;
            }

            foreach (var optimization in optimizations)
            {
                RecordLoopbackEvent(new LoopbackEvent
                {
                    Type = "ParameterOptimization",
                    Description = $"Parameter optimized: {optimization.Key} = {optimization.Value}",
                    Timestamp = DateTime.UtcNow,
                    CycleNumber = _currentLoopCycle,
                    Confidence = 0.8
                });
            }

            UpdateLoopbackMetric("ParameterOptimizations", optimizations.Count, DateTime.UtcNow);
            _logger.LogInformation("⚙️ Loop parameter optimization completed: {Count} parameters optimized", optimizations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in loop parameter optimization");
        }
    }

    private async Task ManageSystemAdaptationAsync(LoopbackResults loopbackResults, CancellationToken cancellationToken)
    {
        try
        {
            var adaptationFunction = _kernel.Plugins.GetFunction("LoopbackPlugin", "ManageSystemAdaptation");
            var result = await _kernel.InvokeAsync(adaptationFunction, new()
            {
                ["systemState"] = JsonSerializer.Serialize(loopbackResults),
                ["adaptationNeeds"] = JsonSerializer.Serialize(loopbackResults.MetaLoopInsights),
                ["adaptationStrategies"] = "dynamic_adjustment,proactive_changes,reactive_corrections"
            }, cancellationToken);

            var adaptationsJson = result.GetValue<string>() ?? "[]";
            var adaptations = JsonSerializer.Deserialize<string[]>(adaptationsJson) ?? Array.Empty<string>();            loopbackResults.SystemAdaptations.Clear();
            foreach (var adaptation in adaptations)
            {
                loopbackResults.SystemAdaptations.Add(adaptation);
            }

            foreach (var adaptation in adaptations)
            {
                RecordLoopbackEvent(new LoopbackEvent
                {
                    Type = "SystemAdaptation",
                    Description = $"System adaptation: {adaptation}",
                    Timestamp = DateTime.UtcNow,
                    CycleNumber = _currentLoopCycle,
                    Confidence = 0.9
                });
            }

            UpdateLoopbackMetric("SystemAdaptations", adaptations.Length, DateTime.UtcNow);
            _logger.LogInformation("🔧 System adaptation management completed: {Count} adaptations applied", adaptations.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in system adaptation management");
        }
    }

    private async Task CoordinateInterAgentCommunicationAsync(LoopbackResults loopbackResults, CancellationToken cancellationToken)
    {
        try
        {
            var coordinationFunction = _kernel.Plugins.GetFunction("LoopbackPlugin", "CoordinateInterAgentCommunication");
            var result = await _kernel.InvokeAsync(coordinationFunction, new()
            {
                ["agentStates"] = JsonSerializer.Serialize(_cycleStates),
                ["communicationNeeds"] = JsonSerializer.Serialize(loopbackResults.MetaLoopInsights),
                ["coordinationStrategies"] = "information_sharing,task_synchronization,collaborative_learning"
            }, cancellationToken);            var coordinationJson = result.GetValue<string>() ?? "{}";
            var coordination = JsonSerializer.Deserialize<Dictionary<string, object>>(coordinationJson) ?? new();

            loopbackResults.InterAgentCoordination.Clear();
            foreach (var kvp in coordination)
            {
                loopbackResults.InterAgentCoordination[kvp.Key] = kvp.Value;
            }

            var coordinationCount = coordination.Count;
            RecordLoopbackEvent(new LoopbackEvent
            {
                Type = "InterAgentCoordination",
                Description = $"Inter-agent coordination performed: {coordinationCount} coordination actions",
                Timestamp = DateTime.UtcNow,
                CycleNumber = _currentLoopCycle,
                Confidence = 0.95
            });

            UpdateLoopbackMetric("CoordinationActions", coordinationCount, DateTime.UtcNow);
            _logger.LogInformation("🤝 Inter-agent communication coordination completed: {Count} actions", coordinationCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in inter-agent communication coordination");
        }
    }

    private async Task MonitorLoopHealthAsync(LoopbackResults loopbackResults, CancellationToken cancellationToken)
    {
        try
        {
            var healthFunction = _kernel.Plugins.GetFunction("LoopbackPlugin", "MonitorLoopHealth");
            var result = await _kernel.InvokeAsync(healthFunction, new()
            {
                ["systemMetrics"] = JsonSerializer.Serialize(_loopbackMetrics),
                ["performanceHistory"] = JsonSerializer.Serialize(_performanceHistory),
                ["healthIndicators"] = "stability,responsiveness,efficiency,error_rates"
            }, cancellationToken);            var healthJson = result.GetValue<string>() ?? "{}";
            var health = JsonSerializer.Deserialize<Dictionary<string, double>>(healthJson) ?? new();

            loopbackResults.LoopHealth.Clear();
            foreach (var kvp in health)
            {
                loopbackResults.LoopHealth[kvp.Key] = kvp.Value;
            }
            loopbackResults.HealthScore = health.Values.Average();

            RecordLoopbackEvent(new LoopbackEvent
            {
                Type = "HealthMonitoring",
                Description = $"Loop health monitored, score: {loopbackResults.HealthScore:F3}",
                Timestamp = DateTime.UtcNow,
                CycleNumber = _currentLoopCycle,
                Confidence = 0.95
            });

            UpdateLoopbackMetric("HealthScore", loopbackResults.HealthScore, DateTime.UtcNow);
            _logger.LogInformation("🏥 Loop health monitoring completed: Score={Score:F3}", loopbackResults.HealthScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in loop health monitoring");
        }
    }

    private async Task PerformLoopDiagnosticsAsync(LoopbackResults loopbackResults, CancellationToken cancellationToken)
    {
        try
        {
            var diagnosticsFunction = _kernel.Plugins.GetFunction("LoopbackPlugin", "PerformLoopDiagnostics");
            var result = await _kernel.InvokeAsync(diagnosticsFunction, new()
            {
                ["diagnosticData"] = JsonSerializer.Serialize(loopbackResults),
                ["eventHistory"] = JsonSerializer.Serialize(_loopbackEvents.ToArray()),
                ["diagnosticTypes"] = "performance_analysis,bottleneck_detection,error_pattern_analysis"
            }, cancellationToken);

            var diagnosticsJson = result.GetValue<string>() ?? "[]";
            var diagnostics = JsonSerializer.Deserialize<string[]>(diagnosticsJson) ?? Array.Empty<string>();            loopbackResults.LoopDiagnostics.Clear();
            foreach (var diagnostic in diagnostics)
            {
                loopbackResults.LoopDiagnostics.Add(diagnostic);
            }

            foreach (var diagnostic in diagnostics)
            {
                RecordLoopbackEvent(new LoopbackEvent
                {
                    Type = "Diagnostics",
                    Description = $"Diagnostic: {diagnostic}",
                    Timestamp = DateTime.UtcNow,
                    CycleNumber = _currentLoopCycle,
                    Confidence = 0.9
                });
            }

            UpdateLoopbackMetric("DiagnosticResults", diagnostics.Length, DateTime.UtcNow);
            _logger.LogInformation("🔍 Loop diagnostics completed: {Count} diagnostic results", diagnostics.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in loop diagnostics");
        }
    }

    private async Task MonitorLoopbackPerformanceAsync(CancellationToken cancellationToken)
    {
        try
        {
            double cpuUsage = 0.0;
            double memoryAvailable = 1024.0; // Default 1GB

            // Get performance metrics only on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    cpuUsage = _cpuCounter?.NextValue() ?? 0.0;
                    memoryAvailable = _memoryCounter?.NextValue() ?? 1024.0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Could not read performance counters");
                }
            }

            var cycleStatesCount = _cycleStates.Count;
            var performanceHistoryCount = _performanceHistory.Count;
            var loopbackEventsCount = _loopbackEvents.Count;

            UpdateLoopbackMetric("SystemCpuUsage", cpuUsage, DateTime.UtcNow);
            UpdateLoopbackMetric("SystemMemoryAvailable", memoryAvailable, DateTime.UtcNow);
            UpdateLoopbackMetric("CycleStatesCount", cycleStatesCount, DateTime.UtcNow);
            UpdateLoopbackMetric("PerformanceHistoryCount", performanceHistoryCount, DateTime.UtcNow);
            UpdateLoopbackMetric("LoopbackEventsCount", loopbackEventsCount, DateTime.UtcNow);

            _logger.LogInformation("📊 Loopback performance: CPU={Cpu:F1}%, Memory={Memory:F0}MB, Cycles={Cycles}, Events={Events}",
                cpuUsage, memoryAvailable, cycleStatesCount, loopbackEventsCount);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in loopback performance monitoring");
        }
    }

    private void InitializeLoopbackSystem()
    {
        // Initialize foundational loop state
        var initialCycleState = new LoopCycleState
        {
            CycleNumber = _currentLoopCycle,
            StartTime = DateTime.UtcNow,
            TriggerReason = "System initialization"
        };

        _cycleStates.TryAdd($"cycle_{_currentLoopCycle}", initialCycleState);
        
        InitializeLoopbackMetrics();
        
        _logger.LogInformation("🔄 Loopback system initialized with initial cycle state");
    }

    private void InitializeLoopbackMetrics()
    {
        var baseMetrics = new[]
        {
            "ConvergenceScore", "EffectivenessScore", "ContinuationDecisions", "TriggeredCycles",
            "CycleHistorySize", "MetaInsights", "ParameterOptimizations", "SystemAdaptations",
            "CoordinationActions", "HealthScore", "DiagnosticResults", "SystemCpuUsage",
            "SystemMemoryAvailable", "CycleStatesCount", "PerformanceHistoryCount", "LoopbackEventsCount"
        };

        foreach (var metric in baseMetrics)
        {
            _loopbackMetrics.TryAdd(metric, new LoopbackMetric
            {
                Name = metric,
                Value = 0.0,
                Timestamp = DateTime.UtcNow,
                Category = "Loopback"
            });
        }
    }

    private void UpdateLoopbackMetric(string name, double value, DateTime timestamp)
    {
        _loopbackMetrics.AddOrUpdate(name, 
            new LoopbackMetric { Name = name, Value = value, Timestamp = timestamp, Category = "Loopback" },
            (key, existing) => 
            {
                existing.Value = value;
                existing.Timestamp = timestamp;
                return existing;
            });
    }

    private void RecordLoopbackEvent(LoopbackEvent loopbackEvent)
    {
        _loopbackEvents.Enqueue(loopbackEvent);
        
        // Keep only recent events to prevent memory bloat
        while (_loopbackEvents.Count > 1000)
        {
            _loopbackEvents.TryDequeue(out _);
        }
    }

    public sealed override void Dispose()
    {
        try
        {
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
            _loopbackSemaphore?.Dispose();
        }
        finally
        {
            base.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}

// Supporting Models for Loopback Agent

public class LoopbackResults
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public int CycleNumber { get; set; }
    public Dictionary<string, double> SystemConvergence { get; } = new();
    public double ConvergenceScore { get; set; }
    public Dictionary<string, double> LoopEffectiveness { get; } = new();
    public double EffectivenessScore { get; set; }
    public bool ShouldContinue { get; set; }
    public string ContinuationReason { get; set; } = string.Empty;
    public Collection<string> MetaLoopInsights { get; } = new();
    public Dictionary<string, object> ParameterOptimizations { get; } = new();
    public Collection<string> SystemAdaptations { get; } = new();
    public Dictionary<string, object> InterAgentCoordination { get; } = new();
    public Dictionary<string, double> LoopHealth { get; } = new();
    public double HealthScore { get; set; }
    public Collection<string> LoopDiagnostics { get; } = new();
}

public class LoopbackMetric
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public string Category { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; } = new();
}

public class LoopbackEvent
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int CycleNumber { get; set; }
    public double Confidence { get; set; }
    public Dictionary<string, object> Details { get; } = new();
}

public class LoopCycleState
{
    public int CycleNumber { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string TriggerReason { get; set; } = string.Empty;
    public LoopbackResults? PreviousCycleResults { get; set; }
    public Dictionary<string, object> StateData { get; } = new();
}

public class LoopPerformanceData
{
    public int CycleNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public double ConvergenceScore { get; set; }
    public double EffectivenessScore { get; set; }
    public TimeSpan Duration { get; set; }
    public int EventCount { get; set; }
    public Dictionary<string, object> AdditionalMetrics { get; } = new();
}
