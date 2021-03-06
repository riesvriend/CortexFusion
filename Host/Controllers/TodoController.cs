using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;
using Stl.Fusion.Server;
using Templates.Blazor2.Abstractions;

namespace Templates.Blazor2.Host.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController, JsonifyErrors]
    public class TodoController : ControllerBase, ITodoService
    {
        private readonly ITodoService _todoService;
        private readonly ISessionResolver _sessionResolver;

        public TodoController(ITodoService todoService, ISessionResolver sessionResolver)
        {
            _todoService = todoService;
            _sessionResolver = sessionResolver;
        }

        // Commands

        [HttpPost]
        public Task<Todo> AddOrUpdate([FromBody] AddOrUpdateTodoCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(_sessionResolver);
            return _todoService.AddOrUpdate(command, cancellationToken);
        }

        [HttpPost]
        public Task Remove([FromBody] RemoveTodoCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(_sessionResolver);
            return _todoService.Remove(command, cancellationToken);
        }

        // Queries

        [HttpGet, Publish]
        public Task<Todo?> TryGet(Session? session, string id, CancellationToken cancellationToken = default)
        {
            session ??= _sessionResolver.Session;
            return _todoService.TryGet(session, id, cancellationToken);
        }

        [HttpGet, Publish]
        public Task<Todo[]> List(Session? session, PageRef<string> pageRef, CancellationToken cancellationToken = default)
        {
            session ??= _sessionResolver.Session;
            return _todoService.List(session, pageRef, cancellationToken);
        }

        [HttpGet, Publish]
        public Task<GetTodoPageResponse> GetTodoPage(Session? session, GetTodoPageRequest request, CancellationToken cancellationToken = default)
        {
            session ??= _sessionResolver.Session;
            return _todoService.GetTodoPage(session, request, cancellationToken);
        }
    }
}
