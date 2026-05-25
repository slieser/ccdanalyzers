## Unreleased

### Configuration
- Default ignored namespaces are now sourced from `namespaces.txt` (shipped with the package) instead of a hardcoded list. The package targets file registers it as an `AdditionalFile` in consumer projects automatically.
- New option `dotnet_diagnostic.CCD0001.additional_namespaces` (CSV) appends to the list from `namespaces.txt`.
- Removed legacy option key `iosp_violation.CCD0001.namespaces` (use `additional_namespaces` instead).

### Coverage
- Expression-bodied methods (`int M() => ...`) are now analyzed.
- Property accessors (both block-bodied and standalone) are now analyzed via `IPropertySymbol.GetMethod`/`SetMethod`.
- Calls to methods with the same name from different containing types are counted separately (dedup key is now `containingType.name` instead of just `name`).
- `for` loop canonical exemption is now restricted to the condition position only.

### Internal
- `CalculateMetric` typo fixed, `StringComparison.Ordinal` consistently used, `HelpLinkUri` added.
- `FindAll` replaced with `DescendantNodesAndSelf()`; nested expression filter via explicit ancestor walk.
