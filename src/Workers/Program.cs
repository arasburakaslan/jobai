using JobRag.Infrastructure;
using JobRag.Workers;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Job RAG Workers");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog(configuration =>
        configuration
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.Console()
            .Enrich.FromLogContext());

    // Infrastructure layer
    builder.Services.AddInfrastructure(builder.Configuration);

    // Background workers
    builder.Services.AddHostedService<EmbeddingWorker>();
    builder.Services.AddHostedService<CrawlingWorker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Workers terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
