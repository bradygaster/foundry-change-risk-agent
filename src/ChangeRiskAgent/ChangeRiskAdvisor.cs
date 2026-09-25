using System.Text.Json;

namespace ChangeRiskAgent;

public sealed record ChangeRecord(
    string Id,
    string Summary,
    string Service,
    string Environment,
    string TestStatus,
    bool RollbackPlanPresent,
    bool ObservabilityPlanPresent,
    string CustomerImpact);

public sealed record ToolResult(string Status, string ChangeId, ChangeRecord? Change, string? Message);

public sealed record ToolCallRequest(string ToolName, string ChangeId);

public sealed record AgentTurn(ToolCallRequest? ToolCall, string? FinalResponse)
{
    public static AgentTurn CallTool(string toolName, string changeId) =>
        new(new ToolCallRequest(toolName, changeId), null);

    public static AgentTurn Complete(string response) => new(null, response);
}

public interface IAdvisoryAgent
{
    Task<AgentTurn> StartAsync(string changeId, CancellationToken cancellationToken);
    Task<AgentTurn> ContinueAsync(
        ToolCallRequest request,
        ToolResult result,
        CancellationToken cancellationToken);
}

public sealed class ChangeRiskAdvisor(ChangeRecordTool tool, IAdvisoryAgent agent)
{
    public async Task<string> AssessAsync(string changeId, CancellationToken cancellationToken)
    {
        var firstTurn = await agent.StartAsync(changeId, cancellationToken);
        var request = firstTurn.ToolCall ??
            throw new InvalidOperationException("The agent must request authoritative evidence before answering.");
        if (firstTurn.FinalResponse is not null)
        {
            throw new InvalidOperationException("The agent cannot answer before the tool result.");
        }
        if (!string.Equals(request.ToolName, "get_change_record", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The agent requested a tool that is not allowlisted.");
        }
        if (!string.Equals(request.ChangeId.Trim(), changeId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The tool request does not match the user's change ID.");
        }

        var evidence = await tool.ExecuteAsync(request.ChangeId, cancellationToken);
        var finalTurn = await agent.ContinueAsync(request, evidence, cancellationToken);
        if (finalTurn.ToolCall is not null)
        {
            throw new InvalidOperationException("The agent exceeded the one-tool-call limit.");
        }
        return finalTurn.FinalResponse ??
            throw new InvalidOperationException("The agent did not produce a final advisory.");
    }
}

public sealed class DeterministicAdvisoryModel : IAdvisoryAgent
{
    public Task<AgentTurn> StartAsync(string changeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(AgentTurn.CallTool("get_change_record", changeId));
    }

    public Task<AgentTurn> ContinueAsync(
        ToolCallRequest request,
        ToolResult evidence,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (evidence.Status != "found" || evidence.Change is null)
        {
            return Task.FromResult(AgentTurn.Complete(string.Join(Environment.NewLine,
                $"Change: {evidence.ChangeId}",
                "Classification: insufficient-evidence",
                "Factors: no authoritative change record was retrieved",
                $"Missing evidence: {evidence.Message ?? "change record"}",
                "Next action: a human release engineer must verify the change record.")));
        }

        var change = evidence.Change;
        var factors = new List<string>();
        var classification = "low";
        if (!string.Equals(change.TestStatus, "passed", StringComparison.OrdinalIgnoreCase))
        {
            classification = "high";
            factors.Add($"test_status={change.TestStatus}");
        }
        if (!change.RollbackPlanPresent)
        {
            classification = classification == "high" ? "high" : "medium";
            factors.Add("rollback_plan_present=false");
        }
        if (!change.ObservabilityPlanPresent)
        {
            classification = classification == "high" ? "high" : "medium";
            factors.Add("observability_plan_present=false");
        }
        if (factors.Count == 0)
        {
            factors.Add("tests passed");
            factors.Add("rollback plan present");
            factors.Add("observability plan present");
        }

        return Task.FromResult(AgentTurn.Complete(string.Join(Environment.NewLine,
            $"Change: {change.Id}",
            $"Classification: {classification}",
            $"Factors: {string.Join("; ", factors)}",
            $"Missing evidence: {(classification == "low" ? "none in the synthetic record" : "resolve the cited gaps")}",
            "Next action: a human release engineer must review this advisory before deployment.")));
    }
}

public sealed class ChangeRecordTool
{
    private readonly IReadOnlyDictionary<string, ChangeRecord> records;

    public ChangeRecordTool(string dataPath)
    {
        const int maximumFixtureBytes = 256 * 1024;
        var info = new FileInfo(dataPath);
        if (!info.Exists) throw new FileNotFoundException("Synthetic change data is missing.", dataPath);
        if (info.Length > maximumFixtureBytes) throw new InvalidDataException("Synthetic change data is too large.");
        var loaded = JsonSerializer.Deserialize<List<ChangeRecord>>(
            File.ReadAllText(dataPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        records = loaded.ToDictionary(record => Normalize(record.Id), StringComparer.OrdinalIgnoreCase);
    }

    public Task<ToolResult> ExecuteAsync(string changeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = Normalize(changeId);
        if (records.TryGetValue(normalized, out var record))
        {
            return Task.FromResult(new ToolResult("found", normalized, record, null));
        }
        return Task.FromResult(new ToolResult(
            "not_found",
            normalized,
            null,
            "No synthetic change record exists for the requested ID."));
    }

    private static string Normalize(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length != 8 ||
            !normalized.StartsWith("CHG-", StringComparison.Ordinal) ||
            !normalized.AsSpan(4).ToString().All(char.IsAsciiDigit))
        {
            throw new ArgumentException("Change ID must match CHG-0000.", nameof(value));
        }
        return normalized;
    }
}
