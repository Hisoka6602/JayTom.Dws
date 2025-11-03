# PublishAot and PublishTrimmed Suitability Assessment Report

## Executive Summary

This document evaluates whether the JayTom.Dws project is suitable for using .NET's PublishAot (Ahead-of-Time compilation) and PublishTrimmed (IL Trimming) features.

**Conclusion:**
- ❌ **PublishAot (Native AOT)**: The current project is **NOT suitable** for PublishAot
- ⚠️ **PublishTrimmed (IL Trimming)**: Some projects **can try** PublishTrimmed, but require careful testing

## Project Structure Analysis

### Main Executable Projects

1. **JayTom.Dws.Client** - WPF Desktop Application (.NET 7.0)
2. **JayTom.Dws.ManagementStudio** - WPF Desktop Application (.NET 6.0)
3. **JayTom.Dws.CloudApi** - ASP.NET Core Web API (.NET 7.0)
4. **JayTom.Dws.LicenseApi** - ASP.NET Core Web API (.NET 7.0)
5. **JayTom.Dws.ManagementApi** - ASP.NET Core Web API (.NET 6.0)
6. **JayTom.Dws.UploadCloudService** - Worker Service (.NET 7.0)
7. **MyApplication** - Blazor WebAssembly (.NET 6.0)

## PublishAot (Native AOT) Assessment

### Incompatibility Reasons

#### 1. WPF Applications Not Supported
- `JayTom.Dws.Client` and `JayTom.Dws.ManagementStudio` are WPF applications
- **WPF framework does not support Native AOT**
- WPF relies heavily on reflection and dynamic type loading

#### 2. Entity Framework Core Limitations
The project uses the following EF Core providers:
- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Pomelo.EntityFrameworkCore.MySql.Json.Microsoft`

**Issues:**
- EF Core 5.x does not support Native AOT
- Even when upgrading to EF Core 7.0+, you need to use compile-time generated DbContext and compiled queries
- Current code uses dynamic queries and expression trees

#### 3. Reflection and Dynamic Code Usage

Found the following reflection usage patterns:
```csharp
// JayTom.Dws.Plugin/Excel/NpoiExport.cs
System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public

// JayTom.Dws.Client/ViewModels/Pages/Preferences/AppSettings/OtherSettingsViewModel.cs
System.Reflection.Assembly.GetExecutingAssembly().Location
```

#### 4. Dynamic Code Compilation
- `Microsoft.CodeAnalysis.Scripting` is used in the CloudApi project
- Runtime code compilation and execution is not supported in Native AOT

#### 5. Newtonsoft.Json Usage
- Multiple projects use `Newtonsoft.Json`
- While some functionality can work under AOT, it requires extensive source generator support
- Migration to `System.Text.Json` is recommended for better AOT support

#### 6. Third-party Library Compatibility Issues
Libraries used in the project may not support Native AOT:
- `MaterialDesignThemes` (WPF UI library)
- `LottieSharp` (Animation library)
- `Prism.DryIoc` (MVVM framework)
- `Vlc.DotNet.Wpf` (Video playback)
- `gong-wpf-dragdrop`

### Native AOT Compatibility Matrix

| Project | Native AOT Compatibility | Reason |
|---------|--------------------------|--------|
| JayTom.Dws.Client | ❌ Incompatible | WPF app, uses reflection, unsupported third-party libraries |
| JayTom.Dws.ManagementStudio | ❌ Incompatible | WPF app |
| JayTom.Dws.CloudApi | ❌ Incompatible | Uses CodeAnalysis.Scripting, EF Core 5.x |
| JayTom.Dws.LicenseApi | ⚠️ Potentially Compatible | Requires major changes (upgrade EF Core, remove dynamic code) |
| JayTom.Dws.ManagementApi | ⚠️ Potentially Compatible | Requires major changes |
| JayTom.Dws.UploadCloudService | ⚠️ Potentially Compatible | Depends on dependencies |
| MyApplication | ❌ Incompatible | Blazor WebAssembly doesn't need AOT (already client-side) |

## PublishTrimmed (IL Trimming) Assessment

IL Trimming has fewer restrictions than Native AOT, but still requires attention:

### Projects That Can Try Trimming

#### ✅ Web API Projects (Need Testing)
- JayTom.Dws.CloudApi
- JayTom.Dws.LicenseApi
- JayTom.Dws.ManagementApi

**Recommended Configuration:**
```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>partial</TrimMode>
  <!-- Or use link mode for better compression -->
  <!-- <TrimMode>link</TrimMode> -->
</PropertyGroup>
```

#### ✅ Worker Service
- JayTom.Dws.UploadCloudService

### Issues to Watch For

#### 1. Entity Framework Core
- EF Core 5.x has limited trimming support
- May need to add `<TrimmerRootAssembly>` to preserve necessary assemblies
- Upgrading to EF Core 7.0+ is recommended for better trimming support

#### 2. Reflection and Dynamic Types
Need to mark with `DynamicallyAccessedMembers` attribute:
```csharp
public void ProcessType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
```

#### 3. Newtonsoft.Json
- Not fully trimming-compatible
- Migration to `System.Text.Json` is recommended
- Or add trim warning suppression (not recommended)

#### 4. WPF Applications
- ⚠️ WPF applications can try trimming, but **high risk**
- Extensive XAML and data binding depends on reflection
- **Not recommended** to enable trimming for WPF projects

### IL Trimming Compatibility Matrix

| Project | Trimming Compatibility | Recommended TrimMode | Notes |
|---------|------------------------|---------------------|-------|
| JayTom.Dws.Client | ⚠️ Not Recommended | - | WPF app, XAML binding issues |
| JayTom.Dws.ManagementStudio | ⚠️ Not Recommended | - | WPF app |
| JayTom.Dws.CloudApi | ✅ Can Try | partial | Needs testing, may need to exclude certain assemblies |
| JayTom.Dws.LicenseApi | ✅ Can Try | partial | Needs testing |
| JayTom.Dws.ManagementApi | ✅ Can Try | partial | Needs testing |
| JayTom.Dws.UploadCloudService | ✅ Can Try | partial | Worker services usually have better compatibility |
| MyApplication | ⚠️ Special Case | - | Blazor WASM has its own trimming mechanism |

## Recommendations and Action Plan

### Short-term Recommendations (Can be implemented immediately)

#### 1. Enable PublishTrimmed for Web API Projects (Experimental)

Can try enabling IL trimming in the following projects:
- JayTom.Dws.LicenseApi
- JayTom.Dws.ManagementApi

**Steps:**
1. Update project files to enable partial trimming
2. Run complete integration tests
3. Check application logs for trimming warnings
4. Test all API endpoints and features

#### 2. Test Configuration Example

Create publish profile (`Properties/PublishProfiles/TrimmedRelease.pubxml`):
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>partial</TrimMode>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
    <SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>
  </PropertyGroup>
</Project>
```

### Mid-term Recommendations (Require some refactoring)

#### 1. Migrate to System.Text.Json
- Replace Newtonsoft.Json with System.Text.Json
- Better trimming and AOT support
- Requires updating serialization/deserialization code

#### 2. Upgrade Entity Framework Core
- Upgrade from EF Core 5.x to EF Core 7.0 or 8.0
- Leverage compile-time models and compiled queries
- Improved trimming support

#### 3. Reduce Reflection Usage
- Use source generators instead of reflection
- Use compile-time known types instead of dynamic types

### Long-term Recommendations (Require major architectural changes)

#### 1. Separate Dynamic Features
- Isolate features using `Microsoft.CodeAnalysis.Scripting` to separate services
- Core API can support AOT, with dynamic scripting as optional extension

#### 2. Consider Minimal APIs
- For simple API services, consider using Minimal APIs
- Native support for Native AOT

#### 3. Plugin System Refactoring
- Current plugin system relies on dynamic loading
- Consider using compile-time plugin registration

### Not Recommended Practices

❌ **DO NOT enable PublishAot for WPF applications**
- WPF does not support Native AOT
- Will cause compilation failures

❌ **DO NOT enable aggressive PublishTrimmed for WPF applications**
- XAML binding depends on reflection
- Likely to cause runtime errors

❌ **DO NOT enable trimming without adequate testing**
- Trimming may cause runtime errors
- Must have complete test coverage

## Other Options for Performance and Size Optimization

If PublishAot and PublishTrimmed are not applicable, consider these alternatives:

### 1. ReadyToRun (R2R)
```xml
<PropertyGroup>
  <PublishReadyToRun>true</PublishReadyToRun>
</PropertyGroup>
```
- Provides some benefits of ahead-of-time compilation
- Good compatibility
- Faster startup time
- But published package will be larger

### 2. Single File Publishing
```xml
<PropertyGroup>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
</PropertyGroup>
```
- All files packaged into one executable
- Easier to distribute
- But doesn't reduce total size

### 3. Framework-Dependent Deployment
```xml
<PropertyGroup>
  <SelfContained>false</SelfContained>
</PropertyGroup>
```
- Requires .NET runtime installed on target machine
- Greatly reduces deployment size

## Testing Checklist

If you decide to enable trimming, perform the following tests:

- [ ] All API endpoints respond correctly
- [ ] Database operations work (CRUD)
- [ ] Entity Framework queries work normally
- [ ] JSON serialization/deserialization is correct
- [ ] Dependency injection container works normally
- [ ] SignalR connections and messaging work
- [ ] Authentication and authorization work
- [ ] All third-party library features work
- [ ] Logging works properly
- [ ] Configuration loading is correct
- [ ] Plugin system (if applicable) works normally

## References

- [Native AOT deployment](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
- [Trim self-contained deployments and executables](https://learn.microsoft.com/dotnet/core/deploying/trimming/trim-self-contained)
- [Introduction to AOT warnings](https://learn.microsoft.com/dotnet/core/deploying/native-aot/fixing-warnings)
- [EF Core and trimming](https://learn.microsoft.com/ef/core/performance/advanced-performance-topics#compiled-queries)
- [ASP.NET Core Native AOT](https://learn.microsoft.com/aspnet/core/fundamentals/native-aot)

## Summary

**PublishAot**: Due to extensive use of WPF, Entity Framework Core 5.x, reflection, and dynamic code compilation, the current project is **NOT suitable** for Native AOT.

**PublishTrimmed**: Web API projects (JayTom.Dws.CloudApi, JayTom.Dws.LicenseApi, JayTom.Dws.ManagementApi) can **cautiously try** using partial trimming mode, but must undergo comprehensive testing. WPF projects are not recommended for trimming.

The most pragmatic optimization approach is to use **ReadyToRun** compilation to improve startup performance while maintaining maximum compatibility.
