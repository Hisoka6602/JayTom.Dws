namespace JayTom.Dws.Abstractions.Imaging;

/// <summary>封装平台图像对象并明确其唯一释放责任。</summary>
public sealed class ImageHandle : IDisposable
{
    /// <summary>当前持有的平台图像对象。</summary>
    private IDisposable? _value;

    /// <summary>创建图像所有权句柄。</summary>
    private ImageHandle(IDisposable value) => _value = value;

    /// <summary>接管指定非空平台图像对象的释放责任。</summary>
    public static ImageHandle TakeOwnership(IDisposable value) => new(value);

    /// <summary>对象存在时接管释放责任；空对象不创建句柄。</summary>
    public static ImageHandle? TakeOwnershipIfPresent(IDisposable? value) =>
        value is null ? null : new ImageHandle(value);

    /// <summary>以指定平台类型访问图像对象。</summary>
    public T As<T>() where T : class, IDisposable =>
        _value as T ?? throw new InvalidOperationException(
            $"图像对象不是所需的 {typeof(T).FullName} 类型。");

    /// <summary>释放当前持有的平台图像对象。</summary>
    public void Dispose() => Interlocked.Exchange(ref _value, null)?.Dispose();
}
