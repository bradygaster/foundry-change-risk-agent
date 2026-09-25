# Scenario Journal: Change Risk Advisor

## Outcome and acceptance criteria

- **Outcome:** Produce a human-reviewed change-risk advisory grounded only in a
  typed, read-only change-record tool.
- **Acceptance:** Known records are classified from retrieved fields; malformed,
  missing, failed, or cancelled lookups fail closed; no tool can mutate or deploy;
  default tests require no credentials or network.
- **Non-goals:** Automated approval, deployment, persistent agent state, hosted
  agent infrastructure, or production change-system access.

## Architecture decision

The Squad architecture gate selected one in-process advisory host and one local
function tool. It rejected Hosted Agent, Prompt Agent, multi-agent orchestration,
OpenAPI deployment, MCP, code execution, search/IQ, Toolbox, and local inference
because none earned its lifecycle or safety cost for one synthetic lookup.

The integrated sample keeps a deterministic `IAdvisoryAgent` for offline use and
adds a real Foundry Responses REST adapter authenticated by `Azure.Identity`. The
adapter uses the project endpoint, preserves stateless response items, dispatches
one host-validated function call, and requests a strict structured advisory.

## Squad activity

| Time | Member | Artifact or action | Handoff / decision | Result |
| --- | --- | --- | --- | --- |
| 2026-09-25 | Squad coordinator | Routed the full scenario to the Foundry team. | Required code, tests, evidence boundaries, and a process journal. | Full-Mode fan-out started. |
| 2026-09-25 | Architect | Wrote the Change Risk Advisor requirements, tool contract, rejected alternatives, evidence, and downstream handoffs. | Approved one ephemeral/in-process agent plus one read-only tool. | Prevented unjustified hosted and multi-agent infrastructure. |
| 2026-09-25 | Parent coordinator | Inspected the first journal and applied the shared schema. | Required scored evidence and moved work from ceremony to code. | Journal validator and summarizer became core lab tooling. |
| 2026-09-25 | Parent implementation | Added the local-first BCL implementation and five offline checks. | Left cloud SDK/runtime integration explicitly unvalidated. | Runnable sample available independently of child-session completion. |

## Evidence and assumptions

| Claim | Evidence timestamp | Authenticated scope | Status | Remaining gap |
| --- | --- | --- | --- | --- |
| A single typed local tool is sufficient for the sample behavior. | 2026-09-25 | Local repository | EVIDENCED | None for local behavior. |
| Malformed, missing, failed-test, and cancelled paths fail safely. | 2026-09-25 | Local repository | EVIDENCED | Model-service failure needs an authenticated adapter test. |
| Current Foundry Responses API can host the same contract. | 2026-09-25 | Authenticated project endpoint and `gpt-5-mini` deployment | EVIDENCED | Model output remains probabilistic and must retain host validation. |
| Local identity can authenticate without stored secrets. | 2026-09-25 | Azure CLI user token for `https://ai.azure.com/.default` | EVIDENCED | Production should use a managed identity with Foundry User at project scope. |

## Validation log

| Command or check | Result | What it proves | What it does not prove |
| --- | --- | --- | --- |
| `dotnet run --project samples/change-risk-agent/tests/ChangeRiskAgent.Tests` | PASS: 10 checks | Observable agent/tool lifecycle, allowlist and binding enforcement, one-call limit, input validation, exact lookup, conservative policy, missing evidence, human ownership, and cancellation. | Foundry SDK/API compatibility or cloud runtime behavior. |
| `dotnet run --project samples/change-risk-agent/src/ChangeRiskAgent -- CHG-1001` | PASS; low-risk advisory emitted | The documented happy path is runnable. | Model quality, latency, cost, quota, or capacity. |
| `dotnet run --project tests/ChangeRiskAgent.Tests` | PASS: 13 offline checks; authenticated suite explicitly skipped | Known, unknown, and adversarial fixtures; exact REST request/function/result/final shape; host binding; one-call limit; structured output and policy gates. | Live service availability. |
| `RUN_FOUNDRY_LIVE_TESTS=1 dotnet run --project tests/ChangeRiskAgent.Tests -- --live` | PASS: CHG-1001 low, CHG-9999 insufficient-evidence, CHG-1003 high; human review required for all | Authenticated project endpoint, real `gpt-5-mini` function call, exact host dispatch, tool continuation, final response, injection resistance, and bounded 429 recovery. | Production managed-identity RBAC and production change-system integration. |
| Two consecutive `Release` live matrices after nondeterministic model advisory | PASS: six scenarios total; known, unknown, and adversarial classifications were stable while exercising bounded 429 recovery | Model tool-result turn remains required; host canonicalizes safety-critical fields from `ToolResult`, removing probabilistic policy failures without weakening binding, injection, or human-review controls. | Model-authored rationale is intentionally not rendered because it is non-authoritative. |

## Authenticated resource evidence

- Foundry project: `/subscriptions/104482b7-4580-4de0-9453-0fc78df0b80e/resourceGroups/rg-squad-imagegen/providers/Microsoft.CognitiveServices/accounts/squad-imagegen-swc-1ntj32/projects/squad-imagegen-swc-1ntj32-proj`
- Model deployment: `/subscriptions/104482b7-4580-4de0-9453-0fc78df0b80e/resourceGroups/rg-squad-imagegen/providers/Microsoft.CognitiveServices/accounts/squad-imagegen-swc-1ntj32/deployments/gpt-5-mini`
- Deployment evidence: `gpt-5-mini` version `2025-08-07`, `GlobalStandard`, capacity `3`, provisioning state `Succeeded`
- Token audience: `https://ai.azure.com/.default`
- No resources were created or changed because the existing project and deployment satisfied the scenario.

## Friction and recovery

| Problem | Effect | Recovery | Root cause | Preventive improvement |
| --- | --- | --- | --- | --- |
| Full Squad work began with several minutes of architecture ceremony. | No application files appeared during the initial target window. | Parent issued an implementation timebox and created a local-first fallback seam. | Full Mode had a 40-60 second target but no first-artifact rule. | Added a 60-second implementation-artifact requirement to response mode and routing. |
| The first journal used custom headings and no comparison scores. | Cross-scenario analysis was not mechanically reliable. | Added validator, summarizer, exact headings, and scored evidence. | Handoff depended on prose rather than an executable schema. | Keep journal validation in repository tests and run it before integration. |
| The local machine only had the .NET 10 SDK template. | A requested .NET 8 scaffold failed. | Used .NET 10, which satisfies the repository's .NET 8-or-later requirement. | Installed SDK/template availability differed from the initial assumption. | Discover SDKs before selecting an exact target framework. |
| Initial fallback called the repository before the agent requested a tool. | The code demonstrated a safe tool contract but not an agent/tool-call lifecycle. | Added explicit request, allowlisted host dispatch, typed tool-result continuation, and final response turns. | The architecture contract did not make lifecycle evidence an implementation gate. | Reviewer now rejects direct repository calls presented as agent evidence. |

## What Squad did well

- The architecture sequence forced an explicit AI/agent/tool justification.
- The Architect rejected nine larger alternatives and defined a narrow typed tool.
- Evidence discipline prevented local tests from becoming cloud-availability claims.
- The handoff exposed safety requirements that became deterministic tests.

## Core FoundrySquad improvements

| Improvement | Core surface | Evidence | Impact (1-5) | Effort (1-5) | Confidence (1-3) |
| --- | --- | --- | --- | --- | --- |
| Timebox Full Mode to a first implementation artifact within 60 seconds. | coordinator response mode and routing | All initial sample sessions exceeded the target before code; the tool session produced architecture first. | 5 | 2 | 3 |
| Enforce experiment journals with an executable schema and summarizer. | templates and validation tooling | The first journal omitted shared headings and scores. | 4 | 2 | 3 |
| Add tool execution location, side effects, reachability, timeout, and logging fields. | architecture decision and reviewer gate | The scenario rejected OpenAPI because Foundry cannot safely assume developer localhost reachability. | 4 | 2 | 3 |
| Separate offline contract tests from opt-in authenticated smoke tests. | quality charter and sample template | Five useful checks ran without credentials while cloud runtime remained unknown. | 5 | 2 | 3 |
| Require observable request-dispatch-result-final lifecycle evidence. | reviewer gate | The first fallback was a rules engine with a model-shaped interface, not an agent-requested tool call. | 5 | 2 | 3 |

## Comparison score

| Dimension | Score (1-5) | Evidence |
| --- | --- | --- |
| Architecture economy | 5 | One host and one read-only tool; larger Foundry surfaces were explicitly rejected. |
| Routing accuracy | 4 | The right specialists were selected, but broad fan-out delayed implementation. |
| Handoff quality | 4 | Architecture handoffs were detailed; the initial journal schema required correction. |
| Evidence discipline | 5 | Local, documentation, authenticated, and runtime evidence stayed separate. |
| Implementation usefulness | 4 | The sample runs and tests offline; the authenticated Foundry adapter remains pending. |
| Quality coverage | 5 | Ten checks cover lifecycle, host enforcement, policy, failure, and cancellation paths; live model grounding remains untested. |
| Security and RAI | 5 | Read-only synthetic data, strict IDs, fail-closed output, no secrets, and human ownership. |
| Ceremony efficiency | 2 | Architecture quality was high, but implementation started well after the Full-Mode target. |
| Recovery behavior | 5 | The coordinator timeboxed work, added tooling, adapted the SDK target, and produced a runnable fallback. |
