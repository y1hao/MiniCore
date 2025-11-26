using MiniCore.Framework.Configuration;
using MiniCore.Framework.Configuration.Json;

namespace MiniCore.Framework.Tests.Configuration;

public class ConfigurationBuilderTests
{
    [Fact]
    public void Add_AddsSourceToSources()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var source = new JsonConfigurationSource { Path = "test.json" };

        // Act
        builder.Add(source);

        // Assert
        Assert.Single(builder.Sources);
        Assert.Same(source, builder.Sources[0]);
    }

    [Fact]
    public void Add_ReturnsSelf()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var source = new JsonConfigurationSource { Path = "test.json" };

        // Act
        var result = builder.Add(source);

        // Assert
        Assert.Same(builder, result);
    }

    [Fact]
    public void Add_NullSource_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.Add(null!));
    }

    [Fact]
    public void Build_WithNoSources_ReturnsEmptyConfiguration()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act
        var config = builder.Build();

        // Assert
        Assert.NotNull(config);
        Assert.Null(config["NonExistentKey"]);
    }

    [Fact]
    public void Properties_CanStoreAndRetrieveValues()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var key = "TestKey";
        var value = "TestValue";

        // Act
        builder.Properties[key] = value;

        // Assert
        Assert.Equal(value, builder.Properties[key]);
    }
}

