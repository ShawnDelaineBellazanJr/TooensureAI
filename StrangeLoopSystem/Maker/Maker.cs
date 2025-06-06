using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.Threading.Tasks;

namespace StrangeLoopSystem.Maker
{
    public class MakerAgent
    {
        private readonly ChatCompletionAgent _agent;

        public MakerAgent(Kernel kernel)
        {
            _agent = new ChatCompletionAgent
            {
                Name = "Maker",
                Instructions = "Execute specific tasks from a given plan, such as retrieving data, performing calculations, or interacting with external systems. Provide detailed output for each task.",
                Kernel = kernel
            };
        }

        public async Task<string> ExecuteTaskAsync(string task)
        {
            var response = await _agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, task));
            return response.ToString();
        }
    }
} 