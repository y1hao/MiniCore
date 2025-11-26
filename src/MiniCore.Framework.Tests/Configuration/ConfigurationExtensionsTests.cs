using MiniCore.Framework.Configuration;
using MiniCore.Framework.Configuration.Json;

namespace MiniCore.Framework.Tests.Configuration;

public class ConfigurationExtensionsTests
{
    [Fact]
    public void GetValue_String_ReturnsValue()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""StringKey"": ""TestValue""}");

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(tempFile);
            var config = builder.Build();

            // Act
            var value = config.GetValue<string>("StringKey");

            // Assert
            Assert.Equal("TestValue", value);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetValue_Int_ReturnsValue()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""IntKey"": ""42""}");

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(tempFile);
            var config = builder.Build();

            // Act
            var value = config.GetValue<int>("IntKey");

            // Assert
            Assert.Equal(42, value);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetValue_Bool_ReturnsValue()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""BoolKey"": ""true""}");

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(tempFile);
            var config = builder.Build();

            // Act
            var value = config.GetValue<bool>("BoolKey");

            // Assert
            Assert.True(value);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetValue_WithDefault_ReturnsDefaultWhenKeyNotFound()
    {
        // Arrange
        var builder = new ConfigurationBuilder();
        var config = builder.Build();

        // Act
        var value = config.GetValue<int>("NonExistentKey", 99);

        // Assert
        Assert.Equal(99, value);
    }

    [Fact]
    public void Bind_SimpleObject_BindsProperties()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""Name"": ""Test"", ""Age"": ""25""}");

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(tempFile);
            var config = builder.Build();

            // Act
            var person = config.Bind<Person>();

            // Assert
            Assert.NotNull(person);
            Assert.Equal("Test", person.Name);
            Assert.Equal(25, person.Age);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Bind_NestedObject_BindsNestedProperties()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, @"{""Section"": {""Key"": ""Value""}}");

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(tempFile);
            var config = builder.Build();
            var section = config.GetSection("Section");

            // Act
            var nested = section.Bind<NestedConfig>();

            // Assert
            Assert.NotNull(nested);
            Assert.Equal("Value", nested.Key);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    public class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public class NestedConfig
    {
        public string Key { get; set; } = string.Empty;
    }
}

