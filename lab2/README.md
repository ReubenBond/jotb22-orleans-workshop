# Lab 2: Hello, Grains!

In this lab, we will create and use our first *grain*. Grains are the building block for any Orleans application, just as *Controllers* are the building block in an MVC application, *Services* are the building block of a gRPC server application, and *Hubs* are the building block of an ASP.NET SignalR application. Since they are the building block of an Orleans application, it is critical to understand grains, so we will discuss them now.

A grain is an object with a stable identity. The meaning of that identity is up to you as a developer to define. For example the identity could represent a username, email, Social Security Number, chat room name, or a device id.

Using the grain's identity, you can get a reference to that grain and send a request to it. Orleans is responsible for managing the lifecycle of your grain for you. If you send a request to a grain which is not currently active, Orleans will activate it for you automatically. Later, when the grain has become idle for some period of time, Orleans will unload your grain instance to free up memory.

Recall that Orleans is a framework for building Cloud Native applications. A key characteristic of cloud based applications is that they scale horizontally: you can increase or decrease the resources available to your application on the fly. In practical terms, this means adding or removing hosts. Each host runs an instance of your application and Orleans connects these application instances together to form a *cluster*.

Relating this back to grains, when you get a reference to a grain, you never need to know which host that grain is currently activated on, or if it's activated on any host at all. The programming model offered by Orleans hides this from you, since the Orleans runtime is responsible for managing the lifecycle of grain instances and routing request and response messages between callers and the grains they're calling.

Orleans applications are scalable by default. Your application would not scale very well if every host had the same set of grains loaded: you would quickly run out of memory as the number of active grains increased. To achieve scalability, Orleans will activate each grain on one host at a time. This means that if each host has enough memory to hold up to 100K grains at once, a cluster of 5 hosts could potentially hold 500K grains at once.

There would be little use in sending a request to a grain if you couldn't decide how to handle each request. Grains in Orleans are defined using .NET *classes* and you declare the contract used to communicate with your grain using .NET *interfaces*. This allows you to declare what kinds of requests a grain can handle and how they're handled.

Since grains are defined using classes, they can store information as fields on that class. This means a grain can act as a cache: a grain can load some state from a database or compute it from some other information, and hold onto it while it remains active.

What happens when a grain is deactivated, though, either because the host it was active on crashes or because the Orleans runtime decided to deactivate it to free up memory? The in-memory state would be lost. For this reason, Orleans also provides a simple persistence model: a grain can declare that it has some persistent state and Orleans will make sure that that state is loaded when the grain is activated and that it is safely stored in a database whenever the grain code makes changes to it.

That's enough background, let's see how this looks in code. Here is an example of a grain interface with two methods:

``` csharp
public interface IUserGrain : IGrainWithStringKey
{
  Task SetName(string name);
  Task<string> GetName();
}
```

Notice how the return types for these methods are `Task` and `Task<string>` respectively. Because a grain might be activated on a remote host, and because they execute methods asynchronously, all grain methods must return one of the following types: `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>`. Discussing asynchronous programming in .NET is outside of the scope of this exercise, so for more detail, please read the article titled [Asynchronous programming](https://docs.microsoft.com/dotnet/csharp/async) from the C# programming guide in the official .NET documentation.

Here is a grain class implementing that interface:

``` csharp
public class UserGrain : Grain, IUserGrain
{
  private string _name;

  public Task SetName(string name)
  {
    _name = name;
    return Task.CompletedTask;
  }

  public Task<string> GetName() => Task.FromResult(_name);
}
```

Notice how the `IUserGrain` interface inherits from `IGrainWithStringKey`. This tells Orleans that the interface is a *grain interface* and that the resulting grain is identified using a `string` key. The identity of a grain consists of two parts: the grain class and the grain key. When we want to get a reference to `UserGrain`, we will use a `string` for the key. There are several options for grain identity:

* `IGrainWithGuidKey`, which uses a `Guid` as the key
* `IGrainWithIntegerKey`, which uses a `long` as the key
* `IGrainWithStringKey`, which uses a `string` as the key
* `IGrainWithGuidCompoundKey`, which uses a `Guid` *plus* a `string` as the key
* `IGrainWithGuidKey`, which uses a `long` *plus* a `string` as the key

It is recommended to use `string` keys for grains (by inheriting from `IGrainWithStringKey`) unless you are sure that the alternatives provide a better fit.

A caller gets for a reference to a grain using the `IGrainFactory` interface, which has various `GetGrain<TInterface>(...)` overloads, each one accepting one of the different key types described above.

Here is an example demonstrating how to get a reference to a `UserGrain`:

``` csharp
var grainFactory = host.GetRequiredService<IGrainFactory>();
var myUserGrain = grainFactory.GetGrain<IUserGrain>("Jotbert");
```

Notice how we pass the grain interface, `IUserGrain`, to the `GetGrain` method and not the class, `UserGrain`. We want a reference to the `UserGrain` grain with a key of *"Jotbert"*, but that grain might be be activated on a different host. For this reason, we cannot communicate with it using the `UserGrain` class: we have to use a *proxy* object instead. `GetGrain` returns a proxy object which we use for communication. That proxy object will be responsible for translating our `GetName` and `SetName` calls into messages and sending those messages to a (potentially remote) `UserGrain` instance. We refer to these proxy objects as *grain references* and Orleans implements them for us at compilation time by generating classes which inherit from a base type called `GrainReference` and implement our interfaces (eg, `IUserGrain`) by translating calls into messages.

## Goal

The goal of this lab is to introduce you to grains in Orleans. There are three steps:

1. Define a grain *interface*
2. Define a grain *class*
3. Get a reference to our grain and call it

## Steps

### Define a grain interface

Add the `Orleans` and `Microsoft.Extensions.DependencyInjection` namespaces to top of *Program.cs*, since we will be using them in this lab. The block of using statements should look like this once you're done:

``` csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
```

At the bottom of *Program.cs*, define your new grain interface as follows:

``` csharp
public interface IHelloWorldGrain : IGrainWithStringKey
{
    Task<string> SayHello(string input);
}
```

### Define a grain class

Below `IHelloWorldGrain`, define an implementation class called `HelloWorldGrain`:

``` csharp
public class HelloWorldGrain : Grain, IHelloWorldGrain
{
    public Task<string> SayHello(string input)
    {
        return Task.FromResult($"Hello, {input}! My name is {this.GetPrimaryKeyString()}.");
    }
}
```

All grain classes inherit from the `Grain` class. This tells Orleans that the class is a grain class and also provides you with some helper methods, such as the `GetPrimaryKeyString()` method used in the above code. `GetPrimaryKeyString()` returns the *key* portion of the current grain's identity.

### Get a reference to a grain and call it

Currently, our program does nothing other than starting an Orleans server and waiting until it's asked to shutdown.
Now, we will modify the main body of the program to get a reference to our grain and call the `SayHello` method on it.

Change the `await hostBuilder.RunConsoleAsync();` line in our program to the following:

``` csharp
using var host = await hostBuilder.StartAsync();

var client = host.Services.GetRequiredService<IGrainFactory>();
var helloGrain = client.GetGrain<IHelloWorldGrain>("Jotbee");

var message = await helloGrain.SayHello("friend");
Console.WriteLine("Received: " + message);

// Wait until Ctrl+C is pressed
await host.WaitForShutdownAsync();
```

Let's examine each line in order to understand what it does:

``` csharp
using var host = await hostBuilder.StartAsync();
```

Previously, we had multiple steps rolled into one: we were building our host, starting it, and waiting for shutdown. Instead, we are now just building and starting it, using the `IHostBuilder.StartAsync()` method. This method gives us an instance of the now started host back. The host has some properties on it, such as `Services` which allows us to access services which have been added to the host. On the next line, we do just that by asking the host for an instance of the `IGrainFactory` service.

``` csharp
var client = host.Services.GetRequiredService<IGrainFactory>();
```

Now that we have our `IGrainFactory`, we can use it to get a reference to our `IHelloWorldGrain` by using its `GetGrain` method:

``` csharp
var helloGrain = client.GetGrain<IHelloWorldGrain>("Jotbee");
```

For this lab, we've chosen the key *"Jotbee"* for our grain, but it could be any string you like. Note that getting a reference to a grain does not activate it. The grain will be activated by Orleans when you send it a request. Let's activate your grain by sending it a request now:

``` csharp
var message = await helloGrain.SayHello("friend");
Console.WriteLine("Received: " + message);
```

The above line sends a `SayHello("friend")` request to our grain and awaits the response. Once it has the response, it prints it out to the console. The last line of our program simply waits for the shutdown signal from the console:

``` csharp
// Wait until Ctrl+C is pressed
await host.WaitForShutdownAsync();
```

### Run the modified program

Run the modified program by entering the following in the terminal:

``` sh
dotnet run
```

Among the output which is printed, you should see the following:

```
Received: Hello, friend! My name is Jotbee.
```

Press Ctrl+C to exit the application.

Great! We have just created our first grain! The grain has no state yet, so we have hardly begun to explore your potential with Orleans. Let's explore that potential further by creating a *stateful* grain in the next lab.

----

<details>
<summary>Click here to see the completed Program.cs</summary>

``` csharp
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

```

</details>