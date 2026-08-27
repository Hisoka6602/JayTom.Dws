using System.Threading.Tasks;

namespace JayTom.Dws.Client.Presentation;

/// <summary>
/// 为视图模型提供显示和关闭模态对话框的展示层边界。
/// </summary>
internal static class UserDialogService
{
    /// <summary>在指定标识的宿主中显示对话框内容。</summary>
    /// <param name="content">对话框内容。</param>
    /// <param name="dialogIdentifier">宿主对话框标识。</param>
    /// <returns>关闭对话框时传递的结果。</returns>
    public static Task<object?> Show(object content, object dialogIdentifier) =>
        MaterialDesignThemes.Wpf.DialogHost.Show(content, dialogIdentifier);

    /// <summary>判断指定宿主当前是否存在已打开的对话框。</summary>
    /// <param name="dialogIdentifier">宿主对话框标识。</param>
    /// <returns>存在已打开的对话框时返回 <see langword="true"/>。</returns>
    public static bool IsDialogOpen(object dialogIdentifier) =>
        MaterialDesignThemes.Wpf.DialogHost.IsDialogOpen(dialogIdentifier);

    /// <summary>关闭指定宿主中的对话框。</summary>
    /// <param name="dialogIdentifier">宿主对话框标识。</param>
    public static void Close(object dialogIdentifier) =>
        MaterialDesignThemes.Wpf.DialogHost.Close(dialogIdentifier);
}
