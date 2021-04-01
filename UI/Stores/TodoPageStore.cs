using Stl.Fusion.Extensions;
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
using Stl.Fusion.Blazor;
using Templates.Blazor2.Abstractions;
using Newtonsoft.Json;
using Stl.CommandR;

namespace Templates.Blazor2.UI.Stores
{
    public class ExceptionStore
    {
        public ExceptionStore(Exception ex)
        {
            Message = ex.Message;
        }

        public string Message { get; set; }
    }

    [Observable]
    public class TodoPageStore : BaseStore<GetTodoPageResponse>
    {
        protected ITodoService TodoService = default!;

        public string? GetTodoPageResponseAsJson { get; set; }
        public ExceptionStore? FusionQueryException { get; set; }
        public ExceptionStore? FusionCommandException { get; set; }

        public GetTodoPageRequest? GetTodoPageRequest;

        [Computed]
        public GetTodoPageResponse? GetTodoPageResponse {
            get {
                if (GetTodoPageResponseAsJson == null)
                    return null;
                else
                    return JsonConvert.DeserializeObject<GetTodoPageResponse>(GetTodoPageResponseAsJson);
            }
        }

        [Computed]
        public DateTime? LastStateUpdateTimeUtc => GetTodoPageResponse?.LastUpdatedUtc;

        [Computed]
        public bool HasMore => GetTodoPageResponse?.HasMore == true;

        [Action]
        public void SetGetTodoPageResponse(GetTodoPageResponse? getTodoPageResponse)
        {
            if (getTodoPageResponse == null)
                GetTodoPageResponseAsJson = null;
            else
                // Consider to pass in the raw JSON from the RestClient when working in WebAssembly
                // saving the serialization
                // We do the serialization so that all the derived values are cached and matched by value
                // automatically saving rerenders if the object tree shape stays similar
                GetTodoPageResponseAsJson = JsonConvert.SerializeObject(getTodoPageResponse);
        }

        public TodoPageStore()
        {
           
        }
        public void Init(
            ISharedState sharedState,
            IStateFactory stateFactory,
            Session session,
            ITodoService todoService,
            CommandRunner commandRunner)
        {
            if (todoService == null)
                throw new ArgumentNullException(nameof(todoService));
            TodoService = todoService;
            GetTodoPageRequest = new GetTodoPageRequest { PageRef = new PageRef<string>(), PageSize = 5 };

            base.Init(sharedState, stateFactory, session, commandRunner);
        }

        protected override async Task<GetTodoPageResponse> ComputeState(CancellationToken cancellationToken)
        {
            var response = await TodoService.GetTodoPage(Session, GetTodoPageRequest, cancellationToken);
            return response;
        }

        protected override void OnLiveStateChanged(IState state, StateEventKind stateEventKind)
        {
            Debug.WriteLine($"OnLiveStateChanged {state}. Todos: {LiveState?.UnsafeValue?.Todos.Length} ");
            SetGetTodoPageResponse(LiveState?.UnsafeValue);
            UpdateQueryException();
        }

        [Action]
        public void LoadMore()
        {
            GetTodoPageRequest.PageSize *= 2;
            Requery();  // See if we can make an autorunner for this
        }

        [Action]
        public async Task CreateTodo(string newTodoTitle)
        {
            var todo = new Todo(Id: "", newTodoTitle, IsDone: false);
            await Call(new AddOrUpdateTodoCommand(Session, todo));
        }

        [Action]
        public async Task ToggleDone(Todo todo)
        {
            todo = todo with { IsDone = !todo.IsDone };
            await Call(new AddOrUpdateTodoCommand(Session, todo));
        }

        [Action]
        public async Task UpdateTitle(Todo todo, string title)
        {
            title = title.Trim();
            if (todo.Title == title)
                return;
            todo = todo with { Title = title };
            await Call(new AddOrUpdateTodoCommand(Session, todo));
        }

        [Action]
        public async Task Remove(Todo todo)
        {
            await Call(new RemoveTodoCommand(Session, todo.Id));
        }

        protected async Task Call(ICommand command)
        {
            await CommandRunner.Call<Task>(command, cancellationToken: default);
            UpdateCommandException();
        }

        protected void UpdateCommandException()
        {
            if (FusionCommandException == null && CommandRunner.Error == null)
                return;

            if (CommandRunner.Error != null)
                FusionCommandException = new ExceptionStore(CommandRunner.Error);
            else
                FusionCommandException = null;
        }

        protected void UpdateQueryException()
        {
            if (FusionQueryException == null && LiveState?.Error == null)
                return;

            if (LiveState?.Error != null)
                FusionQueryException = new ExceptionStore(LiveState.Error);
            else
                FusionQueryException = null;
        }
    }
}
