using MiniCore.Framework.Configuration;
using MiniCore.Framework.Configuration.Json;

namespace MiniCore.Framework.Tests.Configuration;

public class ConfigurationRootTests
{
    [Fact]
    public void Indexer_GetValue_ReturnsValue()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""TestKey"": ""TestValue""}");

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(tempFile);
            var config = builder.Build();

            // Act
            var value = config["TestKey"];

            // Assert
            Assert.Equal("TestValue", value);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Indexer_SetValue_UpdatesValue()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""TestKey"": ""InitialValue""}");

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(tempFile);
            var config = builder.Build();

            // Act
            config["TestKey"] = "TestValue";

            // Assert
            Assert.Equal("TestValue", config["TestKey"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Indexer_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var config = builder.Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => config[null!]);
        Assert.Throws<ArgumentNullException>(() => config[null!] = "value");
    }

    [Fact]
    public void GetSection_ReturnsConfigurationSection()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""Section"": {""Key"": ""Value""}}");

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(tempFile);
            var config = builder.Build();

            // Act
            var section = config.GetSection("Section");

            // Assert
            Assert.NotNull(section);
            Assert.Equal("Section", section.Key);
            Assert.Equal("Section", section.Path);
            Assert.Equal("Value", section["Key"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetChildren_ReturnsChildSections()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""Section1"": {""Key"": ""Value1""}, ""Section2"": {""Key"": ""Value2""}}");

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(tempFile);
            var config = builder.Build();

            // Act
            var children = config.GetChildren().ToList();

            // Assert
            Assert.Equal(2, children.Count);
            Assert.Contains(children, c => c.Key == "Section1");
            Assert.Contains(children, c => c.Key == "Section2");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Reload_ReloadsConfiguration()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""Key"": ""Value1""}");

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(tempFile);
            var config = builder.Build();

            Assert.Equal("Value1", config["Key"]);

            // Act
            File.WriteAllText(tempFile, @"{""Key"": ""Value2""}");
            config.Reload();

            // Assert
            Assert.Equal("Value2", config["Key"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}

