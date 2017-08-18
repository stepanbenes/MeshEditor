using MeshEditor.Common.Logging;
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
			private static int tokenCounter;

			public static readonly Token None;

			public static Token CreateNew(LongOpNotifier source) => new Token(source);

			readonly LongOpNotifier source;

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

			public override int GetHashCode() => LongOpId.GetHashCode();
			public override bool Equals(object obj) => obj is Token other && this.LongOpId == other.LongOpId;

			public static bool operator ==(Token a, Token b) => a.Equals(b);
			public static bool operator !=(Token a, Token b) => !a.Equals(b);
		}

		public struct State
		{
			public static readonly State Empty;

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

		readonly HashSet<Token> runningOperations = new HashSet<Token>();
		readonly Dictionary<Token, State> operationStateMap = new Dictionary<Token, State>();
		readonly Dictionary<Token, IMemoryLogger> operationLoggersMap = new Dictionary<Token, IMemoryLogger>();

		public Token Begin(string taskName, bool isCancellable = false, IMemoryLogger logger = null)
		{
			var token = Token.CreateNew(this);
			Debug.Assert(!runningOperations.Contains(token));
			runningOperations.Add(token);
			operationStateMap[token] = new State(taskName);
			if (logger != null)
			{
				operationLoggersMap[token] = logger;
			}
			HasBegun?.Invoke(token, isCancellable);
			return token;
		}

		public void End(Token operationToken)
		{
			if (runningOperations.Remove(operationToken))
			{
				operationStateMap.Remove(operationToken);
				operationLoggersMap.Remove(operationToken);

				HasEnded?.Invoke(operationToken);
			}
		}

		public void Cancel(Token operationToken)
		{
			CancellationRequested?.Invoke(operationToken);
		}

		public State GetState(Token operationToken) => operationStateMap.TryGetValue(operationToken, out var state) ? state : State.Empty;

		public IMemoryLogger GetLogger(Token operationToken) => operationLoggersMap.TryGetValue(operationToken, out var logger) ? logger : null;

		public void UpdateState(Token operationToken, string operationName, int percentDone = -1)
		{
			State state;
			if (!operationStateMap.TryGetValue(operationToken, out state))
				state = State.Empty;
			operationStateMap[operationToken] = new State(state.TaskName, operationName, percentDone);
			ProgressChanged?.Invoke(operationToken);
		}

		public bool IsRunning(Token token) => runningOperations.Contains(token);
	}
}
