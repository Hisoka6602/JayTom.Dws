extern alias ClientRuntime;

using System.Collections;
using System.Drawing;
using System.Reflection;
using JayTom.Dws.Abstractions.Imaging;
using JayTom.Dws.Application.Configuration;
using JayTom.Dws.Application.Messaging;
using JayTom.Dws.Legacy.Contracts.Dto;
using JayTom.Dws.Legacy.Contracts.Model;
using JayTom.Dws.Legacy.Contracts.Services.ImageService;
using JayTom.Dws.Plugin.SaveImage;
using DeviceService = ClientRuntime::JayTom.Dws.Client.Service.Device.IDeviceService;
using ImageWorkflow = ClientRuntime::JayTom.Dws.Client.Service.BackgroundService.SaveImageBackgroundService;

namespace JayTom.Dws.Tests.Application;

public sealed class ImageBurstQueueTests
{
    /// <summary>暂停消费者模拟磁盘短暂阻塞：小图集中返回必须保留，大图仍受解码内存预算限制。</summary>
    [Theory]
    [InlineData(512, 96, 64, 512, 0)]
    [InlineData(18, 2048, 2048, 16, 2)]
    public void Burst_preserves_small_images_and_releases_large_images_exceeding_memory_budget(
        int count, int width, int height, int retained, int dropped)
    {
        using var workflow = new ImageWorkflow(
            DispatchProxy.Create<IImageStorageService, LowerMachineNoReadWorkflowTests.RuntimeProxy>(),
            DispatchProxy.Create<ISaveImage, LowerMachineNoReadWorkflowTests.RuntimeProxy>(),
            DispatchProxy.Create<ISettingsStore, LowerMachineNoReadWorkflowTests.RuntimeProxy>(),
            DispatchProxy.Create<DeviceService, LowerMachineNoReadWorkflowTests.RuntimeProxy>(),
            DispatchProxy.Create<IEventBus, LowerMachineNoReadWorkflowTests.RuntimeProxy>());
        var enqueue = typeof(ImageWorkflow).GetMethod("EnqueueImage", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var images = new List<ImageHandle>();
        try
        {
            for (var index = 0; index < count; index++)
            {
                var image = ImageHandle.TakeOwnership(new Bitmap(width, height));
                images.Add(image);
                enqueue.Invoke(workflow, [new ImageMessageInfo
                {
                    Image = image, Type = SaveImageType.BarcodeImage, PackageTimestamped = index + 1
                }]);
            }
            var queue = (ICollection)Read(workflow, "_imageItems");
            Assert.Equal(retained, queue.Count);
            Assert.Equal((long)dropped, Read(workflow, "_droppedImageCount"));
            Assert.InRange((long)Read(workflow, "_pendingImageBytes"), 0, 256L * 1024 * 1024);
            for (var index = 0; index < dropped; index++)
            {
                var discarded = images[index];
                Assert.Throws<InvalidOperationException>(() => discarded.As<Bitmap>());
            }
            for (var index = dropped; index < count; index++) Assert.Equal(width, images[index].As<Bitmap>()!.Width);
        }
        finally
        {
            foreach (var image in images) image.Dispose();
        }
    }

    private static object Read(object value, string name) => typeof(ImageWorkflow)
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;
}
