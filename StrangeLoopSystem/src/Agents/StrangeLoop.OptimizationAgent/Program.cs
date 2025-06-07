using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using StrangeLoop.OptimizationAgent;
using StrangeLoop.Infrastructure.ServiceBus;

var builder = Host.CreateApplicationBuilder(args);

// Register Semantic Kernel
builder.Services.AddKernel();

// Register ServiceBus infrastructure
builder.Services.AddStrangeLoopServiceBus();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
