using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using StrangeLoop.Core.Models;
using StrangeLoop.Core.Messages;
using StrangeLoop.Infrastructure.ServiceBus;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace StrangeLoop.MonitoringAgent;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly Kernel _kernel;
    private readonly IServiceBus _serviceBus;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;
    private readonly DirectoryInfo _scanDirectory;
    private readonly Dictionary<string, object> _lastKnownState = new();    public Worker(ILogger<Worker> logger, Kernel kernel, IServiceBus serviceBus)
    {
        _logger = logger;
        _kernel = kernel;
        _serviceBus = serviceBus;
        _scanDirectory = new DirectoryInfo(Environment.CurrentDirectory);
          try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize performance counters. System metrics will be limited.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Strange Loop Monitoring Agent started at: {Time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var monitoringData = await CollectComprehensiveDataAsync(stoppingToken);
                await ProcessMonitoringDataAsync(monitoringData, stoppingToken);
                await DetectAnomaliesAsync(monitoringData, stoppingToken);
                
                _logger.LogInformation("Monitoring cycle completed at: {Time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during monitoring cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Monitor every 30 seconds
        }
    }

    private async Task<MonitoringData> CollectComprehensiveDataAsync(CancellationToken cancellationToken)
    {
        var monitoringData = new MonitoringData
        {
            Timestamp = DateTime.UtcNow,
            SystemId = Environment.MachineName
        };

        // Collect system metrics
        await CollectSystemMetricsAsync(monitoringData);
        
        // Collect performance metrics
        await CollectPerformanceMetricsAsync(monitoringData);
        
        // Collect environment state
        await CollectEnvironmentStateAsync(monitoringData);
        
        // Collect directory scanning data (addressing original issue)
        await CollectDirectoryScanDataAsync(monitoringData, cancellationToken);
        
        // Collect application-specific metrics
        await CollectApplicationMetricsAsync(monitoringData);

        return monitoringData;
    }

    private async Task CollectSystemMetricsAsync(MonitoringData data)
    {
        try
        {
            data.SystemMetrics["machine_name"] = Environment.MachineName;
            data.SystemMetrics["os_version"] = Environment.OSVersion.ToString();
            data.SystemMetrics["processor_count"] = Environment.ProcessorCount;
            data.SystemMetrics["working_set"] = Environment.WorkingSet;
            data.SystemMetrics["system_directory"] = Environment.SystemDirectory;
            data.SystemMetrics["current_directory"] = Environment.CurrentDirectory;
            data.SystemMetrics["tick_count"] = Environment.TickCount64;
              if (_cpuCounter != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                data.SystemMetrics["cpu_usage_percent"] = Math.Round(_cpuCounter.NextValue(), 2);
            }
            
            if (_memoryCounter != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                data.SystemMetrics["available_memory_mb"] = Math.Round(_memoryCounter.NextValue(), 2);
            }

            var process = Process.GetCurrentProcess();
            data.SystemMetrics["process_memory_mb"] = Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 2);
            data.SystemMetrics["process_cpu_time"] = process.TotalProcessorTime.TotalMilliseconds;
            data.SystemMetrics["thread_count"] = process.Threads.Count;
            
            _logger.LogDebug("System metrics collected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect system metrics");
            data.Events.Add($"Error collecting system metrics: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task CollectPerformanceMetricsAsync(MonitoringData data)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Simulate some performance measurements
            var testOperations = new[]
            {
                () => Task.Delay(1),
                () => Task.FromResult(Guid.NewGuid().ToString()),
                () => Task.FromResult(DateTime.UtcNow.ToString())
            };

            var operationTimes = new List<double>();
            foreach (var operation in testOperations)
            {
                var opStopwatch = Stopwatch.StartNew();
                await operation();
                opStopwatch.Stop();
                operationTimes.Add(opStopwatch.Elapsed.TotalMilliseconds);
            }

            data.PerformanceMetrics["operation_avg_ms"] = operationTimes.Average();
            data.PerformanceMetrics["operation_max_ms"] = operationTimes.Max();
            data.PerformanceMetrics["operation_min_ms"] = operationTimes.Min();
            
            stopwatch.Stop();
            data.PerformanceMetrics["collection_time_ms"] = stopwatch.Elapsed.TotalMilliseconds;
            data.PerformanceMetrics["timestamp"] = DateTime.UtcNow.ToString("O");
            
            // Network latency simulation (could be replaced with actual network checks)
            data.PerformanceMetrics["network_latency_ms"] = Random.Shared.NextDouble() * 10 + 5;
            
            _logger.LogDebug("Performance metrics collected in {Time}ms", stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect performance metrics");
            data.Events.Add($"Error collecting performance metrics: {ex.Message}");
        }
    }

    private async Task CollectEnvironmentStateAsync(MonitoringData data)
    {
        try
        {
            data.EnvironmentState["utc_time"] = DateTime.UtcNow.ToString("O");
            data.EnvironmentState["local_time"] = DateTime.Now.ToString("O");
            data.EnvironmentState["timezone"] = TimeZoneInfo.Local.Id;
            data.EnvironmentState["day_of_week"] = DateTime.UtcNow.DayOfWeek.ToString();
            data.EnvironmentState["is_weekend"] = DateTime.UtcNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            
            // Environment variables (filtered for security)
            var safeEnvVars = Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .Where(kvp => !kvp.Key.ToString()!.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase) &&
                             !kvp.Key.ToString()!.Contains("SECRET", StringComparison.OrdinalIgnoreCase) &&
                             !kvp.Key.ToString()!.Contains("TOKEN", StringComparison.OrdinalIgnoreCase))
                .Take(10) // Limit to avoid excessive data
                .ToDictionary(kvp => kvp.Key.ToString()!, kvp => kvp.Value?.ToString() ?? "");
                
            data.EnvironmentState["environment_variables"] = safeEnvVars;
            
            _logger.LogDebug("Environment state collected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect environment state");
            data.Events.Add($"Error collecting environment state: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task CollectDirectoryScanDataAsync(MonitoringData data, CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Scan current directory and subdirectories (addressing original directory scanning issue)
            var scanResults = await ScanDirectoryAsync(_scanDirectory, cancellationToken);
            
            data.BehaviorMetrics["directory_scan_results"] = scanResults;
            data.BehaviorMetrics["files_scanned"] = scanResults.FileCount;
            data.BehaviorMetrics["directories_scanned"] = scanResults.DirectoryCount;
            data.BehaviorMetrics["total_size_bytes"] = scanResults.TotalSizeBytes;
            data.BehaviorMetrics["scan_duration_ms"] = stopwatch.Elapsed.TotalMilliseconds;
              // Detect changes from last scan
            var changes = DetectDirectoryChanges(scanResults);
            if (changes.Count > 0)
            {
                data.Events.Add($"Directory changes detected: {string.Join(", ", changes)}");
                data.Anomalies.Add($"File system changes: {changes.Count} items modified");
            }
            
            stopwatch.Stop();
            _logger.LogDebug("Directory scan completed in {Time}ms - {FileCount} files, {DirCount} directories", 
                stopwatch.Elapsed.TotalMilliseconds, scanResults.FileCount, scanResults.DirectoryCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan directory structure");
            data.Events.Add($"Error during directory scan: {ex.Message}");
        }
    }    private async Task<DirectoryScanResult> ScanDirectoryAsync(DirectoryInfo directory, CancellationToken cancellationToken)
    {
        var result = new DirectoryScanResult();
        var queue = new Queue<DirectoryInfo>();
        queue.Enqueue(directory);

        await Task.Yield(); // Make method truly async
        
        while (queue.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            var currentDir = queue.Dequeue();
            
            try
            {
                result.DirectoryCount++;
                
                // Scan files in current directory
                foreach (var file in currentDir.GetFiles())
                {
                    result.FileCount++;
                    result.TotalSizeBytes += file.Length;
                    result.FileDetails[file.FullName] = new FileDetails
                    {
                        Size = file.Length,
                        LastModified = file.LastWriteTime,
                        Extension = file.Extension
                    };
                }
                
                // Add subdirectories to queue (limit depth to avoid excessive scanning)
                if (result.DirectoryCount < 100) // Limit to prevent excessive scanning
                {
                    foreach (var subDir in currentDir.GetDirectories())
                    {
                        queue.Enqueue(subDir);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we can't access
                result.SkippedDirectories++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error scanning directory {Directory}", currentDir.FullName);
                result.Errors.Add($"Error in {currentDir.Name}: {ex.Message}");
            }
        }

        return result;
    }

    private List<string> DetectDirectoryChanges(DirectoryScanResult currentScan)
    {
        var changes = new List<string>();
        
        if (_lastKnownState.TryGetValue("last_file_count", out var lastFileCountObj) &&
            lastFileCountObj is int lastFileCount)
        {
            if (currentScan.FileCount != lastFileCount)
            {
                changes.Add($"File count changed from {lastFileCount} to {currentScan.FileCount}");
            }
        }
        
        if (_lastKnownState.TryGetValue("last_total_size", out var lastSizeObj) &&
            lastSizeObj is long lastSize)
        {
            if (Math.Abs(currentScan.TotalSizeBytes - lastSize) > 1024) // Ignore small changes
            {
                changes.Add($"Total size changed by {currentScan.TotalSizeBytes - lastSize} bytes");
            }
        }
        
        // Update last known state
        _lastKnownState["last_file_count"] = currentScan.FileCount;
        _lastKnownState["last_total_size"] = currentScan.TotalSizeBytes;
        _lastKnownState["last_scan_time"] = DateTime.UtcNow;
        
        return changes;
    }

    private async Task CollectApplicationMetricsAsync(MonitoringData data)
    {
        try
        {
            // Collect Strange Loop specific metrics
            data.BehaviorMetrics["agent_type"] = "MonitoringAgent";
            data.BehaviorMetrics["agent_status"] = "Running";
            data.BehaviorMetrics["monitoring_cycle_count"] = _lastKnownState.TryGetValue("cycle_count", out var count) 
                ? (int)count + 1 : 1;
            
            _lastKnownState["cycle_count"] = data.BehaviorMetrics["monitoring_cycle_count"];
            
            // Application health indicators
            data.BehaviorMetrics["memory_pressure"] = GC.GetTotalMemory(false);
            data.BehaviorMetrics["gc_gen0_collections"] = GC.CollectionCount(0);
            data.BehaviorMetrics["gc_gen1_collections"] = GC.CollectionCount(1);
            data.BehaviorMetrics["gc_gen2_collections"] = GC.CollectionCount(2);
            
            _logger.LogDebug("Application metrics collected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect application metrics");
            data.Events.Add($"Error collecting application metrics: {ex.Message}");
        }

        await Task.CompletedTask;
    }    private async Task ProcessMonitoringDataAsync(MonitoringData data, CancellationToken cancellationToken)
    {
        try
        {
            // Use Semantic Kernel to analyze the monitoring data
            var analysisPrompt = $@"
Analyze the following system monitoring data and provide insights:

System Metrics: {JsonSerializer.Serialize(data.SystemMetrics)}
Performance Metrics: {JsonSerializer.Serialize(data.PerformanceMetrics)}
Behavior Metrics: {JsonSerializer.Serialize(data.BehaviorMetrics)}
Environment State: {JsonSerializer.Serialize(data.EnvironmentState)}

Please identify:
1. Any performance concerns
2. Resource utilization patterns
3. Potential optimization opportunities
4. System health indicators

Provide a brief analysis.";

            var result = await _kernel.InvokePromptAsync(analysisPrompt, cancellationToken: cancellationToken);
            data.RawData = result.ToString();
            
            // Convert MonitoringData to TelemetryData for message publishing
            var telemetryData = new TelemetryData
            {
                SystemMetrics = data.SystemMetrics,
                Anomalies = data.Anomalies,
                CollectionTime = data.Timestamp,
                SystemPerformance = data.Anomalies.Count == 0 ? "Normal" : "Degraded",
                UserInteractions = data.BehaviorMetrics.TryGetValue("monitoring_cycle_count", out var cycleCount) 
                    ? Convert.ToInt32(cycleCount) : 0
            };

            // Publish monitoring result to ServiceBus for analysis agent consumption
            var monitoringResult = new MonitoringResult
            {
                ExecutionId = Environment.MachineName + "_" + DateTime.UtcNow.Ticks,
                TelemetryData = telemetryData,
                CollectionTime = data.Timestamp,
                IsHealthy = data.Anomalies.Count == 0
            };

            await _serviceBus.PublishAsync("monitoring-results", monitoringResult, cancellationToken);
            
            _logger.LogInformation("Published monitoring result with execution ID: {ExecutionId}", monitoringResult.ExecutionId);
            _logger.LogDebug("Monitoring data processed through Semantic Kernel");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process monitoring data with Semantic Kernel");
            data.Events.Add($"Error processing with AI: {ex.Message}");
        }
    }

    private async Task DetectAnomaliesAsync(MonitoringData data, CancellationToken cancellationToken)
    {
        try
        {
            // Simple anomaly detection rules
            if (data.SystemMetrics.TryGetValue("cpu_usage_percent", out var cpuObj) && 
                cpuObj is double cpuUsage && cpuUsage > 80)
            {
                data.Anomalies.Add($"High CPU usage detected: {cpuUsage:F1}%");
                _logger.LogWarning("High CPU usage anomaly detected: {CpuUsage}%", cpuUsage);
            }
            
            if (data.SystemMetrics.TryGetValue("available_memory_mb", out var memObj) && 
                memObj is double availableMemory && availableMemory < 100)
            {
                data.Anomalies.Add($"Low available memory: {availableMemory:F1}MB");
                _logger.LogWarning("Low memory anomaly detected: {AvailableMemory}MB", availableMemory);
            }
            
            if (data.PerformanceMetrics.TryGetValue("operation_avg_ms", out var opTimeObj) && 
                opTimeObj is double avgOpTime && avgOpTime > 100)
            {
                data.Anomalies.Add($"Slow operation performance: {avgOpTime:F1}ms average");
                _logger.LogWarning("Performance anomaly detected: {AvgTime}ms", avgOpTime);
            }
              // Log anomalies for further processing
            if (data.Anomalies.Count > 0)
            {
                data.Events.Add($"Anomalies detected: {data.Anomalies.Count}");
                _logger.LogInformation("Detected {Count} anomalies in monitoring cycle", data.Anomalies.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect anomalies");
            data.Events.Add($"Error in anomaly detection: {ex.Message}");
        }

        await Task.CompletedTask;
    }    public override void Dispose()
    {
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Supporting classes for directory scanning
public class DirectoryScanResult
{
    public int FileCount { get; set; }
    public int DirectoryCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public int SkippedDirectories { get; set; }
    public Dictionary<string, FileDetails> FileDetails { get; } = new();
    public Collection<string> Errors { get; } = new();
}

public class FileDetails
{
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    public string Extension { get; set; } = string.Empty;
}
