# Feedback release standard

Every public Clarity product, monitor, tool, and follow type must remain reachable for user feedback while the early-feedback phase is enabled.

Required paths:
- General feedback
- Report a problem
- Suggest an improvement
- Positive feedback
- Did this work? Yes / No
- Optional contact information

The canonical endpoint is `https://claritybelongs.com/feedback`.

Feedback links should pass `product`, `source`, and `version` when known. The feedback page preserves the originating product/page and stores the submission in the Clarity database.

A product is not release-ready unless its feedback path is visible, preserves product context, allows anonymous submission, persists a normal submission, and persists the quick usefulness response.

This is intentionally stricter during early releases and can be relaxed later without removing the stored feedback system.
