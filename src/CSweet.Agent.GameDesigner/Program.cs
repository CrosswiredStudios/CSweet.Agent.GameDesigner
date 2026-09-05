using CSweet.Agent.GameDesigner;
using CSweet.Agent.SDK;
using Microsoft.Extensions.Hosting;

if (args.Contains("--self-test", StringComparer.Ordinal))
{
    var agent = new VideoGameDesignerAgent();
    if (agent.AgentId != "com.csweet.video-game-designer" || agent.Version != "2.1.1")
        throw new InvalidOperationException("Video Game Designer identity self-test failed.");
    Console.WriteLine($"{agent.AgentId} {agent.Version} self-test passed.");
    return;
}

var builder = Host.CreateApplicationBuilder(args);
builder.AddCSweetAgent<VideoGameDesignerAgent>();
await builder.Build().RunAsync();
