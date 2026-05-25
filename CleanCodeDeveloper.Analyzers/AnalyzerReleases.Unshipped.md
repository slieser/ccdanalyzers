## Unreleased

- Default ignored namespaces are now sourced from `namespaces.txt` (shipped with the package) instead of a hardcoded list. The package targets file registers it as an `AdditionalFile` in consumer projects automatically.
- New option `dotnet_diagnostic.CCD0001.additional_namespaces` (CSV) appends to the list from `namespaces.txt`.
- Removed legacy option key `iosp_violation.CCD0001.namespaces` (use `additional_namespaces` instead).
- Internal refactor: `CalculateMetric` typo fixed, `StringComparison.Ordinal` consistently used, `HelpLinkUri` added.
