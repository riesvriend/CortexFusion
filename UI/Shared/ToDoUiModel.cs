using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stl.CommandR;
using Stl.Fusion;
using Stl.Fusion.Authentication;
using Stl.Fusion.Blazor;
using Templates.Blazor2.Abstractions;

namespace Templates.Blazor2.UI.Shared
{
	public record ToDoUiItemModel(ToDoUiModel Parent, Todo Todo)
	{
		private Task InvertDone()
		{
			var newTodo = Todo with { IsDone = !Todo.IsDone };
			return Parent.Call<string>(new AddOrUpdateTodoCommand(Parent.Session, newTodo));
		}

		private Task UpdateTitle(Todo todo, string title)
		{
			title = title.Trim();
			if (todo.Title == title)
				return Task.CompletedTask;
			todo = todo with { Title = title };
			return Parent.Call<string>(new AddOrUpdateTodoCommand(Parent.Session, todo));
		}

		private Task Remove(Todo todo)
			=> Parent.Call<string>(new RemoveTodoCommand(Parent.Session, todo.Id));

	}

	public record ToDoUiModel
	{
		public ILiveState<Todo[]> LiveState;
		public ReadOnlyCollection<ToDoUiItemModel> Items;
		public Session Session;
		public CommandRunner CommandRunner;

		public ToDoUiModel(ILiveState<Todo[]> liveState, Session session, CommandRunner commandRunner)
		{
			LiveState = liveState;
			Session = session;
			CommandRunner = commandRunner;
			var uiItems = new List<ToDoUiItemModel>();
			foreach (var todo in LiveState.LastValue) {
				uiItems.Add(new ToDoUiItemModel(this, todo));
			}
			Items = uiItems.AsReadOnly();
		}
		
		public Task Call<TResult>(ICommand command, CancellationToken cancellationToken = default)
		{
			return CommandRunner.Call<TResult>(command, cancellationToken);
		}
	}
}
