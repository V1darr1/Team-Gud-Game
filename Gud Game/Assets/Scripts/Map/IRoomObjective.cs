using System;

public interface IRoomObjective
{
    bool IsComplete { get; }
    event Action<IRoomObjective> OnCompleted;
}