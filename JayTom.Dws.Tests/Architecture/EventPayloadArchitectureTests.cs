using System.Reflection;
using System.Runtime.CompilerServices;
using JayTom.Dws.Application.Events;
using JayTom.Dws.Domain.Packages;

namespace JayTom.Dws.Tests.Architecture;

/// <summary>锁定跨线程与跨层事件载荷初始化后不可重新赋值。</summary>
public sealed class EventPayloadArchitectureTests
{
    /// <summary>所有业务及兼容事件的公开属性必须为只读或 init-only。</summary>
    [Fact]
    public void Event_payload_properties_are_init_only()
    {
        Type[] eventTypes =
        [
            typeof(SettingsChangedEvent),
            typeof(TriggerPositionEvent),
            typeof(BarcodeTypeProviderEvent),
            typeof(PluginParamChangedEvent),
            typeof(RemoteAction),
            typeof(ApplicationStatusChanged),
            typeof(PackageExitUpdateEvent),
            typeof(PushPackageInfo),
            typeof(PushAlternateExitSorterEvent),
            typeof(PackageLifecycleChanged),
            typeof(PackageBarcodeAssigned)
        ];

        string[] mutableProperties = eventTypes
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Where(property => property.SetMethod is not null &&
                               !property.SetMethod.ReturnParameter
                                   .GetRequiredCustomModifiers()
                                   .Contains(typeof(IsExternalInit)))
            .Select(property => $"{property.DeclaringType?.Name}.{property.Name}")
            .ToArray();

        Assert.Empty(mutableProperties);
    }
}
