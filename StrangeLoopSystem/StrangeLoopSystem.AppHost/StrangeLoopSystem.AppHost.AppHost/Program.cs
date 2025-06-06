using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

// See https://aka.ms/dotnet/aspire for more information about .NET Aspire

var builder = DistributedApplication.CreateBuilder(args);

// Add the Strange Loop System project
builder.AddProject<Projects.StrangeLoopSystem>("strangeloop");

builder.Build().Run();
