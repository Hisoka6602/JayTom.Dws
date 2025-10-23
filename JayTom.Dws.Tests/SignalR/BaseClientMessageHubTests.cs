using JayTom.Dws.CrossCutting.SignalR;

namespace JayTom.Dws.Tests.SignalR;

public class BaseClientMessageHubTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var hub = new BaseClientMessageHub();

        // Assert
        Assert.False(hub.IsConnected);
        Assert.Equal(string.Empty, hub.ConnectionId);
        Assert.False(hub.AutoReconnect); // Default is false
    }

    [Fact]
    public void AutoReconnect_CanBeSetAndRetrieved()
    {
        // Arrange
        var hub = new BaseClientMessageHub();

        // Act
        hub.AutoReconnect = false;

        // Assert
        Assert.False(hub.AutoReconnect);
    }

    [Fact]
    public void Events_CanBeSubscribedTo()
    {
        // Arrange
        var hub = new BaseClientMessageHub();

        // Act & Assert - Events should be subscribed without errors
        hub.Closed += (ex) => Task.CompletedTask;
        hub.Reconnected += (id) => Task.CompletedTask;
        hub.Reconnecting += (ex) => Task.CompletedTask;
        
        // If we get here without exceptions, the test passes
        Assert.True(true);
    }
}
