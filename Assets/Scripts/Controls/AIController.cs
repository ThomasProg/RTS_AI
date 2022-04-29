using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// $$$ TO DO :)
[RequireComponent(typeof(StrategyAI))]
public sealed class AIController : UnitController
{
    TargetBuilding[] allCapturePoints;
    List<Squad> idleUnitGroups = new List<Squad>();

    StrategyAI strategyAI;

    #region MonoBehaviour methods

    protected override void Awake()
    {
        base.Awake();

        strategyAI = GetComponent<StrategyAI>();
    }

    float GetCapturePointTacticalScore(TargetBuilding targetBuilding, Vector3 pos)
    {
        return - Vector3.Distance(targetBuilding.transform.position, pos) / 1000f;
    }

    public void CaptureStrategy()
    {
        if (TotalBuildPoints > 0)
        {
            bool isUnitBuilding = FactoryList[0].RequestUnitBuild(0);
            if (isUnitBuilding)
            {
                timer = 0.2f;
                return;
            }
        }

        idleUnitGroups = Squad.MakeSquadsDependingOnDistance(UnitList.FindAll((Unit unit) => unit.IsIdle));

        List<Squad> toRemove = new List<Squad>();

        if (idleUnitGroups.Count <= 0)
            return;

        Squad group = idleUnitGroups[0];
        //foreach (UnitGroup group in idleUnitGroups)
        {
            Vector3 pos = group.GetAveragePosition();

            SortedDictionary<float, TargetBuilding> capturePointsByPriority = new SortedDictionary<float, TargetBuilding>();
            float score = 0f;
            foreach (TargetBuilding capturePoint in allCapturePoints)
            {
                if (capturePoint.GetTeam() == ETeam.Neutral)
                    score = 3f;
                else if (capturePoint.GetTeam() != GetTeam())
                {
                    score = 6f;
                }

                score += GetCapturePointTacticalScore(capturePoint, pos);

                capturePointsByPriority.Add(- score, capturePoint);
            }

            var e = capturePointsByPriority.GetEnumerator();
            e.MoveNext();
            TargetBuilding targetCapturePoint = e.Current.Value;

            Formation formation = new Formation();
            formation.units = group.units;

            foreach (Unit unit in group.units)
            {
                unit.SetCaptureTarget(targetCapturePoint);
                unit.formation = formation;
            }
            toRemove.Add(group);
        }

        foreach (Squad g in toRemove)
        {
            idleUnitGroups.Remove(g);
        }

        timer = 2f;
        return;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        allCapturePoints = FindObjectsOfType(typeof(TargetBuilding)) as TargetBuilding[];
        idleUnitGroups.Add(new Squad { units = UnitList });
    }

    float timer = 3f;

    protected override void Update()
    {
        base.Update();

        if (!strategyAI.taskRunner.IsRunningTask())
        {
            //CaptureStrategy();

            StrategyAI.Blackboard blackboard = new StrategyAI.Blackboard
            {
                controller = this,
                allyUnits = UnitList,
                allyFactories = FactoryList,
                nbBuildPoints = TotalBuildPoints
            };
            strategyAI.RunCaptureStrategy(blackboard);
        }
    }

    #endregion
}
