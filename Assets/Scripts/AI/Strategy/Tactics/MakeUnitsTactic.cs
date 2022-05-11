using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class MakeUnitsTactic : Tactic
{
    public StrategyAI stratAI;

    class ProcessTask : InBetweenTask
    {
        public MakeUnitsTactic tactic;

        public override void OnStart()
        {
            tactic.stratAI.controller.Factories[0].RequestUnitBuild(0);

            // TODO : wait for seconds

            RunNextTask();
        }
    }

    public MakeUnitsTactic()
    {
        processTask = new ProcessTask { tactic = this };
    }

    public override void EvaluatePriority()
    {
        priority = 1f;
    }
}