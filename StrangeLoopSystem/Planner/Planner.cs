using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.Threading.Tasks;

namespace StrangeLoopSystem.Planner
{
    public class PlannerAgent
    {
        private readonly ChatCompletionAgent _agent;

        public PlannerAgent(Kernel kernel)
        {
            _agent = new ChatCompletionAgent
            {
                Name = "Planner",
                Instructions = "Generate a detailed step-by-step plan to resolve complex tasks or customer support tickets. Ensure the plan is actionable and considers past reflections for improvement.",
                Kernel = kernel
            };
        }

        public async Task<string> GeneratePlanAsync(string objective, string pastReflections)
        {
            var prompt = $"{objective} Past reflections for improvement: {pastReflections}";
            var response = await _agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, prompt));
            return response.ToString();
        }
    }
} 