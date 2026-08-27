namespace JayTom.Dws.Client.Events;

/// <summary>定义桌面表现层可以对窗口执行的操作。</summary>
public enum WindowsActionType
{
    /// <summary>Minimize the window.</summary>
    Minimize,

    /// <summary>Maximize the window.</summary>
    Maximize,

    /// <summary>Restore the window.</summary>
    Restore,

    /// <summary>Show the window.</summary>
    Show,

    /// <summary>Hide the window.</summary>
    Hide,

    /// <summary>Close the window.</summary>
    Close,

    /// <summary>Activate the window.</summary>
    Activate
}
