using Tritone.Kernel;
using UnityEngine;

namespace Tritone.Unity
{
    /// <summary>
    /// Bridges the Unity lifecycle to one Tritone application instance.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public abstract class TritoneBootstrap : MonoBehaviour
    {
        /// <summary>
        /// Owns the Tritone application created by this component.
        /// </summary>
        private GameApplication mApplication;

        /// <summary>
        /// Tracks total unscaled time since application startup.
        /// </summary>
        private double mElapsedTime;

        /// <summary>
        /// Tracks the zero-based Tritone frame index.
        /// </summary>
        private ulong mFrameIndex;

        /// <summary>
        /// Stores timing data shared by the update and late-update stages.
        /// </summary>
        private FrameTime mFrameTime;

        /// <summary>
        /// Tracks total unscaled time advanced by fixed simulation updates.
        /// </summary>
        private double mFixedElapsedTime;

        /// <summary>
        /// Tracks the zero-based fixed simulation step index.
        /// </summary>
        private ulong mFixedFrameIndex;

        /// <summary>
        /// Gets the running Tritone application instance.
        /// </summary>
        protected GameApplication Application => mApplication;

        /// <summary>
        /// Adds the game-specific modules required by this Unity project.
        /// </summary>
        /// <param name="builder">The builder used to register application modules.</param>
        protected abstract void Configure(GameApplicationBuilder builder);

        /// <summary>
        /// Builds and starts the Tritone application before normal Unity behaviours run.
        /// </summary>
        protected virtual void Awake()
        {
            var builder = new GameApplicationBuilder();
            Configure(builder);

            mApplication = builder.Build();
            mApplication.Start();
        }

        /// <summary>
        /// Forwards one Unity frame to the Tritone update pipeline.
        /// </summary>
        protected virtual void Update()
        {
            if (mApplication == null)
                return;

            var deltaTime         = (double)Time.deltaTime;
            var unscaledDeltaTime = (double)Time.unscaledDeltaTime;
            mElapsedTime         += unscaledDeltaTime;

            mFrameTime = new FrameTime(deltaTime,
                                       unscaledDeltaTime,
                                       mElapsedTime,
                                       mFrameIndex++);
            mApplication.Update(in mFrameTime);
        }

        /// <summary>
        /// Forwards the Unity late-update stage to the Tritone update pipeline.
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (mApplication == null)
                return;

            mApplication.LateUpdate(in mFrameTime);
        }

        /// <summary>
        /// Forwards one Unity fixed simulation step to the Tritone update pipeline.
        /// </summary>
        protected virtual void FixedUpdate()
        {
            if (mApplication == null)
                return;

            var deltaTime         = (double)Time.fixedDeltaTime;
            var unscaledDeltaTime = (double)Time.fixedUnscaledDeltaTime;
            mFixedElapsedTime    += unscaledDeltaTime;

            var frameTime = new FrameTime(deltaTime,
                                          unscaledDeltaTime,
                                          mFixedElapsedTime,
                                          mFixedFrameIndex++);
            mApplication.FixedUpdate(in frameTime);
        }

        /// <summary>
        /// Stops the Tritone application when Unity destroys this bootstrap component.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (mApplication == null)
                return;

            mApplication.Dispose();
            mApplication = null;
        }
    }
}
