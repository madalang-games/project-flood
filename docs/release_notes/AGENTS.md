# Release Notes Management Policy

This directory manages Store Listing Release Notes for Project Flood across all supported languages.

## Directory Structure
- `vX.Y.Z/`: Directory for a specific version.
- `vX.Y.Z/release_note.txt`: The definitive plain-text release note with language tags.
- `vX.Y.Z/snapshots/`: (Optional) Point-in-time drafts.

## File Format
The `release_note.txt` uses language tags for Google Play Console compatibility:
```text
<en-US>
Contents here...
</en-US>
<ko-KR>
내용...
</ko-KR>
<ar>
المحتوى...
</ar>
```

## AI Agent Workflow
1. **Diff Analysis:** Compare with previous version using git.
2. **Drafting:** Max 4 lines summary. Benefit-driven bullet points.
3. **Translation:** Ensure all supported languages (EN, KO, AR, etc.) are covered.
4. **Metadata:** Append current Git HEAD commit hash at the bottom.

## Release History
| Version | Release Date | Summary | Commit |
|---------|--------------|---------|--------|
| v1.0.0  | 2026-06-12   | Initial MVP Release | `c22b90c` |
