using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MeshEditor.CoreInterface
{
	/// <summary>
	/// trida urcena pro informovani klienta o zacatku a konci nejake operace
	/// </summary>
	public class LongOpNotifier
	{
		public struct Token : IDisposable
		{
			private static int tokenCounter = 0;

			public static Token None = default(Token);

			public static Token CreateNew(LongOpNotifier source)
			{
				return new Token(source);
			}

			private LongOpNotifier source;
			private Token(LongOpNotifier source)
			{
				Debug.Assert(source != null);
				this.source = source;
				tokenCounter = unchecked(tokenCounter + 1);
				LongOpId = tokenCounter;
			}

			public int LongOpId { get; }

			public void Dispose()
			{
				source?.End(this);
			}

			public override int GetHashCode()
			{
				return LongOpId.GetHashCode();
			}

			public override bool Equals(object obj)
			{
				if (!(obj is Token))
					return false;
				return this.LongOpId == ((Token)obj).LongOpId;
			}
		}

		public struct State
		{
			public static readonly State Empty = default(State);

			public string TaskName { get; }
			public string OperationName { get; }
			public int PercentDone { get; }

			public State(string taskName, string operationName, int percentDone)
			{
				TaskName = taskName;
				OperationName = operationName;
				PercentDone = percentDone;
			}

			public State(string taskName, int percentDone)
				: this(taskName, null, percentDone)
			{ }

			public State(string taskName)
				: this(taskName, null, -1)
			{ }

			public override string ToString()
			{
				string state;
				if (!string.IsNullOrEmpty(TaskName) && !string.IsNullOrEmpty(OperationName))
					state = TaskName + " / " + OperationName;
				else if (!string.IsNullOrEmpty(TaskName))
					state = TaskName;
				else
					state = OperationName ?? "";

				if (PercentDone > 0)
					return $"{state} ({PercentDone}%)";

				return state;
			}
		}

		public event Action<Token, bool> HasBegun;
		public event Action<Token> HasEnded;
		public event Action<Token> CancellationRequested;

		public event Action<Token> ProgressChanged;

		HashSet<Token> runningOperations = new HashSet<Token>();
		Dictionary<Token, State> operationStateMap = new Dictionary<Token, State>();

		public Token Begin(string taskName, bool isCancellable = false)
		{
			var token = Token.CreateNew(this);
			Debug.Assert(!runningOperations.Contains(token));
			runningOperations.Add(token);
			operationStateMap[token] = new State(taskName);
			HasBegun?.Invoke(token, isCancellable);
			return token;
		}

		public void End(Token operationToken)
		{
			if (runningOperations.Remove(operationToken))
			{
				HasEnded?.Invoke(operationToken);
			}
		}

		public void Cancel(Token operationToken)
		{
			CancellationRequested?.Invoke(operationToken);
		}

		public State GetState(Token operationToken)
		{
			if (!IsRunning(operationToken))
				return State.Empty;
			return operationStateMap[operationToken];
		}

		public void UpdateState(Token operationToken, string operationName, int percentDone = -1)
		{
			State state;
			if (!operationStateMap.TryGetValue(operationToken, out state))
				state = State.Empty;
			operationStateMap[operationToken] = new State(state.TaskName, operationName, percentDone);
			ProgressChanged?.Invoke(operationToken);
		}

		public bool IsRunning(Token token)
		{
			return runningOperations.Contains(token);
		}
	}
}
