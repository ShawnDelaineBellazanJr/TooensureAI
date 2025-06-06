using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.Threading.Tasks;

namespace StrangeLoopSystem.Reflector
{
    public class ReflectorAgent
    {
        private readonly ChatCompletionAgent _agent;

        public ReflectorAgent(Kernel kernel)
        {
            _agent = new ChatCompletionAgent
            {
                Name = "Reflector",
                Instructions = "Analyze the overall performance of the system based on execution results and validation feedback. Suggest improvements or optimizations for future iterations to enhance efficiency and effectiveness.",
                Kernel = kernel
            };
        }

        public async Task<string> AnalyzePerformanceAsync(string executionResult)
        {
            var prompt = $"Analyze the system performance based on the following execution result: {executionResult}";
            var response = await _agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, prompt));
            return response.ToString();
        }
    }
} 