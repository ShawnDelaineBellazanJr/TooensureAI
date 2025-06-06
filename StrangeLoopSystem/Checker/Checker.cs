using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using System.Threading.Tasks;

namespace StrangeLoopSystem.Checker
{
    public class CheckerAgent
    {
        private readonly ChatCompletionAgent _agent;

        public CheckerAgent(Kernel kernel)
        {
            _agent = new ChatCompletionAgent
            {
                Name = "Checker",
                Instructions = "Validate the output of executed tasks against predefined criteria or quality standards. Provide feedback on whether the output meets expectations or requires revision.",
                Kernel = kernel
            };
        }

        public async Task<string> ValidateOutputAsync(string taskOutput)
        {
            var prompt = $"Validate the following task output: {taskOutput}";
            var response = await _agent.InvokeAsync(new ChatMessageContent(AuthorRole.User, prompt));
            return response.ToString();
        }
    }
} 