# JayTom.Dws Project Cleanup Documentation

## Overview
This document describes the cleanup and restructuring of the JayTom.Dws solution to focus on the core client application.

## Changes Made

### 1. Created Central Package Management
A `Directory.Packages.props` file has been created at the solution root to centrally manage all NuGet package versions. This ensures consistency across all projects.

**Benefits:**
- Single source of truth for package versions
- Easier to update packages across all projects
- Prevents version conflicts
- Reduces maintenance overhead

**Location:** `/Directory.Packages.props`

### 2. Created Focused Solution
A new solution file `JayTom.Dws.Client.sln` has been created that includes only the core projects needed for the client application.

**Included Projects:**
- **Core**: JayTom.Dws.Client (Main WPF Application)
- **Domain**: JayTom.Dws.Data, JayTom.Dws.Domain
- **Infrastructure**: JayTom.Dws.Infrastructure, JayTom.Dws.Interface, JayTom.Dws.Utils
- **Device**: JayTom.Dws.Camera, JayTom.Dws.Nvr, JayTom.Dws.Ocr
- **Plugin**: JayTom.Dws.Plugin, JayTom.Dws.PluginInterface
- **License**: JayTom.Dws.License

**Excluded Projects:**
The following projects have been removed from the focused solution as they are:
- Test/demo applications
- API services not required by the client
- Temporary/experimental projects
- Management/administration tools

Categories of excluded projects:
- Test Projects (WpfApp1, WinFormsApp1, ConsoleApp1-7, etc.)
- API Projects (ManagementApi, VideoApi, CloudApi, LicenseApi, PostSoapApi)
- Management Tools (ManagementStudio, UpdaterClient)
- Service Projects (Service, DataInteractionService, SystemStatusMonitorService, UploadCloudService)
- Test/Demo Projects (All camera tests, HID tests, OCR tests, etc.)
- Database Test Projects (LicenseDBTest, VideoApiDbTest, CloudApiDbTest, LicenseApiDbTest)
- Temporary Projects (TemporaryClient, ForTestPr)

### 3. Project Structure

```
JayTom.Dws/
├── Directory.Packages.props          # Central package management
├── JayTom.Dws.sln                    # Original full solution (preserved)
├── JayTom.Dws.Client.sln             # New focused solution
├── EVENT_DRIVEN_ARCHITECTURE_PLAN.md # Migration plan
├── PROJECT_CLEANUP.md                # This file
│
├── Core/
│   └── JayTom.Dws.Client/           # Main WPF application
│
├── Domain/
│   ├── JayTom.Dws.Data/             # Data models
│   └── JayTom.Dws.Domain/           # Domain logic
│
├── Infrastructure/
│   ├── JayTom.Dws.Infrastructure/   # Core infrastructure
│   ├── JayTom.Dws.Interface/        # Service interfaces
│   └── JayTom.Dws.Utils/            # Utility classes
│
├── Device/
│   ├── JayTom.Dws.Camera/           # Camera integration
│   ├── JayTom.Dws.Nvr/              # NVR integration
│   └── JayTom.Dws.Ocr/              # OCR functionality
│
├── Plugin/
│   ├── JayTom.Dws.Plugin/           # Plugin implementations
│   └── JayTom.Dws.PluginInterface/  # Plugin interfaces
│
└── License/
    └── JayTom.Dws.License/          # License management
```

## Using the Focused Solution

### Building the Project
```bash
# Build the focused solution
dotnet build JayTom.Dws.Client.sln

# Build in Release mode
dotnet build JayTom.Dws.Client.sln -c Release
```

### Running the Application
```bash
# Run the client application
dotnet run --project JayTom.Dws.Client/JayTom.Dws.Client.csproj
```

### Adding/Updating Packages
To add or update a package:

1. Update the version in `Directory.Packages.props`
2. Add the package reference without version to the project file:
   ```xml
   <PackageReference Include="PackageName" />
   ```

## Migration to Event-Driven Architecture

See `EVENT_DRIVEN_ARCHITECTURE_PLAN.md` for detailed information about the planned migration to an event-driven architecture.

## Dependency Graph

```
JayTom.Dws.Client
├── JayTom.Dws.Camera
│   └── JayTom.Dws.Ocr
├── JayTom.Dws.Infrastructure
│   ├── JayTom.Dws.Data
│   ├── JayTom.Dws.Domain
│   │   ├── JayTom.Dws.Data
│   │   ├── JayTom.Dws.Interface
│   │   │   ├── JayTom.Dws.Plugin
│   │   │   └── JayTom.Dws.Utils
│   │   └── JayTom.Dws.Plugin
│   └── JayTom.Dws.Plugin
├── JayTom.Dws.Interface
│   ├── JayTom.Dws.Plugin
│   └── JayTom.Dws.Utils
├── JayTom.Dws.License
├── JayTom.Dws.Nvr
├── JayTom.Dws.PluginInterface
└── JayTom.Dws.Plugin
```

## Benefits of Cleanup

1. **Simplified Solution**: Easier to navigate and understand
2. **Faster Build Times**: Only building necessary projects
3. **Better Focus**: Development team can focus on core functionality
4. **Easier Onboarding**: New developers can understand the project structure quickly
5. **Reduced Complexity**: Less cognitive overhead
6. **Consistent Packages**: Central package management prevents version conflicts

## Original Solution

The original `JayTom.Dws.sln` file has been preserved and remains available for:
- Building API services
- Running tests
- Working with management tools
- Backward compatibility

## Next Steps

1. Review the focused solution and ensure all required functionality is present
2. Begin implementing the event-driven architecture (see EVENT_DRIVEN_ARCHITECTURE_PLAN.md)
3. Update CI/CD pipelines to use the new focused solution
4. Update developer documentation
5. Consider archiving or removing unused test projects

## Questions or Issues

If you have questions about this cleanup or need access to excluded projects, please:
1. Check if the functionality exists in the core projects
2. Review the original `JayTom.Dws.sln` for the full project list
3. Contact the development team

## Changelog

### 2025-10-23
- Created `Directory.Packages.props` for central package management
- Created `JayTom.Dws.Client.sln` focused solution
- Documented cleanup process
- Created event-driven architecture migration plan
