---
title: "Model Output Is Not Authority"
subtitle: "The case for a governed runtime between machine cognition and the systems it is allowed to affect."
author: "Dennis A. Landi"
version: "0.01"
date: "2026-09-23"
category: "Essay"
volume: "Vol. II"
folio: "Nº I"
project: "LimboDancer"
source: "https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime"
---

## A request

Consider an ordinary request made to an AI system with access to business systems:

> Reconcile this customer's current status with our records and correct anything that is inconsistent.

The request is reasonable, and today it is easy to wire up. Give a language model a few tools (read the customer record, read the billing record, update a field, send a notification), describe them in a prompt, and let the model call them in a loop until it reports that it is done.

The loop works often enough to be persuasive. It also answers, silently, a series of questions that nobody asked it to answer:

- What actions exist here?
- What does each action mean in this domain?
- Is this caller allowed to perform it?
- Does the current state satisfy what the action requires?
- Is the action safe at this level of risk?
- Did the action actually accomplish what it was supposed to accomplish?

In the conventional loop, the model answers all six. It answers the first by reading tool descriptions, the second by inference from names, the third by the absence of an error, the fourth by what it remembers from a few turns ago, the fifth not at all, and the sixth by reading an HTTP status code.

These are not the same kind of question. Some are questions of meaning. Some are questions of permission. Some are questions of fact about the present state of the world. One of them, which of several acceptable actions is best, is a question of judgment. A language model is good at some of these and has no standing to answer others. The conventional loop gives it all of them because they arrive in one prompt.

LimboDancer is a runtime built on the refusal of that arrangement. It separates the questions, assigns each one an owner, and allows a model to answer only the questions a model should answer. Its principle fits on one line: model output is not authority.

---

## Why plausible is not permitted

The reason this matters is not that models are often wrong. It is that when they are wrong, they are wrong fluently.

A tool call is a piece of generated text. It has a function name, typed arguments, and often an explanation of why it is the right step. It is well formed whether or not the step is sound. Nothing in the form of the call distinguishes an action that exists, is permitted, and fits the current state from one that does not. The fluency is constant; the soundness varies.

Volume I of the Realization Engine names this asymmetry in the setting of a practitioner reading a model's prose: the presentation channel renders fluently regardless of the substance behind it. When the rendering is an action, the asymmetry has consequences that do not wait for anyone to read them. A plausible request to refund an order, delete a record, or confirm a reservation executes with the same fluency whether or not it should.

A system that treats a plausible request as a permitted one has given authority to the presentation channel. LimboDancer is designed so that it cannot.

---

## Separating the questions

LimboDancer organizes the runtime by the kind of responsibility being exercised, not by the kind of software component that happens to exercise it. The result is a set of logical planes, each with one primary question:

| Plane | Question it owns |
|---|---|
| Interaction | How does an actor communicate with the runtime? |
| Reasoning | What are we trying to accomplish? |
| Semantic | What does the current world mean, and what actions are possible? |
| Decision | Which permissible action should be selected? |
| Execution | How is the selected action performed? |
| State | What does the system know, remember, and persist? |

Three concerns cross every plane. **Governance** determines what is permitted: tenant isolation, permissions, policy, risk, confirmation, budgets. **Orchestration** moves work through the planes without owning any of their answers. **Diagnostics** evaluates whether the runtime is coherent and behaving within its declared expectations; it is an evaluator of invariants, not a log.

The division of authority is compact enough to memorize:

```text
Reasoning proposes.
Semantics constrains.
Governance permits.
Decision selects.
Execution acts.
State remembers.
Orchestration coordinates.
Diagnostics assures.
```

Two of these separations carry most of the weight.

The first is between **possibility and permission**. The Semantic Plane decides what actions are meaningful for a given entity in a given state: whether the property exists, whether the relationship is valid, whether the action's preconditions hold. Governance decides whether this caller, in this tenant, at this level of risk, may perform the action. An action can be possible and forbidden. It can be permitted in general and meaningless here. Neither question is answered by asking a model.

The second is between **reasoning and decision**. Reasoning is open-ended: interpreting a request, decomposing a goal, identifying what information is missing, drafting a plan, revising it after an observation. Its output space is enormous, and that is where language models are strong. Decision is bounded: given an explicit list of permitted alternatives, choose one, or decline to. Its output space is small, and that smallness is what makes it measurable. One can check whether the chosen candidate was correct, whether the stated confidence was calibrated, what the choice cost, and whether a different provider would have chosen better.

The orchestrator deserves a sentence of its own because it is where authority most easily leaks. LimboDancer's rule is that the orchestrator may ask every authoritative component a question, but may not answer those questions on their behalf. It sequences. It does not decide what things mean, what is allowed, or what is true.

---

## How authority narrows

If the model does not hold authority, the runtime must construct it, and the construction has to be something one can inspect after the fact. LimboDancer constructs authority in typed stages. Each stage has a name, each is a distinct type in the runtime's contracts, and each removes possibilities.

```text
Goal
  -> ActionCandidate
  -> PermittedAction
  -> SelectedAction
  -> AuthorizedAction
  -> ExecutedAction
```

A **Goal** is the desired outcome, such as the reconciliation request above. It is not an action and it grants nothing.

An **ActionCandidate** is a registered semantic action grounded in the current context: this action, on this entity, with these arguments. Candidates come from the runtime's registry of semantic actions and the domain ontology, not from the model's imagination. A candidate is not permitted.

A **PermittedAction** is a candidate that has passed the deterministic semantic and governance constraints. Its preconditions are evaluated from trusted, server-side action definitions. A caller supplies intent and arguments; a caller does not get to supply the rules under which its request becomes acceptable.

A **SelectedAction** is the one chosen, either by a directed caller who named it or by a Decision provider choosing among the permitted set. Selection is a preference. It is not authorization.

An **AuthorizedAction** is a selected action that has passed the final Execution Gate, which revalidates policy and state immediately before execution. Only an AuthorizedAction reaches an executor.

After execution, the runtime observes the result and, where the action declares expected effects, verifies them.

The narrowing is the point. By the time a probabilistic component is allowed to choose anything, the choice has already been reduced to a short list of actions that exist, mean what they say, and are permitted. The model's influence is real, and it is confined to the one question it is suited to answer.

---

## The world changes while you think

Agentic systems act on mutable state, and reasoning takes time. Suppose the runtime observes an account:

```text
Account balance: $1,000
Version: 42
```

A model reasons about that observation and selects an action that assumes the balance. Before the action executes, another system changes the account:

```text
Account balance: $200
Version: 43
```

The model's reasoning was sound when it was performed. It is unsound now. A loop that executes the selected call anyway has committed a classic time-of-check to time-of-use error, and the model cannot detect it, because the model is not looking at the account; it is looking at its memory of the account.

The Execution Gate is placed at the last responsible moment for exactly this reason. It compares the state the decision relied on with the state that exists now. When they differ, the selection is stale. The runtime does not execute it. It re-observes and reconsiders.

The same logic governs human confirmation. When policy requires a person to approve an action, the approval is a governance event, not a decision. And approval does not skip revalidation: state may change while the person is deciding, and an approval of an action against yesterday's state is not an approval of the same action against today's.

---

## Success is not success

A remote API returns success. The reservation is still pending.

Execution success and semantic success are different facts. The first says that a call completed without error. The second says that the world is now in the state the action was supposed to produce. Most agent loops observe only the first, because it is the only one the tool returns.

LimboDancer lets an action declare its expected effects, for example that the reservation's status becomes confirmed, and then verifies them against observed state after execution. Verification has four outcomes, not two:

- **Verified.** The expected effects are observed.
- **Partially verified.** Some are observed; some are not.
- **Unverifiable.** The runtime cannot observe what it would need to observe.
- **Contradicted.** The observed state conflicts with the expected effect.

Each outcome supports a different response: continue, escalate, retry where retrying is safe, or begin recovery. Recovery is itself governed. If a compensating action is needed, it goes through the same authority path as any other action. The runtime never assumes that undoing something is authorized merely because doing it was.

The distinction between unverifiable and verified is easy to lose and important to keep. A system that cannot tell the difference between "it worked" and "I cannot tell whether it worked" will eventually report success for something that did not happen.

---

## Knowing is not doing

Not every request asks for a change. Many ask for an answer: is this allowed, what applies here, what follows from these facts.

LimboDancer treats an answer of this kind as a first-class outcome called a **DomainConclusion**: an explanation of what the modeled rules and the available evidence imply, with its provenance, assumptions, and state dependencies kept explicit. A conclusion can be definitive, qualified, indeterminate, or an abstention. "The rules do not settle this with the evidence available" is a legitimate result, and a runtime that cannot say it will say something worse.

A conclusion is not permission. If a conclusion says that an action would be valid, and someone then wants the action performed, the action is a new request. It enters the authority path on its own and is constrained, selected, gated, and verified like any other. The runtime does not let an explanation of what may be done turn into the doing of it.

---

## Directed and autonomous, one path

LimboDancer supports two ways in.

In **directed execution**, a caller names a known action: read this session's history, append a message, query the graph, search memory. The request arrives through a protocol (MCP, HTTP, a command line, a user interface, a scheduler, another agent), is bound to a semantic action, and passes through the same constraints, diagnostics, gate, and audit as everything else.

In **autonomous goal execution**, a caller states an outcome and the runtime works toward it:

```text
Observe -> Reason -> Resolve -> Constrain -> Decide
        -> Gate -> Execute -> Verify -> Continue or Complete
```

The important property is that there is only one authority path. An autonomous step is not a privileged mode with its own rules. Every step of an autonomous loop passes through the same constraints and the same gate as a directed request. There are not two execution systems, one careful and one fast.

This is also why LimboDancer does not describe itself as an MCP server, though it can be reached through one. MCP is an interaction protocol. It is one adapter among several, and it does not carry authority. Every adapter converges on the same semantic and authority model, and swapping the protocol does not change what the runtime is permitted to do.

---

## Models as replaceable advisors

The Decision boundary in LimboDancer is not an LLM abstraction. It is a contract for bounded choice, and many kinds of provider can satisfy it: deterministic rules, a hosted language model, a local classifier, a small language model, a specialized decision model, a human, or a composite that routes among them. A provider receives the decision context and the already permitted candidates. It may select one of them, abstain, or escalate. It may not create candidates, see rejected ones, redefine risk or policy, or invoke execution.

Abstention is a successful outcome, not a failure. A mature decision component must be able to say that it does not have sufficient confidence to choose. How much confidence is sufficient depends on the action: a read-only query can tolerate uncertainty that a destructive external side effect cannot. Risk is part of the trusted action definition. It is never supplied by the caller.

Because providers are interchangeable, the question of which one to use becomes an empirical question rather than a matter of preference or fashion. LimboDancer's current answer is conservative on purpose. The deterministic rule provider is the default. A hosted-model provider is implemented, bounded in time, tokens, and cost, and disabled by default. Its review is explicit that the tests so far prove the measurement machinery, not the model's quality on representative decisions. Adoption waits for a representative corpus, labeled independently, with thresholds declared in advance, and measured for correctness, abstention quality, invalid output, calibration, disagreement with the deterministic baseline, and the severity of wrong choices.

The executors do not change when the provider changes. Neither does anything downstream of the Decision boundary. That is the practical meaning of model neutrality: the intelligence is replaceable because the authority was never in it.

---

## Boundaries that probability must not widen

LimboDancer is multi-tenant, and it treats tenant isolation as a runtime invariant rather than a database convention. Tenant identity is established when a request is admitted and remains structurally enforced through the goal, the observations, action resolution, decision, execution, audit, and every access to state. Cross-tenant reads and writes fail closed.

This deserves emphasis in an agentic system because probabilistic reasoning is exactly the kind of process that should never be allowed to widen a boundary. A model that infers that a record in another tenant is relevant has not thereby acquired access to it. The runtime's default is the same everywhere: where authority is missing, the answer is no. Autonomous admission is deny-by-default until a trusted adapter supplies an authenticated principal and a runtime budget. A descriptor that declares preconditions fails closed unless something capable of evaluating those preconditions is registered. The runtime does not invent authority to fill a gap.

---

## Authority before capability

The order in which LimboDancer was built is part of its argument.

The first things built were the boundary: action identity and descriptors, the registry and protocol bindings, diagnostics, the Execution Gate and the AuthorizedAction type, audit, and tenant-safe state. These were proven end to end through directed requests before any autonomous behavior existed. Only then were goals, observations, candidates, and constraints added; then a deterministic Decision provider; then orchestration; then effect verification; then replay-capable evidence; and only after all of that, an experimental model-based Decision provider.

The usual order is the reverse. Capability is built first because it is what can be demonstrated, and governance is added when something goes wrong. The difficulty with that order is that governance added later has to be retrofitted into paths that were designed without it, and every path it does not reach remains a path where the model holds authority by default.

Building the authority substrate first means that each new cognitive capability arrives into a structure that already constrains it. The runtime becomes more capable without becoming less governed.

---

## A demanding reference domain

A runtime like this is easy to describe and hard to test with toy examples, because toy examples do not stress the separation of meaning, permission, state, and choice. LimboDancer's reference domain is chosen to stress all four: Advanced Squad Leader, a tactical board game whose rulebook is famous for its density. It has deeply cross-referenced rules, exceptions to exceptions, tables, terrain, spatial relationships, phases, and state that changes every turn.

The domain is useful precisely because it is unforgiving. There is an authoritative source with a known edition. Questions have answers the rules determine, or answers the rules leave open, and both can be checked. Retrieval alone fails, because the relevant rule is rarely the one that matches the words of the question. And the difference between a conclusion and an action is concrete: whether a unit may enter a building is a question; moving it is an action.

Turning the rulebook into something the runtime can use is its own discipline, kept outside the runtime. Rulebook text is registered as immutable source fragments, extracted into a structured intermediate form, validated deterministically, and reviewed by people before anything is published as a versioned domain package. Validation and review are separate authorities: validation shows that declared invariants hold; review records an accountable human disposition; neither extraction confidence nor validation alone grants semantic authority. Partial formalization is stated explicitly, and prose that has not been modeled is never treated as executable meaning.

None of the game's vocabulary is allowed into the runtime kernel. Domains attach to the runtime through their own packages: their ontology, semantic actions, policy, state adapters, and executors. The runtime supplies the authority structure. The domain supplies the meaning.

---

## What this is not

LimboDancer is not primarily an MCP server, an LLM wrapper, a chatbot, a vector database abstraction, a workflow engine, a policy engine, a planner, or an agent framework. It uses or integrates capabilities from several of those categories. None of them defines it.

It is also not a claim that models are untrustworthy and should be kept away from consequential work. The runtime exists so that models can be given consequential work. It is a claim about where trust is placed. Trust is placed in authored meaning, declared policy, observed state, and verified effect, all of which can be inspected. It is not placed in the fluency of a request.

---

## Where people stand

A runtime that pursues goals on its own invites a fair question: where are the people?

In LimboDancer they are upstream of the goal, and their work is authorship, not supervision. Domain authors define what exists, what actions mean, what they require, and what they should produce. Curators review machine-proposed domain material against authoritative sources and answer for their decisions. Policy authors decide who may do what, at what risk, under what budget, and with what confirmation. Operators confirm actions that policy says a person must confirm. Evaluators label the cases against which any decision model must prove itself before it is trusted.

The runtime makes these roles structurally necessary: without an authored ontology there are no candidates, without policy there is no permission, without labeled evidence there is no adoption. It cannot make them substantive. A domain model can be thin, a review can be a signature, a policy can be permissive by default. The structure ensures that when that happens, it is visible and attributable. The quality of the authorship remains a human responsibility, and the runtime is built so that it stays one.

---

## Status

This essay describes a runtime under active construction. The governed directed-execution path, deterministic autonomous selection and orchestration, opt-in effect verification, and replay-capable decision evidence are implemented and reviewed. The initial state providers are deterministic in-memory reference implementations; production persistence is admitted when a concrete provider is selected. The deterministic rule provider is the default Decision provider; a hosted-model provider is an experiment, disabled by default, pending representative evidence. The ASL authoring pipeline has its source registry, structural intermediate representation, and the first increments of its validation and review workflow; its first adjudication scenario has not yet been accepted.

The architecture documents, the normative runtime specification, and the implementation plan are published with the source in the [LimboDancer repository](https://github.com/Realization-Engine/LimboDancer.Agentic.CognitiveRuntime). This essay will be revised as the implementation proves, or fails to prove, what it claims.
