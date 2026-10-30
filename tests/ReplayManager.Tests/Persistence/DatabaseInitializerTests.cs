using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ReplayManager.Infrastructure.Data;
using Xunit;

namespace ReplayManager.Tests.Persistence;

public class DatabaseInitializerTests : IDisposable
{
    private readonly TestDatabaseFixture _fixture;

    public DatabaseInitializerTests()
    {
        _fixture = new TestDatabaseFixture();
    }

    [Fact]
    public async Task InitializeAsync_ShouldSeedDefaultSettings_AndDefaultDescriptionTemplate()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var initializer = new DatabaseInitializer(context);

        // Act
        await initializer.InitializeAsync();

        // Assert
        var hasTemplate = await context.DescriptionTemplates.AnyAsync(t => t.IsDefault);
        hasTemplate.Should().BeTrue();

        var closeActionSetting = await context.AppSettings.FirstOrDefaultAsync(s => s.Key == "CloseAction");
        closeActionSetting.Should().NotBeNull();
        closeActionSetting!.Value.Should().Be("Prompt");

        var maxUploadsSetting = await context.AppSettings.FirstOrDefaultAsync(s => s.Key == "MaxConcurrentUploads");
        maxUploadsSetting.Should().NotBeNull();
        maxUploadsSetting!.Value.Should().Be("1");

        // Executar novamente não deve duplicar registros
        await initializer.InitializeAsync();
        var templateCount = await context.DescriptionTemplates.CountAsync(t => t.IsDefault);
        templateCount.Should().Be(1);
    }

    public void Dispose()
    {
        _fixture.Dispose();
        GC.SuppressFinalize(this);
    }
}
