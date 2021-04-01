using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Net;
using Cortex.Net.Api;
using Stl.Async;
using Stl.Fusion;
using Stl.Fusion.Blazor;
using Stl.Internal;
using Stl.Fusion.Authentication;

namespace Templates.Blazor2.UI.Stores
{
    public enum FusionStateStatusEnum { Loading, Updating, UpdatePending, InSync };

    /// <summary>
    /// TODO: Use composition instead of inheritance: allow a Cortrex store to keep one or more
    /// FusionLiveStates in a wrapper class, no need to inherit just forward the relevant events
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Observable]
    public abstract class BaseStore<T> : IDisposable
    {
        protected IStateFactory? StateFactory;
        protected ISharedState? SharedState;
        protected Session? Session;
        protected CommandRunner? CommandRunner;
        private bool _disposedValue;

        protected Action<IState, StateEventKind>? StateChanged { get; set; }
        protected ILiveState<T>? LiveState { get; set; }

        public FusionStateStatusEnum FusionStateStatus { get; set; }

        public void Init(ISharedState sharedState, IStateFactory stateFactory, Session session, CommandRunner commandRunner)
        {
            SharedState = sharedState;
            StateFactory = stateFactory;
            Session = session;
            CommandRunner = commandRunner;
            StateChanged = (state, eventKind) => {
                RefreshFusionStateStatus();
                if (eventKind == StateEventKind.Updated)
                    OnLiveStateChanged(state, eventKind);
            };
            EnsureCreate();
        }

        protected virtual void OnLiveStateChanged(IState state, StateEventKind stateEventKind)
        {
        }

        [Action]
        public void RefreshFusionStateStatus()
        {
            if (LiveState == null! || LiveState.Snapshot.UpdateCount == 0)
                FusionStateStatus = FusionStateStatusEnum.Loading;
            else if (LiveState.Snapshot.IsUpdating)
                FusionStateStatus = FusionStateStatusEnum.Updating;
            else if (LiveState.Snapshot.Computed.IsInvalidated())
                FusionStateStatus = FusionStateStatusEnum.UpdatePending;
            else
                FusionStateStatus = FusionStateStatusEnum.InSync;
        }

        public void EnsureCreate()
        {
            if (LiveState != null)
                return;

            LiveState = CreateState();
            ((IState)LiveState).AddEventHandler(StateEventKind.All, StateChanged);
        }

        protected LiveComponentOptions Options { get; set; } =
            LiveComponentOptions.SynchronizeComputeState
            | LiveComponentOptions.InvalidateOnParametersSet;

        protected ILiveState<T> CreateState()
        {
            if (0 != (Options & LiveComponentOptions.SynchronizeComputeState)) {
                var state = StateFactory.NewLive<T>(
                    ConfigureState,
                    async (_, ct) => {
                        // Synchronizes ComputeStateAsync call as per:
                        // https://github.com/servicetitan/Stl.Fusion/issues/202
                        var ts = TaskSource.New<T>(false);
                        await InvokeAsync(async () => {
                            try {
                                ts.TrySetResult(await ComputeState(ct));
                            }
                            catch (OperationCanceledException) {
                                ts.TrySetCanceled();
                            }
                            catch (Exception e) {
                                ts.TrySetException(e);
                            }
                        });
                        return await ts.Task.ConfigureAwait(false);
                    }, this);
                return state;
            }

            return StateFactory.NewLive<T>(ConfigureState, (_, ct) => ComputeState(ct), this);
        }

        protected async Task InvokeAsync(Func<Task> thedelegate)
        {
            // TODO: use context from Cortex/BlazorComponent shared state
            // We can use the <App> component instance for InvokeAsync as its globally available
            await thedelegate();
        }

        protected virtual void ConfigureState(LiveState<T>.Options options) { }
        protected abstract Task<T> ComputeState(CancellationToken cancellationToken);

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue) {
                if (disposing) {
                    ((IState?)LiveState)?.RemoveEventHandler(StateEventKind.All, StateChanged);
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Typically you need to call this method after UI actions to ensure
        /// the update from server is requested instantly.
        /// </summary>
        /// <param name="cancelUpdateDelay">Cancels update delay, i.e. requests instant update.</param>
        /// <param name="cancellationDelay">The delay between this call and update delay cancellation.
        /// The default (null) means it's governed by <see cref="IUpdateDelayer{T}"/>, which does this
        /// in 50ms by default.</param>
        protected void Requery(bool cancelUpdateDelay = true, TimeSpan? cancellationDelay = null)
        {
            LiveState?.Invalidate();
            if (cancelUpdateDelay)
                LiveState?.UpdateDelayer.CancelDelays(cancellationDelay);
        }
    }
}
