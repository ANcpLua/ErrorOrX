---
name: tournament-review
description: Competitive parallel code review - agents compete for finding valid issues
arguments:
  - name: target
    description: "Target to review: pr|staged|branch|path"
    default: "staged"
  - name: path
    description: "Specific path to review (optional)"
    required: false
---

# Tournament Code Review

4 specialized agents compete to find issues. Nitpicks are penalized.

## Target: {{ target }}
{{#if path}}Path: {{ path }}{{/if}}

## CRITICAL: Launch ALL 4 agents in ONE message

### Scoring
| Finding Type | Points |
|--------------|--------|
| Security vulnerability | +10 |
| Crash bug | +5 |
| Logic error | +3 |
| Architecture violation | +3 |
| Performance issue | +2 |
| Missing error handling | +2 |
| Style/formatting | -2 |
| Nitpick | -3 |
| False positive | -5 |

---

## The Competitors

### Agent ALPHA: The Architect
```yaml
subagent_type: framework-migration:architect-review
description: Architecture and design review
prompt: |
  # Tournament Code Review - You are Agent ALPHA

  You're competing against BETA, GAMMA, and DELTA. Highest score wins.

  ## Your Specialty: Architecture & Design

  Review {{#if path}}{{ path }}{{else}}the staged/changed files{{/if}} for:
  - SOLID principle violations
  - Dependency inversion issues
  - Layer boundary breaches
  - Abstraction leaks
  - God class/method anti-patterns
  - Missing interfaces where needed

  ## Scoring Reminder
  - Architecture violations: +3 points
  - Nitpicks about naming: -2 points
  - Be specific with file:line references

  ## Output Format
  ```
  ## Agent ALPHA Findings

  ### [CRITICAL] Issue Title
  **File:** path/to/file.cs:123
  **Category:** Architecture
  **Description:** Clear explanation
  **Evidence:** Code snippet or reasoning
  **Suggested Fix:** How to resolve

  ### [HIGH] Next Issue...
  ```

  Focus on REAL issues. Every nitpick costs you points.
```

### Agent BETA: The Bug Hunter
```yaml
subagent_type: deep-debugger
description: Bug and logic error detection
prompt: |
  # Tournament Code Review - You are Agent BETA

  You're competing against ALPHA, GAMMA, and DELTA. Highest score wins.

  ## Your Specialty: Bugs & Logic Errors

  Review {{#if path}}{{ path }}{{else}}the staged/changed files{{/if}} for:
  - Null reference exceptions waiting to happen
  - Off-by-one errors
  - Race conditions
  - Resource leaks (IDisposable)
  - Exception swallowing
  - Incorrect equality comparisons
  - Integer overflow risks
  - Roslyn symbol resolution edge cases

  ## Scoring Reminder
  - Crash bugs: +5 points
  - Logic errors: +3 points
  - Theoretical edge cases that won't happen: -2 points

  ## Output Format
  ```
  ## Agent BETA Findings

  ### [CRITICAL] Null Reference in X
  **File:** path/to/file.cs:123
  **Category:** Bug
  **Reproduction:** Steps to trigger
  **Evidence:** Code path analysis
  **Suggested Fix:** Defensive code
  ```

  Prove your bugs. Unproven speculation costs points.
```

### Agent GAMMA: The Security Auditor
```yaml
subagent_type: feature-dev:code-reviewer
description: Security vulnerability detection
prompt: |
  # Tournament Code Review - You are Agent GAMMA

  You're competing against ALPHA, BETA, and DELTA. Highest score wins.

  ## Your Specialty: Security

  Review {{#if path}}{{ path }}{{else}}the staged/changed files{{/if}} for:
  - Injection vulnerabilities in generated code
  - Sensitive data exposure
  - Path traversal risks
  - Unsafe deserialization
  - Missing input validation
  - Hardcoded secrets
  - Insecure string operations

  ## Roslyn Generator Security Focus
  - User input flowing into generated code
  - String interpolation with untrusted data
  - File path construction from user input

  ## Scoring Reminder
  - Security vulnerability: +10 points
  - False security alarm: -5 points (be certain!)

  ## Output Format
  ```
  ## Agent GAMMA Findings

  ### [CRITICAL] Code Injection via Attribute
  **File:** path/to/file.cs:123
  **Category:** Security
  **Attack Vector:** How an attacker exploits this
  **Impact:** What damage is possible
  **Suggested Fix:** Sanitization/escaping needed
  ```

  Only report REAL security issues. False alarms hurt your score badly.
```

### Agent DELTA: The Perfectionist
```yaml
subagent_type: feature-dev:code-reviewer
description: Code quality and edge cases
prompt: |
  # Tournament Code Review - You are Agent DELTA

  You're competing against ALPHA, BETA, and GAMMA. Highest score wins.

  ## Your Specialty: Code Quality & Edge Cases

  Review {{#if path}}{{ path }}{{else}}the staged/changed files{{/if}} for:
  - Missing error handling
  - Incomplete switch/pattern matching
  - Async/await issues (missing ConfigureAwait, deadlocks)
  - Collection modification during enumeration
  - String comparison culture issues
  - Cancellation token not respected
  - Edge cases in Roslyn syntax analysis

  ## Scoring Reminder
  - Missing error handling: +2 points
  - Performance issues: +2 points
  - Style preferences: -2 points (DON'T DO IT)

  ## Output Format
  ```
  ## Agent DELTA Findings

  ### [HIGH] Missing Null Check Before Enumeration
  **File:** path/to/file.cs:123
  **Category:** Edge Case
  **Trigger Condition:** When X is null/empty
  **Evidence:** Code analysis
  **Suggested Fix:** Add guard clause
  ```

  Resist the urge to comment on style. Focus on correctness.
```

---

## Aggregation Instructions

After all 4 agents complete:

1. **Collect all findings**
2. **Deduplicate** - If two agents found the same issue, first reporter gets credit
3. **Validate** - Is each finding legitimate?
4. **Score** - Apply point values
5. **Rank** - Determine winner
6. **Report** - Create merged findings document

### Final Report Template

```markdown
# Tournament Review Results

## Scoreboard
| Agent | Valid Issues | Points | Penalties | Final Score |
|-------|-------------|--------|-----------|-------------|
| ALPHA |             |        |           |             |
| BETA  |             |        |           |             |
| GAMMA |             |        |           |             |
| DELTA |             |        |           |             |

## Winner: Agent [X] with [N] points

---

## All Validated Findings (by severity)

### Critical
[merged critical findings]

### High
[merged high findings]

### Medium
[merged medium findings]

---

## Disputed/Invalid Findings
[findings that were rejected and why]
```

---

## Quick Start

Run this command, then launch all 4 Task tools in ONE message:

```
/tournament-review target=staged
```

Or for a specific path:
```
/tournament-review target=path path=src/ErrorOr.Endpoints/Generators/
```
