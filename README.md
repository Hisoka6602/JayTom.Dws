# JayTom.Dws - Dynamic Weighing System

## Overview
JayTom.Dws is a comprehensive dynamic weighing system with integrated camera, OCR, and device management capabilities. The system is built using WPF and follows a modular architecture.

## Getting Started

### Prerequisites
- .NET 7.0 SDK or later
- Windows 10/11 (for WPF applications)
- Visual Studio 2022 or later (recommended)

### Building the Solution

#### Focused Solution (Recommended for Client Development)
```bash
# Build the client-focused solution
dotnet build JayTom.Dws.Client.sln

# Run the client application
dotnet run --project JayTom.Dws.Client/JayTom.Dws.Client.csproj
```

#### Full Solution (All Projects)
```bash
# Build the complete solution
dotnet build JayTom.Dws.sln
```

## Project Structure

### Core Projects
- **JayTom.Dws.Client**: Main WPF client application
- **JayTom.Dws.Data**: Data models and entities
- **JayTom.Dws.Domain**: Domain logic and business rules
- **JayTom.Dws.Infrastructure**: Core infrastructure services
- **JayTom.Dws.Interface**: Service interfaces and contracts
- **JayTom.Dws.Utils**: Utility classes and helpers

### Device Integration
- **JayTom.Dws.Camera**: Camera integration (Hikvision, Dahua, USB cameras)
- **JayTom.Dws.Nvr**: NVR integration for video recording
- **JayTom.Dws.Ocr**: OCR functionality for image processing

### Plugin System
- **JayTom.Dws.PluginInterface**: Plugin interfaces
- **JayTom.Dws.Plugin**: Plugin implementations

### Security
- **JayTom.Dws.License**: License management and validation

## Package Management
This project uses centralized package management via `Directory.Packages.props`. All package versions are defined in this file to ensure consistency across projects.

To add a new package:
1. Add the version to `Directory.Packages.props`
2. Reference the package without version in your project file

Example:
```xml
<!-- In Directory.Packages.props -->
<PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />

<!-- In your .csproj -->
<PackageReference Include="Newtonsoft.Json" />
```

## Documentation

### Recent Changes
- **[PROJECT_CLEANUP.md](PROJECT_CLEANUP.md)**: Details about solution cleanup and restructuring
- **[EVENT_DRIVEN_ARCHITECTURE_PLAN.md](EVENT_DRIVEN_ARCHITECTURE_PLAN.md)**: Plan for migrating to event-driven architecture

### Historical Documentation
- **[REFACTORING_RECOMMENDATIONS.md](REFACTORING_RECOMMENDATIONS.md)**: General refactoring recommendations
- **[CLIENT_OPTIMIZATION_COMPLETE.md](CLIENT_OPTIMIZATION_COMPLETE.md)**: Client optimization history
- **[PERFORMANCE_ISSUES_SUMMARY.md](PERFORMANCE_ISSUES_SUMMARY.md)**: Performance issues and resolutions
- **[MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)**: Migration guides
- **[IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)**: Implementation summaries
- **[README_OPTIMIZATION.md](README_OPTIMIZATION.md)**: Optimization notes

## Key Features
- Real-time weight measurement
- Multiple camera support (Industrial cameras, IP cameras, USB cameras)
- Barcode scanning integration
- OCR for image text recognition
- Plugin system for extensibility
- Data export capabilities (Excel, PDF)
- Cloud synchronization
- Multi-language support

## Architecture
The project follows a layered architecture pattern:
- **Presentation Layer**: WPF client application
- **Domain Layer**: Business logic and domain models
- **Infrastructure Layer**: Data access, external services
- **Device Layer**: Hardware integration
- **Plugin Layer**: Extensibility support

### Future: Event-Driven Architecture
The project is planning to migrate to an event-driven architecture to improve:
- Scalability
- Maintainability
- Testability
- Loose coupling

See [EVENT_DRIVEN_ARCHITECTURE_PLAN.md](EVENT_DRIVEN_ARCHITECTURE_PLAN.md) for details.

## Development Workflow

### Branching Strategy
- `main`: Production-ready code
- `develop`: Development branch
- `feature/*`: Feature branches
- `bugfix/*`: Bug fix branches

### Code Standards
- Follow C# coding conventions
- Use async/await for I/O operations
- Implement proper error handling and logging
- Write unit tests for business logic

## Configuration
The application uses multiple configuration files:
- `appsettings.json`: Application settings
- `App.config`: Legacy configuration
- `Nlog.config`: Logging configuration

## Logging
The application uses NLog for logging. Logs are written to:
- Console (during development)
- File (in production)
- Database (for critical errors)

## Testing
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

## Deployment
(To be added)

## Troubleshooting

### Common Issues
1. **Camera not connecting**: Check device drivers and permissions
2. **License validation fails**: Verify license file location
3. **Database connection errors**: Check connection string in config

### Logs
Check the logs in `Logs/` directory for detailed error information.

## Contributing
(To be added)

## License
(To be added)

## Support
For issues and questions, please contact the development team.

## Version History
- **2025-10-23**: Project cleanup and central package management
- Previous versions: See git history
