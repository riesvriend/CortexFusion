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
    [Observable]
    public abstract class BaseStore<T> : IDisposable
    {
        protected IStateFactory StateFactory;
        protected ISharedState SharedState;
        protected Session Session;
        private bool _disposedValue;

        protected Action<IState, StateEventKind> StateChanged { get; set; }
        protected IState<T>? LiveState { get; set; }

        public BaseStore(ISharedState sharedState, IStateFactory stateFactory, Session session)
        {
            SharedState = sharedState;
            StateFactory = stateFactory;
            Session = session;
            StateChanged = (state, eventKind) => {
                OnLiveStateChanged(state, eventKind);
            };
            EnsureCreate();
        }

        public void EnsureCreate()
        {
            if (LiveState != null)
                return;

            LiveState = CreateState();
            ((IState)LiveState).AddEventHandler(StateEventKind.Updated, StateChanged);
        }

        protected virtual void OnLiveStateChanged(IState state, StateEventKind stateEventKind)
        {
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
                    ((IState?)LiveState)?.RemoveEventHandler(StateEventKind.Updated, StateChanged);
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
    }
}
