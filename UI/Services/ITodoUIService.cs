using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Blazor;
using Stl.Text;

namespace Templates.Blazor2.Abstractions
{
	public interface ITodoUIService
	{
		// Queries
		[ComputeMethod]
		Task<TodoUIModel> Get(Session session, CancellationToken cancellationToken = default);

		[ComputeMethod]
		Task<TodoUIItemModel> GetItem(Session session, string id, CancellationToken cancellationToken = default);

		// Commands (actions)
		Task LoadMore(Session session, CancellationToken cancellationToken = default);
		void Clear(Session session);
	}

	public record TodoUIModel
	{
		public ReadOnlyCollection<TodoUIItemModel> Items { get; init; } = Array.AsReadOnly(Array.Empty<TodoUIItemModel>());
		public Symbol SessionId { get; init; }
		public int PageSize { get; init; } = 5;
		public bool HasMore { get; init; } = false;
		public DateTime LastStateUpdateTime { get; init; } = DateTime.UtcNow;
	}

	public record TodoUIItemModel(Todo Todo)
	{
		//public Task InvertDone()
		//{
		//	var newTodo = Todo with { IsDone = !Todo.IsDone };
		//	return Parent.Call<string>(new AddOrUpdateTodoCommand(Parent.Session, newTodo));
		//}

		//public Task UpdateTitle(Todo todo, string title)
		//{
		//	title = title.Trim();
		//	if (todo.Title == title)
		//		return Task.CompletedTask;
		//	todo = todo with { Title = title };
		//	return Parent.Call<string>(new AddOrUpdateTodoCommand(Parent.Session, todo));
		//}

		//public Task Remove(Todo todo)
		//	=> Parent.Call<string>(new RemoveTodoCommand(Parent.Session, todo.Id));
	}

}
