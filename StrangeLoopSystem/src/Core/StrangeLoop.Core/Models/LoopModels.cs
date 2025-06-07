using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace StrangeLoop.Core.Models;

/// <summary>
/// Represents data collected during the monitoring stage
/// </summary>
public class MonitoringData
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string SystemId { get; set; } = string.Empty;
    public Dictionary<string, object> Metrics { get; } = new();
    public Collection<string> Events { get; } = new();
    public PerformanceMetrics Performance { get; set; } = new();
    public Collection<string> Anomalies { get; } = new();
    public string RawData { get; set; } = string.Empty;
    
    // Additional properties used by the engine
    public Dictionary<string, object> SystemMetrics { get; } = new();
    public Dictionary<string, object> PerformanceMetrics { get; } = new();
    public Dictionary<string, object> BehaviorMetrics { get; } = new();
    public Dictionary<string, object> EnvironmentState { get; } = new();
}

/// <summary>
/// Represents the results of the analysis stage
/// </summary>
public class AnalysisResults
{
    public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public MonitoringData SourceData { get; set; } = new();
    public Collection<string> Patterns { get; } = new();
    public Collection<string> Insights { get; } = new();
    public Dictionary<string, double> ConfidenceScores { get; } = new();
    public Collection<string> Recommendations { get; } = new();
    public AnalysisMetrics Metrics { get; set; } = new();
    
    // Additional properties used by the engine
    public string RawAnalysis { get; set; } = string.Empty;
    public Collection<string> IdentifiedPatterns { get; } = new();
    public Collection<string> ImprovementOpportunities { get; } = new();
    public string RiskAssessment { get; set; } = string.Empty;
}

/// <summary>
/// Represents an optimization plan generated from analysis
/// </summary>
public class OptimizationPlan
{
    public string PlanId { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public AnalysisResults SourceAnalysis { get; set; } = new();
    public Collection<OptimizationAction> Actions { get; } = new();
    public int Priority { get; set; }
    public double ExpectedImpact { get; set; }
    public Dictionary<string, object> Parameters { get; } = new();
    public Collection<string> Prerequisites { get; } = new();
    
    // Additional properties used by the engine
    public string RawPlan { get; set; } = string.Empty;
    public string Timeline { get; set; } = string.Empty;
}

/// <summary>
/// Represents insights gained from reflection on the optimization process
/// </summary>
public class ReflectionInsights
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public OptimizationPlan SourcePlan { get; set; } = new();
    public double QualityScore { get; set; }
    public Collection<string> LessonsLearned { get; } = new();
    public Collection<string> ImprovementOpportunities { get; } = new();
    public double EffectivenessScore { get; set; }
    public Collection<string> NextSteps { get; } = new();
    
    // Additional properties used by the engine
    public string RawReflection { get; set; } = string.Empty;
    public Collection<string> ProcessImprovements { get; } = new();
    public Collection<string> MetaLearning { get; } = new();
    public Dictionary<string, object> MetaInsights { get; } = new();
}

/// <summary>
/// Represents updates to the system's knowledge base
/// </summary>
public class KnowledgeUpdates
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public ReflectionInsights SourceInsights { get; set; } = new();
    public Dictionary<string, object> KnowledgeBase { get; } = new();
    public Collection<string> UpdatedConcepts { get; } = new();
    public Collection<string> NewKnowledge { get; } = new();
    public int Version { get; set; }
    public Dictionary<string, object> Metadata { get; } = new();
    public Collection<string> UpdatedPolicies { get; } = new();
    public Collection<string> UpdatedPatterns { get; } = new();
    public Collection<string> UpdatedHeuristics { get; } = new();
    public Collection<string> UpdatedMetrics { get; } = new();
}

/// <summary>
/// Represents an optimization action to be taken
/// </summary>
public class OptimizationAction
{
    public string ActionId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public Dictionary<string, object> Parameters { get; } = new();
    public double ExpectedImpact { get; set; }
    public DateTime ScheduledTime { get; set; } = DateTime.UtcNow;
    public bool IsCompleted { get; set; }
}

/// <summary>
/// Represents metrics collected during analysis
/// </summary>
public class AnalysisMetrics
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double AnalysisAccuracy { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public int DataPointsAnalyzed { get; set; }
    public int PatternsIdentified { get; set; }
    public Dictionary<string, object> Metadata { get; } = new();
    public double ConfidenceLevel { get; set; }
}

/// <summary>
/// Represents performance metrics collected during monitoring
/// </summary>
public class PerformanceMetrics
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public double NetworkLatency { get; set; }
    public double Throughput { get; set; }
    public double ErrorRate { get; set; }
    public Dictionary<string, double> CustomMetrics { get; } = new();
}

/// <summary>
/// Represents metrics about the quality of analysis and decisions
/// </summary>
public class QualityMetrics
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double DecisionAccuracy { get; set; }
    public double PredictionAccuracy { get; set; }
    public Dictionary<string, double> QualityScores { get; } = new();
    public double OverallQuality { get; set; }
}

/// <summary>
/// Represents statistics about the Strange Loop execution
/// </summary>
public class StrangeLoopStatistics
{
    public int TotalIterations { get; set; }
    public bool IsRunning { get; set; }
    public DateTime LastIterationTime { get; set; }
    public TimeSpan AverageIterationDuration { get; set; }
    public Dictionary<string, int> StageCompletionCounts { get; } = new();
    public Dictionary<string, double> StageAverageTimes { get; } = new();
    public double SuccessRate { get; set; }
    public Collection<string> RecentAchievements { get; } = new();
    public double SystemImprovementScore { get; set; }
    public Dictionary<string, object> ImprovementMetrics { get; } = new();
}

/// <summary>
/// Tracks self-improvement metrics and progress
/// </summary>
public class SelfImprovementTracker
{
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> ImprovementMetrics { get; } = new();
    
    public Dictionary<string, double> ImprovementScores { get; } = new();
    public Collection<string> ImprovementAreas { get; } = new();
    public Dictionary<string, List<double>> TrendData { get; } = new();
    public DateTime LastAssessment { get; set; } = DateTime.UtcNow;

    public void RecordIteration(LoopIteration iteration)
    {
        if (TrendData.TryGetValue("success_rate", out var successTrend))
        {
            successTrend.Add(iteration.Success ? 1.0 : 0.0);
        }
        else
        {
            TrendData["success_rate"] = [iteration.Success ? 1.0 : 0.0];
        }

        LastUpdated = DateTime.UtcNow;
    }

    public Dictionary<string, object> GetMetrics()
    {
        var metrics = new Dictionary<string, object>();
        
        foreach (var kvp in ImprovementScores)
        {
            metrics[kvp.Key] = kvp.Value;
        }

        return metrics;
    }

    public Dictionary<string, object> GetTrendAnalysis()
    {
        var analysis = new Dictionary<string, object>();
        
        foreach (var kvp in TrendData)
        {
            if (kvp.Value.Count > 0)
            {
                analysis[$"{kvp.Key}_trend"] = kvp.Value.Average();
            }
        }

        return analysis;
    }
}

/// <summary>
/// Represents a complete iteration through the Strange Loop pattern
/// </summary>
public class LoopIteration
{
    public string Id { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;
    
    public MonitoringData? MonitoringData { get; set; }
    public AnalysisResults? AnalysisResults { get; set; }
    public OptimizationPlan? OptimizationPlan { get; set; }
    public ReflectionInsights? ReflectionInsights { get; set; }
    public KnowledgeUpdates? KnowledgeUpdates { get; set; }
    
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
}

/// <summary>
/// Tracks iteration history
/// </summary>
public class IterationHistory
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool Success { get; set; }
    public string Details { get; set; } = string.Empty;
}

/// <summary>
/// Represents reflection metrics for self-analysis
/// </summary>
public class ReflectionMetric
{
    public string Name { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public Collection<double> HistoricalValues { get; } = new();
    public DateTime LastUpdated { get; set; }
    public string Trend { get; set; } = string.Empty;
    public double AverageValue => HistoricalValues.Count > 0 ? HistoricalValues.Average() : 0.0;
    public double TrendSlope { get; set; }
}

/// <summary>
/// Represents an event in the reflection system
/// </summary>
public class ReflectionEvent
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public object Data { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; } = new();
}

/// <summary>
/// Comprehensive reflection analysis results
/// </summary>
public class ReflectionResults
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public int CycleNumber { get; set; }
    
    public SystemReflectionAnalysis? SystemReflection { get; set; }
    public PerformanceReflectionAnalysis? PerformanceReflection { get; set; }
    public BehavioralReflectionAnalysis? BehavioralReflection { get; set; }
    public EffectivenessReflectionAnalysis? EffectivenessReflection { get; set; }
    public AdaptationReflectionAnalysis? AdaptationReflection { get; set; }
    
    public Collection<ReflectionInsight> Insights { get; } = new();
    public Collection<ReflectionRecommendation> Recommendations { get; } = new();
    public Collection<RiskAssessment> RiskAssessments { get; } = new();
}

/// <summary>
/// System reflection data and analysis
/// </summary>
public class SystemReflectionData
{
    public TimeSpan Uptime { get; set; }
    public float CpuUsage { get; set; }
    public float MemoryUsage { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
}

public class SystemReflectionAnalysis
{
    public double HealthScore { get; set; }
    public SystemReflectionData? SystemMetrics { get; set; }
    public string StabilityTrend { get; set; } = string.Empty;
    public double ResourceEfficiency { get; set; }
    public double PerformanceConsistency { get; set; }
}

/// <summary>
/// Performance reflection data and analysis
/// </summary>
public class PerformanceReflectionData
{
    public Collection<double> ResponseTimes { get; } = new();
    public Dictionary<string, double> ThroughputMetrics { get; } = new();
    public Dictionary<string, object> ErrorRates { get; } = new();
    public Collection<string> BottleneckAnalysis { get; } = new();
}

public class PerformanceReflectionAnalysis
{
    public double OverallScore { get; set; }
    public PerformanceReflectionData? PerformanceData { get; set; }
    public Dictionary<string, object> PerformanceTrends { get; } = new();
    public Collection<string> OptimizationOpportunities { get; } = new();
}

/// <summary>
/// Behavioral reflection data and analysis
/// </summary>
public class BehavioralReflectionData
{
    public Dictionary<string, object> DecisionPatterns { get; } = new();
    public Dictionary<string, object> AdaptationBehaviors { get; } = new();
    public Dictionary<string, object> LearningPatterns { get; } = new();
    public Dictionary<string, object> InteractionPatterns { get; } = new();
}

public class BehavioralReflectionAnalysis
{
    public double BehavioralScore { get; set; }
    public BehavioralReflectionData? BehavioralData { get; set; }
    public Dictionary<string, object> BehavioralTrends { get; } = new();
    public Collection<string> AdaptationRecommendations { get; } = new();
}

/// <summary>
/// Effectiveness reflection data and analysis
/// </summary>
public class EffectivenessReflectionData
{
    public double GoalAchievementRate { get; set; }
    public Dictionary<string, double> ImprovementMeasures { get; } = new();
    public Dictionary<string, double> SuccessMetrics { get; } = new();
    public Dictionary<string, object> ImpactAnalysis { get; } = new();
}

public class EffectivenessReflectionAnalysis
{
    public double EffectivenessScore { get; set; }
    public EffectivenessReflectionData? EffectivenessData { get; set; }
    public Collection<string> ImprovementAreas { get; } = new();
    public Collection<string> SuccessFactors { get; } = new();
}

/// <summary>
/// Adaptation reflection data and analysis
/// </summary>
public class AdaptationReflectionData
{
    public double AdaptationSpeed { get; set; }
    public double LearningEffectiveness { get; set; }
    public double ChangeResponsiveness { get; set; }
    public Dictionary<string, object> EvolutionPatterns { get; } = new();
}

public class AdaptationReflectionAnalysis
{
    public double AdaptationScore { get; set; }
    public AdaptationReflectionData? AdaptationData { get; set; }
    public Dictionary<string, double> AdaptationCapabilities { get; } = new();
    public Collection<string> EvolutionRecommendations { get; } = new();
}

/// <summary>
/// Individual reflection insight
/// </summary>
public class ReflectionInsight
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; } = new();
}

/// <summary>
/// Reflection-based recommendation
/// </summary>
public class ReflectionRecommendation
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public double ImpactScore { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Collection<string> ActionItems { get; } = new();
}

/// <summary>
/// Risk assessment from reflection analysis
/// </summary>
public class RiskAssessment
{
    public string Id { get; set; } = string.Empty;
    public string RiskType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public double Probability { get; set; }
    public double Impact { get; set; }
    public Collection<string> MitigationStrategies { get; } = new();
    public DateTime IdentifiedAt { get; set; }
}
