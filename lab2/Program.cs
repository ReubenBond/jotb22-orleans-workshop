using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;

var hostBuilder = Host.CreateDefaultBuilder(args);

hostBuilder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
});

using var host = await hostBuilder.StartAsync();

var client = host.Services.GetRequiredService<IGrainFactory>();
var helloGrain = client.GetGrain<IHelloWorldGrain>("Jotbee");

var message = await helloGrain.SayHello("friend");
Console.WriteLine("Received: " + message);

// Wait until Ctrl+C is pressed
await host.WaitForShutdownAsync();

public interface IHelloWorldGrain : IGrainWithStringKey
{
    Task<string> SayHello(string input);
}

public class HelloWorldGrain : Grain, IHelloWorldGrain
{
    public Task<string> SayHello(string input)
    {
        return Task.FromResult($"Hello, {input}! My name is {this.GetPrimaryKeyString()}.");
    }
}
