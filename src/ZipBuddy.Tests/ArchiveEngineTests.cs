using ZipBuddy.Core.Interfaces;
using ZipBuddy.Core.Models;
using ZipBuddy.Formats;
using Xunit;

namespace ZipBuddy.Tests;

public class ArchiveEngineTests
{
    [Fact]
    public void EmptyProviderCollection_UsesBuiltInProviders()
    {
        // Microsoft.Extensions.DependencyInjection supplies an empty IEnumerable<T>
        // when no custom IArchiveProvider registrations exist.
        var engine = new ArchiveEngine(Array.Empty<IArchiveProvider>());

        Assert.Contains(engine.Providers, provider => provider.Format == ArchiveFormat.Zip);
        Assert.Contains(engine.Providers, provider => provider.Format == ArchiveFormat.SevenZip);
    }
}
