# Lab 3: Dealing With State

In this lab, we will start to explore state in Orleans. Grains in Orleans can keep state in-memory while they are active, but if it's important that no data is lost in the event of a grain being deactivated (either due to a fault or because it became idle), then the state should be persisted to a database. Orleans offers a declarative persistence model: grains can inherit from `Grain<TState>` or use dependency injection to inject `IPersistentState<TState>`, where `TState` is a class which holds the grain's state. Orleans will automatically load the state during activation and the grain can choose when to save the state by calling `WriteStateAsync()`.

Orleans uses a provider model for persistence and there are many supported databases, often developed by community members and contributed to the [*OrleansContib*](https://github.com/OrleansContrib) organization on GitHub where the community can collaborate on them.

For cases where the persistence model offered by Orleans does not match your application's needs, you can implement your own abstractions or use other libraries to load and store state from your grain classes.

## Goal

The goal of this lab is to become familiar with state in Orleans. The lab explores both in-memory, volatile state, as well as persistent state.

## Part 1: Creating a URL shortener

We are going to create a basic URL shortener. We will start by storing those URLs in-memory and eventually evolve the application to support persistent state. To make this more interesting, we will use ASP.NET Minimal APIs to create a Web server which performs the shortening for us.

### Create a new Web project

From the terminal, create a new web project by invoking the following command:

``` sh
dotnet new web
```

Now, add Orleans to our new Web project by invoking the following commands:

``` sh
dotnet add package Microsoft.Orleans.Server
dotnet add package Microsoft.Orleans.CodeGenerator.MSBuild
```

Open Program.cs in your editor and replace the contents with the following:

``` csharp
using Microsoft.AspNetCore.Http.Extensions;
using Orleans;
using Orleans.Hosting;

var builder = WebApplication.CreateBuilder();

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
});

var app = builder.Build();
var client = app.Services.GetRequiredService<IGrainFactory>();

app.MapGet("/shorten/{*path}", async (HttpContext context, string path) =>
{
});

app.MapGet("/go/{alias}", async (string alias) =>
{
});

app.Run();
```

This scaffolds our application. We have a Web API with two routes:

* `/shorten/{*path}` will be used to shorten everything after the `/shorten/` part of the URL. It will generate a short URL which can be used as an alias for the original URL. The short URL will point to the second route.
* `/go/{alias}` will be the short URL endpoint. It accepts an alias and redirects the user to the original URL.

Now, let's implement the grains. We will have one grain for now. It will be keyed on a string (the shortened URL id) and will store the original URL in a field on the grain implementation class. Add the following to the bottom of *Program.cs* to define your volatile URL shortener grain:

``` csharp
public interface IUrlShortenerGrain : IGrainWithStringKey
{
    Task SetUrl(string address);
    Task<string?> GetUrl();
}

public class UrlShortenerGrain : Grain, IUrlShortenerGrain
{
    private string? _url;

    public Task SetUrl(string newValue)
    {
        _url = newValue;
        return Task.CompletedTask;
    }

    public Task<string?> GetUrl()
    {
        return Task.FromResult(_url);
    }
}
```

Hopefully this is rather self-explanatory: we have a private `string` field called `_url` on our `UrlShortenerGrain` which holds the previously provided URL. The grain's *key* will be the alias for the URL.

### Implement the `/shorten` route

Now let's turn our attention to the implementation of the `/shorten` route, which is defined in the first `app.MapGet` call in our program. We are going to use a very basic algorithm to shorten URLs: we will generate a short, random string. First, we should validate that the user has provided us with a valid URL to shorten: we don't want them storing just any text. Add the following to the top of the body of the aforementioned `MapGet` call:

``` csharp
if (!Uri.TryCreate(path, UriKind.Absolute, out var url) || !(url.Scheme is "http" or "https"))
{
    return Results.Problem($"The value \"{path}\" is not a valid URI");
}
```

The above snippet of code attempts to decode the `path` parameter as a *URI* and then checks to ensure that it's an HTTP or HTTPS URL.

Below that, add the following to generate a URL alias and get a reference to the corresponding `UrlShortenerGrain`:

``` csharp
var alias = Guid.NewGuid().GetHashCode().ToString("X");
var shortenerGrain = client.GetGrain<IUrlShortenerGrain>(alias);
```

Now that we have a reference to our `UrlShortenerGrain`, lets set the original URL value on it by adding the following line:

``` csharp
await shortenerGrain.SetUrl(path);
```

Finally, we will tell the user what the newly created alias for their URL is by adding the following:

``` csharp
var resultBuilder = new UriBuilder(context.Request.GetEncodedUrl())
{
    Path = $"/go/{alias}",
    Query = null
};

return Results.Ok(resultBuilder.Uri);
```

This completes the body of our shortener route, now onto the redirection route.

### Implement the `/go` route

The body of the other `app.MapGet` call is left as an exercise to the reader.

## Part 2: Adding persistence

In this section, we will be adding persistence to our URL shortener. As mentioned at the top of this article, there are two ways to add persistent state to a grain:

* By inheriting from `Grain<TState>`
* By injecting `IPersistentState<TState>` into your grain from the constructor

In this lab, we will be using the second approach. Before we can add persistent state to our grains, we need to configure a storage provider. An Orleans app can have multiple storage providers, each identified by name. In this lab, we will use an in-memory storage provider, which actually provides no persistence whatsoever. This is suitable for development and testing, but it is not suitable for a production environment. For a production environment, there are many persistence providers available on nuget.org, covering a wide range of storage options, including:

* Azure Table Storage
* Azure Blob Storage
* AWS DynamoDB
* ADO.NET (SQL Server, MySQL/MariaDB, Postgres)
* Redis
* MongoDB
* Azure CosmosDB

When you are ready to start testing your application with durable storage options, you only need to update your application configuration to add those providers instead of the in-memory provider which we will use in this lab and do not need to change your grain code.

### Add the in-memory storage provider

We will add a storage provider called `urls` to our application by adding the following line in the `UseOrleans` configuration block near the top of *Program.cs*. Add the following after the `siloBuilder.UseLocalhostClustering();` line:

``` csharp
siloBuilder.AddMemoryGrainStorage("urls");
```

### Define a class to hold our grain's state

Add a new class, `UrlShortenerGrainState`, as follows:

```csharp
[Serializable]
public class UrlShortenerGrainState
{
    public string? Address { get; set; }
}
```

### Modify our grain to use persistent storage

Change the definition of `UrlShortenerGrain` to the following:

``` csharp
public class UrlShortenerGrain : Grain, IUrlShortenerGrain
{
    private readonly // TODO: add a field to hold the persistent state

    public UrlShortenerGrain(
        [PersistentState(stateName: "url", storageName: "urls")] IPersistentState<UrlShortenerGrainState> state)
    {
        // TODO: assign the state variable
    }

    public Task SetUrl(string newValue)
    {
        // TODO: store and save the state
    }

    public Task<string?> GetUrl()
    {
        // TODO: return the state
    }
}
```

Next, attempt to fill in the remainder of the class. If you get stuck, an example completed program listing is available below.

This lab has shown us that grains can hold state and keep that state between requests. In the next lab, we will address something we've now put off for long enough: scaling out our application by adding additional host processes.

----

<details>
<summary>Click here to see the completed Program.cs from Part 1</summary>

``` csharp
using Microsoft.AspNetCore.Http.Extensions;
using Orleans;
using Orleans.Hosting;

var builder = WebApplication.CreateBuilder();

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
});

var app = builder.Build();
var client = app.Services.GetRequiredService<IGrainFactory>();

app.MapGet("/shorten/{*path}", async (HttpContext context, string path) =>
{
    if (!Uri.TryCreate(path, UriKind.Absolute, out var url) || !(url.Scheme is "http" or "https"))
    {
        return Results.Problem($"The value \"{path}\" is not a valid URI");
    }

    var alias = Guid.NewGuid().GetHashCode().ToString("X");
    var shortenerGrain = client.GetGrain<IUrlShortenerGrain>(alias);
    await shortenerGrain.SetUrl(path);
    var resultBuilder = new UriBuilder(context.Request.GetEncodedUrl())
    {
        Path = $"/go/{alias}",
        Query = null
    };

    return Results.Ok(resultBuilder.Uri);
});

app.MapGet("/go/{alias}", async (string alias) =>
{
    var shortenerGrain = client.GetGrain<IUrlShortenerGrain>(alias);
    var url = await shortenerGrain.GetUrl();

    return url switch
    {
        null => Results.NotFound(),
        _ => Results.Redirect(url)
    };
});

app.Run();

public interface IUrlShortenerGrain : IGrainWithStringKey
{
    Task SetUrl(string address);
    Task<string?> GetUrl();
}

public class UrlShortenerGrain : Grain, IUrlShortenerGrain
{
    private string? _url;

    public Task SetUrl(string newValue)
    {
        _url = newValue;
        return Task.CompletedTask;
    }

    public Task<string?> GetUrl()
    {
        return Task.FromResult(_url);
    }
}
```

</details>

<details>
<summary>Click here to see the completed Program.cs from Part 2</summary>

``` csharp
using Microsoft.AspNetCore.Http.Extensions;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;

var builder = WebApplication.CreateBuilder();

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
    siloBuilder.AddMemoryGrainStorage("urls");
});

var app = builder.Build();
var client = app.Services.GetRequiredService<IGrainFactory>();

app.MapGet("/shorten/{*path}", async (HttpContext context, string path) =>
{
    if (!Uri.TryCreate(path, UriKind.Absolute, out var url) || !(url.Scheme is "http" or "https"))
    {
        return Results.Problem($"The value \"{path}\" is not a valid URI");
    }

    var alias = Guid.NewGuid().GetHashCode().ToString("X");
    var shortenerGrain = client.GetGrain<IUrlShortenerGrain>(alias);
    await shortenerGrain.SetUrl(path);
    var resultBuilder = new UriBuilder(context.Request.GetEncodedUrl())
    {
        Path = $"/go/{alias}",
        Query = null
    };

    return Results.Ok(resultBuilder.Uri);
});

app.MapGet("/go/{alias}", async (string alias) =>
{
    var shortenerGrain = client.GetGrain<IUrlShortenerGrain>(alias);
    var url = await shortenerGrain.GetUrl();

    return url switch
    {
        null => Results.NotFound(),
        _ => Results.Redirect(url)
    };
});

app.Run();

public interface IUrlShortenerGrain : IGrainWithStringKey
{
    Task SetUrl(string address);
    Task<string?> GetUrl();
}

public class UrlShortenerGrain : Grain, IUrlShortenerGrain
{
    private readonly IPersistentState<string?> _state;

    public UrlShortenerGrain(
        [PersistentState(stateName: "url", storageName: "urls")] IPersistentState<string?> state)
    {
        _state = state;
    }

    public async Task SetUrl(string newValue)
    {
        _state.State = newValue;
        await _state.WriteStateAsync();
    }

    public Task<string?> GetUrl()
    {
        return Task.FromResult(_state.State);
    }
}
```

</details>