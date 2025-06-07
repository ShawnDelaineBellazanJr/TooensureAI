using System.Collections.ObjectModel;

namespace StrangeLoop.Core.Messages;

// ============================================================================
// MONITORING AGENT MESSAGES
// ============================================================================

public sealed class MonitoringRequest
{
    public required string ExecutionId { get; init; }
    public required string SystemComponent { get; init; }
    public DateTime RequestTime { get; init; } = DateTime.UtcNow;
    public Collection<string> MetricsToCollect { get; init; } = new();
}

public sealed class MonitoringResult
{
    public required string ExecutionId { get; init; }
    public required TelemetryData TelemetryData { get; init; }
    public DateTime CollectionTime { get; init; } = DateTime.UtcNow;
    public bool IsHealthy { get; init; } = true;
}

public sealed class MonitoringParameters
{
    public TimeSpan CollectionInterval { get; init; } = TimeSpan.FromSeconds(30);
    public Collection<string> MetricNames { get; init; } = new();
    public bool IncludeSystemMetrics { get; init; } = true;
}

// ============================================================================
// ANALYSIS AGENT MESSAGES
// ============================================================================

public sealed class AnalysisRequest
{
    public required string ExecutionId { get; init; }
    public required TelemetryData TelemetryData { get; init; }
    public DateTime RequestTime { get; init; } = DateTime.UtcNow;
    public AnalysisParameters? Parameters { get; init; }
}

public sealed class AnalysisResult
{
    public required AnalysisInsights Insights { get; init; }
    public Collection<string> ImprovementOpportunities { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public double ConfidenceScore { get; init; } = 0.0;
}

public sealed class AnalysisParameters
{
    public TimeSpan AnalysisWindow { get; init; } = TimeSpan.FromHours(1);
    public double AnomalyThreshold { get; init; } = 0.95;
    public Collection<string> MetricsToAnalyze { get; init; } = new();
}

public sealed class AnalysisInsights
{
    public required string Trends { get; init; }
    public required string Anomalies { get; init; }
    public Collection<string> Recommendations { get; init; } = new();
    public double RiskScore { get; init; } = 0.0;
}

// ============================================================================
// OPTIMIZATION AGENT MESSAGES
// ============================================================================

public sealed class OptimizationRequest
{
    public required string ExecutionId { get; init; }
    public required AnalysisResult AnalysisResult { get; init; }
    public DateTime RequestTime { get; init; } = DateTime.UtcNow;
    public OptimizationParameters? Parameters { get; init; }
}

public sealed class OptimizationResult
{
    public required string Strategy { get; init; }
    public Collection<string> Actions { get; init; } = new();
    public double ExpectedImprovement { get; init; } = 0.0;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public TimeSpan EstimatedDuration { get; init; } = TimeSpan.Zero;
}

public sealed class OptimizationParameters
{
    public double LearningRate { get; init; } = 0.01;
    public int MaxIterations { get; init; } = 1000;
    public double ConvergenceThreshold { get; init; } = 0.001;
    public Collection<string> OptimizationTargets { get; init; } = new();
}

// ============================================================================
// REFLECTION AGENT MESSAGES
// ============================================================================

public sealed class ReflectionRequest
{
    public required string ExecutionId { get; init; }
    public required OptimizationResult OptimizationResult { get; init; }
    public required SystemMetrics SystemMetrics { get; init; }
    public DateTime RequestTime { get; init; } = DateTime.UtcNow;
}

public sealed class ReflectionResult
{
    public required string Insights { get; init; }
    public bool ShouldContinueLoop { get; init; } = true;
    public Collection<string> LessonsLearned { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public double EffectivenessScore { get; init; } = 0.0;
}

// ============================================================================
// KNOWLEDGE UPDATE AGENT MESSAGES
// ============================================================================

public sealed class KnowledgeUpdateRequest
{
    public required string ExecutionId { get; init; }
    public required ReflectionResult ReflectionResult { get; init; }
    public DateTime RequestTime { get; init; } = DateTime.UtcNow;
    public KnowledgeUpdateParameters? Parameters { get; init; }
}

public sealed class KnowledgeUpdateResult
{
    public bool Success { get; init; } = true;
    public int UpdatedKnowledgeCount { get; init; } = 0;
    public Collection<string> UpdatedConcepts { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? ErrorMessage { get; init; }
}

public sealed class KnowledgeUpdateParameters
{
    public double ConfidenceThreshold { get; init; } = 0.7;
    public bool EnableAutoUpdate { get; init; } = true;
    public Collection<string> KnowledgeDomains { get; init; } = new();
}

// ============================================================================
// LOOPBACK AGENT MESSAGES
// ============================================================================

public sealed class LoopbackRequest
{
    public required string ExecutionId { get; init; }
    public required KnowledgeUpdateResult KnowledgeUpdateResult { get; init; }
    public DateTime RequestTime { get; init; } = DateTime.UtcNow;
    public LoopbackParameters? Parameters { get; init; }
}

public sealed class LoopbackResult
{
    public bool Success { get; init; } = true;
    public int AdjustedParametersCount { get; init; } = 0;
    public Collection<string> SystemAdjustments { get; init; } = new();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public bool ShouldRestartLoop { get; init; } = false;
}

public sealed class LoopbackParameters
{
    public double AdjustmentStrength { get; init; } = 0.5;
    public bool EnableSystemAdjustments { get; init; } = true;
    public TimeSpan LoopDelay { get; init; } = TimeSpan.FromSeconds(10);
}

// ============================================================================
// SHARED DATA TYPES
// ============================================================================

public sealed class TelemetryData
{
    public Dictionary<string, object> SystemMetrics { get; init; } = new();
    public Collection<string> Anomalies { get; init; } = new();
    public DateTime CollectionTime { get; init; } = DateTime.UtcNow;
    public string SystemPerformance { get; init; } = "Normal";
    public int UserInteractions { get; init; } = 0;
}

public sealed class SystemMetrics
{
    public double CpuUsage { get; init; } = 0.0;
    public double MemoryUsage { get; init; } = 0.0;
    public double NetworkLatency { get; init; } = 0.0;
    public int ActiveConnections { get; init; } = 0;
    public Collection<string> PerformanceIndicators { get; init; } = new();
    public DateTime MeasurementTime { get; init; } = DateTime.UtcNow;
}

public sealed class SystemState
{
    public required string State { get; init; }
    public Dictionary<string, object> Properties { get; init; } = new();
    public DateTime LastUpdate { get; init; } = DateTime.UtcNow;
    public bool IsStable { get; init; } = true;
}
