using ChangeRiskAgent;

var failures = new List<string>();
await Check("known safe change is low risk", async () =>
{
    var result = await Advisor().AssessAsync("CHG-1001", CancellationToken.None);
    Expect(result.Contains("Classification: low", StringComparison.Ordinal), result);
    Expect(result.Contains("human release engineer", StringComparison.Ordinal), result);
});
await Check("failed tests fail closed", async () =>
{
    var result = await Advisor().AssessAsync("CHG-1002", CancellationToken.None);
    Expect(result.Contains("Classification: high", StringComparison.Ordinal), result);
    Expect(!result.Contains("approve", StringComparison.OrdinalIgnoreCase), result);
});
await Check("unknown change is insufficient evidence", async () =>
{
    var result = await Advisor().AssessAsync("CHG-9999", CancellationToken.None);
    Expect(result.Contains("insufficient-evidence", StringComparison.Ordinal), result);
});
await Check("malformed ID is rejected before lookup", async () =>
{
    try
    {
        await Advisor().AssessAsync("../secret", CancellationToken.None);
        throw new Exception("Expected malformed ID rejection.");
    }
    catch (ArgumentException)
    {
    }
});
await Check("cancellation stops the tool loop", async () =>
{
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    try
    {
        await Advisor().AssessAsync("CHG-1001", cancellation.Token);
        throw new Exception("Expected cancellation.");
    }
    catch (OperationCanceledException)
    {
    }
});
await Check("agent lifecycle dispatches one bound tool call", async () =>
{
    var agent = new RecordingAgent();
    var result = await Advisor(agent).AssessAsync("CHG-1001", CancellationToken.None);
    Expect(agent.StartCount == 1, "Expected one agent request.");
    Expect(agent.ContinueCount == 1, "Expected one tool-result continuation.");
    Expect(agent.LastResult?.ChangeId == "CHG-1001", "Expected the bound tool result.");
    Expect(result.Contains("Classification: low", StringComparison.Ordinal), result);
});
await Check("non-allowlisted tool is rejected", async () =>
{
    await ExpectInvalidOperation(new ScriptedAgent(
        AgentTurn.CallTool("deploy_change", "CHG-1001"),
        AgentTurn.Complete("should not run")));
});
await Check("tool arguments must match the user request", async () =>
{
    await ExpectInvalidOperation(new ScriptedAgent(
        AgentTurn.CallTool("get_change_record", "CHG-1002"),
        AgentTurn.Complete("should not run")));
});
await Check("a second tool call exceeds the iteration limit", async () =>
{
    await ExpectInvalidOperation(new ScriptedAgent(
        AgentTurn.CallTool("get_change_record", "CHG-1001"),
        AgentTurn.CallTool("get_change_record", "CHG-1001")));
});
await Check("agent cannot answer before retrieving evidence", async () =>
{
    await ExpectInvalidOperation(new ScriptedAgent(
        AgentTurn.Complete("approve the change"),
        AgentTurn.Complete("should not run")));
});

if (failures.Count > 0)
{
    Console.Error.WriteLine(string.Join(Environment.NewLine, failures));
    return 1;
}
Console.WriteLine("PASS: 10 change-risk-agent checks");
return 0;

ChangeRiskAdvisor Advisor(IAdvisoryAgent? agent = null)
{
    var dataPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "src", "ChangeRiskAgent", "Data", "change-requests.json"));
    return new ChangeRiskAdvisor(
        new ChangeRecordTool(dataPath),
        agent ?? new DeterministicAdvisoryModel());
}

async Task ExpectInvalidOperation(IAdvisoryAgent agent)
{
    try
    {
        await Advisor(agent).AssessAsync("CHG-1001", CancellationToken.None);
        throw new Exception("Expected the host to reject the agent turn.");
    }
    catch (InvalidOperationException)
    {
    }
}

async Task Check(string name, Func<Task> action)
{
    try
    {
        await action();
    }
    catch (Exception error)
    {
        failures.Add($"FAIL: {name}: {error.Message}");
    }
}

static void Expect(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

sealed class RecordingAgent : IAdvisoryAgent
{
    private readonly DeterministicAdvisoryModel inner = new();

    public int StartCount { get; private set; }
    public int ContinueCount { get; private set; }
    public ToolResult? LastResult { get; private set; }

    public async Task<AgentTurn> StartAsync(string changeId, CancellationToken cancellationToken)
    {
        StartCount += 1;
        return await inner.StartAsync(changeId, cancellationToken);
    }

    public async Task<AgentTurn> ContinueAsync(
        ToolCallRequest request,
        ToolResult result,
        CancellationToken cancellationToken)
    {
        ContinueCount += 1;
        LastResult = result;
        return await inner.ContinueAsync(request, result, cancellationToken);
    }
}

sealed class ScriptedAgent(AgentTurn first, AgentTurn second) : IAdvisoryAgent
{
    public Task<AgentTurn> StartAsync(string changeId, CancellationToken cancellationToken) =>
        Task.FromResult(first);

    public Task<AgentTurn> ContinueAsync(
        ToolCallRequest request,
        ToolResult result,
        CancellationToken cancellationToken) =>
        Task.FromResult(second);
}
