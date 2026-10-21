using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Application.Validators;
using Microsoft.AspNetCore.Http;
using Moq;

namespace UnitTests.Plugins.Application.Validators;

public sealed class UploadPluginValidatorTests
{
    private readonly UploadPluginValidator _validator = new();

    [Fact]
    public void Validate_WhenFileIsNull_ReturnsBadRequest()
    {
        UploadPluginRequest request = new()
        {
            File = null!
        };

        Result result = _validator.Validate(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public void Validate_WhenFileIsEmpty_ReturnsBadRequest()
    {
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(0L);
        fileMock.Setup(f => f.FileName).Returns("Plugin.v1.0.0.dll");

        UploadPluginRequest request = new()
        {
            File = fileMock.Object
        };

        Result result = _validator.Validate(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public void Validate_WhenNotDllExtension_ReturnsBadRequest()
    {
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(100L);
        fileMock.Setup(f => f.FileName).Returns("Plugin.v1.0.0.txt");

        UploadPluginRequest request = new()
        {
            File = fileMock.Object
        };

        Result result = _validator.Validate(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public void Validate_WhenInvalidVersionPattern_ReturnsBadRequest()
    {
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(100L);
        fileMock.Setup(f => f.FileName).Returns("InvalidPluginNaming.dll");

        UploadPluginRequest request = new()
        {
            File = fileMock.Object
        };

        Result result = _validator.Validate(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public void Validate_WhenBinaryIsNotManagedAssembly_ReturnsBadRequest()
    {
        byte[] fakeNativeBytes = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00];
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(fakeNativeBytes.Length);
        fileMock.Setup(f => f.FileName).Returns("NativeLib.v1.0.0.dll");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(fakeNativeBytes));

        UploadPluginRequest request = new()
        {
            File = fileMock.Object
        };

        Result result = _validator.Validate(request);

        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public void Validate_WhenValidManagedAssembly_ReturnsSuccess()
    {
        byte[] validAssemblyBytes = File.ReadAllBytes(typeof(UploadPluginValidator).Assembly.Location);
        Mock<IFormFile> fileMock = new();
        fileMock.Setup(f => f.Length).Returns(validAssemblyBytes.Length);
        fileMock.Setup(f => f.FileName).Returns("Contoso.Plugin.v1.0.0.dll");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(validAssemblyBytes));

        UploadPluginRequest request = new()
        {
            File = fileMock.Object
        };

        Result result = _validator.Validate(request);

        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
    }
}
