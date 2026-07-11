# Contributing

Thank you for your interest in improving BeeOverlay. This project welcomes
focused bug reports, documentation improvements, compatibility notes, and small
code changes that are easy to review. The maintainer is listed in
[CODEOWNERS](.github/CODEOWNERS).

## Before you start

- Check the existing [issues](https://github.com/aoirint/BeeOverlay/issues) and
  [pull requests](https://github.com/aoirint/BeeOverlay/pulls) to avoid
  duplicate work.
- Open an issue first for larger behavior changes, compatibility changes, or
  anything that may affect packaging.
- Keep changes focused. Separate unrelated fixes, refactors, and documentation
  updates into separate pull requests when practical.

## Reporting issues

- Use [GitHub Issues](https://github.com/aoirint/BeeOverlay/issues) for bug
  reports, feature requests, compatibility notes, and documentation requests.
- When you share logs, screenshots, or other supporting material in a public
  issue, expect maintainers to use that material within a reasonable scope
  related to the report, including understanding, reproducing, discussing,
  fixing, and communicating with end users about it.
- Only share material that you have the right to share. Do not include secrets,
  personal information, private data, or content that should not be public.
- If AI assistance significantly shapes an issue report or comment, disclose it
  in that material. See [AI assistance](#ai-assistance).
- Do not submit sample code, documentation text, patches, or other material
  that could be included in the project unless you provide it under the
  [Contribution License Agreement](#contribution-license-agreement). State:
  `I have read CONTRIBUTING.md and agree to the Contribution License
  Agreement.`
- Do not report security issues in public GitHub Issues. See
  [Reporting security issues](#reporting-security-issues).

## Stalled issues

- Respond to maintainer questions as much as you reasonably can. If you need
  more time, are blocked, or find that the issue is no longer reproducible,
  leave a short update.
- To keep maintainer work manageable and the issue list current, issues that
  cannot move forward because information is missing, the behavior no longer
  matches the current project, or the discussion has been inactive for a
  reasonable period may be closed. This does not prevent a later reopening when
  the issue is still relevant.

## Development setup

Follow the setup, formatting, build, package management, and release notes in
[README.md](./README.md). At minimum, install the documented .NET SDK and
restore packages before building:

```powershell
dotnet restore --locked-mode
```

## Making changes

- Prefer the existing project structure and naming conventions.
- Keep user-facing behavior explicit in code or documentation when it changes.
- Update files under [assets/](./assets/) when Thunderstore package metadata,
  icon, README, or release notes change.
- Update the canonical [CHANGELOG.md](./CHANGELOG.md) for user-visible changes.
- Do not commit build output, downloaded game files, local mod-manager
  profiles, or local machine configuration.

## Verification

Run the checks that match your change before opening a pull request:

```powershell
dotnet format --no-restore --verify-no-changes
docker run --rm --network none --user 1000:1000 -v ".:/workdir" davidanson/markdownlint-cli2:v0.22.1@sha256:0ed9a5f4c77ef447da2a2ac6e67caf74b214a7f80288819565e8b7d2ac148fe5
actionlint -pyflakes=
shellcheck .github/actions/publish-thunderstore/publish-thunderstore.sh
pinact run --check --min-age 7
DOTNET_CLI_UI_LANGUAGE=en dotnet build
```

Use the commands as follows:

- Run `dotnet format` and `DOTNET_CLI_UI_LANGUAGE=en dotnet build` for source
  changes.
- Run Markdown lint for documentation changes. The Docker command is the
  documented pinned path, but Docker is not required. Another
  `markdownlint-cli2` installation method is acceptable when it uses the
  repository configuration.
- Run `actionlint` and `pinact` when changing GitHub Actions workflows or
  related repository automation.
- Run ShellCheck when changing the Thunderstore publishing action.

For package or release changes, also verify the release documentation in
[README.md](./README.md) and confirm that Thunderstore-facing files under
`assets/` are still correct.

## Pull requests

- Use a clear title that summarizes the change.
- Describe what changed and how you verified it.
- Link related issues when applicable.
- Keep the pull request small enough for maintainers to review without guessing
  at unrelated intent.
- If AI assistance significantly shapes the pull request, disclose that
  assistance in its description. See [AI assistance](#ai-assistance).
- Include the [Contribution License Agreement](#contribution-license-agreement)
  confirmation by checking the item in the pull request template.

## Stalled pull requests

- Respond to maintainer feedback as much as you reasonably can. If you need
  more time, are blocked, or no longer plan to continue the work, leave a short
  update.
- To keep work moving, maintainers may accept another contribution for the same
  issue without first rejecting an inactive pull request.
- Pull requests that remain inactive for a reasonable period may be closed. This
  is not a judgment on the contributor and does not prevent a useful later
  contribution.

## Contribution License Agreement

By submitting a contribution to this project, you agree to this Contribution
License Agreement. If this agreement changes, new pull requests must use the
current agreement.

For this agreement, "you" means the person or organization submitting the
contribution. A contribution means code, documentation, assets, patches,
generated output, or other material intentionally submitted for inclusion in
this project. Ordinary issue reports, pull-request discussion, questions, and
suggestions are not contributions unless they are clearly submitted for
inclusion in the project.

By submitting a contribution, you represent and agree that:

- You have the legal right to submit the contribution and grant these rights.
- Your contribution may be distributed under the same license as this project,
  without additional terms or conditions.
- You grant the maintainer and downstream recipients a perpetual, worldwide,
  non-exclusive, no-charge, royalty-free, irrevocable copyright license to use,
  copy, modify, merge, publish, distribute, sublicense, and otherwise use your
  contribution as part of this project.
- You grant the maintainer and downstream recipients a perpetual, worldwide,
  non-exclusive, no-charge, royalty-free, irrevocable patent license to make,
  have made, use, offer to sell, sell, import, and otherwise transfer your
  contribution as part of this project. This patent license applies only to
  patent claims that you can license and that are necessarily infringed by your
  contribution alone or by combining it with the project.
- You keep any copyright you hold in your contribution. This agreement is a
  license grant, not a copyright assignment.
- The maintainer is not required to accept, publish, retain, or distribute any
  contribution.
- Do not submit material if you do not have the right to contribute it under
  this agreement.

## AI assistance

AI tools may be used as aids, but the human submitter remains responsible for
the material they submit.

When AI assistance is significant, disclose it where maintainers and reviewers
will see the relevant context:

- For a pull request, disclose it in the pull request description.
- For an issue report, issue comment, or other project-facing material,
  disclose it where you submit that material.

Examples that should be disclosed include AI-generated or substantially
AI-rewritten code, documentation, assets, tests, patches, release notes, issue
reports, comments, or maintainer-facing text. When possible, mention the rough
prompt, the requested changes, and areas that received less review.

Review AI-assisted material yourself. Do not assume generated code,
documentation, tests, explanations, summaries, or issue details are correct.
Do not present AI-performed review, inspection, editing, verification, or other
work as manual. Keep project-facing material reviewable and do not submit
low-effort AI-generated contributions that a human contributor cannot explain
or maintain.

## Reporting security issues

If you suspect a security issue, do not share exploit details publicly or with
untrusted recipients. Report it to the maintainer through a private and secure
channel when possible, or to a trusted security organization.
