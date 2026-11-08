using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReplayManager.Application.Contracts.YouTube;
using ReplayManager.Domain.Entities;
using ReplayManager.Domain.Enums;
using ReplayManager.Infrastructure.Security;
using ReplayManager.Infrastructure.YouTube;
using Xunit;

namespace ReplayManager.Tests.YouTube;

public class YouTubeUploaderTests
{
    private readonly Mock<ITitleFormatter> _titleFormatterMock = new();
    private readonly Mock<IDescriptionTemplateEngine> _templateEngineMock = new();

    [Fact]
    public async Task UploadVideoAsync_ReturnsFailed_WhenFileMissing()
    {
        // Arrange
        var video = new VideoRecord
        {
            Id = Guid.NewGuid(),
            FilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".mp4"),
            PrivacyStatus = YouTubePrivacyStatus.Unlisted
        };

        var authService = new YouTubeAuthService(new DpapiCredentialStorage());

        var uploader = new YouTubeUploader(
            authService: authService,
            titleFormatter: _titleFormatterMock.Object,
            templateEngine: _templateEngineMock.Object,
            logger: NullLogger<YouTubeUploader>.Instance);

        // Act
        var result = await uploader.UploadVideoAsync(video, null, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Arquivo de vídeo não encontrado");
    }

    [Fact]
    public async Task UploadVideoAsync_ReturnsFailed_WhenUnauthenticated()
    {
        // Arrange: temporary empty file exists
        var tempFile = Path.GetTempFileName();
        try
        {
            var video = new VideoRecord
            {
                Id = Guid.NewGuid(),
                FilePath = tempFile,
                PrivacyStatus = YouTubePrivacyStatus.Unlisted
            };

            var emptyDir = Path.Combine(Path.GetTempPath(), "EmptyTokens_" + Guid.NewGuid().ToString("N"));
            var authService = new YouTubeAuthService(
                new DpapiCredentialStorage(Path.Combine(emptyDir, "cred")),
                tokenDirectory: Path.Combine(emptyDir, "tokens"));

            var uploader = new YouTubeUploader(
                authService: authService,
                titleFormatter: _titleFormatterMock.Object,
                templateEngine: _templateEngineMock.Object,
                logger: NullLogger<YouTubeUploader>.Instance);

            // Act
            var result = await uploader.UploadVideoAsync(video, null, CancellationToken.None);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Conta do YouTube não autenticada");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
