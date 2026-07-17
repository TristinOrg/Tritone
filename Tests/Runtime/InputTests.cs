using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tritone.Kernel;
using Tritone.Unity.Input;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies changed input dispatch and automatic module cleanup.
    /// </summary>
    public sealed class InputTests
    {
        [Test]
        public void InputBindings_DispatchAndReleaseWithModule()
        {
            TestInputSource source = new();
            InputConsumer consumer = new();
            var application = new GameApplicationBuilder()
                .UseInput(source)
                .AddModule(consumer)
                .Build();
            application.Start();
            source.Button = true;
            source.Axis   = 0.5f;
            FrameTime time = new(0.016, 0.016, 0.016, 0);
            application.Update(in time);
            application.Update(in time);

            Assert.AreEqual(1, consumer.ButtonCount);
            Assert.AreEqual(0.5f, consumer.Axis);
            application.Stop();
        }

        private sealed class InputConsumer : ModuleBase
        {
            internal int ButtonCount;
            internal float Axis;

            protected override void OnStart()
            {
                BindInput("Jump", OnJump);
                BindInputAxis("Move", OnMove);
            }

            private void OnJump()
            {
                ButtonCount++;
            }

            private void OnMove(float value)
            {
                Axis = value;
            }
        }

        private sealed class TestInputSource : IInputSource
        {
            internal bool Button;
            internal float Axis;

            public bool GetButtonDown(string action)
            {
                var value = Button;
                Button = false;
                return value;
            }

            public float GetAxis(string action)
            {
                return Axis;
            }
        }
    }
}
