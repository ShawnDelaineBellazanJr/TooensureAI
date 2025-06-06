using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.Threading.Tasks;

namespace StrangeLoopSystem.MetaOrchestrator
{
    public class MetaOrchestratorAgent
    {
        private readonly ChatCompletionAgent _agent;

        public MetaOrchestratorAgent(Kernel kernel)
        {
            _agent = new ChatCompletionAgent
            {
                Name = "MetaOrchestrator",
                Instructions = "Oversee the entire system operation, adjusting parameters and strategies based on Reflector feedback. Optimize the overall system performance by refining agent instructions or system workflows.",
                Kernel = kernel
            };
        }

        public async Task<string> AdjustSystemAsync(string reflection)
        {
            var prompt = $"Based on the following reflection, adjust system parameters or strategies: {reflection}";
            var response = await _agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, prompt));
            return response.ToString();
        }
    }
} 