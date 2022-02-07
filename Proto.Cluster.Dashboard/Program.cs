using Proto;
using Proto.Cluster;
using Proto.Cluster.Identity;
using Proto.Cluster.Partition;
using Proto.Cluster.Testing;
using Proto.Remote.GrpcNet;
using MudBlazor.Services;
using Proto.Cluster.Dashboard;
using Proto.Cluster.Seed;
using Proto.Remote;
using Proto.Remote;

var system = await GetSystem();


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(system);
builder.Services.AddSingleton(system.Cluster());
builder.Services.AddHostedService<ActorSystemHostedService>();
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton(system);
builder.Services.AddMudServices();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

async Task<ActorSystem> GetSystem()
{
    Proto.Log.SetLoggerFactory(
        LoggerFactory.Create(l => l.AddConsole().SetMinimumLevel(LogLevel.Information)));
    var port = 0;
    var advertisedHost = "localhost";
    var provider = new SeedNodeClusterProvider();
    var actorSystem = 
        new ActorSystem(ActorSystemConfig.Setup().WithDeveloperSupervisionLogging(true))
        .WithRemote(GrpcNetRemoteConfig.BindToAllInterfaces(advertisedHost, port).WithProtoMessages(SeedContractsReflection.Descriptor).WithProtoMessages(Empty.Descriptor.File))
        .WithCluster(ClusterConfig.Setup("MyCluster", provider, new PartitionIdentityLookup()));

    return actorSystem;
}

public record DummyRequest;

public record DummyResponse;

public class DummyActor : IActor
{
    public async Task ReceiveAsync(IContext context)
    {
        await Task.Delay(20);
        if (context.Message is DummyRequest)
        {
            var id = context.Get<ClusterIdentity>();
            context.Respond(new DummyResponse());
        }
    }
}