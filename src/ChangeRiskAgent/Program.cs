using ChangeRiskAgent;
using Azure.Identity;

var live = args.Contains("--live", StringComparer.Ordinal);
var diagnostics = args.Contains("--diagnostics", StringComparer.Ordinal);
var positional = args.Where(arg => !arg.StartsWith("--", StringComparison.Ordinal)).ToArray();
if (positional.Length != 1 || args.Any(arg =>
        arg.StartsWith("--", StringComparison.Ordinal) &&
        arg is not "--live" and not "--diagnostics"))
{
    Console.Error.WriteLine(
        "Usage: dotnet run --project src/ChangeRiskAgent -- [--live] [--diagnostics] CHG-1001");
    return 2;
}

try
{
    var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "change-requests.json");
    IAdvisoryAgent agent;
    if (live)
    {
        var options = FoundryOptions.FromEnvironment();
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            TenantId = options.TenantId,
            ExcludeInteractiveBrowserCredential = true
        });
        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        agent = new FoundryResponsesAgent(
            httpClient,
            credential,
            options,
            diagnostics ? new ConsoleFoundryDiagnostics() : NullFoundryDiagnostics.Instance);
    }
    else
    {
        agent = new DeterministicAdvisoryModel();
    }
    var host = new ChangeRiskAdvisor(
        new ChangeRecordTool(dataPath),
        agent);
    Console.WriteLine(await host.AssessAsync(positional[0], CancellationToken.None));
    return 0;
}
catch (ArgumentException error)
{
    Console.Error.WriteLine(error.Message);
    return 2;
}
catch (Exception)
{
    Console.Error.WriteLine("The advisory could not be completed safely.");
    return 1;
}
