using ChangeRiskAgent;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: dotnet run --project src/ChangeRiskAgent -- CHG-1001");
    return 2;
}

try
{
    var dataPath = Path.Combine(AppContext.BaseDirectory, "Data", "change-requests.json");
    var host = new ChangeRiskAdvisor(
        new ChangeRecordTool(dataPath),
        new DeterministicAdvisoryModel());
    Console.WriteLine(await host.AssessAsync(args[0], CancellationToken.None));
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
