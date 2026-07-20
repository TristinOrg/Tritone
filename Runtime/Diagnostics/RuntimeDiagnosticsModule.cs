using System;
using Tritone.Entities;
using Tritone.Flows;
using Tritone.Kernel;

namespace Tritone.Diagnostics
{
    /// <summary>
    /// Samples application state and frame timing for runtime inspection.
    /// </summary>
    public sealed class RuntimeDiagnosticsModule : ModuleBase, IRuntimeDiagnosticsService, IUpdateSystem
    {
        /// <summary>
        /// Stores the fixed-capacity recent log buffer.
        /// </summary>
        private readonly RuntimeLogBufferSink mLogs;

        /// <summary>
        /// Stores the frame sampling window in seconds.
        /// </summary>
        private readonly double mSampleWindow;

        /// <summary>
        /// Stores the running application.
        /// </summary>
        private GameApplication mApplication;

        /// <summary>
        /// Stores scene module status.
        /// </summary>
        private ISceneModuleService mScenes;

        /// <summary>
        /// Stores flow status.
        /// </summary>
        private IFlowService mFlows;

        /// <summary>
        /// Stores entity world status.
        /// </summary>
        private IEntityService mEntities;

        /// <summary>
        /// Stores elapsed time in the current sample window.
        /// </summary>
        private double mElapsed;

        /// <summary>
        /// Stores accumulated frame duration in seconds.
        /// </summary>
        private double mTotalFrameTime;

        /// <summary>
        /// Stores the minimum frame duration in seconds.
        /// </summary>
        private double mMinimumFrameTime;

        /// <summary>
        /// Stores the maximum frame duration in seconds.
        /// </summary>
        private double mMaximumFrameTime;

        /// <summary>
        /// Stores the frame count in the current sample window.
        /// </summary>
        private int mFrameCount;

        /// <summary>
        /// Initializes runtime diagnostics sampling.
        /// </summary>
        /// <param name="logs">The log buffer shared with the application logger.</param>
        /// <param name="sampleWindow">The positive frame sampling window in seconds.</param>
        /// <param name="order">The normal update execution order.</param>
        public RuntimeDiagnosticsModule(RuntimeLogBufferSink logs, double sampleWindow = 0.5, int order = int.MaxValue)
        {
            mLogs = logs ?? throw new ArgumentNullException(nameof(logs));
            if (double.IsNaN(sampleWindow) || double.IsInfinity(sampleWindow) || sampleWindow <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(sampleWindow));

            mSampleWindow = sampleWindow;
            Order         = order;
            ResetSample();
        }

        /// <inheritdoc />
        public int Order { get; }

        /// <inheritdoc />
        public RuntimeDiagnosticSnapshot Snapshot { get; private set; }

        /// <inheritdoc />
        public RuntimeLogBufferSink Logs => mLogs;

        /// <inheritdoc />
        protected override void OnConfigure(IServiceRegistry services)
        {
            mApplication = services.GetRequired<GameApplication>();
            mScenes      = services.GetRequired<ISceneModuleService>();
            mFlows       = services.GetRequired<IFlowService>();
            mEntities    = services.GetRequired<IEntityService>();
            services.AddSingleton<IRuntimeDiagnosticsService>(this);
        }

        /// <inheritdoc />
        public void Update(in FrameTime time)
        {
            var frameTime = Math.Max(0.0, time.UnscaledDeltaTime);
            mElapsed          += frameTime;
            mTotalFrameTime   += frameTime;
            mMinimumFrameTime  = Math.Min(mMinimumFrameTime, frameTime);
            mMaximumFrameTime  = Math.Max(mMaximumFrameTime, frameTime);
            mFrameCount++;

            if (mElapsed < mSampleWindow)
                return;

            var average = mFrameCount > 0 ? mTotalFrameTime / mFrameCount : 0.0;
            var fps     = mElapsed > 0.0 ? mFrameCount / mElapsed : 0.0;
            var sceneEntityCount = mScenes.ActiveModuleType != null ? mEntities.Scene.Count : 0;
            Snapshot = new RuntimeDiagnosticSnapshot(mApplication.State, mScenes.ActiveModuleType?.Name, mFlows.ActiveFlowType?.Name, mEntities.Application.Count, sceneEntityCount, fps, average * 1000.0, mMinimumFrameTime * 1000.0, mMaximumFrameTime * 1000.0, time.FrameIndex);
            ResetSample();
        }

        /// <inheritdoc />
        protected override void OnStop()
        {
            mApplication = null;
            mScenes      = null;
            mFlows       = null;
            mEntities    = null;
            Snapshot     = default;
            ResetSample();
        }

        /// <summary>
        /// Resets the current sampling window without allocating storage.
        /// </summary>
        private void ResetSample()
        {
            mElapsed          = 0.0;
            mTotalFrameTime   = 0.0;
            mMinimumFrameTime = double.MaxValue;
            mMaximumFrameTime = 0.0;
            mFrameCount       = 0;
        }
    }
}
