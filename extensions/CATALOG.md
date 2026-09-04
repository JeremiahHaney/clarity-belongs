# Clarity Browser Extension Catalog

Clarity extensions are browser companions for capturing, comparing, tracking and understanding information while the user is already looking at it.

## Product rules

- The extension must connect naturally to Clarity's core jobs: see what changed, understand what matters, track what matters, or preserve evidence.
- Prefer capture and analysis workflows that are awkward from a normal web page.
- Local-first capture where practical.
- Minimal permissions and explicit user actions.
- No ads and no sale of user data.
- The extension should remain useful even if the Clarity web app evolves.
- Avoid building generic utilities that belong in Software Belongs instead.

## Catalog

| # | Extension | Folder | Primary job | Status |
| ---: | --- | --- | --- | --- |
| 1 | Clarity Watch | `clarity-watch` | Select part of a page and create a change watch | Planned |
| 2 | Clarity Compare | `clarity-compare` | Save a page snapshot and compare it later | Planned |
| 3 | Clarity Explain | `clarity-explain` | Send selected text into an explanation workflow | Planned |
| 4 | Clarity Capture | `clarity-capture` | Preserve URL, timestamp, screenshot and selected evidence | Planned |
| 5 | Clarity Track | `clarity-track` | Turn a visible value/date/status into a tracked item | Planned |
| 6 | Clarity Summarize | `clarity-summarize` | Send the current page into a summary workflow | Planned |
| 7 | Clarity Source Check | `clarity-source-check` | Capture claims, links and source metadata | Planned |
| 8 | Clarity Inbox | `clarity-inbox` | Save the current page or selection for later review | Planned |

## Recommended build order

1. Clarity Capture
2. Clarity Inbox
3. Clarity Watch
4. Clarity Compare
5. Clarity Track
6. Clarity Summarize
7. Clarity Explain
8. Clarity Source Check

The first four establish the shared capture, storage, screenshot and page-selection primitives that most of the remaining extensions can reuse.
