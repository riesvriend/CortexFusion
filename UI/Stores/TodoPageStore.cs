using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Net;
using Cortex.Net.Api;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Templates.Blazor2.Abstractions;

namespace Templates.Blazor2.UI.Stores
{
    [Observable]
    public class TodoPageStore : BaseStore<Todo[]>
    {
        protected ITodoService TodoService;

        public string? Value { get; set; }

        [Action]
        public void SetValue(string value)
        {
            Value = value;
        }

        public TodoPageStore(ISharedState sharedState, IStateFactory stateFactory, Session session, ITodoService todoService) 
            : base(sharedState, stateFactory, session)
        {
            if (todoService == null)
                throw new ArgumentNullException(nameof(todoService));
            TodoService = todoService;
        }

        protected override async Task<Todo[]> ComputeState(CancellationToken cancellationToken)
        {
            var items = await TodoService.List(Session, pageRef: 10, cancellationToken);
            return items;
        }

        protected override void OnLiveStateChanged(IState state, StateEventKind stateEventKind)
        {
            Debug.WriteLine($"OnLiveStateChanged {state}. Todos: {LiveState?.UnsafeValue?.Length} ");
        }
    }
}
