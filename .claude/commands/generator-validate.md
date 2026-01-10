---
name: generator-validate
description: Pipeline validation for generator changes - staged validation with gates
arguments:
  - name: quick
    description: "Quick mode - only 3 agents instead of 6"
    type: boolean
    default: false
---

# Generator Change Validation Pipeline

Multi-stage validation with parallel agents within each stage.

{{#if quick}}
## QUICK MODE (3 agents)
{{else}}
## FULL MODE (6 agents across 2 stages)
{{/if}}

---

{{#if quick}}
## Quick Validation - 3 Parallel Agents

Launch ALL 3 in ONE message:

### Agent 1: Change Impact Analysis
```yaml
subagent_type: feature-dev:code-explorer
prompt: |
  Analyze impact of recent generator changes.

  1. Find modified files in src/ErrorOr.Endpoints/Generators/
  2. Trace all callers/dependents of modified code
  3. Identify affected generated output
  4. List tests that should be run

  Output: Impact report with affected files list
```

### Agent 2: Build & Test
```yaml
subagent_type: dotnet-mtp-advisor
prompt: |
  Validate generator changes compile and pass tests.

  1. Run: dotnet build src/ErrorOr.Endpoints/
  2. Run: dotnet test tests/ErrorOr.Endpoints.Tests/
  3. Report any failures with details
  4. Check for new warnings

  Output: Build/test results summary
```

### Agent 3: Incremental Cache Check
```yaml
subagent_type: deep-debugger
prompt: |
  Verify incremental generator caching still works.

  1. Check ForAttributeWithMetadataName predicates unchanged
  2. Verify model equality implementations
  3. Look for cache-breaking changes (closures, new state)

  Output: Cache integrity assessment
```

{{else}}
## Stage 1: Analysis (3 Parallel Agents)

Launch these 3 FIRST, then wait for completion before Stage 2.

### Agent 1A: Change Impact Analysis
```yaml
subagent_type: feature-dev:code-explorer
prompt: |
  Analyze impact of generator changes.

  1. Identify modified generator files
  2. Trace dependencies and dependents
  3. Map affected code paths
  4. List all potentially affected outputs

  Output: Impact analysis with file list
```

### Agent 1B: API Surface Check
```yaml
subagent_type: feature-dev:code-architect
prompt: |
  Check for API surface changes.

  1. Compare public APIs before/after
  2. Identify breaking changes
  3. Check attribute contracts
  4. Verify generated code structure unchanged

  Output: API compatibility report
```

### Agent 1C: Test Identification
```yaml
subagent_type: Explore
prompt: |
  Find all tests affected by generator changes.

  1. Map test files to generator components
  2. Identify snapshot tests that may need updates
  3. Find integration tests to run
  4. List any missing test coverage

  Output: Test execution plan
```

---

## GATE: Stage 1 → Stage 2

Wait for ALL Stage 1 agents to complete.
Merge their outputs:
- Combined affected files list
- API change summary
- Test execution plan

If BLOCKING issues found in Stage 1, STOP and report.

---

## Stage 2: Validation (3 Parallel Agents)

Launch these 3 AFTER Stage 1 completes:

### Agent 2A: Build Validation
```yaml
subagent_type: dotnet-mtp-advisor
prompt: |
  Full build validation.

  1. Clean build: dotnet clean && dotnet build
  2. Check for warnings (treat as errors)
  3. Verify all projects compile
  4. Check package restore

  Output: Build report with any issues
```

### Agent 2B: Test Execution
```yaml
subagent_type: dotnet-mtp-advisor
prompt: |
  Run affected tests from Stage 1 analysis.

  1. Run unit tests: dotnet test tests/ErrorOr.Endpoints.Tests/
  2. Run integration tests if identified
  3. Update snapshots if needed (report which ones)
  4. Report failures with details

  Output: Test results with any failures
```

### Agent 2C: Generated Code Diff
```yaml
subagent_type: deep-debugger
prompt: |
  Compare generated code before/after changes.

  1. Build sample project
  2. Capture generated code output
  3. Diff against expected/baseline
  4. Identify unexpected changes

  Output: Generated code diff analysis
```
{{/if}}

---

## Final Validation Report

After all agents complete:

```markdown
# Generator Validation Report

## Summary
- **Mode:** {{#if quick}}Quick{{else}}Full{{/if}}
- **Result:** [PASS/FAIL]
- **Duration:** [time]

## Impact Analysis
[From analysis agents]

## Build Status
- **Compilation:** [PASS/FAIL]
- **Warnings:** [count]
- **Errors:** [count]

## Test Results
- **Passed:** [count]
- **Failed:** [count]
- **Skipped:** [count]

## Generated Code
- **Changes Detected:** [yes/no]
- **Expected Changes:** [yes/no]
- **Snapshot Updates Needed:** [list]

## Blocking Issues
[Any issues that must be fixed]

## Recommendations
[What to do next]
```

---

## Usage

Quick validation (3 agents):
```
/generator-validate quick=true
```

Full validation (6 agents, 2 stages):
```
/generator-validate
```
