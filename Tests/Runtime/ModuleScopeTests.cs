using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tritone.Kernel;

namespace Tritone.Tests
{
    /// <summary>
    /// Verifies generic module ownership and complete reverse-order cleanup.
    /// </summary>
    public sealed class ModuleScopeTests
    {
        [Test]
        public void Dispose_ReleasesAllResourcesInReverseOrder()
        {
            List<int> order = new();
            ModuleScope scope = new();
            scope.Own(new TestResource(1, order));
            scope.Own(new TestResource(2, order, true));
            scope.Own(new TestResource(3, order));

            Assert.Throws<AggregateException>(scope.Dispose);
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, order);
            Assert.DoesNotThrow(scope.Dispose);
        }

        private sealed class TestResource : IDisposable
        {
            // Stores the identifier appended during disposal.
            private readonly int mId;

            // Stores shared disposal order.
            private readonly List<int> mOrder;

            // Indicates whether disposal should fail after recording.
            private readonly bool mThrow;

            internal TestResource(int id, List<int> order, bool shouldThrow = false)
            {
                mId    = id;
                mOrder = order;
                mThrow = shouldThrow;
            }

            public void Dispose()
            {
                mOrder.Add(mId);
                if (mThrow)
                    throw new InvalidOperationException("Expected test failure.");
            }
        }
    }
}
