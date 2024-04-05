using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanCodeDeveloper.Analyzers.Tests;

public class Examples
{
    public async Task<int> GetNumber_IntegrationOnly() {
        var list = GetList();
        var number = await FilterNumberAsync(list);
        return number;
    }

    /// <remarks>
    /// With 'CleanCodeDeveloper.Analyzers 0.0.6':
    /// Warning CCD0001 Method 'GetNumber_IntegrationOnlyWithConfigureAwait' mixes integration with operation.
    ///   Integration: call to 'GetList'
    ///   Integration: call to 'FilterNumberAsync'
    ///   Operation: calling API 'ConfigureAwait'
    /// </remarks>
    public async Task<int> GetNumber_IntegrationOnlyWithConfigureAwait() {
        var list = GetList();
        var number = await FilterNumberAsync(list).ConfigureAwait(continueOnCapturedContext: false);
        return number;
    }

    private Task<int> FilterNumberAsync(IReadOnlyList<int> list) {
        int Value() {
            if (list.Count > 0) {
                return list[list.Count - 1];
            }
            return 0;
        }

        var task = Task.Run(Value);
        return task;
    }

    private static IReadOnlyList<int> GetList() {
        return new List<int>() { 1, 2, 3, 4 };
    }
}