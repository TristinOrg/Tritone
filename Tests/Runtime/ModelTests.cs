using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tritone.Models;

namespace Tritone.Kernel.Tests
{
    /// <summary>
    /// Verifies explicit model registration, sharing, reset, and lifetime ownership.
    /// </summary>
    public sealed class ModelTests
    {
        /// <summary>
        /// Verifies that application models are lazy, shared, resettable, and application-owned.
        /// </summary>
        [Test]
        public void ApplicationModel_IsLazySharedAndReleasedOnStop()
        {
            List<string> events = new();
            var application     = new GameApplicationBuilder()
                .AddApplicationModel(() => new RecordingModel("Player", events))
                .Build();

            application.Start();
            var service = application.Services.GetRequired<IModelService>();
            Assert.IsEmpty(events);

            var first  = service.Get<RecordingModel>();
            var second = service.Get<RecordingModel>();
            Assert.AreSame(first, second);
            Assert.IsTrue(service.Reset<RecordingModel>());
            application.Stop();

            CollectionAssert.AreEqual(
                new[] { "Player.Initialize", "Player.Reset", "Player.Dispose" },
                events);
        }

        /// <summary>
        /// Verifies that application models survive scene changes while scene models are recreated.
        /// </summary>
        [Test]
        public void SceneModel_IsRecreatedWhileApplicationModelSurvives()
        {
            List<string> events = new();
            var sceneIndex      = 0;
            var application     = new GameApplicationBuilder()
                .AddApplicationModel(() => new ApplicationStateModel(events))
                .AddSceneModel(() => new SceneStateModel(++sceneIndex, events))
                .AddSceneModule<FirstSceneConsumer>()
                .AddSceneModule<SecondSceneConsumer>()
                .Build();

            application.Start();
            var first     = application.SwitchModule<FirstSceneConsumer>();
            var appModel  = first.ApplicationModel;
            var sceneModel = first.SceneModel;
            var second    = application.SwitchModule<SecondSceneConsumer>();

            Assert.AreSame(appModel, second.ApplicationModel);
            Assert.AreNotSame(sceneModel, second.SceneModel);
            Assert.AreEqual(2, sceneIndex);
            application.Stop();

            CollectionAssert.AreEqual(new[]
            {
                "Application.Initialize",
                "Scene1.Initialize",
                "Scene1.Dispose",
                "Scene2.Initialize",
                "Scene2.Dispose",
                "Application.Dispose"
            }, events);
        }

        /// <summary>
        /// Verifies that scene models cannot escape an inactive scene ownership boundary.
        /// </summary>
        [Test]
        public void SceneModel_RequiresActiveSceneModule()
        {
            var application = new GameApplicationBuilder()
                .AddSceneModel<EmptyModel>()
                .Build();
            application.Start();
            var service = application.Services.GetRequired<IModelService>();

            var exception = Assert.Throws<InvalidOperationException>(
                () => service.Get<EmptyModel>());
            StringAssert.Contains("requires an active scene module", exception.Message);
            application.Stop();
        }

        /// <summary>
        /// Verifies duplicate model types are rejected even across different lifetimes.
        /// </summary>
        [Test]
        public void Build_RejectsDuplicateModelRegistration()
        {
            var builder = new GameApplicationBuilder()
                .AddApplicationModel<EmptyModel>();

            var exception = Assert.Throws<InvalidOperationException>(
                () => builder.AddSceneModel<EmptyModel>());
            StringAssert.Contains("already registered", exception.Message);
        }

        /// <summary>
        /// Verifies invalid factory results never enter the shared model cache.
        /// </summary>
        [Test]
        public void Get_RejectsInvalidFactoryResult()
        {
            var application = new GameApplicationBuilder()
                .AddApplicationModel<EmptyModel>(() => null)
                .Build();
            application.Start();
            var service = application.Services.GetRequired<IModelService>();

            var exception = Assert.Throws<InvalidOperationException>(
                () => service.Get<EmptyModel>());
            StringAssert.Contains("returned an invalid instance", exception.Message);
            application.Stop();
        }

        /// <summary>
        /// Verifies failed initialization is cleaned up and a later request can retry.
        /// </summary>
        [Test]
        public void Get_InitializationFailureDisposesAndAllowsRetry()
        {
            List<string> events = new();
            var attempt         = 0;
            var application     = new GameApplicationBuilder()
                .AddApplicationModel(
                    () => new RetryModel(++attempt == 1, events))
                .Build();
            application.Start();
            var service = application.Services.GetRequired<IModelService>();

            Assert.Throws<InvalidOperationException>(
                () => service.Get<RetryModel>());
            var model = service.Get<RetryModel>();
            Assert.IsNotNull(model);
            application.Stop();

            CollectionAssert.AreEqual(new[]
            {
                "Initialize.Fail",
                "Dispose.Fail",
                "Initialize.Success",
                "Dispose.Success"
            }, events);
        }

        /// <summary>
        /// Verifies scene startup rollback releases every model created by the failed scene.
        /// </summary>
        [Test]
        public void SceneStartupFailure_ReleasesCreatedSceneModels()
        {
            List<string> events = new();
            var application     = new GameApplicationBuilder()
                .AddSceneModel(() => new SceneStateModel(1, events))
                .AddSceneModule<FailingSceneConsumer>()
                .Build();
            application.Start();

            Assert.Throws<InvalidOperationException>(
                () => application.SwitchModule<FailingSceneConsumer>());
            Assert.IsNull(application.ActiveModuleType);
            CollectionAssert.AreEqual(
                new[] { "Scene1.Initialize", "Scene1.Dispose" },
                events);
            application.Stop();
        }

        /// <summary>
        /// Verifies every model is released in reverse creation order despite disposal failures.
        /// </summary>
        [Test]
        public void Stop_ReleasesAllModelsInReverseOrder()
        {
            List<string> events = new();
            var application     = new GameApplicationBuilder()
                .AddApplicationModel(() => new OrderedModel("First", events))
                .AddApplicationModel(
                    () => new FailingDisposeModel("Second", events))
                .AddModule(new ApplicationModelConsumer())
                .Build();
            application.Start();

            Assert.Throws<AggregateException>(application.Stop);
            CollectionAssert.AreEqual(new[]
            {
                "First.Initialize",
                "Second.Initialize",
                "Second.Dispose",
                "First.Dispose"
            }, events);
        }

        /// <summary>
        /// Records deterministic model lifecycle callbacks.
        /// </summary>
        private class RecordingModel : IModel
        {
            // Stores the readable model name.
            private readonly string mName;

            // Stores shared lifecycle output.
            private readonly List<string> mEvents;

            /// <summary>
            /// Initializes one recording model.
            /// </summary>
            /// <param name="name">The readable model name.</param>
            /// <param name="events">The shared lifecycle output.</param>
            internal RecordingModel(string name, List<string> events)
            {
                mName   = name;
                mEvents = events;
            }

            /// <inheritdoc />
            public void Initialize()
            {
                mEvents.Add($"{mName}.Initialize");
            }

            /// <inheritdoc />
            public void Reset()
            {
                mEvents.Add($"{mName}.Reset");
            }

            /// <inheritdoc />
            public virtual void Dispose()
            {
                mEvents.Add($"{mName}.Dispose");
            }
        }

        /// <summary>
        /// Represents persistent state shared across scene modules.
        /// </summary>
        private sealed class ApplicationStateModel : RecordingModel
        {
            /// <summary>
            /// Initializes persistent test state.
            /// </summary>
            /// <param name="events">The shared lifecycle output.</param>
            internal ApplicationStateModel(List<string> events)
                : base("Application", events) { }
        }

        /// <summary>
        /// Represents state owned by one scene module lifetime.
        /// </summary>
        private sealed class SceneStateModel : RecordingModel
        {
            /// <summary>
            /// Initializes scene-scoped test state.
            /// </summary>
            /// <param name="index">The scene lifetime creation index.</param>
            /// <param name="events">The shared lifecycle output.</param>
            internal SceneStateModel(int index, List<string> events)
                : base($"Scene{index}", events) { }
        }

        /// <summary>
        /// Represents a model with no recorded behavior.
        /// </summary>
        private sealed class EmptyModel : IModel
        {
            /// <inheritdoc />
            public void Initialize() { }

            /// <inheritdoc />
            public void Reset() { }

            /// <inheritdoc />
            public void Dispose() { }
        }

        /// <summary>
        /// Represents a model whose first initialization attempt fails.
        /// </summary>
        private sealed class RetryModel : IModel
        {
            // Indicates whether initialization should fail.
            private readonly bool mFail;

            // Stores shared lifecycle output.
            private readonly List<string> mEvents;

            /// <summary>
            /// Initializes one retry test model.
            /// </summary>
            /// <param name="fail">Whether initialization should fail.</param>
            /// <param name="events">The shared lifecycle output.</param>
            internal RetryModel(bool fail, List<string> events)
            {
                mFail   = fail;
                mEvents = events;
            }

            /// <inheritdoc />
            public void Initialize()
            {
                mEvents.Add(mFail ? "Initialize.Fail" : "Initialize.Success");
                if (mFail)
                    throw new InvalidOperationException("Expected initialization failure.");
            }

            /// <inheritdoc />
            public void Reset() { }

            /// <inheritdoc />
            public void Dispose()
            {
                mEvents.Add(mFail ? "Dispose.Fail" : "Dispose.Success");
            }
        }

        /// <summary>
        /// Provides a distinct model type for disposal ordering.
        /// </summary>
        private class OrderedModel : RecordingModel
        {
            /// <summary>
            /// Initializes one ordered model.
            /// </summary>
            /// <param name="name">The readable model name.</param>
            /// <param name="events">The shared lifecycle output.</param>
            internal OrderedModel(string name, List<string> events)
                : base(name, events) { }
        }

        /// <summary>
        /// Provides a distinct model type that fails after recording disposal.
        /// </summary>
        private sealed class FailingDisposeModel : RecordingModel
        {
            /// <summary>
            /// Initializes one failing disposal model.
            /// </summary>
            /// <param name="name">The readable model name.</param>
            /// <param name="events">The shared lifecycle output.</param>
            internal FailingDisposeModel(string name, List<string> events)
                : base(name, events) { }

            /// <inheritdoc />
            public override void Dispose()
            {
                base.Dispose();
                throw new InvalidOperationException("Expected disposal failure.");
            }
        }

        /// <summary>
        /// Resolves both model lifetimes during scene startup.
        /// </summary>
        private abstract class SceneConsumer : ModuleBase
        {
            /// <summary>
            /// Gets the persistent model resolved during configuration.
            /// </summary>
            internal ApplicationStateModel ApplicationModel { get; private set; }

            /// <summary>
            /// Gets the scene model resolved during configuration.
            /// </summary>
            internal SceneStateModel SceneModel { get; private set; }

            /// <inheritdoc />
            protected override void OnConfigure(IServiceRegistry services)
            {
                ApplicationModel = GetModel<ApplicationStateModel>();
                SceneModel       = GetModel<SceneStateModel>();
            }
        }

        /// <summary>
        /// Represents the first model-consuming scene module.
        /// </summary>
        private sealed class FirstSceneConsumer : SceneConsumer { }

        /// <summary>
        /// Represents the second model-consuming scene module.
        /// </summary>
        private sealed class SecondSceneConsumer : SceneConsumer { }

        /// <summary>
        /// Creates a scene model and then fails startup.
        /// </summary>
        private sealed class FailingSceneConsumer : ModuleBase
        {
            /// <inheritdoc />
            protected override void OnConfigure(IServiceRegistry services)
            {
                GetModel<SceneStateModel>();
            }

            /// <inheritdoc />
            protected override void OnStart()
            {
                throw new InvalidOperationException("Expected scene startup failure.");
            }
        }

        /// <summary>
        /// Creates application models in a deterministic order.
        /// </summary>
        private sealed class ApplicationModelConsumer : ModuleBase
        {
            /// <inheritdoc />
            protected override void OnConfigure(IServiceRegistry services)
            {
                GetModel<OrderedModel>();
                GetModel<FailingDisposeModel>();
            }
        }
    }
}
