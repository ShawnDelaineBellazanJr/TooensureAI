# Pure Semantic Kernel Native Strange Loop System

## Overview
This project implements a production-ready **Pure Semantic Kernel Native Strange Loop System**, an autonomous, multi-agent AI system designed to process complex tasks through a six-stage loop architecture. Built with **Microsoft Semantic Kernel 1.55.0+**, **C# 13**, **.NET 9.0**, and **.NET Aspire 9.3**, it leverages cutting-edge AI orchestration, enterprise-grade scalability, and observability. The system incorporates Douglas Hofstadter's concept of strange loops, enabling recursive self-improvement through feedback cycles.

## Architecture
The system comprises six distinct stages, each represented by a specialized agent:
- **Planner**: Generates detailed step-by-step plans for task resolution.
- **Maker**: Executes tasks from the plan, such as data retrieval or external interactions.
- **Checker**: Validates task outputs against quality criteria.
- **Reflector**: Analyzes system performance and suggests improvements.
- **Orchestrator**: Coordinates interactions between agents for seamless execution.
- **Meta-Orchestrator**: Oversees the system, adjusting parameters based on Reflector feedback.

The **strange loop** mechanism is achieved by feeding Reflector insights back to the Planner and Meta-Orchestrator, fostering continuous improvement.

## Requirements
- **.NET 9.0 SDK**
- **NuGet Packages**:
  - `Microsoft.SemanticKernel` (1.55.0)
  - `Microsoft.SemanticKernel.Agents.Core`
  - `Microsoft.SemanticKernel.Agents.OpenAI` (prerelease)
  - `Microsoft.Extensions.Logging`
  - `Microsoft.Extensions.Configuration`
- **Azure OpenAI API key and endpoint**
- **.NET Aspire 9.3** (for deployment and monitoring)

## Setup
1. **Clone the Repository** (if applicable):
   ```bash
   git clone https://github.com/ShawnDelaineBellazanJr/TooensureAI.git
   cd TooensureAI/StrangeLoopSystem
   ```
2. **Install Dependencies**:
   Dependencies are already added to project files. Restore them with:
   ```bash
   dotnet restore
   ```
3. **Configure Azure OpenAI Credentials**:
   Edit `StrangeLoopSystem/StrangeLoopSystem/appsettings.json` to include your Azure OpenAI details:
   ```json
   {
     "AzureOpenAI": {
       "DeploymentName": "gpt-4o",
       "Endpoint": "your_endpoint",
       "ApiKey": "your_api_key"
     }
   }
   ```
4. **Build and Run**:
   ```bash
   dotnet build
   dotnet run --project StrangeLoopSystem/StrangeLoopSystem
   ```

## Usage
The system is preconfigured to resolve a sample customer support ticket ("Resolve customer support ticket for order #12345"). Upon execution, it will iterate through the six-stage loop up to 3 times or until convergence/cycle detection. Logs will display the output of each agent at every stage.

To modify the objective, update the `RunStrangeLoopAsync` call in `Program.cs` with a new task:
```csharp
await RunStrangeLoopAsync("Your new objective here", maxIterations: 3);
```

## Enterprise Patterns
- **Scalability**: Designed for containerization with .NET Aspire, deployable on Azure Container Apps for horizontal scaling.
- **Security**: Azure OpenAI API keys are stored in configuration, suitable for integration with Azure Key Vault in production.
- **Monitoring**: .NET Aspire integration provides a dashboard for real-time telemetry (setup pending).
- **Error Handling**: Robust try-catch blocks ensure fallback to human escalation on failures.
- **Logging**: Detailed event logging via `Microsoft.Extensions.Logging`.

## Strange Loop Mechanism
The Reflector stores feedback in a `VolatileMemoryStore`, which the Planner retrieves to refine future plans. The Meta-Orchestrator adjusts system strategies based on this feedback, creating a self-referential improvement cycle. Cycle detection via state hashing prevents infinite loops.

## C# 13 and .NET 9.0 Features
- Leverages performance optimizations and new threading models for efficient agent coordination.
- Uses enhanced collection features for state management.

## Future Improvements
- Implement a Blazor dashboard for real-time monitoring.
- Integrate Azure Cosmos DB for persistent memory storage.
- Develop advanced plugins for Maker tasks (e.g., database queries, external APIs).
- Complete .NET Aspire AppHost setup for full distributed system monitoring.

## Limitations
- Currently console-based, lacking a visual interface.
- Uses `VolatileMemoryStore` for simplicity; not suitable for production persistence.
- Simplified task execution; real-world scenarios may require complex plugin integration.

## References
- [Semantic Kernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
- [C# 13 New Features](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13)
- [.NET 9.0 Overview](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview)
- [.NET Aspire 9.3](https://learn.microsoft.com/en-us/dotnet/aspire/whats-new/dotnet-aspire-9.3)

## License
See [LICENSE.txt](../../LICENSE.txt) for details. 