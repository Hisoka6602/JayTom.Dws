using System.Drawing;
using JayTom.Dws.Interface.Cloud;
using JayTom.Dws.Interface.Cloud.CloudVideo;

namespace JayTom.Dws.Tests;

/// <summary>
/// 验证云视频上传的响应判定和调用方数据完整性。
/// </summary>
public sealed class CloudVideoUploadApiTests
{
    /// <summary>
    /// 验证包含 true 字样但不表示成功的响应不会被误判。
    /// </summary>
    [Fact]
    public async Task UploadRejectsAmbiguousTrueSubstring()
    {
        using var handler = new StubHttpMessageHandler(
            _ => StubHttpMessageHandler.CreateOkResponse("not true"));
        var api = new CloudVideoUploadApi(new StubHttpClientFactory(handler));
        await api.SetParameters(new CloudVideoUploadApi.CloudVideoApiParameters
        {
            WebDoMain = "http://unit.test/cloud",
            Timeout = 2000
        });

        var response = await api.UploadData(new PackageCloudInfo());

        Assert.False(response.IsSuccessful);
    }

    /// <summary>
    /// 验证上传序列化不会清空调用方持有的图片对象。
    /// </summary>
    [Fact]
    public async Task UploadDoesNotMutateImageMetadata()
    {
        using var handler = new StubHttpMessageHandler(
            _ => StubHttpMessageHandler.CreateOkResponse("{\"success\":true}"));
        var api = new CloudVideoUploadApi(new StubHttpClientFactory(handler));
        await api.SetParameters(new CloudVideoUploadApi.CloudVideoApiParameters
        {
            WebDoMain = "http://unit.test/cloud",
            Timeout = 2000
        });
        using var image = new Bitmap(1, 1);
        var imageInfo = new PackageCloudImageInfo
        {
            Type = 0,
            CameraSerialNumber = "CAM01",
            CustomCameraName = "扫码相机",
            Image = image
        };
        var package = new PackageCloudInfo
        {
            ImageInfos = [imageInfo]
        };

        var response = await api.UploadData(package);

        Assert.True(response.IsSuccessful);
        Assert.Same(image, imageInfo.Image);
        Assert.DoesNotContain("\"Image\"", response.UploadContent, StringComparison.Ordinal);
    }
}
