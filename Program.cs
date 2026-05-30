using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configuración e inyección del proveedor TLS de Redis
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("RedisOptions"));
builder.Services.AddSingleton<IRedisProvider>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    var config = new ConfigurationOptions
    {
        EndPoints = { { opts.Host, opts.Port } },
        Password = opts.Password,
        Ssl = true,
        AbortOnConnectFail = false,
        ConnectTimeout = 5000
    };

    config.CertificateValidation += (sender, cert, chain, errors) =>
    {
        if (errors == SslPolicyErrors.None) return true;
        if (string.IsNullOrEmpty(opts.CaCertificatePath) || !File.Exists(opts.CaCertificatePath)) return false;
        
        using var customChain = new X509Chain();
        customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        customChain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
        customChain.ChainPolicy.ExtraStore.Add(new X509Certificate2(opts.CaCertificatePath));
        return customChain.Build(new X509Certificate2(cert));
    };

    return new RedisProvider(ConnectionMultiplexer.Connect(config));
});

builder.Services.AddSingleton<ISpreadEngine, SpreadEngine>();

var app = builder.Build();

// Endpoint para Webhooks de mitigación de Cloudflare
app.MapPost("/api/v1/infra/cloudflare-alert", async (HttpRequest request, IRedisProvider redis, Microsoft.Extensions.Logging.ILogger<Program> logger) =>
{
    if (!request.Headers.TryGetValue("X-Cloudflare-Webhook-Secret", out var secret) || secret != "TOKEN_PERIMETRAL")
    {
        return Results.Unauthorized();
    }

    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();
    using var doc = JsonDocument.Parse(body);
    var root = doc.RootElement;
    
    string alertType = root.GetProperty("alert_type").GetString() ?? "unknown";
    logger.LogWarning("Alerta perimetral detectada: {Type}", alertType);

    if (alertType == "dos_attack_l7")
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync("ark:status:mitigation_mode", "TRUE", TimeSpan.FromMinutes(30));
        await db.PublishAsync("ark:pubsub:infra_alerts", "MITIGATION_ON");
    }

    return Results.Ok(new { status = "processed" });
});

// Endpoint para inyección de liquidez adaptativa (Pasivo-Agresiva)
app.MapPost("/api/v1/liquidity/update", async ([FromBody] MarketUpdate update, IRedisProvider redis, ISpreadEngine spreadEngine) =>
{
    var db = redis.GetDatabase();
    var (clientBid, clientAsk, appliedSpread) = spreadEngine.CalculateAggressiveSpread(update.Bid, update.Ask);

    var resultPayload = new
    {
        Pair = update.Pair,
        Bid = clientBid,
        Ask = clientAsk,
        Spread = $"{appliedSpread}%",
        Timestamp = DateTime.UtcNow
    };

    string json = JsonSerializer.Serialize(resultPayload);
    await db.StringSetAsync($"ark:liquidity:live:{update.Pair}", json, TimeSpan.FromSeconds(15));
    await db.PublishAsync("ark:pubsub:liquidity_updates", json);

    return Results.Ok(resultPayload);
});

app.Run();

// Componentes de soporte de Infraestructura
public class RedisOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Password { get; set; } = string.Empty;
    public string CaCertificatePath { get; set; } = string.Empty;
}

public interface IRedisProvider { IDatabase GetDatabase(); }
public class RedisProvider(IConnectionMultiplexer mux) : IRedisProvider 
{
    public IDatabase GetDatabase() => mux.GetDatabase();
}

public interface ISpreadEngine { (decimal Bid, decimal Ask, decimal Percent) CalculateAggressiveSpread(decimal bid, decimal ask); }
public class SpreadEngine : ISpreadEngine
{
    public (decimal Bid, decimal Ask, decimal Percent) CalculateAggressiveSpread(decimal bid, decimal ask)
    {
        decimal mid = (bid + ask) / 2m;
        decimal baseSpread = 0.0035m; // 0.35% margen de beneficio adaptativo
        
        decimal clientBid = Math.Round(mid * (1m - (baseSpread / 2m)), 2);
        decimal clientAsk = Math.Round(mid * (1m + (baseSpread / 2m)), 2);
        return (clientBid, clientAsk, baseSpread * 100m);
    }
}

public record MarketUpdate(string Pair, decimal Bid, decimal Ask);
