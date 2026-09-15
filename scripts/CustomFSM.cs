using Godot;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NJUPTClubGame.scripts
{
	internal class CustomFSM
	{
		public const string EVENT_UPDATE = "UPDATE";
		public const string EVENT_FINISHED = "FINISHED";

		public delegate Task FsmState(CustomFSM fsm, CancellationToken cancellationToken);

		private record class FsmStateContext(Task Task, 
			CancellationTokenSource Cancellation);

		private FsmStateContext current_state;
		private readonly Dictionary<string, FsmState> transitions = [];
		private readonly Dictionary<string, List<TaskCompletionSource>> obEvents = [];
		private readonly Dictionary<string, FsmState> global_events = [];
		private readonly AsyncLocal<bool> is_fsm_state = new();

		private int switch_times = 0;

		public void SwitchToState(FsmState state, bool noException = false)
		{
			var in_state = is_fsm_state.Value;
			Cancel(); //终止当前 state

			if(Interlocked.Increment(ref switch_times) > 10000)
			{
				throw new Exception("ctmd");
			}

			is_fsm_state.Value = true;
			current_state = null;

			var cts = new CancellationTokenSource();
			var task = state(this, cts.Token);
			var cur = new FsmStateContext(state(this, cts.Token), cts);
			if (current_state == null)
			{
				current_state = cur;
			}

			is_fsm_state.Value = false;

			if(cur.Task.IsCompleted)
			{
				Callable.From(() =>
				{
					if (cur == current_state)
					{
						SendEvent("FINISHED");
					}
				}).CallDeferred();
			}

			cur.Task.ContinueWith(_ =>
			{
				Callable.From(() =>
				{
					if (cur == current_state)
					{
						SendEvent("FINISHED");
					}
				}).CallDeferred();
			}, TaskContinuationOptions.OnlyOnRanToCompletion);

			GD.Print("Switch to " + state.Method.Name);

			if (in_state && !noException)
			{
				throw new TaskCanceledException();
			}
		}

		public Task WaitForEvent(string ev)
		{
			var source = new TaskCompletionSource();
			lock (obEvents)
			{
				if(!obEvents.TryGetValue(ev, out var list))
				{
					list = [];
					obEvents[ev] = list;
				}
				list.Add(source);
			}
			return source.Task;
		}

		public Task NextFrame()
		{
			return WaitForEvent(EVENT_UPDATE);
		}

		public void SendEvent(string ev)
		{
			if(transitions.TryGetValue(ev, out var state))
			{
				SwitchToState(state);
				return;
			}
			
			if(global_events.TryGetValue(ev, out state))
			{
				SwitchToState(state);
				return;
			}

			TaskCompletionSource[] sources = null;
			lock(obEvents)
			{
				if(obEvents.TryGetValue(ev, out var list))
				{
					sources = [.. list];
					list.Clear();
				}
			}

			if(sources != null)
			{
				foreach(var v in sources)
				{
					try
					{
						v.SetResult();
					}catch(TaskCanceledException)
					{ }
				}
			}
		}

		public void AddGlobalTransition(string ev, FsmState state)
		{
			global_events.Add(ev, state);
		}
		public void AddTransition(string ev, FsmState state)
		{
			transitions.Add(ev, state);
		}

		public void Cancel()
		{
			is_fsm_state.Value = false;
			current_state?.Cancellation.Cancel();
			current_state = null;

			transitions.Clear();
			lock (obEvents)
			{
				obEvents.Clear();
			}
		}

		public void Update(double delta)
		{
			switch_times = 0;

			SendEvent(EVENT_UPDATE);
		}
		public void Destroy()
		{
			Cancel();
		}
	}
}
