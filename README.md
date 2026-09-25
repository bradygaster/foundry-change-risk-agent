# Change Risk Advisor

A standalone .NET console sample proving the complete Microsoft Foundry tool
calling lifecycle: model request, strict function call, exact host dispatch to one
typed read-only tool, tool result continuation, structured final response, and
mandatory human review.

The default deterministic agent remains network-free. `--live` uses the official
Foundry project Responses REST route with `DefaultAzureCredential` and the
`https://ai.azure.com/.default` token scope. No keys or secrets are stored.

> **IMPORTANT: Every Azure resource name, identifier, and endpoint displayed in
> this repository is fictional. It is NOT LIVE and does NOT identify a real
> Azure resource.**

## Run

Requires the stable .NET 10 LTS SDK. The repository pins SDK `10.0.301` in
`global.json`, permits later stable patches in the same feature band, and does
not opt into prerelease or .NET 11 SDKs:

```sh
dotnet --version
dotnet run --project src/ChangeRiskAgent -- CHG-1001
dotnet run --project src/ChangeRiskAgent -- CHG-9999

# Authenticated Microsoft Foundry execution
export FOUNDRY_PROJECT_ENDPOINT="https://example-foundry-account.services.ai.azure.com/api/projects/example-project"
export FOUNDRY_MODEL_DEPLOYMENT="example-model-deployment"
export AZURE_TENANT_ID="00000000-0000-4000-8000-000000000001"
az login --tenant "$AZURE_TENANT_ID"
dotnet run --project src/ChangeRiskAgent -- --live --diagnostics CHG-1001
```

The following values illustrate the expected shapes only. They are fictional,
NOT LIVE, and NOT REAL:

- Project endpoint: `https://example-foundry-account.services.ai.azure.com/api/projects/example-project`
- Project resource ID: `/subscriptions/00000000-0000-4000-8000-000000000002/resourceGroups/example-resource-group/providers/Microsoft.CognitiveServices/accounts/example-foundry-account/projects/example-project`
- Deployment: `example-model-deployment`
- Deployment resource ID: `/subscriptions/00000000-0000-4000-8000-000000000002/resourceGroups/example-resource-group/providers/Microsoft.CognitiveServices/accounts/example-foundry-account/deployments/example-model-deployment`
- Tenant: `00000000-0000-4000-8000-000000000001`

Live execution has no checked-in endpoint or deployment defaults. Set
`FOUNDRY_PROJECT_ENDPOINT` and `FOUNDRY_MODEL_DEPLOYMENT` explicitly.
`AZURE_TENANT_ID` is optional and can constrain `DefaultAzureCredential` to a
specific tenant. The endpoint is restricted to HTTPS `services.ai.azure.com`
project URLs.

## Test

```sh
# Complete offline release validation
dotnet restore ChangeRiskAgent.slnx
dotnet build ChangeRiskAgent.slnx -c Release --no-restore
dotnet run --project tests/ChangeRiskAgent.Tests -c Release --no-build
dotnet format ChangeRiskAgent.slnx --verify-no-changes --no-restore

# Network-free contract and safety tests
dotnet run --project tests/ChangeRiskAgent.Tests

# Explicitly authorized live known, unknown, and adversarial scenarios
RUN_FOUNDRY_LIVE_TESTS=1 \
  dotnet run --project tests/ChangeRiskAgent.Tests -- --live
```

Without both `--live` and `RUN_FOUNDRY_LIVE_TESTS=1`, authenticated tests report
`SKIP` or `BLOCKED` and make no model calls. Diagnostics contain only event name,
random correlation ID, duration, and HTTP status; they exclude prompts, fixture
data, tokens, and credentials.

## Safety boundary

- Only `get_change_record(change_id)` is exposed, and the host validates the tool
  name, exact user-bound ID, and call ID before dispatch.
- Exactly one tool call is permitted. The continuation exposes no tools, and a
  second function call is rejected.
- Tool output is labeled untrusted data. Record prose is excluded from risk
  evidence and the final output uses a strict JSON schema.
- The model must complete the tool-result turn, but the host canonically derives
  every safety-critical final field from the authoritative tool result. Model
  variability cannot lower risk, change the bound ID, bypass review, or inject
  final instructions.
- Responses use `store: false`; request time is bounded; failures do not expose
  service bodies, prompts, fixture contents, or credentials.
- HTTP 429 responses use at most five total attempts with capped backoff and
  explicit sanitized retry diagnostics.

## Verified API and packages

Verified on 2026-09-25 against current Microsoft documentation and an
authenticated deployment whose identifying details have been removed:

- Microsoft Foundry project endpoint route: `/openai/v1/responses`
- Microsoft Entra token scope: `https://ai.azure.com/.default`
- Deployment: identifying name and version removed
- `Azure.Identity` `1.21.0` (used)
- `Azure.AI.Projects` `2.0.1`, `Azure.AI.Extensions.OpenAI` `2.0.0`, and
  `OpenAI` `2.14.0` (verified current stable alternatives)

References:

- https://learn.microsoft.com/azure/foundry/how-to/develop/sdk-overview
- https://learn.microsoft.com/azure/foundry/openai/how-to/responses
