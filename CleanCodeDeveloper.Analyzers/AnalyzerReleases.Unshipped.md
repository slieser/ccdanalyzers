## Unreleased

### Configuration
- Default ignored namespaces are now sourced from `namespaces.txt` (shipped with the package) instead of a hardcoded list. The package targets file registers it as an `AdditionalFile` in consumer projects automatically.
- New option `dotnet_diagnostic.CCD0001.additional_namespaces` (CSV) appends to the list from `namespaces.txt`.
- New option `dotnet_diagnostic.CCD0001.exclude_tests` (bool, default `false`): when enabled, methods carrying a NUnit, MSTest or xUnit test (or test lifecycle) attribute are skipped. The decision is made per method via the method's own attributes, never per test fixture, so non-test helpers in a test class are still analyzed.
- Removed legacy option key `iosp_violation.CCD0001.namespaces` (use `additional_namespaces` instead).
- `Moq` is now ignored by default (added to `namespaces.txt`).

### Coverage
- Expression-bodied methods (`int M() => ...`) are now analyzed.
- Property accessors (both block-bodied and standalone) are now analyzed via `IPropertySymbol.GetMethod`/`SetMethod`.
- Calls to methods with the same name from different containing types are counted separately (dedup key is now `containingType.name` instead of just `name`).
- `for` loop canonical exemption is now restricted to the condition position only.
- Calls inside a lambda argument of an ignored-namespace call (e.g. `s.Add(...)` in Moq's `mock.Setup(s => s.Add(...))`) are no longer classified, since they describe rather than execute the call.

### Internal
- `CalculateMetric` typo fixed, `StringComparison.Ordinal` consistently used, `HelpLinkUri` added.
- `FindAll` replaced with `DescendantNodesAndSelf()`; nested expression filter via explicit ancestor walk.
