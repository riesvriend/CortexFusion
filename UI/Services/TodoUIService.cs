using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;
using Stl.Fusion.Blazor;
using Templates.Blazor2.Abstractions;
using System.Collections.Concurrent;
using Stl.Async;

namespace Templates.Blazor2.UI.Services
{
	[ComputeService(typeof(ITodoUIService))]
	public class TodoUIService : ITodoUIService
	{
		private readonly ITodoService _todoService;
		private readonly ConcurrentDictionary<string, TodoUIModel> _uiModelsBySession = new();

		public TodoUIService(ITodoService todoService)
		{
			_todoService = todoService;
		}

		public virtual async Task<TodoUIModel> Get(Session session, CancellationToken cancellationToken = default)
		{
			AnyGetItemDependency(session).Ignore();

			_uiModelsBySession.TryGetValue(session.Id, out var model);
			model ??= new() { SessionId = session.Id };
			//var model = new TodoUIModel { SessionId = session.Id };

			var items = await _todoService.List(session, model.PageSize + 1, cancellationToken);
			var hasMore = items.Length > model.PageSize;
			if (hasMore)
				items = items[0..model.PageSize];
			var uiItems = new List<TodoUIItemModel>();
			foreach (var todo in items) {
				uiItems.Add(new TodoUIItemModel(todo));
			}

			model = model with
			{
				Items = uiItems.AsReadOnly(),
				HasMore = hasMore,
				LastStateUpdateTime = DateTime.UtcNow
			};

			SaveModel(session, model);

			return model;
		}

		private void SaveModel(Session session, TodoUIModel model) => _uiModelsBySession[session.Id] = model;

		public async virtual Task<TodoUIItemModel> GetItem(Session session, string id, CancellationToken cancellationToken = default)
		{
			AnyGetItemDependency(session).Ignore();

			return default!;
		}

		/// <summary>
		/// Dependency used for invalidating all GetItem subscriptions
		/// </summary>
		public virtual Task AnyGetItemDependency(Session session, CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}


		public async virtual Task LoadMore(Session session, CancellationToken cancellationToken = default)
		{
			if (Computed.IsInvalidating()) {
				Get(session, cancellationToken: default).Ignore();
				AnyGetItemDependency(session, cancellationToken: default).Ignore();
				return;
			}
			var model = await Get(session, cancellationToken);
			model = model with { PageSize = model.PageSize * 2 };
			SaveModel(session, model);
		}

		public void Clear(Session session) { } // => _uiModelsBySession.TryRemove(session.Id, out _);
	}
}
