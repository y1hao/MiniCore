using MiniCore.Framework.Configuration.Json;

namespace MiniCore.Framework.Tests.Configuration;

public class JsonConfigurationProviderTests
{
    [Fact]
    public void Load_SimpleJson_LoadsValues()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""Key1"": ""Value1"", ""Key2"": ""Value2""}");

        try
        {
            var source = new JsonConfigurationSource { Path = tempFile };
            var provider = new JsonConfigurationProvider(source);

            // Act
            provider.Load();

            // Assert
            Assert.True(provider.TryGet("Key1", out var value1));
            Assert.Equal("Value1", value1);
            Assert.True(provider.TryGet("Key2", out var value2));
            Assert.Equal("Value2", value2);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_NestedJson_LoadsHierarchicalValues()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""Section"": {""Key"": ""Value""}}");

        try
        {
            var source = new JsonConfigurationSource { Path = tempFile };
            var provider = new JsonConfigurationProvider(source);

            // Act
            provider.Load();

            // Assert
            Assert.True(provider.TryGet("Section:Key", out var value));
            Assert.Equal("Value", value);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_ArrayJson_LoadsArrayValues()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""Array"": [""Value1"", ""Value2""]}");

        try
        {
            var source = new JsonConfigurationSource { Path = tempFile };
            var provider = new JsonConfigurationProvider(source);

            // Act
            provider.Load();

            // Assert
            Assert.True(provider.TryGet("Array:0", out var value1));
            Assert.Equal("Value1", value1);
            Assert.True(provider.TryGet("Array:1", out var value2));
            Assert.Equal("Value2", value2);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_OptionalFile_DoesNotThrow()
    {
        // Arrange
        var source = new JsonConfigurationSource { Path = "nonexistent.json", Optional = true };
        var provider = new JsonConfigurationProvider(source);

        // Act & Assert
        provider.Load(); // Should not throw
    }

    [Fact]
    public void Load_RequiredFileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var source = new JsonConfigurationSource { Path = "nonexistent.json", Optional = false };
        var provider = new JsonConfigurationProvider(source);

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => provider.Load());
    }

    [Fact]
    public void Load_InvalidJson_ThrowsFormatException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{invalid json}");

        try
        {
            var source = new JsonConfigurationSource { Path = tempFile };
            var provider = new JsonConfigurationProvider(source);

            // Act & Assert
            Assert.Throws<FormatException>(() => provider.Load());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_UpdatesValue()
    {
        // Arrange
        var source = new JsonConfigurationSource { Path = "test.json" };
        var provider = new JsonConfigurationProvider(source);

        // Act
        provider.Set("TestKey", "TestValue");

        // Assert
        Assert.True(provider.TryGet("TestKey", out var value));
        Assert.Equal("TestValue", value);
    }
}

