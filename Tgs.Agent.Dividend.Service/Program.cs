using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.Agent.Dividend.Core.DependencyInjection;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.PlayerService.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry();

builder.Logging
    .AddOpenTelemetry(options =>
    {
        options.IncludeFormattedMessage = true;
        options.ParseStateValues = true;
        _ = options.AddFluentdForwardExporter(fluentOptions =>
        {
            var nodeIp = builder.Configuration.GetValue<string>("NODE_IP");
            fluentOptions.Host = nodeIp!;
            fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
            fluentOptions.UseMessagePack();
        });
    });

builder.Services
    .AddOptions<OtlpExporterOptions>()
    .Configure<IConfiguration>((options, conifg) =>
    {
        var nodeIp = conifg.GetValue<string>("NODE_IP");

        if (!string.IsNullOrWhiteSpace(nodeIp)) options.Endpoint = new Uri($"http://{nodeIp}:4317");

        conifg.GetSection("Opentelemetry:Tracing").Bind(options);
    });

builder.Services.AddOpenTelemetry()
    .ConfigureResource(_ => _.AddService(builder.Configuration.GetValue<string>("Otel:OTEL_SERVICE_NAME")))
    .WithTracing(traceProviderBuilder => traceProviderBuilder
        .AddSource("Tgs.*")
        .AddSource("TGS.*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
        .AddProfileViewExporter()
        .AddOtlpExporter(options => options.Endpoint = builder.Configuration.GetValue<Uri>("Otel:OTEL_COLLECTOR")))
    .Services
    .Configure<AspNetCoreTraceInstrumentationOptions>(options => options.Filter =
        httpContext => !httpContext.Request.Path.HasValue ||
                       (!httpContext.Request.Path.StartsWithSegments("/swagger") &&
                        !httpContext.Request.Path.StartsWithSegments("/healthz") &&
                        !httpContext.Request.Path.StartsWithSegments("/profiler/") &&
                        !new[] { "html", "ico", "png", "jpg" }.Any(
                            ext => httpContext.Request.Path.Value.EndsWith($".{ext}"))));
builder.Services.AddProfileViewer();

builder.Services.AddHostedService<AgentDividendBackground>()
    .AddHostedService<QueuedHostedService>()
    .AddSingleton<IAgentDividendTaskQueue, AgentDividendTaskQueue>()
    .AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

builder.Services.AddCore()
    .AddRepository((_, dbOptionsBuilder) => dbOptionsBuilder
        .UseNpgsql("name=Tgs",
            x =>
            {
                x.UseNetTopologySuite();
                x.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            }))
    .AddCashServiceClient((options, sp) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();

        options.BaseUrl = config.GetValue<Uri>("AgentDividend:CashBaseUrl")!;
    })
    .AddMemoryCache()
    .AddAgentClient()
    .ConfigureHttpClient(httpBuilder => httpBuilder.BaseAddress =
        builder.Configuration.GetValue<Uri>("AgentDividend:AgentBaseUrl"));

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ExceptionMiddleware>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseSwaggerUI();
app.MapControllers();
app.MapSwagger();
app.MapHealthChecks("/healthz");

app.Run();