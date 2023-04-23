﻿using MediatorCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MediatorCore.RequestTypes.Queue;

internal sealed class QueueBackgroundService<TMessage> :
    IHostedService
    where TMessage : IQueueMessage
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly LockingQueue<TMessage> queue = new();
    private bool running = true;

    public QueueBackgroundService(IServiceScopeFactory serviceScopeFactory)
    {
        this.serviceScopeFactory = serviceScopeFactory;
    }

    internal void Enqueue(TMessage message)
    {
        queue.Enqueue(message);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && running)
        {
            var messageResult = await queue.TryDequeueAsync(cancellationToken);

            if (!messageResult.Success)
                continue;

            _ = Task.Run(async () =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetService(typeof(IQueueHandler<TMessage>))
                    as IQueueHandler<TMessage>;
                await handler.HandleAsync(messageResult.Item);
            })
                .ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        running = false;
        queue.Dispose();
        return Task.CompletedTask;
    }
}
