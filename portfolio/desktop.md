# Clarity Belongs Desktop Portfolio

## Purpose

Clarity Belongs should have a small, focused desktop catalog for private, local, large, or awkward-to-upload documents.

Desktop exists here because local files, OCR, batch processing, privacy, and offline analysis can materially improve the Clarity workflow. Clarity should not mirror the broad Software Belongs utility catalog.

## Target desktop products

1. Clarity Reader
2. Document Compare
3. Local OCR
4. Document Redactor
5. Document Privacy Check
6. Batch Document Processor
7. Transcript Cleaner
8. Local Document Search
9. Document Organizer
10. Clarity Desktop

## Product definitions

### Clarity Reader

Open a local document and surface plain-language summaries, important points, obligations, dates, risks, and questions to ask.

### Document Compare

Compare two versions of a document and explain meaningful changes, additions, removals, and changed obligations.

### Local OCR

Convert scans, images, and image-only PDFs into searchable text for downstream Clarity workflows.

### Document Redactor

Find and remove selected sensitive content from local documents before sharing or further processing.

### Document Privacy Check

Inspect a document for metadata, hidden information, personal data, and other content a user may want to review before sharing.

### Batch Document Processor

Apply supported Clarity operations to a folder or group of documents in one local workflow.

### Transcript Cleaner

Clean, structure, label, and organize local transcript text or transcript files for easier review.

### Local Document Search

Index an approved local folder and search document contents without requiring a cloud document library.

### Document Organizer

Classify, rename, and organize local document folders using extracted document information and user-approved rules.

### Clarity Desktop

The combined shell that discovers and exposes installed Clarity desktop modules from one place.

## Shared engine plan

Clarity should reuse shared Belongs engines where appropriate:

- desktop shell and module discovery
- document parsing and PDF primitives
- OCR
- local indexing/search
- filesystem operations
- metadata inspection

Clarity-specific logic should remain focused on explanation, comparison, extraction, document risk/context, and document-oriented workflows.

## Suggested shell

```text
Clarity Desktop
  Understand
    Clarity Reader
    Document Compare
    Transcript Cleaner
  Prepare
    Local OCR
    Document Redactor
    Document Privacy Check
  Organize
    Local Document Search
    Document Organizer
    Batch Document Processor
```

## Initial build order

### Wave 1

1. Clarity Desktop shell
2. Clarity Reader
3. Local OCR
4. Document Compare
5. Document Privacy Check

### Wave 2

6. Document Redactor
7. Local Document Search
8. Document Organizer

### Wave 3

9. Batch Document Processor
10. Transcript Cleaner

## Identity rule

Clarity desktop means understand and work with private/local documents.

Generic local utilities belong in Software Belongs.

Device diagnosis, security posture, inventory, networking, and support workflows belong in AutoPilot IT.
