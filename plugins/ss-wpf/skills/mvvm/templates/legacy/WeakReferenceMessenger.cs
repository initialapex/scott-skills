// Minimal messenger for toolchains that cannot use CommunityToolkit.Mvvm
// (Visual Studio 2012, .NET Framework 4.5, C# 5).
// Usage matches CommunityToolkit.Mvvm.Messaging:
//   messenger.Register<RecipeSavedMessage>(this, (r, m) => ((RecipeListViewModel)r).OnRecipeSaved(m));
//   messenger.Send(new RecipeSavedMessage(recipe));
// Recipients are held weakly. The handler is held strongly, so it must use its
// recipient argument and never capture "this"; a captured "this" keeps the recipient alive.
// Handlers run on the thread that calls Send.
// Replace the namespace with <RootNamespace>.Mvvm when copying into a project.

using System;
using System.Collections.Generic;

namespace MyApp.Mvvm
{
    /// <summary>
    /// Handles a message for a recipient.
    /// </summary>
    public delegate void MessageHandler<in TRecipient, in TMessage>(TRecipient recipient, TMessage message)
        where TRecipient : class
        where TMessage : class;

    /// <summary>
    /// Sends messages between objects that do not reference each other.
    /// </summary>
    public interface IMessenger
    {
        void Register<TMessage>(object recipient, MessageHandler<object, TMessage> handler)
            where TMessage : class;

        bool IsRegistered<TMessage>(object recipient)
            where TMessage : class;

        void Unregister<TMessage>(object recipient)
            where TMessage : class;

        void UnregisterAll(object recipient);

        TMessage Send<TMessage>(TMessage message)
            where TMessage : class;
    }

    /// <summary>
    /// IMessenger that holds its recipients through weak references.
    /// </summary>
    public sealed class WeakReferenceMessenger : IMessenger
    {
        private static readonly WeakReferenceMessenger DefaultInstance = new WeakReferenceMessenger();

        private readonly object _gate = new object();
        private readonly Dictionary<Type, List<Subscription>> _subscriptions =
            new Dictionary<Type, List<Subscription>>();

        /// <summary>
        /// Shared instance for applications without DI. Prefer injecting IMessenger.
        /// </summary>
        public static WeakReferenceMessenger Default
        {
            get { return DefaultInstance; }
        }

        public void Register<TMessage>(object recipient, MessageHandler<object, TMessage> handler)
            where TMessage : class
        {
            if (recipient == null)
            {
                throw new ArgumentNullException("recipient");
            }

            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            lock (_gate)
            {
                List<Subscription> list;
                if (!_subscriptions.TryGetValue(typeof(TMessage), out list))
                {
                    list = new List<Subscription>();
                    _subscriptions.Add(typeof(TMessage), list);
                }

                list.RemoveAll(s => !s.Recipient.IsAlive);
                if (list.Exists(s => ReferenceEquals(s.Recipient.Target, recipient)))
                {
                    throw new InvalidOperationException(
                        string.Format("The recipient is already registered for {0}.", typeof(TMessage).Name));
                }

                list.Add(new Subscription(new WeakReference(recipient), handler));
            }
        }

        public bool IsRegistered<TMessage>(object recipient)
            where TMessage : class
        {
            lock (_gate)
            {
                List<Subscription> list;
                if (!_subscriptions.TryGetValue(typeof(TMessage), out list))
                {
                    return false;
                }

                return list.Exists(s => ReferenceEquals(s.Recipient.Target, recipient));
            }
        }

        public void Unregister<TMessage>(object recipient)
            where TMessage : class
        {
            lock (_gate)
            {
                List<Subscription> list;
                if (_subscriptions.TryGetValue(typeof(TMessage), out list))
                {
                    list.RemoveAll(s => !s.Recipient.IsAlive || ReferenceEquals(s.Recipient.Target, recipient));
                }
            }
        }

        public void UnregisterAll(object recipient)
        {
            lock (_gate)
            {
                foreach (List<Subscription> list in _subscriptions.Values)
                {
                    list.RemoveAll(s => !s.Recipient.IsAlive || ReferenceEquals(s.Recipient.Target, recipient));
                }
            }
        }

        public TMessage Send<TMessage>(TMessage message)
            where TMessage : class
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            // Copy under the lock and invoke outside it, so handlers may register or unregister.
            Subscription[] snapshot;
            lock (_gate)
            {
                List<Subscription> list;
                if (!_subscriptions.TryGetValue(typeof(TMessage), out list))
                {
                    return message;
                }

                list.RemoveAll(s => !s.Recipient.IsAlive);
                snapshot = list.ToArray();
            }

            foreach (Subscription subscription in snapshot)
            {
                object recipient = subscription.Recipient.Target;
                if (recipient != null)
                {
                    ((MessageHandler<object, TMessage>)subscription.Handler)(recipient, message);
                }
            }

            return message;
        }

        private sealed class Subscription
        {
            private readonly WeakReference _recipient;
            private readonly Delegate _handler;

            public Subscription(WeakReference recipient, Delegate handler)
            {
                _recipient = recipient;
                _handler = handler;
            }

            public WeakReference Recipient
            {
                get { return _recipient; }
            }

            public Delegate Handler
            {
                get { return _handler; }
            }
        }
    }
}
