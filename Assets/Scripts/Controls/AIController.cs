using System.Collections;
using System.Collections.Generic;
using UnityEngine;


class UnitGroup
{
    public List<Unit> units;

    public Vector3 GetAveragePosition()
    {
        Vector3 averagePosition = Vector3.zero;
        foreach (Unit unit in units)
        {
            averagePosition += unit.transform.position;
        }
        return averagePosition / units.Count;
    }

    // Modifiy in place
    public static void FuseGroupsDependingOnDistance(List<UnitGroup> groups)
    {
        for (int i = 0; i < groups.Count; i++)
        {
            for (int j = i + 1; j < groups.Count; j++)
            {
                if (Vector3.Distance(groups[i].GetAveragePosition(), groups[j].GetAveragePosition()) < 50)
                {
                    groups[i].units.AddRange(groups[j].units);
                    groups.RemoveAt(j);
                    i--;
                    break;
                }
            }
        }
    }
}


// $$$ TO DO :)

public sealed class AIController : UnitController
{
    TargetBuilding[] allCapturePoints;
    List<UnitGroup> idleUnitGroups = new List<UnitGroup>(); 

    #region MonoBehaviour methods

    protected override void Awake()
    {
        base.Awake();
    }


    float GetCapturePointTacticalScore(TargetBuilding targetBuilding, Vector3 pos)
    {
        return - Vector3.Distance(targetBuilding.transform.position, pos) / 1000f;
    }

    void GetIdleUnitGroups(out List<UnitGroup> idleUnitGroups)
    {
        idleUnitGroups = new List<UnitGroup>();

        foreach (Unit unit in UnitList)
        {
            if (unit.IsIdle)
            {
                UnitGroup unitGroup = new UnitGroup { units = new List<Unit>() };
                unitGroup.units.Add(unit);
                idleUnitGroups.Add(unitGroup);
            }
        }

        UnitGroup.FuseGroupsDependingOnDistance(idleUnitGroups);
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

        GetIdleUnitGroups(out idleUnitGroups);

        List<UnitGroup> toRemove = new List<UnitGroup>();

        if (idleUnitGroups.Count <= 0)
            return;

        UnitGroup group = idleUnitGroups[0];
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

        foreach (UnitGroup g in toRemove)
        {
            idleUnitGroups.Remove(g);
        }

        timer = 2f;
        return;
    }

    protected override void Start()
    {
        base.Start();

        allCapturePoints = FindObjectsOfType(typeof(TargetBuilding)) as TargetBuilding[];
        idleUnitGroups.Add(new UnitGroup { units = UnitList });
    }

    float timer = 3f;

    protected override void Update()
    {
        base.Update();

        if (timer < 0f)
        {
            CaptureStrategy();
        }
        else
            timer -= Time.deltaTime;
    }

    #endregion
}
