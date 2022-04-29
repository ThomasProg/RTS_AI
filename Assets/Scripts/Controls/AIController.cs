using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// $$$ TO DO :)
[RequireComponent(typeof(StrategyAI))]
public sealed class AIController : UnitController
{
    TargetBuilding[] allCapturePoints;

    StrategyAI strategyAI;

    #region MonoBehaviour methods

    protected override void Awake()
    {
        base.Awake();

        strategyAI = GetComponent<StrategyAI>();
    }

    protected override void Start()
    {
        base.Start();

        allCapturePoints = FindObjectsOfType(typeof(TargetBuilding)) as TargetBuilding[];
    }

    protected override void Update()
    {
        base.Update();

        if (!strategyAI.taskRunner.IsRunningTask())
        {
            StrategyAI.Blackboard blackboard = new StrategyAI.Blackboard
            {
                controller = this,
                allyUnits = UnitList,
                allyFactories = FactoryList,
                nbBuildPoints = TotalBuildPoints,
                allCapturePoints = allCapturePoints,
                squadManager = strategyAI.squadManager
            };
            strategyAI.RunCaptureStrategy(blackboard);
        }
    }

    #endregion
}
