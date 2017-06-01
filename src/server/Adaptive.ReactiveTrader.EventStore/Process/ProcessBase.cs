﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adaptive.ReactiveTrader.EventStore.Process
{
    public abstract class ProcessBase : IProcess
    {
        private readonly List<object> _uncommittedEvents = new List<object>();
        private readonly List<Message> _undispatchedMessages = new List<Message>();

        public abstract object Identifier { get; }

        public int Version { get; private set; } = -1;

        public void Transition(object @event)
        {
            ((dynamic)this).OnEvent((dynamic)@event);
            Version++;
            _uncommittedEvents.Add(@event);
        }

        public IReadOnlyList<object> GetUncommittedEvents()
        {
            return _uncommittedEvents.AsReadOnly();
        }

        public IReadOnlyList<object> GetUndispatchedMessages()
        {
            return _undispatchedMessages.AsReadOnly();
        }

        public void ClearUncommittedEvents()
        {
            _uncommittedEvents.Clear();
        }

        public void ClearUndispatchedMessages()
        {
            _undispatchedMessages.Clear();
        }

        protected void AddMessageToDispatch(Message message)
        {
            _undispatchedMessages.Add(message);
        }

        public async Task DispatchMessages()
        {
            foreach (var message in _undispatchedMessages)
            {
                await message();
            }
        }
    }
}