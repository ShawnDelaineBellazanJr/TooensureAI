using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using StrangeLoopSystem.Planner;
using StrangeLoopSystem.Maker;
using StrangeLoopSystem.Checker;
using StrangeLoopSystem.Reflector;
using StrangeLoopSystem.Orchestrator;
using StrangeLoopSystem.MetaOrchestrator;

namespace StrangeLoopSystem
{
    public class Program
    {
        private static Kernel kernel;
        private static IMemoryStore memoryStore;
        private static ILogger<Program> logger;
        private static PlannerAgent planner;
        private static MakerAgent maker;
        private static CheckerAgent checker;
        private static ReflectorAgent reflector;
        private static OrchestratorAgent orchestrator;
        private static MetaOrchestratorAgent metaOrchestrator;

        public static async Task Main(string[] args)
        {
            // Initialize configuration and logging
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            logger = loggerFactory.CreateLogger<Program>();

            // Initialize Semantic Kernel
            kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(
                    config["AzureOpenAI:DeploymentName"],
                    config["AzureOpenAI:Endpoint"],
                    config["AzureOpenAI:ApiKey"])
                .Build();
            memoryStore = new VolatileMemoryStore();

            // Initialize agents
            planner = new PlannerAgent(kernel);
            maker = new MakerAgent(kernel);
            checker = new CheckerAgent(kernel);
            reflector = new ReflectorAgent(kernel);
            orchestrator = new OrchestratorAgent(kernel);
            metaOrchestrator = new MetaOrchestratorAgent(kernel);

            // Run the strange loop
            await RunStrangeLoopAsync("Resolve customer support ticket for order #12345", maxIterations: 3);
        }

        private static async Task RunStrangeLoopAsync(string objective, int maxIterations)
        {
            var context = new KernelArguments { ["objective"] = objective };
            int iteration = 0;
            string stateHash = "";
            bool converged = false;

            while (iteration < maxIterations && !converged)
            {
                logger.LogInformation($"Starting iteration {iteration + 1}");

                try
                {
                    // Retrieve past reflections
                    var pastReflections = await memoryStore.GetAsync("reflections") ?? "";
                    context["pastReflections"] = pastReflections;

                    // Planner: Generate plan
                    var plan = await planner.GeneratePlanAsync(context["objective"].ToString(), context["pastReflections"].ToString());
                    logger.LogInformation($"Plan: {plan}");
                    context["plan"] = plan;

                    // Orchestrator: Coordinate plan execution
                    var coordination = await orchestrator.CoordinateAsync(plan);
                    logger.LogInformation($"Coordination: {coordination}");

                    // Execute plan with Maker and Checker
                    var executionResult = await ExecutePlanAsync(plan);
                    context["executionResult"] = executionResult;
                    logger.LogInformation($"Execution Result: {executionResult}");

                    // Reflector: Analyze performance
                    var reflection = await reflector.AnalyzePerformanceAsync(executionResult);
                    logger.LogInformation($"Reflection: {reflection}");
                    context["reflection"] = reflection;

                    // Store reflection
                    await memoryStore.UpsertAsync("reflections", reflection);

                    // Meta-Orchestrator: Adjust system
                    var adjustment = await metaOrchestrator.AdjustSystemAsync(reflection);
                    logger.LogInformation($"Adjustment: {adjustment}");
                    context["adjustment"] = adjustment;

                    // Check for cycles and convergence
                    var newStateHash = ComputeStateHash(context);
                    if (newStateHash == stateHash)
                    {
                        logger.LogWarning("Cycle detected, terminating loop.");
                        break;
                    }
                    stateHash = newStateHash;
                    converged = reflection.Contains("converged");

                    iteration++;
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error in iteration {iteration + 1}: {ex.Message}");
                    context["executionResult"] = "Fallback: Unable to process ticket, escalating to human agent.";
                    break;
                }
            }

            logger.LogInformation($"Final Output: {context["executionResult"]}");
        }

        private static async Task<string> ExecutePlanAsync(string plan)
        {
            // Simplified plan execution
            var tasks = plan.Split(';'); // Assume plan is semicolon-separated tasks
            string result = "";
            foreach (var task in tasks)
            {
                if (!string.IsNullOrWhiteSpace(task))
                {
                    var taskResult = await maker.ExecuteTaskAsync(task);
                    var validation = await checker.ValidateOutputAsync(taskResult);
                    if (validation.Contains("valid"))
                    {
                        result += taskResult + ";";
                    }
                    else
                    {
                        result += "Invalid task result;";
                    }
                }
            }
            return result;
        }

        private static string ComputeStateHash(KernelArguments context)
        {
            return JsonSerializer.Serialize(context).GetHashCode().ToString();
        }
    }
}
