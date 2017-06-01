﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Serilog;
using Serilog.Events;

namespace Adaptive.ReactiveTrader.EventStore.Domain
{
    public class AggregateRepository : RepositoryBase, IAggregateRepository, IDisposable
    {
        public AggregateRepository(IEventStoreConnection eventStoreConnection,
                                   EventTypeResolver eventTypeResolver) : base(eventStoreConnection, eventTypeResolver)
        {
        }

        public void Dispose()
        {
            Log.Warning("Not Disposing.");
        }

        public async Task<TAggregate> GetByIdAsync<TAggregate>(object id) where TAggregate : IAggregate, new()
        {
            var aggregate = new TAggregate();
            var streamName = $"{aggregate.Identifier}{id}";

            if (Log.IsEnabled(LogEventLevel.Information))
            {
                Log.Information("Loading aggregate {streamName} from Event Store", streamName);
            }

            var result = await ReadEventsAsync(streamName, e => aggregate.ApplyEvent(e));

            if (result == SliceReadStatus.StreamNotFound)
            {
                throw new AggregateNotFoundException(id, typeof(TAggregate));
            }

            if (result == SliceReadStatus.StreamDeleted)
            {
                throw new AggregateDeletedException(id, typeof(TAggregate));
            }

            return aggregate;
        }

        public async Task<int> SaveAsync(AggregateBase aggregate, params KeyValuePair<string, string>[] extraHeaders)
        {
            var streamName = aggregate.Identifier.ToString();
            var pendingEvents = aggregate.GetPendingEvents();
            var expectedVersion = aggregate.Version - pendingEvents.Count;
            var commitId = Guid.NewGuid().ToString();

            var allExtraHeaders = new List<KeyValuePair<string, string>>(extraHeaders)
            {
                new KeyValuePair<string, string>(MetadataKeys.AggregateClrTypeHeader,
                                                 aggregate.GetType().AssemblyQualifiedName)
            };
            
            if (Log.IsEnabled(LogEventLevel.Information))
            {
                Log.Information("Saving aggregate {streamName}", streamName);
            }

            var result = await WriteEventsAsync(streamName, expectedVersion, pendingEvents, allExtraHeaders, commitId);

            aggregate.ClearPendingEvents();

            if (Log.IsEnabled(LogEventLevel.Information))
            {
                Log.Information("Aggregate {streamName} pending events cleaned up", streamName);
            }

            return result.NextExpectedVersion;
        }
    }
}