# Event-Driven Architecture Migration Plan

## Overview
This document outlines the plan to refactor JayTom.Dws.Client to adopt an event-driven architecture (EDA) pattern. The goal is to improve modularity, scalability, and maintainability of the application.

## Current Architecture
The current architecture follows a traditional layered approach:
- **Presentation Layer**: JayTom.Dws.Client (WPF UI)
- **Domain Layer**: JayTom.Dws.Domain, JayTom.Dws.Data
- **Infrastructure Layer**: JayTom.Dws.Infrastructure, JayTom.Dws.Interface, JayTom.Dws.Utils
- **Device Layer**: JayTom.Dws.Camera, JayTom.Dws.Nvr, JayTom.Dws.Ocr
- **Plugin Layer**: JayTom.Dws.Plugin, JayTom.Dws.PluginInterface
- **License Layer**: JayTom.Dws.License

## Target Event-Driven Architecture

### Core Principles
1. **Loose Coupling**: Components communicate through events, not direct method calls
2. **Asynchronous Processing**: Events are processed asynchronously
3. **Scalability**: New features can be added by subscribing to existing events
4. **Testability**: Components can be tested in isolation

### Proposed Architecture Components

#### 1. Event Bus (Central Message Broker)
- **Technology**: MediatR or custom implementation
- **Purpose**: Central hub for all domain events
- **Location**: JayTom.Dws.Infrastructure

#### 2. Domain Events
Define events for key domain operations:
- **Device Events**
  - `DeviceConnectedEvent`
  - `DeviceDisconnectedEvent`
  - `MeasurementCompletedEvent`
  - `CameraImageCapturedEvent`
  
- **Workflow Events**
  - `WorkflowStartedEvent`
  - `WorkflowStepCompletedEvent`
  - `WorkflowCompletedEvent`
  - `WorkflowFailedEvent`

- **Data Events**
  - `DataValidatedEvent`
  - `DataSavedEvent`
  - `DataSyncedEvent`

- **UI Events**
  - `NotificationRequiredEvent`
  - `ViewNavigationRequestedEvent`

#### 3. Event Handlers
Create dedicated handlers for each event type:
- Located in appropriate layers (Domain, Infrastructure, or Presentation)
- Implement single responsibility principle
- Support async/await patterns

#### 4. Event Store (Optional)
- Store event history for auditing and replay
- Implement Event Sourcing pattern if needed

### Migration Phases

#### Phase 1: Foundation (Weeks 1-2)
- [ ] Set up central event bus using MediatR
- [ ] Define core domain event interfaces and base classes
- [ ] Create event handler infrastructure
- [ ] Update dependency injection configuration

#### Phase 2: Device Integration (Weeks 3-4)
- [ ] Migrate camera operations to event-driven model
- [ ] Migrate weight measurement to event-driven model
- [ ] Migrate barcode scanner to event-driven model
- [ ] Implement device connection/disconnection events

#### Phase 3: Business Logic (Weeks 5-6)
- [ ] Convert workflow engine to event-driven
- [ ] Implement data validation events
- [ ] Convert data persistence operations
- [ ] Add event-based logging and monitoring

#### Phase 4: UI Integration (Weeks 7-8)
- [ ] Update UI to subscribe to domain events
- [ ] Implement UI notification system via events
- [ ] Convert view navigation to event-driven
- [ ] Update plugin system to use events

#### Phase 5: Testing & Optimization (Weeks 9-10)
- [ ] Add unit tests for all event handlers
- [ ] Add integration tests for event flows
- [ ] Performance testing and optimization
- [ ] Documentation and training

### Technical Implementation Details

#### Event Bus Configuration
```csharp
// In Startup or App.xaml.cs
services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblies(
        typeof(Client).Assembly,
        typeof(Domain).Assembly,
        typeof(Infrastructure).Assembly
    );
});
```

#### Event Definition Example
```csharp
public class MeasurementCompletedEvent : INotification
{
    public string DeviceId { get; set; }
    public double Weight { get; set; }
    public DateTime Timestamp { get; set; }
    public byte[] Image { get; set; }
}
```

#### Event Handler Example
```csharp
public class MeasurementCompletedEventHandler : INotificationHandler<MeasurementCompletedEvent>
{
    private readonly IDataRepository _repository;
    private readonly INotificationService _notificationService;
    
    public async Task Handle(MeasurementCompletedEvent notification, CancellationToken cancellationToken)
    {
        // Save measurement data
        await _repository.SaveMeasurementAsync(notification);
        
        // Notify UI
        await _notificationService.NotifyAsync("Measurement completed");
    }
}
```

### Benefits

1. **Improved Maintainability**: Clear separation of concerns
2. **Enhanced Testability**: Components can be tested independently
3. **Better Scalability**: New features can be added without modifying existing code
4. **Increased Flexibility**: Easy to add/remove event handlers
5. **Audit Trail**: All events can be logged for debugging and compliance

### Risks and Mitigation

1. **Learning Curve**
   - Mitigation: Provide training sessions and documentation
   
2. **Performance Overhead**
   - Mitigation: Use async processing, optimize critical paths
   
3. **Debugging Complexity**
   - Mitigation: Implement comprehensive logging and event tracing

4. **Event Versioning**
   - Mitigation: Design events with backward compatibility in mind

### Next Steps

1. Review and approve this plan
2. Set up MediatR in the infrastructure layer
3. Begin Phase 1 implementation
4. Schedule regular review meetings

## References
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [Event-Driven Architecture Pattern](https://martinfowler.com/articles/201701-event-driven.html)
- [Domain Events Pattern](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)
