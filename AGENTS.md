# AGENTS.md

## Purpose

This file defines non-negotiable rules for editing `wallet-withdrawal.usageguide.md`
and similar frontend/mobile-facing API docs in this workspace.

## Core Rule

`usageguide` is for frontend/mobile developers first.
It is not an internal architecture note, not a backlog, and not a compressed implementation memo.

When updating a `usageguide`, optimize for client integration clarity.

## Non-Negotiable Principles For `usageguide`

### 1. Preserve frontend/mobile structure

Do not destroy or flatten a structure that is intentionally organized for client developers.

Preferred structure for this module:
- Overview
- Authentication & Authorization
- Actor-based sections
  - `Expert/Member Business + Expert/Member APIs`
  - `Admin Business + Admin APIs`
- Shared Data Models
- Verified Endpoint List
- Changelog

### 2. Never remove API contract detail

Do not replace concrete API contract content with vague summaries.

`usageguide` must keep enough detail for frontend/mobile developers to integrate without reading backend code:
- endpoint path
- HTTP method
- auth requirement
- request body
- request constraints
- success response shape
- important field notes
- role/ownership restrictions when relevant

### 3. Keep example payloads

Do not strip example request/response payloads unless they are clearly wrong and replaced with corrected examples in the same edit.

Examples are part of the contract.

### 4. Keep user-facing language clean

Avoid internal caveat-style wording in `usageguide`.
Do not write the doc as a backend retrospective.

Allowed:
- concise business rules
- client-relevant behavior
- verified integration notes

Avoid:
- internal debate history
- backlog/task prose
- architecture speculation
- low-signal implementation chatter

### 5. Separate doc roles strictly

- `usageguide.md`: frontend/mobile integration contract
- `implementation.md`: backend design/implementation decisions
- `backlog.md`: phase tracking, tasks, gaps, internal planning

Do not move internal planning content into `usageguide`.
Do not remove contract detail from `usageguide` just because it also exists in backend code.

### 6. Actor-based sections are intentional

For this withdrawal module:
- `Expert` and `Member` are treated the same in flow and docs
- admin APIs stay separate

Do not collapse actor-based sections unless the user explicitly requests a structural rewrite.

### 7. Prefer correction over compression

If a `usageguide` is too long:
- remove duplication
- tighten wording
- shorten explanations

Do not solve length by deleting API contract, response schemas, or endpoint examples.

### 8. Update docs with code changes

When backend contract changes, update `usageguide` in the same task if the change affects:
- request fields
- response fields
- endpoint existence
- endpoint method
- state transitions
- auth/authorization expectations
- client-visible business rules

### 9. Mark verified behavior carefully

Only document behavior as active/current when it is code-verified in the repo.
If behavior is planned but not implemented, keep it out of `usageguide` and place it in `implementation` or `backlog`.

### 10. Do not make frontend/mobile devs infer critical behavior

If the client needs to know it, document it explicitly.

Examples:
- whether balance is deducted at `create`, `approve`, or `complete`
- whether bank account is masked
- whether QR image may be `null`
- whether cancel is `POST` or `DELETE`
- whether admin responses differ from user responses

## Editing Checklist For `usageguide`

Before finishing any edit to a frontend/mobile-facing API guide, verify:

- [ ] Structure still matches frontend/mobile reading flow
- [ ] Endpoint list is still present
- [ ] Request/response contracts are still explicit
- [ ] Example payloads still exist for important endpoints
- [ ] Actor-based sections are preserved when intended
- [ ] Internal backlog/implementation content has not leaked in
- [ ] Changelog reflects contract changes

## Default Decision

If there is tension between:
- shorter doc
- clearer integration contract

choose clearer integration contract.
