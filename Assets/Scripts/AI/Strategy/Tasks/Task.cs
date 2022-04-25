using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskRunner
{
    Task currentTask;
    
    public object blackboard;

    public bool IsRunningTask()
    {
        return currentTask != null;
    }

    public void StopCurrentTask()
    {
        if (currentTask != null)
            currentTask.OnStop();

        currentTask.taskRunner = null;
        currentTask = null;
    }

    public void AssignNewTask(Task newTask)
    {
        if (currentTask != null)
        {
            currentTask.OnEnd();
            currentTask.taskRunner = null;
        }
        currentTask = newTask;
        if (currentTask != null)
        {
            currentTask.taskRunner = this;
            currentTask.OnStart();
        }
    }

    public void UpdateCurrentTask()
    {
        currentTask.OnUpdate();
    }
}

public abstract class Task 
{
    public TaskRunner taskRunner;

    public abstract void OnStart();
    public virtual void OnEnd() {}
    public virtual void OnUpdate() {}
    public virtual void OnStop() {}
}

abstract class PredicateTask : Task
{
    public Task falseCase;
    public Task trueCase;

    public override void OnStart()
    {
        RunNextTask();
    }

    protected void RunNextTask()
    {
        taskRunner.AssignNewTask(IsPredicateTrue() ? trueCase : falseCase);
    }

    protected abstract bool IsPredicateTrue();
}