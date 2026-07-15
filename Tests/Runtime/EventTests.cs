using NUnit.Framework;
using Tritone.Events;

namespace Tritone.Kernel.Tests
{
    /// <summary>
    /// Verifies strongly typed publication and automatic module lifetime cleanup.
    /// </summary>
    public sealed class EventTests
    {
        /// <summary>
        /// Verifies that a strongly typed event forwards all payload values.
        /// </summary>
        [Test]
        public void Publish_ForwardsTypedPayload()
        {
            Event<int, string> eventSource = new();
            var receivedId                = 0;
            string receivedName           = null;
            using var binding = eventSource.Bind((id, name) =>
            {
                receivedId   = id;
                receivedName = name;
            });

            eventSource.Publish(7, "Tristin");

            Assert.AreEqual(7, receivedId);
            Assert.AreEqual("Tristin", receivedName);
        }

        /// <summary>
        /// Verifies that disposing a binding prevents later callbacks.
        /// </summary>
        [Test]
        public void Dispose_RemovesListener()
        {
            Event eventSource = new();
            var callbackCount = 0;
            var binding       = eventSource.Bind(() => callbackCount++);

            binding.Dispose();
            eventSource.Publish();

            Assert.AreEqual(0, callbackCount);
        }

        /// <summary>
        /// Verifies that ModuleBase releases every owned binding when stopped.
        /// </summary>
        [Test]
        public void Stop_AutomaticallyUnbindsModuleListeners()
        {
            Event<int> eventSource            = new();
            EventConsumerModule consumer      = new(eventSource);
            GameApplicationBuilder builder    = new();
            GameApplication application       = builder.AddModule(consumer).Build();
            application.Start();

            eventSource.Publish(3);
            application.Stop();
            eventSource.Publish(5);

            Assert.AreEqual(3, consumer.Total);
        }

        /// <summary>
        /// Provides a module listener used to verify automatic cleanup.
        /// </summary>
        private sealed class EventConsumerModule : ModuleBase
        {
            private readonly Event<int> mEventSource;

            /// <summary>
            /// Initializes the module with the event it listens to.
            /// </summary>
            /// <param name="eventSource">The event published by the test.</param>
            internal EventConsumerModule(Event<int> eventSource)
            {
                mEventSource = eventSource;
            }

            /// <summary>
            /// Gets the sum of values received before shutdown.
            /// </summary>
            internal int Total { get; private set; }

            /// <summary>
            /// Binds the listener through ModuleBase lifetime management.
            /// </summary>
            protected override void OnStart()
            {
                BindEvent(mEventSource, OnEvent);
            }

            /// <summary>
            /// Accumulates one published value.
            /// </summary>
            /// <param name="value">The published integer value.</param>
            private void OnEvent(int value)
            {
                Total += value;
            }
        }
    }
}
