using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.Threading.Tasks;

namespace StrangeLoopSystem.Orchestrator
{
    public class OrchestratorAgent
    {
        private readonly ChatCompletionAgent _agent;

        public OrchestratorAgent(Kernel kernel)
        {
            _agent = new ChatCompletionAgent
            {
                Name = "Orchestrator",
                Instructions = "Coordinate the interactions between different agents to ensure smooth execution of the plan. Manage the flow of information and tasks between Planner, Maker, Checker, and Reflector agents.",
                Kernel = kernel
            };
        }

        public async Task<string> CoordinateAsync(string plan)
        {
            var prompt = $"Coordinate the execution of the following plan: {plan}";
            var response = await _agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, prompt));
            return response.ToString();
        }
    }
} 