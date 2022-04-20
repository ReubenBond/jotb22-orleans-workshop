using Microsoft.Extensions.Hosting;
using Orleans.Hosting;

// Define a host builder with defaults for logging, configuration sources, and dependency injection.
var hostBuilder = Host.CreateDefaultBuilder(args);

// Add an Orleans silo to the host builder.
hostBuilder.UseOrleans(siloBuilder =>
{
    // Configure dev/test clustering for Orleans.
    siloBuilder.UseLocalhostClustering();
});

// Configure the host to listen to console events (Ctrl+C/SIGINT), build the host, run it, and wait for a shutdown event.
await hostBuilder.RunConsoleAsync();
