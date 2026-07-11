---
name: implementation-analysis-quality-check
description: Review developer-facing implementation-analysis documentation, including reverse-engineering findings, member references, and behavior analysis. Use when creating, updating, or validating an implementation-analysis document for structure, evidence, version provenance, and safe source handling.
---

# Implementation Analysis Quality Check

## When to Use

Use this skill for implementation-analysis documents that describe how a
third-party or target-version implementation behaves. Do not use it for UI,
architecture, or general project documentation unless that document contains
implementation-analysis findings.

## Goals

- Keep the document independently reviewable and scoped to its stated subject.
- Distinguish code to inspect from conclusions about behavior.
- Preserve evidence, uncertainty, provenance, and source-handling boundaries.

## Workflow

1. Identify the document's primary subject, target version, and audience.
   Check that the target game or product version and its manifest or equivalent
   build identifier are recorded. Require replacement of findings in the
   existing concern-specific document when the target version changes.
2. Check document boundaries. Keep each independent concern in its own file;
   keep UI, visualization, and architecture decisions outside an
   implementation-analysis document unless they are themselves the subject.
3. Check the implementation reference.
   - Use `Implementation reference` for relevant fields and methods, including
     non-public members; do not call this collection an API or a surface.
   - Group entries by owning class. Inside a class subsection, use short member
     and method names; qualify names in headings and cross-references when the
     owner is not otherwise clear.
   - Separate fields from methods. Record each field's C# type and role; record
     each method's return type, parameters, and role.
   - Put related classes in their own subsection and state how they affect the
     primary subject.
4. Check behavior analysis. Keep it separate from the implementation
   reference. Tie each claim to the relevant members, calls, assignments, and
   guards. Do not infer a state transition from a related condition alone. Do
   not title partial checks as a transition; until the state-update path is
   established, describe them as checks evaluated in the relevant state.
5. Check evidence and provenance.
   - Ground claims in the target-version implementation and, when available,
     runtime observations.
   - Separate observations from inferences and state material limitations or
     unresolved transition conditions.
   - Keep only the evidence needed for an independent reviewer to verify a
     finding; do not present speculation as fact.
6. Check source handling. Do not include local absolute paths, user names,
   home directories, or other machine-specific information. Do not reproduce
   decompiled proprietary method bodies or substantial proprietary code.
   Runtime logs may be quoted when needed, but must mask sensitive or
   machine-specific information; prefer the relevant excerpt.
7. Report findings by checklist item. For every failed item, state the affected
   location, the problem, and the smallest corrective action. State explicitly
   when no issues are found. Do not rewrite the document unless the requesting
   task also asks for edits.
