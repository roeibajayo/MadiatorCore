using MediatorCore.RequestTypes.Notification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MediatorCore.Tests.RequestTypes;

public class Notification : BaseUnitTest
{
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task PublishBubbleMessage_ReturnNoErrorsAndDequeue(int counts)
    {
        //Arrange
        var publisher = ServiceProvider.GetService<IPublisher>()!;
        var logger = ServiceProvider.GetService<ILogger>()!;
        var id = "1_" + Guid.NewGuid();

        //Act
        for (var i = 0; i < counts; i++)
        {
            await publisher.PublishAsync(new NotificationMessage(id));
        }

        //Assert
        if (ReceivedDebugs(logger, "FirstNotification1Message: " + id) == counts &&
            ReceivedDebugs(logger, "Notification2Message: " + id) == counts)
        {
            return;
        }
        throw new Exception("No dequeue executed");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task PublishBubbleMessage_TwoMessagesSameHanlder_ReturnNoErrorsAndDequeue(int counts)
    {
        //Arrange
        var publisher = ServiceProvider.GetService<IPublisher>()!;
        var logger = ServiceProvider.GetService<ILogger>()!;
        var id = "1_" + Guid.NewGuid();

        //Act
        for (var i = 0; i < counts; i++)
        {
            await publisher.PublishAsync(new NotificationMessage(id));
            await publisher.PublishAsync(new SecondNotificationMessage(id));
        }

        //Assert
        if (ReceivedDebugs(logger, "FirstNotification1Message: " + id) == counts &&
            ReceivedDebugs(logger, "SecondNotification1Message: " + id) == counts &&
            ReceivedDebugs(logger, "Notification2Message: " + id) == counts)
        {
            return;
        }
        throw new Exception("No dequeue executed");
    }
}

public record NotificationMessage(string Id) : INotificationMessage;
public record SecondNotificationMessage(string Id) : INotificationMessage;
public class Notification1Handler : INotificationHandler<NotificationMessage>, INotificationHandler<SecondNotificationMessage>
{
    public readonly ILogger logger;

    public Notification1Handler(ILogger logger)
    {
        this.logger = logger;
    }

    public Task Handle(object? message, CancellationToken cancellationToken)
    {
        return message switch
        {
            NotificationMessage m => HandleAsync(m, cancellationToken),
            SecondNotificationMessage m => HandleAsync(m, cancellationToken),
            _ => Task.CompletedTask
        };
    }

    public Task HandleAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        logger.LogDebug("FirstNotification1Message: " + message.Id);
        return Task.CompletedTask;
    }

    public Task HandleAsync(SecondNotificationMessage message, CancellationToken cancellationToken)
    {
        logger.LogDebug("SecondNotification1Message: " + message.Id);
        return Task.CompletedTask;
    }
}
public class Notification2Handler : INotificationHandler<NotificationMessage>
{
    public readonly ILogger logger;

    public Notification2Handler(ILogger logger)
    {
        this.logger = logger;
    }

    public Task HandleAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        logger.LogDebug("Notification2Message: " + message.Id);
        return Task.CompletedTask;
    }
}