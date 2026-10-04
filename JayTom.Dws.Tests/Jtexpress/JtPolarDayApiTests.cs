using System.Drawing;
using System.Net;
using System.Text.Json;
using JayTom.Dws.Abstractions.Graphics;
using JayTom.Dws.Abstractions.Imaging;
using JayTom.Dws.Client.Service.ImageService;
using JayTom.Dws.Integrations.Jtexpress;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Legacy.Contracts.Dto.BaseInfoModels;
using JayTom.Dws.Legacy.Contracts.Services.ImageService;
using JayTom.Dws.Tests.Application;

namespace JayTom.Dws.Tests;

/// <summary>验证极昼复用本地存图完成事件的文件，以及无需再次编码的上传路径。</summary>
public sealed partial class JtPolarDayApiTests
{
    /// <summary>存图事件发生时文件已完整可读；合规水印图保持原始字节，缺少字段的图片才补水印。</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SavedFileIsReadyBeforeUpload(bool completeWatermark)
    {
        var directory = Directory.CreateTempSubdirectory("jaytom-polar-day-saved-");
        try
        {
            var settings = new ImageSettingsDto
            {
                ImageRootDirectory = directory.FullName,
                IsSaveBarcodeImage = true,
                IsUseWatermark = true,
                SubDirectoryTemplate = [new ItemTemplateInfo
                {
                    ApplicationType = ItemApplicationType.SubDirectory, Type = 1, Content = "{Year}"
                }],
                ImageNamingTemplate = [new ItemTemplateInfo
                {
                    ApplicationType = ItemApplicationType.ImageNaming, Type = 1, Content = "{BarCode}"
                }],
                WatermarkInfo = new WatermarkInfo
                {
                    WatermarkFontSize = 12,
                    WatermarkColor = new RgbaColor(255, 255, 255, 255),
                    ItemTemplate = [new ItemTemplateInfo { Content = "{BarCode}" },
                        new ItemTemplateInfo { Content = "{ScanTime}" }]
                }
            };
            if (completeWatermark)
            {
                settings.WatermarkInfo.ItemTemplate.Add(new ItemTemplateInfo { Content = "{Weight}" });
            }
            var store = new InMemorySettingsStore(new Dictionary<string, string>
            {
                ["SaveImageSettings"] = JsonSerializer.Serialize(settings)
            });
            await using var storage = new DefaultImageStorageService(
                new JayTom.Dws.Plugin.SaveImage.SaveImage(), store,
                new JayTom.Dws.Plugin.Ftp.FluentFtpClient(), new SavedImageTestEventBus());
            ImageSavedEventArgs? completed = null;
            byte[]? savedContent = null;
            storage.ImageSaved += (_, args) =>
            {
                Assert.Null(completed);
                Assert.True(File.Exists(args.FilePath));
                savedContent = File.ReadAllBytes(args.FilePath!);
                using var stream = new MemoryStream(savedContent);
                using var savedImage = new Bitmap(stream);
                Assert.Equal(new Size(640, 480), savedImage.Size);
                completed = args;
            };
            storage.ImageSaveFailed += (_, exception) => Assert.Fail(exception.ToString());
            var scanTime = new DateTime(2026, 10, 3, 9, 30, 0);
            await storage.SaveAndDisposeImageAsync(1_234_567,
                ImageHandle.TakeOwnership(new Bitmap(640, 480)), SaveImageType.BarcodeImage,
                "JT001", 1.235m, scanTime, 0, 0, 0, 0, "CAM01");
            Assert.NotNull(completed);
            Assert.NotNull(savedContent);
            Assert.Equal(1_234_567, completed.PackageTimestamp);
            Assert.Equal(completeWatermark, completed.HasBarcodeScanWeightWatermark);
            using var handler = new StubHttpMessageHandler(async (request, token) =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/getUploadUrl", StringComparison.Ordinal))
                {
                    return StubHttpMessageHandler.CreateOkResponse(
                        "{\"code\":1,\"data\":{\"halfPath\":\"saved.jpg\",\"fullPath\":\"https://storage.test/saved.jpg\",\"contentType\":\"image/jpeg\"}}");
                }
                if (request.Method == HttpMethod.Put)
                {
                    var uploadedContent = await request.Content!.ReadAsByteArrayAsync(token);
                    Assert.InRange(uploadedContent.Length, 4, JtPolarDayScanImageProcessor.MaxImageSizeBytes);
                    if (completeWatermark)
                    {
                        Assert.Equal(savedContent, uploadedContent);
                    }
                    else
                    {
                        using var stream = new MemoryStream(uploadedContent);
                        using var uploadedImage = new Bitmap(stream);
                        Assert.True(uploadedImage.Height > 480);
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                Assert.Contains("saved.jpg", await request.Content!.ReadAsStringAsync(token), StringComparison.Ordinal);
                return StubHttpMessageHandler.CreateOkResponse("{\"code\":1,\"msg\":\"success\"}");
            });
            var api = new JtPolarDayApi(new StubHttpClientFactory(handler));
            Assert.True((await api.SetParameters(CreateParameters())).Key);
            var context = new JtPolarDayApi.UploadContext
            {
                SavedScanImageFilePath = completed.FilePath,
                SavedScanImageHasRequiredWatermark = completed.HasBarcodeScanWeightWatermark,
                CarNum = "1", GridNo = "44", GridCode = "111"
            };
            await api.UploadInBackground("JT001", 1.235m, scanTime, other: context);
            Assert.Equal(4, handler.RequestCount);
            Assert.Equal("saved.jpg", context.HalfPath);
            Assert.Null(context.ScanImageContent);
            Assert.Equal(savedContent, await File.ReadAllBytesAsync(completed.FilePath!));
            Assert.Empty(Directory.EnumerateFiles(directory.FullName, "*.tmp", SearchOption.AllDirectories));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    /// <summary>图片文件缺失时必须保留失败状态，禁止先发送无图设备报文。</summary>
    [Fact]
    public async Task MissingSavedFileDoesNotSubmitDeviceInfo()
    {
        using var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.CreateOkResponse("{\"code\":1}"));
        var api = new JtPolarDayApi(new StubHttpClientFactory(handler));
        Assert.True((await api.SetParameters(CreateParameters())).Key);
        var context = new JtPolarDayApi.UploadContext
        {
            SavedScanImageFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".jpg"),
            CarNum = "1", GridNo = "44", GridCode = "111"
        };
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            api.UploadInBackground("JT001", 1, DateTime.Now, other: context));
        Assert.Equal(0, handler.RequestCount);
        Assert.Null(context.HalfPath);
    }

    /// <summary>图片上传完成后设备接口失败，即使本地图片随后清理，重试也复用半路径。</summary>
    [Fact]
    public async Task DeviceRetryUsesHalfPathWithoutReopeningLocalFile()
    {
        var directory = Directory.CreateTempSubdirectory("jaytom-polar-day-retry-");
        try
        {
            var path = Path.Combine(directory.FullName, "saved.jpg");
            using (var image = new Bitmap(2, 2))
            {
                await File.WriteAllBytesAsync(path,
                    JtPolarDayApi.PrepareScanImageContent(image, "JT001", DateTime.Now, 1));
            }
            var deviceRequests = 0;
            using var handler = new StubHttpMessageHandler(request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/getUploadUrl", StringComparison.Ordinal))
                {
                    return StubHttpMessageHandler.CreateOkResponse(
                        "{\"code\":1,\"data\":{\"halfPath\":\"retry.jpg\",\"fullPath\":\"https://storage.test/retry.jpg\",\"contentType\":\"image/jpeg\"}}");
                }
                if (request.Method == HttpMethod.Put)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                return ++deviceRequests == 1
                    ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    : StubHttpMessageHandler.CreateOkResponse("{\"code\":1,\"msg\":\"success\"}");
            });
            var api = new JtPolarDayApi(new StubHttpClientFactory(handler));
            var parameters = CreateParameters();
            parameters.RetryCount = 1;
            Assert.True((await api.SetParameters(parameters)).Key);
            var context = new JtPolarDayApi.UploadContext
            {
                SavedScanImageFilePath = path, SavedScanImageHasRequiredWatermark = true,
                CarNum = "1", GridNo = "44", GridCode = "111"
            };
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                api.UploadInBackground("JT001", 1, DateTime.Now, other: context));
            File.Delete(path);
            await api.UploadInBackground("JT001", 1, DateTime.Now, other: context);
            Assert.Equal("retry.jpg", context.HalfPath);
            Assert.Equal(5, handler.RequestCount);
            Assert.Equal(3, deviceRequests);
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
