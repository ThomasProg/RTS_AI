using System;
using System.Collections.Generic;
using System.Linq;

public interface IPOITask<BLACKBOARD>
{
    // After running this task, if you want to execute the following task of the POI, just yield break;
    // After running this task, if you don't want to execute the following task of the POI, just yield return null;
    // After running this task, if you want to wait for some delay, just return new WaitForSeconds(time);
    // It will simulate the task taking time ; don't wait for too long or it will cause issues (other tasks won't be executed during that time)
    System.Collections.IEnumerator Execute(BLACKBOARD blackboard);
}

// Tasks are added in the priority they should be executed
public class PriorityTaskRunner : TaskRunner
{
    public readonly List<Task> tasks = new List<Task>();

    public bool HasTasksRunning()
    {
        return tasks.Count != 0;
    }

    public void AddNewTask(Task newTask)
    {
        if (tasks.Count == 0)
            base.AssignNewTask(newTask);

        tasks.Add(newTask);
    }

    public void Update()
    {
        foreach (Task task in tasks)
        {
            StopCurrentTask();
            AssignNewTask(task);
            UpdateCurrentTask();
        }
    }
}

public class PoolTaskRunner : TaskRunner
{
    public readonly List<Task> tasks = new List<Task>();

    public void Clear()
    {
        StopCurrentTask();
        tasks.Clear();
    }

    public void ProcessNextTask()
    {
        if (currentTask != null)
        {
            currentTask.OnStop();
            tasks.Remove(currentTask);
        }


        currentTask = tasks.Count == 0 ? null : tasks.First();
        
        if (IsRunningTask())
        {
            currentTask.taskRunner = this;
            currentTask.OnStart();
        }
    }

    public override void AssignNewTask(Task newTask)
    {
        StopCurrentTask();
        tasks.Clear();
        tasks.Add(newTask);
        base.AssignNewTask(newTask);
    }
    
    public void AddNewTask(Task newTask)
    {
        if (tasks.Count == 0)
            base.AssignNewTask(newTask);

        tasks.Add(newTask);
    }
}

public class TaskRunner
{
    protected Task currentTask;

    public object blackboard;

    public bool IsTaskRunning(Task task)
    {
        return currentTask == task;
    }

    public bool IsRunningTask()
    {
        return currentTask != null;
    }

    public void StopCurrentTask()
    {
        if (IsRunningTask())
        {
            currentTask.OnStop();
            currentTask.taskRunner = null;
            currentTask = null;
        }
    }

    public virtual void AssignNewTask(Task newTask)
    {
        if (IsRunningTask())
        {
            currentTask.OnEnd();
            currentTask.taskRunner = null;
        }

        currentTask = newTask;
        if (IsRunningTask())
        {
            currentTask.taskRunner = this;
            currentTask.OnStart();
        }
    }

    public void UpdateCurrentTask()
    {
        if (IsRunningTask())
            currentTask.OnUpdate();
    }
}

public abstract class Task
{
    public TaskRunner taskRunner;

    public abstract void OnStart();

    public virtual void OnEnd()
    {
    }

    public virtual void OnUpdate()
    {
    }

    public virtual void OnStop()
    {
    }
}

public abstract class PredicateTask : Task
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

public abstract class InBetweenTask : Task
{
    public Task next;

    protected void RunNextTask()
    {
        taskRunner.AssignNewTask(next);
    }
}

public class ActionTask : Task
{
    public Action<TaskRunner> action;

    public override void OnStart()
    {
        action?.Invoke(taskRunner);
    }
}