using ZipBuddy.Core.Models;

namespace ZipBuddy.Tests;

public class OperationResultTests
{
    [Theory]
    [InlineData("Invalid password", true)]
    [InlineData("Archive or entries are encrypted", true)]
    [InlineData("Bad cryptographic key", true)]
    [InlineData("CRC mismatch", false)]
    [InlineData("File not found", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IndicatesPasswordFailure_DetectsOnlyCredentialErrors(string? message, bool expected)
    {
        Assert.Equal(expected, OperationResult.IndicatesPasswordFailure(message));
    }

    [Fact]
    public void FailedResult_RequiresPassword_DefaultsToFalse()
    {
        Assert.False(OperationResult.Failed("Some error").RequiresPassword);
    }
}
