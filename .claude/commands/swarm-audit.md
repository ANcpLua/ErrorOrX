---
name: swarm-audit
description: Launch parallel agent swarm for comprehensive ErrorOrX codebase audit
arguments:
  - name: mode
    description: "Audit mode: full|generators|analyzers|tests|tournament"
    default: "full"
---

# ErrorOrX Swarm Audit

You are orchestrating a parallel agent swarm to audit the ErrorOrX codebase.

## CRITICAL: Maximum Parallelism

**You MUST launch ALL agents in a SINGLE message with multiple Task tool calls.**
This ensures true parallel execution where each agent gets its own 200k context window.

DO NOT:
- Launch agents one by one
- Wait for one agent before launching the next
- Use sequential tool calls

DO:
- Put ALL Task tool invocations in ONE response
- Let them run independently
- Aggregate results after ALL complete

---

## Mode: {{ mode }}

{{#if (eq mode "full")}}
## FULL AUDIT - Launch 8 Agents in Parallel

Launch ALL of these agents in a SINGLE message:

### Agent 1: Core API Mapper
```
subagent_type: feature-dev:code-explorer
prompt: |
  TERRITORY: src/ErrorOr.Core/**

  Your mission: Map the complete public API surface of ErrorOr.Core.

  Deliverables:
  1. List ALL public types, methods, properties
  2. Identify dead code (unreachable public members)
  3. Find API inconsistencies (naming, patterns)
  4. Check XML doc coverage

  Output format: Markdown report with file:line references
  DO NOT modify any files - this is read-only analysis.
```

### Agent 2: Generator Architecture Analyst
```
subagent_type: feature-dev:code-architect
prompt: |
  TERRITORY: src/ErrorOr.Endpoints/Generators/**

  Your mission: Analyze the Roslyn source generator architecture.

  Focus areas:
  1. Incremental generator patterns (ForAttributeWithMetadataName usage)
  2. Model extraction pipeline (syntax → semantic → model)
  3. Code emission patterns (StringBuilder vs IndentedTextWriter)
  4. Caching correctness (are we caching too much/too little?)

  Deliverables:
  - Architecture diagram (text-based)
  - Pattern violations list
  - Optimization opportunities

  DO NOT modify any files - this is read-only analysis.
```

### Agent 3: Analyzer Correctness Reviewer
```
subagent_type: feature-dev:code-reviewer
prompt: |
  TERRITORY: src/ErrorOr.Endpoints/Analyzers/**

  Your mission: Review ALL 26 diagnostics (EOE001-EOE026) for correctness.

  For EACH diagnostic:
  1. Read the analyzer implementation
  2. Check: Can it produce false positives? (valid code flagged)
  3. Check: Can it produce false negatives? (bad code missed)
  4. Check: Is the diagnostic message clear?
  5. Check: Is the severity appropriate?

  Output: Table with columns [ID, Name, FP Risk, FN Risk, Issues]

  DO NOT modify any files - this is read-only analysis.
```

### Agent 4: Test Coverage Auditor
```
subagent_type: dotnet-mtp-advisor
prompt: |
  TERRITORY: tests/**

  Your mission: Audit test coverage and MTP configuration.

  Tasks:
  1. Run: dotnet test --list-tests (count test cases)
  2. Identify untested generators/analyzers
  3. Check xUnit v3 + MTP configuration correctness
  4. Find flaky test patterns (timing, ordering dependencies)
  5. Check snapshot test coverage (Verify.cs)

  Deliverables:
  - Coverage gap report
  - MTP config recommendations
  - Flaky test candidates

  You MAY run dotnet commands for analysis.
```

### Agent 5: Build System Validator
```
subagent_type: dotnet-mtp-advisor
prompt: |
  TERRITORY: *.csproj, Directory.*.props, *.slnx, Directory.Packages.props

  Your mission: Validate build system integrity.

  Checks:
  1. Central Package Management (CPM) compliance
  2. Version.props symlink correctness
  3. TFM consistency across projects
  4. No PackageReference versions in csproj (must be in Directory.Packages.props)
  5. PrivateAssets/IncludeAssets correctness for analyzers

  Run: dotnet build --no-restore to verify

  Deliverables: Build system health report with violations
```

### Agent 6: OpenAPI Pipeline Validator
```
subagent_type: xml-pastry-chef
prompt: |
  TERRITORY: samples/ErrorOr.Endpoints.Sample/**

  Your mission: Validate OpenAPI documentation pipeline.

  Checks:
  1. XML doc comments → OpenAPI descriptions flow
  2. Results<Success, NotFound, BadRequest> → correct response codes
  3. ErrorOr endpoint error responses in OpenAPI spec
  4. AddOpenApi() interceptor configuration

  Run the sample and inspect generated openapi.json

  Deliverables: OpenAPI pipeline health report
```

### Agent 7: Incremental Cache Validator
```
subagent_type: deep-debugger
prompt: |
  TERRITORY: src/ErrorOr.Endpoints/Generators/**

  Your mission: Validate incremental generator caching behavior.

  Critical tests:
  1. Modify endpoint method body → generator should NOT re-run
  2. Modify [ErrorOrEndpoint] attribute → generator SHOULD re-run
  3. Modify unrelated file → generator should NOT re-run
  4. Add new endpoint → only NEW endpoint generated, others cached

  Method:
  - Read generator code, trace caching logic
  - Identify cache key composition
  - Find potential cache invalidation bugs

  DO NOT modify source files permanently.
  Deliverables: Cache correctness report with any bugs found
```

### Agent 8: Legacy Cleanup Scout
```
subagent_type: cleanup-specialist
prompt: |
  TERRITORY: Entire repository

  Your mission: Identify legacy/duplicate code for cleanup.

  Known issues to verify:
  1. ErrorOr.Core/ vs src/ErrorOr.Core/ (duplicate locations?)
  2. Old MinimalApi folders vs new structure
  3. Unused test fixtures
  4. Dead configuration files
  5. Orphaned documentation

  Deliverables:
  - List of files/folders safe to delete
  - Migration plan for any remaining legacy code
  - Estimated cleanup effort

  DO NOT delete anything - just report.
```

---

## Aggregation Phase

After ALL 8 agents complete, create:

### SWARM-AUDIT-REPORT.md

```markdown
# ErrorOrX Swarm Audit Report
Generated: [timestamp]
Mode: full
Agents: 8

## Executive Summary
[1-2 paragraph overview of findings]

## Critical Issues (Blocking)
[Issues that must be fixed before release]

## High Priority Issues
[Should be fixed soon]

## Medium Priority Issues
[Technical debt to address]

## Low Priority / Suggestions
[Nice to have improvements]

## Agent Reports

### 1. Core API Mapper
[Agent 1 findings]

### 2. Generator Architecture
[Agent 2 findings]

... [etc for all 8]

## Recommended Actions
1. [Prioritized action items]
```
{{/if}}

{{#if (eq mode "tournament")}}
## TOURNAMENT MODE - Competitive Review (4 Agents)

Launch ALL agents in a SINGLE message. They compete for points.

### Scoring Rules
- CRITICAL issue: 5 points
- HIGH issue: 3 points
- MEDIUM issue: 1 point
- Nitpick/style: -2 points (penalty!)
- Duplicate of another agent: 0 points

### Agent Alpha: Architecture Hunter
```
subagent_type: framework-migration:architect-review
prompt: |
  You are competing against 3 other agents. Most valid issues wins.

  FOCUS: Architecture violations only
  - SOLID violations
  - Layer breaches (generator calling analyzer internals?)
  - Dependency direction violations
  - God classes / methods

  TERRITORY: src/**

  Rules:
  - Every issue needs file:line proof
  - Nitpicks = -2 points, be careful
  - Quality over quantity

  Output: Numbered list with [CRITICAL/HIGH/MED] severity
```

### Agent Beta: Bug Hunter
```
subagent_type: deep-debugger
prompt: |
  You are competing against 3 other agents. Most valid issues wins.

  FOCUS: Bugs only (not style)
  - Null reference risks
  - Race conditions
  - Off-by-one errors
  - Exception handling gaps
  - Roslyn symbol resolution bugs

  TERRITORY: src/ErrorOr.Endpoints/**

  Rules:
  - Show reproduction path for each bug
  - Nitpicks = -2 points
  - Crash bugs = 5 points

  Output: Numbered list with severity and repro steps
```

### Agent Gamma: Security Auditor
```
subagent_type: feature-dev:code-reviewer
prompt: |
  You are competing against 3 other agents. Most valid issues wins.

  FOCUS: Security issues in generated code
  - Injection risks in emitted code
  - Sensitive data exposure
  - Unsafe string interpolation
  - Missing input validation in generators

  TERRITORY: **/*Emitter*.cs, **/*Generator*.cs

  Rules:
  - Security issues only, not style
  - Must show attack vector
  - False security alarms = -2 points

  Output: Numbered list with attack scenario
```

### Agent Delta: API Contract Guardian
```
subagent_type: feature-dev:code-explorer
prompt: |
  You are competing against 3 other agents. Most valid issues wins.

  FOCUS: Breaking changes and API contract violations
  - Binary breaking changes vs v3.0
  - Source breaking changes
  - Behavioral changes without documentation
  - Missing [Obsolete] on deprecated APIs

  TERRITORY: src/ErrorOr.Core/**, public APIs

  Rules:
  - Must prove with before/after comparison
  - Intended changes don't count
  - Undocumented breaks = 5 points

  Output: Numbered list with breaking change details
```

---

## Tournament Judging

After all 4 agents complete:

1. Deduplicate findings (first reporter gets credit)
2. Validate each issue (is it real?)
3. Apply scoring
4. Declare winner
5. Create merged report with attribution
{{/if}}

{{#if (eq mode "generators")}}
## GENERATORS FOCUS - 4 Parallel Agents

### Agent 1: ErrorOrEndpointGenerator Deep Dive
```
subagent_type: feature-dev:code-explorer
prompt: |
  Deep analysis of ErrorOrEndpointGenerator.cs

  Trace complete flow:
  1. Initialize() → what's registered?
  2. Predicate filtering → what triggers generation?
  3. Transform → model extraction logic
  4. Output → code emission

  Identify: Complexity hotspots, potential bugs, optimization opportunities
```

### Agent 2: Model Extraction Review
```
subagent_type: feature-dev:code-architect
prompt: |
  Review all *Model.cs and *Extractor.cs files

  Focus:
  1. Are models immutable? (they should be for caching)
  2. Equality implementation correct?
  3. Nullable handling consistent?
  4. Symbol resolution defensive?
```

### Agent 3: Code Emission Quality
```
subagent_type: feature-dev:code-reviewer
prompt: |
  Review all *Emitter.cs files

  Focus:
  1. Generated code readability
  2. Proper indentation handling
  3. No string allocation in hot paths
  4. Correct using statements emitted
```

### Agent 4: Incremental Correctness
```
subagent_type: deep-debugger
prompt: |
  Verify incremental generator correctness

  Check:
  1. ForAttributeWithMetadataName predicates
  2. Cache key composition
  3. Equality comparers on models
  4. No closure captures breaking caching
```
{{/if}}

{{#if (eq mode "analyzers")}}
## ANALYZERS FOCUS - 4 Parallel Agents

Split the 26 diagnostics across 4 agents:

### Agent 1: EOE001-EOE007
```
subagent_type: feature-dev:code-reviewer
prompt: Review analyzers EOE001-EOE007 for correctness, false positives, false negatives.
```

### Agent 2: EOE008-EOE014
```
subagent_type: feature-dev:code-reviewer
prompt: Review analyzers EOE008-EOE014 for correctness, false positives, false negatives.
```

### Agent 3: EOE015-EOE021
```
subagent_type: feature-dev:code-reviewer
prompt: Review analyzers EOE015-EOE021 for correctness, false positives, false negatives.
```

### Agent 4: EOE022-EOE026
```
subagent_type: feature-dev:code-reviewer
prompt: Review analyzers EOE022-EOE026 for correctness, false positives, false negatives.
```
{{/if}}

{{#if (eq mode "tests")}}
## TESTS FOCUS - 3 Parallel Agents

### Agent 1: Unit Test Coverage
```
subagent_type: dotnet-mtp-advisor
prompt: |
  Analyze unit test coverage in tests/ErrorOr.Endpoints.Tests/
  Find untested code paths in generators and analyzers.
```

### Agent 2: Integration Test Quality
```
subagent_type: feature-dev:code-reviewer
prompt: |
  Review integration/snapshot tests for completeness.
  Are edge cases covered? Are snapshots up to date?
```

### Agent 3: Test Infrastructure
```
subagent_type: deep-debugger
prompt: |
  Review test infrastructure: fixtures, helpers, MTP config.
  Find potential flaky test patterns.
```
{{/if}}

---

## Execution Reminder

**LAUNCH ALL AGENTS IN ONE MESSAGE.**

Example (for full mode):
```
<Task agent 1>
<Task agent 2>
<Task agent 3>
<Task agent 4>
<Task agent 5>
<Task agent 6>
<Task agent 7>
<Task agent 8>
```

All in ONE response = true parallelism.
