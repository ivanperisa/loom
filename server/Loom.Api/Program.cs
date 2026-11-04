using Loom.Api.Extensions;
using Loom.Application;
using Loom.Infrastructure;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().GetCurrentClassLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseNLog(new NLogAspNetCoreOptions { RemoveLoggerFactoryFilter = false });

    builder.AddLoomApi();
    builder.AddLoomAuthentication();
    var devAuthEnabled = builder.IsDevAuthEnabled();

    builder.Services.AddInfrastructure(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        disableJit: builder.Configuration.GetValue("Database:DisableJit", true));
    builder.Services.AddApplication();

    var app = builder.Build();
    app.UseLoomApi(devAuthEnabled);
    app.Run();
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Flush NLog before exit (avoids a segmentation fault on Linux).
    LogManager.Shutdown();
}

/// <summary>Exposed for WebApplicationFactory-based tests.</summary>
public partial class Program;
