using Microsoft.EntityFrameworkCore;
using Tgs.Agent.Dividend.Core.DependencyInjection;
using Tgs.Agent.Dividend.Core.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<AgentDividendBackground>()
    .AddSingleton<IAgentDividendTaskQueue, AgentDividendTaskQueue>();

builder.Services.AddCore()
    .AddRepository((_, dbOptionsBuilder) => dbOptionsBuilder
        .UseNpgsql("name=Tgs", x => x.UseNetTopologySuite()))
    .AddCashServiceClient((options, sp) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();

        options.BaseUrl = config.GetValue<Uri>("AgentDividend:CashBaseUrl")!;
    })
    .AddMemoryCache();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();