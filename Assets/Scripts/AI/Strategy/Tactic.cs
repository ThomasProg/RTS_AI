using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Tactic
{
    protected InBetweenTask processTask;
    public float baseEvalPriorityScore = 1f;
    public float lastEvaluationTime;

    public float priority = 0f;

    public abstract void EvaluatePriority();

    public float GetEvaluationFrequencyScore()
    {
        return baseEvalPriorityScore - Time.time + lastEvaluationTime;
    }

    public InBetweenTask GetProcessTask()
    {
        return processTask;
    }
}