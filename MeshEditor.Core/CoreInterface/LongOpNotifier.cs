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

		public event Action<Token> HasBegun;
		public event Action<Token> HasEnded;
		public event Action<Token> CancellationRequested;

		public event Action<State> ProgressChanged;

		public State LastReportedState { get; private set; }

		HashSet<Token> runningOperations = new HashSet<Token>();

		public Token Begin()
		{
			var token = Token.CreateNew(this);
			Debug.Assert(!runningOperations.Contains(token));
			runningOperations.Add(token);
			HasBegun?.Invoke(token);
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

		public void ReportProgress(State operationState)
		{
			LastReportedState = operationState;
			ProgressChanged?.Invoke(operationState);
		}

		public bool IsRunningSingle(Token operationToken)
		{
			return runningOperations.Count == 1 && runningOperations.Contains(operationToken);
		}
	}
}
