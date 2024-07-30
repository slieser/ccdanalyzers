## Release 0.3.3

Calls to base class methods are now ignored by the analyzer.
- Otherwise base.F() would be flagged as an integration call

Calls to ILogger are now ignored by the analyzer.
- This is to prevent false positives when using the ILogger interface.

### New Rules
| Rule ID | Category                        | Severity | Notes |
|---------|---------------------------------|----------|-------|
| CCD0001 | Clean Code Developer Principles | Warning  |       | 
