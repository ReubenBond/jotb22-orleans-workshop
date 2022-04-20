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

[Serializable]
public class UrlShortenerGrainState
{
    public string? Address { get; set; }
}

public class UrlShortenerGrain : Grain, IUrlShortenerGrain
{
    private readonly IPersistentState<UrlShortenerGrainState> _state;

    public UrlShortenerGrain(
        [PersistentState(stateName: "url", storageName: "urls")] IPersistentState<UrlShortenerGrainState> state)
    {
        _state = state;
    }

    public async Task SetUrl(string newValue)
    {
        _state.State = new UrlShortenerGrainState { Address = newValue };
        await _state.WriteStateAsync();
    }

    public Task<string?> GetUrl()
    {
        return Task.FromResult(_state.State?.Address);
    }
}
