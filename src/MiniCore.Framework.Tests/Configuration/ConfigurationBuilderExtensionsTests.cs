using MiniCore.Framework.Configuration;
using MiniCore.Framework.Configuration.EnvironmentVariables;
using MiniCore.Framework.Configuration.Json;

namespace MiniCore.Framework.Tests.Configuration;

public class ConfigurationBuilderExtensionsTests
{
    [Fact]
    public void AddJsonFile_AddsJsonConfigurationSource()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            builder.AddJsonFile(tempFile);

            // Assert
            Assert.Single(builder.Sources);
            Assert.IsType<JsonConfigurationSource>(builder.Sources[0]);
            var source = (JsonConfigurationSource)builder.Sources[0];
            Assert.Equal(tempFile, source.Path);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void AddJsonFile_WithOptional_ConfiguresSource()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            builder.AddJsonFile(tempFile, optional: true);

            // Assert
            var source = (JsonConfigurationSource)builder.Sources[0];
            Assert.True(source.Optional);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void AddEnvironmentVariables_AddsEnvironmentVariablesSource()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddEnvironmentVariables();

        // Assert
        Assert.Single(builder.Sources);
        Assert.IsType<EnvironmentVariablesConfigurationSource>(builder.Sources[0]);
    }

    [Fact]
    public void AddEnvironmentVariables_WithPrefix_ConfiguresSource()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        builder.AddEnvironmentVariables("TEST_");

        // Assert
        var source = (EnvironmentVariablesConfigurationSource)builder.Sources[0];
        Assert.Equal("TEST_", source.Prefix);
    }

    [Fact]
    public void MultipleSources_LoadsInOrder()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""Key"": ""JsonValue""}");
        Environment.SetEnvironmentVariable("Key", "EnvValue");

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(tempFile);
            builder.AddEnvironmentVariables();
            var config = builder.Build();

            // Act
            var value = config["Key"];

            // Assert
            // Later sources override earlier ones, so environment variables take precedence
            Assert.Equal("EnvValue", value);
        }
        finally
        {
            File.Delete(tempFile);
            Environment.SetEnvironmentVariable("Key", null);
        }
    }

    [Fact]
    public void MultipleSources_EnvironmentVariablesOverrideJson()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""Key"": ""JsonValue""}");
        Environment.SetEnvironmentVariable("Key", "EnvValue");

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddEnvironmentVariables();
            builder.AddJsonFile(tempFile);
            var config = builder.Build();

            // Act
            var value = config["Key"];

            // Assert
            // Environment variables loaded first, JSON loaded second takes precedence
            Assert.Equal("JsonValue", value);
        }
        finally
        {
            File.Delete(tempFile);
            Environment.SetEnvironmentVariable("Key", null);
        }
    }
}

