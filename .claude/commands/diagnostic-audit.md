---
name: diagnostic-audit
description: Deep parallel audit of a specific analyzer diagnostic (EOE0XX)
arguments:
  - name: id
    description: "Diagnostic ID to audit (e.g., EOE023)"
    required: true
---

# Diagnostic Deep Audit: {{ id }}

4 specialized agents examine one diagnostic from every angle.

## Launch ALL 4 agents in ONE message

### Agent 1: Specification Tracer
```yaml
subagent_type: feature-dev:code-explorer
prompt: |
  # Diagnostic Audit: {{ id }} - Specification Trace

  Trace the complete implementation of diagnostic {{ id }}.

  ## Tasks
  1. Find the DiagnosticDescriptor for {{ id }}
  2. Find the Analyzer that reports it
  3. Trace: RegisterXXXAction → Analysis → ReportDiagnostic
  4. Document the exact conditions that trigger this diagnostic
  5. Find all tests for this diagnostic

  ## Deliverables
  - Flow diagram (text-based)
  - Trigger conditions (precise)
  - Test coverage list
  - File:line references for all relevant code
```

### Agent 2: False Positive Hunter
```yaml
subagent_type: deep-debugger
prompt: |
  # Diagnostic Audit: {{ id }} - False Positive Analysis

  Find cases where {{ id }} would incorrectly flag VALID code.

  ## Test Cases to Consider
  - Generic types
  - Nullable reference types
  - Inheritance hierarchies
  - Partial classes
  - Extension methods
  - Nested types
  - Conditional compilation (#if)
  - Generated code (other generators)

  ## For Each False Positive Found
  1. Show the valid code that would be flagged
  2. Explain why it's valid
  3. Trace why the analyzer flags it
  4. Suggest fix to analyzer

  ## Deliverables
  - List of false positive scenarios
  - Code examples for each
  - Analyzer fix suggestions
```

### Agent 3: False Negative Hunter
```yaml
subagent_type: feature-dev:code-reviewer
prompt: |
  # Diagnostic Audit: {{ id }} - False Negative Analysis

  Find cases where {{ id }} SHOULD fire but doesn't.

  ## Test Cases to Consider
  - Edge cases in syntax patterns
  - Semantic edge cases
  - Cross-file scenarios
  - Dynamic/reflection usage
  - Expression-bodied members
  - Local functions
  - Lambda expressions
  - LINQ query syntax

  ## For Each False Negative Found
  1. Show the invalid code that should be flagged
  2. Explain why it's invalid
  3. Trace why the analyzer misses it
  4. Suggest fix to analyzer

  ## Deliverables
  - List of false negative scenarios
  - Code examples for each
  - Analyzer fix suggestions
```

### Agent 4: Message & Severity Reviewer
```yaml
subagent_type: feature-dev:code-reviewer
prompt: |
  # Diagnostic Audit: {{ id }} - UX Review

  Review the diagnostic message and severity.

  ## Checks
  1. Is the message clear to developers?
  2. Does it explain WHAT is wrong?
  3. Does it explain HOW to fix it?
  4. Is the severity appropriate?
     - Error: Must fix, won't compile/run
     - Warning: Should fix, potential bug
     - Info: Consider fixing, best practice
  5. Is the category correct?
  6. Is it enabled by default appropriately?

  ## Compare to Similar Diagnostics
  - Find similar diagnostics in Roslyn, other analyzers
  - Is our message style consistent?
  - Is our severity consistent?

  ## Deliverables
  - Message clarity score (1-10)
  - Severity recommendation (keep/change)
  - Improved message suggestion (if needed)
  - Comparison with industry standards
```

---

## Aggregation: Diagnostic Health Report

After all 4 agents complete, create:

```markdown
# Diagnostic Health Report: {{ id }}

## Overview
- **Name:** [diagnostic title]
- **Severity:** [Error/Warning/Info]
- **Category:** [category]
- **Enabled by Default:** [yes/no]

## Implementation Summary
[From Agent 1: brief flow description]

## Test Coverage
- Unit tests: [count]
- Integration tests: [count]
- Edge case coverage: [percentage estimate]

## False Positive Risk
- **Risk Level:** [High/Medium/Low/None]
- **Scenarios Found:** [count]
[List of scenarios from Agent 2]

## False Negative Risk
- **Risk Level:** [High/Medium/Low/None]
- **Scenarios Found:** [count]
[List of scenarios from Agent 3]

## Message Quality
- **Clarity Score:** [1-10]
- **Severity Assessment:** [appropriate/should change]
[Recommendations from Agent 4]

## Recommended Actions
1. [Prioritized fixes]
2. [Additional tests needed]
3. [Message improvements]

## Health Score: [A/B/C/D/F]
```

---

## Usage

```
/diagnostic-audit id=EOE023
```

Then launch all 4 agents in ONE message for parallel execution.
