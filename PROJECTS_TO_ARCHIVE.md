# Projects to Archive or Remove

This document lists projects that are not part of the core JayTom.Dws.Client solution and could be archived or removed to further simplify the repository.

## Categories of Projects to Consider

### 1. Test/Demo Applications (High Priority for Removal)
These are test applications created during development that are no longer needed:

- `WpfApp1/` - Test WPF application
- `WpfApp2/` - Test WPF application  
- `WpfApp3/` - Test WPF application
- `WinFormsApp1/` - Test WinForms application
- `ConsoleApp1/` - Test console application
- `ConsoleApp2/` - Test console application
- `ConsoleApp3/` - Test console application
- `ConsoleApp4/` - Test console application
- `ConsoleApp5/` - Test console application
- `ConsoleApp6/` - Test console application
- `ConsoleApp7/` - Test console application
- `WebApplication1/` - Test web application

### 2. Hardware/Device Test Projects (Medium Priority)
These are device-specific test applications that could be archived:

- `HuaraytechTest/` - Huaraytech device test
- `Wpf.HuaraytechTest/` - Huaraytech WPF test
- `HikvisionIndustrialCameraTest/` - Hikvision test
- `Wpf.HikvisionIndustrialCameraTest/` - Hikvision WPF test
- `HikvisionSmartCameraTest/` - Hikvision smart camera test
- `Wpf.HikvisionSmartCameraTest/` - Hikvision smart camera WPF test
- `CameraTest/` - Camera test application
- `IraypleTest/` - Irayple test
- `UsbCameraTest/` - USB camera test
- `WayzimTest/` - Wayzim test
- `BaseDaHuatech.Wpf/` - Dahua test

### 3. HID/Input Device Tests (Medium Priority)
Tests for various input devices:

- `Hid-Test/` - HID test
- `TestHidSharp/` - HidSharp test
- `TestHidSharp2/` - HidSharp test 2
- `TestRawInput/` - Raw input test
- `Test.Hid.Net/` - HID.Net test
- `Test.Usb.Net/` - USB.Net test
- `Test.WindowsHook/` - Windows hook test
- `Test.HIDDevices/` - HID devices test
- `Test.Dx.RawInput/` - DirectX raw input test
- `RawInputAA/` - Raw input test
- `BarcodeScannerDeviceTest/` - Barcode scanner test

### 4. Machine Learning/AI Tests (Low Priority - May be Needed)
These might be needed for future features:

- `OnnxTest/` - ONNX model test
- `YoloTest/` - YOLO detection test
- `OpenCVTest/` - OpenCV test
- `BaiDuOcrTest/` - Baidu OCR test

### 5. API Projects (Low Priority - Separate Services)
These are separate API services that could be in their own repositories:

- `JayTom.Dws.ManagementApi/` - Management API
- `JayTom.Dws.VideoApi/` - Video API
- `JayTom.Dws.CloudApi/` - Cloud API
- `JayTom.Dws.LicenseApi/` - License API
- `JayTom.Dws.PostSoapApi/` - SOAP API

### 6. API Clients (Low Priority - May be Needed)
API client libraries that might be used:

- `JayTom.Dws.VideoApiClient/` - Video API client
- `JayTom.Dws.CloudApiClient/` - Cloud API client
- `JayTom.Dws.LicenseApiClient/` - License API client

### 7. Management/Admin Tools (Low Priority)
Separate management applications:

- `JayTom.Dws.ManagementStudio/` - Management studio application
- `JayTom.Dws.UpdaterClient/` - Update client
- `JayTom.Dws.LicenseClient/` - License client
- `JayTom.Dws.TemporaryClient/` - Temporary client

### 8. Background Services (Low Priority - May be Needed)
Windows services that run in the background:

- `JayTom.Dws.Service/` - Main service
- `JayTom.Dws.DataInteractionService/` - Data interaction service
- `JayTom.Dws.SystemStatusMonitorService/` - System monitor service
- `JayTom.Dws.UploadCloudService/` - Cloud upload service
- `PostSoapCoreService/` - SOAP service

### 9. Database Test Projects (High Priority for Removal)
Database testing projects:

- `LicenseDBTest/` - License DB test
- `JayTom.Dws.VideoApiDbTest/` - Video API DB test
- `JayTom.Dws.CloudApiDbTest/` - Cloud API DB test
- `JayTom.Dws.LicenseApiDbTest/` - License API DB test

### 10. Networking Tests (Medium Priority)
Network testing projects:

- `TCPTest/` - TCP test
- `TCPTest2/` - TCP test 2
- `ApiTest/` - API test
- `SignalRTest/` - SignalR test

### 11. Miscellaneous (Medium Priority)
Other test/demo projects:

- `ExitTestDemo/` - Exit test demo
- `ForTestPr/` - Test PR project
- `WeightForwarder/` - Weight forwarder
- `GenerateMachineCode/` - Machine code generator
- `LicenseTest/` - License test
- `MudBlazorTemplates1/` - Blazor template
- `BlazorApp1/` - Blazor test 1
- `BlazorApp2/` - Blazor test 2
- `BlazorApp3/` - Blazor test 3
- `BlazorApp4/` - Blazor test 4

### 12. Other Solution Files
- `MyApplication.sln` - Duplicate solution
- `MyApplication.csproj` - Duplicate project
- Root-level Blazor files (`App.razor`, `Program.cs`, `_Imports.razor`, etc.)

### 13. Sunnen-Specific Projects (Low Priority - Client Specific)
These appear to be client-specific:

- `JayTom.Dws.Sunnen/` - Sunnen specific code
- `JayTom.Dws.SunnenPlugin/` - Sunnen plugin

### 14. Application Layer (Keep - Part of Core)
This should be evaluated if it's needed:
- `JayTom.Dws.Application/` - Application layer (currently not in focused solution)

## Recommendations

### Immediate Actions (High Priority)
1. **Remove all test console/WPF/WinForms apps** - These clutter the repository
2. **Remove database test projects** - Tests should be in test projects
3. **Archive device test projects** to a separate "tests" branch or repository

### Short-term Actions (Medium Priority)
1. **Move API projects** to separate repositories if they're maintained separately
2. **Consolidate input device tests** into a single test project if still needed
3. **Archive network test projects** if functionality is covered by unit/integration tests

### Long-term Considerations (Low Priority)
1. **Evaluate API client libraries** - Only keep if actively used by client
2. **Review background services** - Determine if they should be in same repo
3. **Consider AI/ML test projects** - Keep if planning to use these features

## Migration Strategy

### Option 1: Archive to Separate Branch
```bash
# Create archive branch
git checkout -b archive/test-projects
git checkout main

# Remove projects from main
git rm -r ConsoleApp1/ ConsoleApp2/ ...
git commit -m "Archive test projects to archive/test-projects branch"
```

### Option 2: Keep History but Remove Files
```bash
# Simply remove the directories
rm -rf ConsoleApp1/ ConsoleApp2/ ...
git add -A
git commit -m "Remove test/demo projects"
```

### Option 3: Separate Repositories
Create separate repositories for:
- API services (ManagementApi, VideoApi, CloudApi, LicenseApi)
- Test utilities
- Admin tools (ManagementStudio, UpdaterClient)

## Impact Assessment

### Low Risk (Safe to Remove)
- All `ConsoleApp*`, `WpfApp*` test projects
- Database test projects
- Most device test projects

### Medium Risk (Review First)
- API projects (may be needed for client)
- Background services (may be required)
- Client-specific projects (Sunnen)

### High Risk (Keep for Now)
- JayTom.Dws.Application (may be needed but not currently referenced)
- AI/ML test projects (if planning features)
- API client libraries (if used by client)

## Decision Required
Before removing any projects, the team should:
1. Review each category
2. Determine which projects are still actively used
3. Decide on archival strategy
4. Plan migration for API services if moving to separate repos

## Notes
- The focused solution (JayTom.Dws.Client.sln) already excludes these projects
- The original solution (JayTom.Dws.sln) still references them
- Removing from git will not affect the focused solution
- Can always recover from git history if needed
