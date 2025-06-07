using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using StrangeLoop.KnowledgeUpdateAgent;
using StrangeLoop.Infrastructure.ServiceBus;

var builder = Host.CreateApplicationBuilder(args);

// Add Semantic Kernel
builder.Services.AddKernel();

// Add ServiceBus infrastructure
builder.Services.AddStrangeLoopServiceBus();

// Add the hosted service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
