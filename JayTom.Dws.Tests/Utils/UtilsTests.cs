using JayTom.Dws.Utils;
using System.Runtime.Versioning;

namespace JayTom.Dws.Tests.Utils;

public class UtilsTests
{
    [Fact]
    public void SetPath_ShouldAddPathsToEnvironmentVariable()
    {
        // Arrange
        var testPath1 = @"C:\TestPath1";
        var testPath2 = @"C:\TestPath2";
        var originalPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);

        // Act
        JayTom.Dws.Utils.Utils.SetPath(testPath1, testPath2);

        // Assert
        var updatedPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
        Assert.NotNull(updatedPath);
        Assert.Contains(testPath1, updatedPath);
        Assert.Contains(testPath2, updatedPath);

        // Cleanup
        if (originalPath != null)
        {
            Environment.SetEnvironmentVariable("PATH", originalPath, EnvironmentVariableTarget.Process);
        }
    }

    [Fact]
    public void SetPath_WithNullPath_ShouldHandleGracefully()
    {
        // Arrange
        var originalPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
        
        // Clear the PATH temporarily
        Environment.SetEnvironmentVariable("PATH", null, EnvironmentVariableTarget.Process);

        // Act
        JayTom.Dws.Utils.Utils.SetPath(@"C:\TestPath");

        // Assert - method should return without throwing
        Assert.True(true);

        // Cleanup
        if (originalPath != null)
        {
            Environment.SetEnvironmentVariable("PATH", originalPath, EnvironmentVariableTarget.Process);
        }
    }
}
