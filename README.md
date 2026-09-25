# Change Risk Advisor

A local-first .NET console sample showing the smallest useful tool-using agent
shape: an observable agent request, host-validated dispatch to one typed read-only
tool, a tool-result continuation, and one final advisory.

The checked-in deterministic agent makes the default sample and tests network-free.
The seam represented by `IAdvisoryAgent` is where a Microsoft Foundry Responses
API or Agent Framework adapter can be added after current package, model, region,
quota, capacity, and runtime support are verified.

## Run

Requires .NET 10 or later for this checked-in project:

```sh
dotnet run --project src/ChangeRiskAgent -- CHG-1001
dotnet run --project src/ChangeRiskAgent -- CHG-9999
```

## Test

```sh
dotnet run --project tests/ChangeRiskAgent.Tests
```

The tests prove input validation, exact fixture lookup, observable request/tool/
result/final lifecycle, allowlisted dispatch, exact request binding, a one-call
limit, safe missing-evidence behavior, conservative risk policy, human ownership,
and cancellation. They do not prove Microsoft Foundry model/tool compatibility
or authenticated runtime behavior.

## Foundry integration boundary

An authenticated adapter should:

1. use the current Microsoft Foundry project endpoint and `DefaultAzureCredential`;
2. require HTTPS and validate configuration before requests;
3. expose only `get_change_record(change_id)` as a read-only function tool;
4. bound model/tool iterations and request duration;
5. return the same safe `insufficient-evidence` outcome on tool or model failure;
6. log correlation, duration, tool name, and error category without prompt,
   fixture, token, or credential contents.

No cloud adapter is included because this lab did not have authenticated project,
deployment, quota, capacity, or runtime evidence. The Squad-led branch separately
tests the current SDK integration and records that evidence boundary.
