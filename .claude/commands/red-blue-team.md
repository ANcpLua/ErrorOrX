---
name: red-blue-team
description: Adversarial security/quality review - Red Team attacks, Blue Team defends
---

# Red Team / Blue Team Adversarial Review

Red Team tries to break the code. Blue Team defends and fixes.

## Phase 1: Red Team Attack (3 Parallel Agents)

Launch ALL Red Team agents in ONE message:

### RED-1: Crash Hunter
```yaml
subagent_type: deep-debugger
prompt: |
  # RED TEAM AGENT - Crash Hunter

  Your mission: Find ways to CRASH the generator.

  ## Attack Vectors
  1. Malformed attribute arguments
  2. Circular type references
  3. Extremely long identifiers
  4. Unicode edge cases in names
  5. Null/empty inputs
  6. Missing referenced types
  7. Compilation errors in user code
  8. Incremental compilation edge cases

  ## For Each Crash Found
  - Minimal reproduction code
  - Stack trace (if possible to determine)
  - Affected component

  ## Output Format
  ```
  ### CRASH-001: Generator crashes on circular generic constraints
  **Severity:** Critical
  **Reproduction:**
  \`\`\`csharp
  [ErrorOrEndpoint]
  public partial class Endpoint<T> where T : Endpoint<T> { }
  \`\`\`
  **Expected:** Diagnostic error
  **Actual:** NullReferenceException in ModelExtractor
  ```

  Find as many crashes as possible. Each crash = points for Red Team.
```

### RED-2: Security Attacker
```yaml
subagent_type: feature-dev:code-reviewer
prompt: |
  # RED TEAM AGENT - Security Attacker

  Your mission: Find SECURITY vulnerabilities in generated code.

  ## Attack Vectors
  1. Code injection via attribute values
  2. Path traversal in file generation
  3. Sensitive data in generated code
  4. Unsafe string interpolation
  5. XML injection in doc comments → OpenAPI
  6. ReDoS in any regex patterns

  ## Proof of Concept Required
  For each vulnerability, show:
  1. Malicious input
  2. Generated vulnerable code
  3. Exploitation method

  ## Output Format
  ```
  ### SEC-001: Code injection via Description attribute
  **Severity:** Critical
  **Attack Input:**
  \`\`\`csharp
  [ErrorOrEndpoint(Description = "\"); Environment.Exit(1); //")]
  \`\`\`
  **Generated Code:**
  \`\`\`csharp
  // Description: "); Environment.Exit(1); //
  \`\`\`
  **Exploitation:** [how to exploit]
  ```

  Real exploits only. Theoretical issues without proof = 0 points.
```

### RED-3: API Breaker
```yaml
subagent_type: feature-dev:code-explorer
prompt: |
  # RED TEAM AGENT - API Breaker

  Your mission: Find ways to BREAK the public API contract.

  ## Attack Vectors
  1. Find ways generated code differs from documented behavior
  2. Find edge cases where API behaves unexpectedly
  3. Find missing validation that allows invalid state
  4. Find ways to bypass intended restrictions
  5. Find inconsistencies between similar APIs

  ## For Each Break Found
  - Code demonstrating the break
  - Expected vs actual behavior
  - Impact on consumers

  ## Output Format
  ```
  ### BREAK-001: ErrorOr<T> equality broken for nested errors
  **Documented:** Two ErrorOr<T> are equal if their errors are equal
  **Actual:** Nested error lists use reference equality
  **Impact:** Dictionary lookups fail unexpectedly
  **Proof:**
  \`\`\`csharp
  var e1 = ErrorOr<int>.From(Error.Failure("x"));
  var e2 = ErrorOr<int>.From(Error.Failure("x"));
  e1.Equals(e2); // returns false, should be true
  \`\`\`
  ```
```

---

## Phase 2: Blue Team Defense

After Red Team completes, spawn Blue Team agents - ONE per Red finding.

For each RED finding, launch a BLUE defender:

### BLUE-N Template
```yaml
subagent_type: feature-dev:code-architect
prompt: |
  # BLUE TEAM AGENT - Defender

  You must defend against this Red Team finding:

  [PASTE RED TEAM FINDING HERE]

  ## Your Mission
  1. Verify the finding is real (not false alarm)
  2. Design a fix that resolves the issue
  3. Ensure fix doesn't introduce regressions
  4. Write test case that would catch this

  ## Output Format
  ```
  ### Defense for [RED-ID]

  **Finding Valid:** [Yes/No/Partial]

  **Root Cause:**
  [Why this vulnerability/bug exists]

  **Proposed Fix:**
  \`\`\`csharp
  // Show the fix
  \`\`\`

  **Regression Check:**
  - [x] Existing tests still pass
  - [x] New test covers this case
  - [x] No performance impact

  **Test Case:**
  \`\`\`csharp
  [Fact]
  public void Should_Not_[vulnerability]()
  {
      // Test that proves fix works
  }
  \`\`\`
  ```
```

---

## Phase 3: Verification

Red Team re-attacks each Blue Team fix:

### RED Re-Attack Template
```yaml
subagent_type: deep-debugger
prompt: |
  # RED TEAM - Re-Attack

  Blue Team proposed this fix for [RED-ID]:

  [PASTE BLUE TEAM FIX]

  ## Your Mission
  1. Try to bypass the fix
  2. Find edge cases the fix misses
  3. Check for regressions introduced

  ## Output
  - DEFEATED: Fix works, cannot bypass
  - BYPASSED: Found way around fix [show how]
  - INCOMPLETE: Fix partially works [show gaps]
```

---

## Scoring

| Event | Red Points | Blue Points |
|-------|-----------|-------------|
| Valid critical finding | +10 | - |
| Valid high finding | +5 | - |
| Valid medium finding | +2 | - |
| Invalid finding | -5 | - |
| Fix verified | - | +5 |
| Fix bypassed | +3 | -3 |
| Test case accepted | - | +2 |

## Final Report

```markdown
# Red Team / Blue Team Results

## Scoreboard
| Team | Points |
|------|--------|
| Red Team | [X] |
| Blue Team | [Y] |

## Winner: [Red/Blue] Team

---

## Red Team Findings
[All findings with status: Fixed/Open/Disputed]

## Blue Team Fixes
[All fixes with status: Verified/Bypassed/Pending]

## Outstanding Issues
[Issues still open after all rounds]

## Release Recommendation
- [ ] SAFE TO RELEASE - All critical/high fixed
- [ ] BLOCK RELEASE - Outstanding critical issues
```

---

## Usage

```
/red-blue-team
```

1. Launch Red Team (3 agents) in ONE message
2. Collect findings
3. Launch Blue Team (1 agent per finding)
4. Collect fixes
5. Launch Red re-attack (1 agent per fix)
6. Generate final report
