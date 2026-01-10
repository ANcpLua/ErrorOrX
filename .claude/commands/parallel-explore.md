---
name: parallel-explore
description: Launch multiple explore agents for broad codebase questions
arguments:
  - name: questions
    description: "Comma-separated questions to explore in parallel"
    required: true
---

# Parallel Exploration

Split multiple questions across parallel explore agents.

## Questions to Explore

Parse the questions and launch ONE agent per question:

{{ questions }}

---

## Agent Template (for each question)

```yaml
subagent_type: Explore
model: haiku  # Fast exploration
prompt: |
  QUESTION: [individual question from list]

  Explore the ErrorOrX codebase to answer this question.

  Provide:
  1. Direct answer
  2. Relevant file paths with line numbers
  3. Key code snippets
  4. Related areas to investigate

  Be thorough but concise.
```

---

## Example Usage

```
/parallel-explore questions="Where are diagnostics defined?, How does route matching work?, What triggers code generation?"
```

This launches 3 Explore agents in parallel:
- Agent 1: "Where are diagnostics defined?"
- Agent 2: "How does route matching work?"
- Agent 3: "What triggers code generation?"

Each gets its own context window and runs simultaneously.

---

## Aggregation

After all agents complete, merge answers:

```markdown
# Exploration Results

## Question 1: [question]
[Answer from Agent 1]

## Question 2: [question]
[Answer from Agent 2]

## Question 3: [question]
[Answer from Agent 3]

## Cross-References
[Any connections between answers]
```

---

## Pro Tips

1. **Use for broad reconnaissance:**
   ```
   /parallel-explore questions="Project structure, Main entry points, Test organization, Build configuration"
   ```

2. **Use for specific deep dives:**
   ```
   /parallel-explore questions="ErrorOrEndpointGenerator flow, DiagnosticDescriptor locations, Model extraction process"
   ```

3. **Combine with other commands:**
   - First: `/parallel-explore` to understand the codebase
   - Then: `/swarm-audit` for detailed review
