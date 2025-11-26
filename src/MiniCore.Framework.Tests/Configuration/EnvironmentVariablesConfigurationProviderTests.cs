using MiniCore.Framework.Configuration.EnvironmentVariables;

namespace MiniCore.Framework.Tests.Configuration;

public class EnvironmentVariablesConfigurationProviderTests
{
    [Fact]
    public void Load_LoadsEnvironmentVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TEST_CONFIG_KEY", "TestValue");
        var source = new EnvironmentVariablesConfigurationSource();
        var provider = new EnvironmentVariablesConfigurationProvider(source);

        try
        {
            // Act
            provider.Load();

            // Assert
            Assert.True(provider.TryGet("TEST_CONFIG_KEY", out var value));
            Assert.Equal("TestValue", value);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST_CONFIG_KEY", null);
        }
    }

    [Fact]
    public void Load_WithPrefix_FiltersVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PREFIX_TEST_KEY", "TestValue");
        Environment.SetEnvironmentVariable("OTHER_KEY", "OtherValue");
        var source = new EnvironmentVariablesConfigurationSource { Prefix = "PREFIX_" };
        var provider = new EnvironmentVariablesConfigurationProvider(source);

        try
        {
            // Act
            provider.Load();

            // Assert
            Assert.True(provider.TryGet("TEST_KEY", out var value));
            Assert.Equal("TestValue", value);
            Assert.False(provider.TryGet("OTHER_KEY", out _));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PREFIX_TEST_KEY", null);
            Environment.SetEnvironmentVariable("OTHER_KEY", null);
        }
    }

    [Fact]
    public void Load_DoubleUnderscore_ReplacesWithColon()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TEST__SECTION__KEY", "TestValue");
        var source = new EnvironmentVariablesConfigurationSource();
        var provider = new EnvironmentVariablesConfigurationProvider(source);

        try
        {
            // Act
            provider.Load();

            // Assert
            Assert.True(provider.TryGet("TEST:SECTION:KEY", out var value));
            Assert.Equal("TestValue", value);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST__SECTION__KEY", null);
        }
    }
}

