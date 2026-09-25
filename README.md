# Change Risk Advisor

A standalone .NET console sample proving the complete Microsoft Foundry tool
calling lifecycle: model request, strict function call, exact host dispatch to one
typed read-only tool, tool result continuation, structured final response, and
mandatory human review.

The default deterministic agent remains network-free. `--live` uses the official
Foundry project Responses REST route with `DefaultAzureCredential` and the
`https://ai.azure.com/.default` token scope. No keys or secrets are stored.

## Run

Requires .NET 10 or later for this checked-in project:

```sh
dotnet run --project src/ChangeRiskAgent -- CHG-1001
dotnet run --project src/ChangeRiskAgent -- CHG-9999

# Authenticated Microsoft Foundry execution
az login --tenant 72f988bf-86f1-41af-91ab-2d7cd011db47
az account set --subscription 104482b7-4580-4de0-9453-0fc78df0b80e
dotnet run --project src/ChangeRiskAgent -- --live --diagnostics CHG-1001
```

The checked-in non-secret defaults target:

- Project endpoint: `https://squad-imagegen-swc-1ntj32.services.ai.azure.com/api/projects/squad-imagegen-swc-1ntj32-proj`
- Project resource ID: `/subscriptions/104482b7-4580-4de0-9453-0fc78df0b80e/resourceGroups/rg-squad-imagegen/providers/Microsoft.CognitiveServices/accounts/squad-imagegen-swc-1ntj32/projects/squad-imagegen-swc-1ntj32-proj`
- Deployment: `gpt-5-mini`
- Deployment resource ID: `/subscriptions/104482b7-4580-4de0-9453-0fc78df0b80e/resourceGroups/rg-squad-imagegen/providers/Microsoft.CognitiveServices/accounts/squad-imagegen-swc-1ntj32/deployments/gpt-5-mini`
- Tenant: `72f988bf-86f1-41af-91ab-2d7cd011db47`

Override them with `FOUNDRY_PROJECT_ENDPOINT`, `FOUNDRY_MODEL_DEPLOYMENT`, and
`AZURE_TENANT_ID`. The endpoint is restricted to HTTPS `services.ai.azure.com`
project URLs. The current project and deployment are sufficient; no resource
creation is required.

## Test

```sh
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
- The host independently validates the risk classification and human-review
  requirement before rendering output.
- Responses use `store: false`; request time is bounded; failures do not expose
  service bodies, prompts, fixture contents, or credentials.
- HTTP 429 responses use at most five total attempts with capped backoff and
  explicit sanitized retry diagnostics.

## Verified API and packages

Verified on 2026-09-25 against current Microsoft documentation and the live
deployment:

- Microsoft Foundry project endpoint route: `/openai/v1/responses`
- Microsoft Entra token scope: `https://ai.azure.com/.default`
- Deployment: `gpt-5-mini` version `2025-08-07`
- `Azure.Identity` `1.21.0` (used)
- `Azure.AI.Projects` `2.0.1`, `Azure.AI.Extensions.OpenAI` `2.0.0`, and
  `OpenAI` `2.14.0` (verified current stable alternatives)

References:

- https://learn.microsoft.com/azure/foundry/how-to/develop/sdk-overview
- https://learn.microsoft.com/azure/foundry/openai/how-to/responses
