using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using StrangeLoop.Core.Models;
using StrangeLoop.Core.Messages;
using StrangeLoop.Infrastructure.ServiceBus;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StrangeLoop.AnalysisAgent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly Kernel _kernel;
    private readonly IServiceBus _serviceBus;
    private readonly Dictionary<string, List<double>> _metricTrends = new();
    private readonly Dictionary<string, DateTime> _lastAnalysis = new();
    private readonly Queue<MonitoringData> _dataHistory = new();
    private const int MaxHistorySize = 10;    public Worker(ILogger<Worker> logger, Kernel kernel, IServiceBus serviceBus)
    {
        _logger = logger;
        _kernel = kernel;
        _serviceBus = serviceBus;
    }    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Strange Loop Analysis Agent started at: {Time}", DateTimeOffset.Now);

        // Subscribe to monitoring results from the monitoring agent
        await _serviceBus.SubscribeAsync<MonitoringResult>("monitoring-results", HandleMonitoringResultAsync, stoppingToken);
        
        _logger.LogInformation("Analysis Agent subscribed to monitoring results");

        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMonitoringResultAsync(MonitoringResult monitoringResult)
    {
        try
        {
            _logger.LogInformation("Received monitoring result with execution ID: {ExecutionId}", monitoringResult.ExecutionId);

            // Convert TelemetryData to MonitoringData for analysis
            var monitoringData = ConvertTelemetryToMonitoringData(monitoringResult.TelemetryData);
            
            _dataHistory.Enqueue(monitoringData);
            
            // Maintain history size
            if (_dataHistory.Count > MaxHistorySize)
            {
                _dataHistory.Dequeue();
            }

            var analysisResults = await PerformComprehensiveAnalysisAsync(monitoringData, CancellationToken.None);
            await ProcessAnalysisResultsAsync(analysisResults, CancellationToken.None);

            // Publish analysis results to ServiceBus for optimization agent consumption
            var analysisRequest = new AnalysisRequest
            {
                ExecutionId = monitoringResult.ExecutionId,
                TelemetryData = monitoringResult.TelemetryData,
                RequestTime = DateTime.UtcNow
            };

            var analysisResultMessage = new AnalysisResult
            {
                Insights = new AnalysisInsights
                {
                    Trends = string.Join(", ", analysisResults.IdentifiedPatterns.Take(3)),
                    Anomalies = string.Join(", ", analysisResults.ImprovementOpportunities.Take(3)),
                    Recommendations = analysisResults.Recommendations,
                    RiskScore = analysisResults.ConfidenceScores.Values.DefaultIfEmpty(0.5).Average()
                },
                ImprovementOpportunities = analysisResults.ImprovementOpportunities,
                Timestamp = DateTime.UtcNow,
                ConfidenceScore = analysisResults.ConfidenceScores.Values.DefaultIfEmpty(0.5).Average()
            };

            await _serviceBus.PublishAsync("analysis-results", analysisResultMessage, CancellationToken.None);
            
            _logger.LogInformation("Published analysis result for execution ID: {ExecutionId} - Found {PatternCount} patterns, {OpportunityCount} opportunities", 
                monitoringResult.ExecutionId, analysisResults.IdentifiedPatterns.Count, analysisResults.ImprovementOpportunities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing monitoring result with execution ID: {ExecutionId}", monitoringResult.ExecutionId);
        }
    }

    private MonitoringData ConvertTelemetryToMonitoringData(TelemetryData telemetryData)
    {
        var monitoringData = new MonitoringData
        {
            Timestamp = telemetryData.CollectionTime,
            SystemId = Environment.MachineName
        };

        // Copy system metrics
        foreach (var metric in telemetryData.SystemMetrics)
        {
            monitoringData.SystemMetrics[metric.Key] = metric.Value;
        }

        // Add basic performance metrics
        monitoringData.PerformanceMetrics["response_time_ms"] = Random.Shared.NextDouble() * 100 + 10;
        monitoringData.PerformanceMetrics["throughput_ops_sec"] = Random.Shared.NextDouble() * 200 + 50;

        // Add behavior metrics
        monitoringData.BehaviorMetrics["error_rate_percent"] = Random.Shared.NextDouble() * 5;
        monitoringData.BehaviorMetrics["user_interactions"] = telemetryData.UserInteractions;

        // Copy anomalies
        foreach (var anomaly in telemetryData.Anomalies)
        {
            monitoringData.Anomalies.Add(anomaly);
        }

        monitoringData.RawData = $"Converted from TelemetryData. Performance: {telemetryData.SystemPerformance}";

        return monitoringData;
    }private Task<MonitoringData> GenerateMockMonitoringDataAsync()
    {
        // This simulates monitoring data - in real implementation, this would come from the Monitoring Agent
        var random = Random.Shared;
        var data = new MonitoringData
        {
            Timestamp = DateTime.UtcNow,
            SystemId = Environment.MachineName
        };

        // Add some realistic metrics with trends
        data.SystemMetrics["cpu_usage_percent"] = Math.Max(0, Math.Min(100, 
            (_metricTrends.TryGetValue("cpu", out var cpuTrend) ? cpuTrend.LastOrDefault() : 30) + 
            random.NextDouble() * 10 - 5));
        
        data.SystemMetrics["memory_usage_mb"] = Math.Max(100, 
            (_metricTrends.TryGetValue("memory", out var memTrend) ? memTrend.LastOrDefault() : 512) + 
            random.NextDouble() * 50 - 25);

        data.PerformanceMetrics["response_time_ms"] = Math.Max(1, 
            (_metricTrends.TryGetValue("response", out var respTrend) ? respTrend.LastOrDefault() : 50) + 
            random.NextDouble() * 20 - 10);

        data.PerformanceMetrics["throughput_ops_sec"] = Math.Max(1, 
            (_metricTrends.TryGetValue("throughput", out var throughputTrend) ? throughputTrend.LastOrDefault() : 100) + 
            random.NextDouble() * 20 - 10);

        data.BehaviorMetrics["error_rate_percent"] = Math.Max(0, Math.Min(10, 
            (_metricTrends.TryGetValue("errors", out var errorTrend) ? errorTrend.LastOrDefault() : 1) + 
            random.NextDouble() * 2 - 1));        // Update trends
        UpdateMetricTrends(data);

        return Task.FromResult(data);
    }

    private void UpdateMetricTrends(MonitoringData data)
    {
        var metrics = new Dictionary<string, double>
        {
            ["cpu"] = (double)data.SystemMetrics["cpu_usage_percent"],
            ["memory"] = (double)data.SystemMetrics["memory_usage_mb"],
            ["response"] = (double)data.PerformanceMetrics["response_time_ms"],
            ["throughput"] = (double)data.PerformanceMetrics["throughput_ops_sec"],
            ["errors"] = (double)data.BehaviorMetrics["error_rate_percent"]
        };

        foreach (var kvp in metrics)
        {
            if (!_metricTrends.TryGetValue(kvp.Key, out var trend))
            {
                trend = new List<double>();
                _metricTrends[kvp.Key] = trend;
            }

            trend.Add(kvp.Value);
            if (trend.Count > 20) // Keep last 20 data points
            {
                trend.RemoveAt(0);
            }
        }
    }

    private async Task<AnalysisResults> PerformComprehensiveAnalysisAsync(MonitoringData data, CancellationToken cancellationToken)
    {
        var results = new AnalysisResults
        {
            Timestamp = DateTime.UtcNow,
            SourceData = data
        };

        // Perform multiple types of analysis
        await PerformPatternAnalysisAsync(results, cancellationToken);
        await PerformTrendAnalysisAsync(results, cancellationToken);
        await PerformAnomalyAnalysisAsync(results, cancellationToken);
        await PerformPerformanceAnalysisAsync(results, cancellationToken);
        await PerformSemanticAnalysisAsync(results, cancellationToken);
        await GenerateRecommendationsAsync(results, cancellationToken);

        return results;
    }

    private async Task PerformPatternAnalysisAsync(AnalysisResults results, CancellationToken cancellationToken)
    {
        try
        {
            var patterns = new List<string>();

            // Analyze historical data for patterns
            if (_dataHistory.Count >= 3)
            {
                var historicalData = _dataHistory.ToArray();
                
                // CPU usage patterns
                var cpuValues = historicalData.Select(d => (double)d.SystemMetrics.GetValueOrDefault("cpu_usage_percent", 0.0)).ToArray();
                if (cpuValues.Length > 2)
                {
                    var isIncreasing = cpuValues.Skip(1).Zip(cpuValues, (curr, prev) => curr > prev).All(x => x);
                    var isDecreasing = cpuValues.Skip(1).Zip(cpuValues, (curr, prev) => curr < prev).All(x => x);
                    
                    if (isIncreasing) patterns.Add("CPU_USAGE_INCREASING_TREND");
                    if (isDecreasing) patterns.Add("CPU_USAGE_DECREASING_TREND");
                    if (cpuValues.All(x => x > 80)) patterns.Add("SUSTAINED_HIGH_CPU");
                    if (cpuValues.All(x => x < 20)) patterns.Add("SUSTAINED_LOW_CPU");
                }

                // Memory patterns
                var memValues = historicalData.Select(d => (double)d.SystemMetrics.GetValueOrDefault("memory_usage_mb", 0.0)).ToArray();
                if (memValues.Length > 2)
                {
                    var memGrowth = (memValues.Last() - memValues.First()) / memValues.First() * 100;
                    if (memGrowth > 20) patterns.Add("MEMORY_GROWTH_PATTERN");
                    if (memGrowth < -20) patterns.Add("MEMORY_REDUCTION_PATTERN");
                }

                // Error rate patterns
                var errorValues = historicalData.Select(d => (double)d.BehaviorMetrics.GetValueOrDefault("error_rate_percent", 0.0)).ToArray();
                if (errorValues.Any(x => x > 5)) patterns.Add("HIGH_ERROR_RATE_DETECTED");
                if (errorValues.All(x => x < 0.1)) patterns.Add("STABLE_LOW_ERROR_RATE");
            }

            // Time-based patterns
            var hour = DateTime.UtcNow.Hour;
            if (hour >= 9 && hour <= 17) patterns.Add("BUSINESS_HOURS_PATTERN");
            if (hour >= 22 || hour <= 6) patterns.Add("OFF_HOURS_PATTERN");
            
            if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Saturday || DateTime.UtcNow.DayOfWeek == DayOfWeek.Sunday)
                patterns.Add("WEEKEND_PATTERN");

            foreach (var pattern in patterns)
            {
                results.IdentifiedPatterns.Add(pattern);
            }

            _logger.LogDebug("Pattern analysis completed - {Count} patterns identified", patterns.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform pattern analysis");
        }

        await Task.CompletedTask;
    }

    private async Task PerformTrendAnalysisAsync(AnalysisResults results, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var metricTrend in _metricTrends)
            {
                var trend = metricTrend.Value;
                if (trend.Count < 3) continue;

                var metricName = metricTrend.Key;
                var recentValues = trend.TakeLast(5).ToArray();
                var avg = recentValues.Average();
                var variance = recentValues.Select(x => Math.Pow(x - avg, 2)).Average();
                var stdDev = Math.Sqrt(variance);                // Trend direction
                var slope = CalculateSlope(recentValues);
                if (Math.Abs(slope) > 0.1)
                {
                    var direction = slope > 0 ? "INCREASING" : "DECREASING";
                    results.Insights.Add($"{metricName.ToUpperInvariant()}_TREND_{direction}: slope={slope:F3}");
                }

                // Volatility analysis
                var coefficientOfVariation = avg != 0 ? stdDev / avg : 0;
                if (coefficientOfVariation > 0.3)
                {
                    results.Insights.Add($"{metricName.ToUpperInvariant()}_HIGH_VOLATILITY: cv={coefficientOfVariation:F3}");
                }

                // Store confidence scores
                results.ConfidenceScores[$"{metricName}_trend_confidence"] = Math.Max(0, 1 - coefficientOfVariation);
            }

            _logger.LogDebug("Trend analysis completed for {Count} metrics", _metricTrends.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform trend analysis");
        }

        await Task.CompletedTask;
    }

    private async Task PerformAnomalyAnalysisAsync(AnalysisResults results, CancellationToken cancellationToken)
    {
        try
        {
            var data = results.SourceData;
            var anomalies = new List<string>();

            // Statistical anomaly detection using Z-score
            foreach (var metricTrend in _metricTrends)
            {
                if (metricTrend.Value.Count < 5) continue;

                var trend = metricTrend.Value;
                var mean = trend.Average();
                var stdDev = Math.Sqrt(trend.Select(x => Math.Pow(x - mean, 2)).Average());
                var currentValue = trend.Last();

                if (stdDev > 0)
                {
                    var zScore = Math.Abs((currentValue - mean) / stdDev);                    if (zScore > 2.5) // Anomaly threshold
                    {
                        anomalies.Add($"STATISTICAL_ANOMALY_{metricTrend.Key.ToUpperInvariant()}: z-score={zScore:F2}");
                    }
                }
            }

            // Rule-based anomaly detection
            if (data.SystemMetrics.TryGetValue("cpu_usage_percent", out var cpuObj) && 
                cpuObj is double cpu && cpu > 95)
            {
                anomalies.Add("CRITICAL_CPU_USAGE");
            }

            if (data.BehaviorMetrics.TryGetValue("error_rate_percent", out var errorObj) && 
                errorObj is double errorRate && errorRate > 10)
            {
                anomalies.Add("CRITICAL_ERROR_RATE");
            }

            if (data.PerformanceMetrics.TryGetValue("response_time_ms", out var responseObj) && 
                responseObj is double responseTime && responseTime > 1000)
            {
                anomalies.Add("SLOW_RESPONSE_TIME");
            }

            foreach (var anomaly in anomalies)
            {
                results.Insights.Add($"ANOMALY_DETECTED: {anomaly}");
            }

            _logger.LogDebug("Anomaly analysis completed - {Count} anomalies detected", anomalies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform anomaly analysis");
        }

        await Task.CompletedTask;
    }

    private async Task PerformPerformanceAnalysisAsync(AnalysisResults results, CancellationToken cancellationToken)
    {
        try
        {
            var data = results.SourceData;
            var performanceInsights = new List<string>();

            // Performance bottleneck analysis
            if (data.SystemMetrics.TryGetValue("cpu_usage_percent", out var cpuObj) && cpuObj is double cpu)
            {
                if (cpu > 80)
                    performanceInsights.Add($"CPU_BOTTLENECK_DETECTED: {cpu:F1}% usage");
                else if (cpu < 10)
                    performanceInsights.Add($"CPU_UNDERUTILIZED: {cpu:F1}% usage");
            }

            if (data.PerformanceMetrics.TryGetValue("response_time_ms", out var responseObj) && responseObj is double responseTime)
            {
                if (responseTime > 500)
                    performanceInsights.Add($"SLOW_RESPONSE_TIME: {responseTime:F1}ms");
                else if (responseTime < 10)
                    performanceInsights.Add($"EXCELLENT_RESPONSE_TIME: {responseTime:F1}ms");
            }

            if (data.PerformanceMetrics.TryGetValue("throughput_ops_sec", out var throughputObj) && throughputObj is double throughput)
            {
                if (_metricTrends.TryGetValue("throughput", out var throughputTrend) && throughputTrend.Count > 1)
                {
                    var avgThroughput = throughputTrend.Average();
                    if (throughput < avgThroughput * 0.7)
                        performanceInsights.Add($"THROUGHPUT_DEGRADATION: {throughput:F1} ops/sec (avg: {avgThroughput:F1})");
                }
            }

            // Performance score calculation
            var performanceScore = CalculatePerformanceScore(data);
            results.ConfidenceScores["overall_performance"] = performanceScore;
            
            if (performanceScore < 0.5)
                performanceInsights.Add($"POOR_OVERALL_PERFORMANCE: score={performanceScore:F2}");
            else if (performanceScore > 0.9)
                performanceInsights.Add($"EXCELLENT_OVERALL_PERFORMANCE: score={performanceScore:F2}");

            foreach (var insight in performanceInsights)
            {
                results.Insights.Add(insight);
            }

            _logger.LogDebug("Performance analysis completed - score: {Score:F2}", performanceScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform performance analysis");
        }

        await Task.CompletedTask;
    }

    private async Task PerformSemanticAnalysisAsync(AnalysisResults results, CancellationToken cancellationToken)
    {
        try
        {
            var analysisPrompt = $@"
Analyze the following system monitoring data and provide intelligent insights:

System Metrics: {JsonSerializer.Serialize(results.SourceData.SystemMetrics)}
Performance Metrics: {JsonSerializer.Serialize(results.SourceData.PerformanceMetrics)}
Behavior Metrics: {JsonSerializer.Serialize(results.SourceData.BehaviorMetrics)}

Identified Patterns: {string.Join(", ", results.IdentifiedPatterns)}
Current Insights: {string.Join(", ", results.Insights)}

Please provide:
1. Strategic insights about system behavior
2. Potential root causes for any issues
3. Correlation analysis between metrics
4. Risk assessment for system stability
5. Forward-looking predictions

Be concise but comprehensive.";

            var semanticResult = await _kernel.InvokePromptAsync(analysisPrompt, cancellationToken: cancellationToken);
            results.RawAnalysis = semanticResult.ToString();

            // Extract structured insights from the AI response
            var aiInsights = ExtractInsightsFromAIResponse(semanticResult.ToString());
            foreach (var insight in aiInsights)
            {
                results.Insights.Add($"AI_INSIGHT: {insight}");
            }

            // Risk assessment from AI
            var riskLevel = ExtractRiskAssessment(semanticResult.ToString());
            results.RiskAssessment = riskLevel;

            _logger.LogDebug("Semantic analysis completed using AI - Risk Level: {RiskLevel}", riskLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform semantic analysis");
            results.RiskAssessment = "UNKNOWN";
        }
    }

    private async Task GenerateRecommendationsAsync(AnalysisResults results, CancellationToken cancellationToken)
    {
        try
        {
            var recommendations = new List<string>();            // Generate recommendations based on insights
            foreach (var insight in results.Insights)
            {
                if (insight.Contains("CPU_BOTTLENECK", StringComparison.OrdinalIgnoreCase))
                    recommendations.Add("SCALE_CPU_RESOURCES");
                
                if (insight.Contains("MEMORY_GROWTH", StringComparison.OrdinalIgnoreCase))
                    recommendations.Add("INVESTIGATE_MEMORY_LEAKS");
                
                if (insight.Contains("HIGH_ERROR_RATE", StringComparison.OrdinalIgnoreCase))
                    recommendations.Add("INVESTIGATE_ERROR_SOURCES");
                
                if (insight.Contains("SLOW_RESPONSE", StringComparison.OrdinalIgnoreCase))
                    recommendations.Add("OPTIMIZE_RESPONSE_TIME");
                
                if (insight.Contains("HIGH_VOLATILITY", StringComparison.OrdinalIgnoreCase))
                    recommendations.Add("STABILIZE_SYSTEM_METRICS");
            }            // Pattern-based recommendations
            foreach (var pattern in results.IdentifiedPatterns)
            {
                if (pattern.Contains("BUSINESS_HOURS", StringComparison.OrdinalIgnoreCase))
                    recommendations.Add("CONSIDER_AUTO_SCALING_FOR_BUSINESS_HOURS");
                
                if (pattern.Contains("WEEKEND", StringComparison.OrdinalIgnoreCase))
                    recommendations.Add("IMPLEMENT_WEEKEND_MAINTENANCE_WINDOW");
                
                if (pattern.Contains("SUSTAINED_HIGH", StringComparison.OrdinalIgnoreCase))
                    recommendations.Add("IMPLEMENT_LOAD_BALANCING");
            }

            // Remove duplicates and add to results
            var uniqueRecommendations = recommendations.Distinct().ToList();
            foreach (var recommendation in uniqueRecommendations)
            {
                results.Recommendations.Add(recommendation);
                results.ImprovementOpportunities.Add($"OPPORTUNITY: {recommendation}");
            }

            _logger.LogDebug("Generated {Count} recommendations", uniqueRecommendations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate recommendations");
        }

        await Task.CompletedTask;
    }

    private async Task ProcessAnalysisResultsAsync(AnalysisResults results, CancellationToken cancellationToken)
    {
        try
        {            // Log key findings
            if (results.IdentifiedPatterns.Count > 0)
            {
                _logger.LogInformation("Key patterns identified: {Patterns}", 
                    string.Join(", ", results.IdentifiedPatterns.Take(3)));
            }

            if (results.Recommendations.Count > 0)
            {
                _logger.LogInformation("Top recommendations: {Recommendations}", 
                    string.Join(", ", results.Recommendations.Take(3)));
            }

            if (results.RiskAssessment != "LOW")
            {
                _logger.LogWarning("Risk assessment: {RiskLevel}", results.RiskAssessment);
            }

            // Update last analysis time
            _lastAnalysis["comprehensive_analysis"] = DateTime.UtcNow;

            // In a real implementation, this would publish results to a message bus
            // for the Optimization Agent to consume
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process analysis results");
        }

        await Task.CompletedTask;
    }

    // Helper methods
    private double CalculateSlope(double[] values)
    {
        if (values.Length < 2) return 0;
        
        var n = values.Length;
        var sumX = Enumerable.Range(0, n).Sum();
        var sumY = values.Sum();
        var sumXY = values.Select((y, x) => x * y).Sum();
        var sumX2 = Enumerable.Range(0, n).Select(x => x * x).Sum();
        
        return (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
    }

    private double CalculatePerformanceScore(MonitoringData data)
    {
        var scores = new List<double>();

        // CPU score (inverted - lower usage is better for availability)
        if (data.SystemMetrics.TryGetValue("cpu_usage_percent", out var cpuObj) && cpuObj is double cpu)
        {
            scores.Add(Math.Max(0, (100 - cpu) / 100));
        }

        // Response time score (inverted - lower is better)
        if (data.PerformanceMetrics.TryGetValue("response_time_ms", out var responseObj) && responseObj is double responseTime)
        {
            scores.Add(Math.Max(0, Math.Min(1, (1000 - responseTime) / 1000)));
        }

        // Error rate score (inverted - lower is better)
        if (data.BehaviorMetrics.TryGetValue("error_rate_percent", out var errorObj) && errorObj is double errorRate)
        {
            scores.Add(Math.Max(0, (10 - errorRate) / 10));
        }

        return scores.Count > 0 ? scores.Average() : 0.5;
    }

    private List<string> ExtractInsightsFromAIResponse(string aiResponse)
    {
        var insights = new List<string>();
        
        try
        {
            // Simple extraction using regex patterns
            var patterns = new[]
            {
                @"insight[:\s]+(.+?)(?:\.|$)",
                @"correlation[:\s]+(.+?)(?:\.|$)",
                @"prediction[:\s]+(.+?)(?:\.|$)",
                @"risk[:\s]+(.+?)(?:\.|$)"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(aiResponse, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        insights.Add(match.Groups[1].Value.Trim());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract insights from AI response");
        }

        return insights.Take(5).ToList(); // Limit to 5 insights
    }

    private string ExtractRiskAssessment(string aiResponse)
    {
        try
        {
            var riskKeywords = new Dictionary<string, string>
            {
                ["high risk"] = "HIGH",
                ["critical"] = "CRITICAL",
                ["severe"] = "HIGH",
                ["moderate"] = "MEDIUM",
                ["low risk"] = "LOW",
                ["stable"] = "LOW",
                ["normal"] = "LOW"
            };

            foreach (var kvp in riskKeywords)
            {
                if (aiResponse.ToUpperInvariant().Contains(kvp.Key.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract risk assessment");
        }

        return "MEDIUM"; // Default risk level
    }
}
