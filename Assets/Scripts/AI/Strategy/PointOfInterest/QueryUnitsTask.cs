using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class QueryUnitsTask : IPOITask<StrategyAI.Blackboard>
{
    public PointOfInterest pointOfInterest;

    public float strengthRequired = 3;
    // Removed from the travel time of a unit if the unit was already on this point of interest 
    public float persistency = 0f; 

    public IEnumerator Execute(StrategyAI.Blackboard blackboard)
    {
        // We can't delete or search items, but we can have multiple items with the same key
        SortedList<float, System.Object> unitsSources = new SortedList<float, System.Object>(Comparer<float>.Create((float f1, float f2) => 
        {
            int result = f1.CompareTo(f2);

            if (result == 0)
                result = 1;

            return result;
        }));

        foreach (Squad squad in blackboard.squadManager.squads)
        {
            if (squad.PointOfInterest == null || squad.PointOfInterest == pointOfInterest || squad.PointOfInterest.priority <= pointOfInterest.priority)
            {
                float time = (pointOfInterest.position - squad.GetAveragePosition()).magnitude / squad.GetSquadSpeed();
                if (squad.PointOfInterest == pointOfInterest)
                    time -= persistency;

                foreach (Unit unit in squad.UnitList)
                {
                    unitsSources.Add(time, unit);
                }
            }
        }

        foreach (Factory factory in blackboard.AllyFactories)
        {
            UnitDataScriptable data = factory.GetBuildableUnitData(0); // TODO: Change it depending on unit
            Vector2 factoryToPoI = (pointOfInterest.position - factory.GetInfluencePosition()).normalized;

            if (pointOfInterest is FactoryPoI factoryPoI && factory == factoryPoI.factory)
            {
                unitsSources.Add(0f, factory);
                continue;
            }

            float pathLength = GameUtility.GetPathLength(factory.GetInfluencePosition() + factory.Size * factoryToPoI, pointOfInterest.position - factory.Size *  factoryToPoI);

            float time = pathLength + data.Cost;
            unitsSources.Add(time, factory);
        }

        //List<Squad> newSquads = new List<Squad>();
        List<Unit> newUnits = new List<Unit>();
        float currentStrength = 0;
        int nbUnitsBeingCreated = 0;
        foreach (System.Object unitsSource in unitsSources.Values)
        {
            switch (unitsSource)
            {
                //case Squad squad:
                //    newSquads.Add(squad);
                //    currentStrength += squad.GetStrength();
                //    break;
                case Unit unit:
                    newUnits.Add(unit);
                    currentStrength += unit.GetStrength();
                    break;

                case Factory factory:
                    factory.RequestUnitBuild(EvaluateUnitToBuild(factory, pointOfInterest));
                    currentStrength += 1;
                    nbUnitsBeingCreated++;
                    break;
            }

            if (currentStrength >= strengthRequired)
                break;
        }

        //foreach (Squad squad in squads)
        //{
        //    if (!newSquads.Contains(squad))
        //        squad.Stop();
        //}
        //squads = newSquads;
        //foreach (Squad squad in newSquads)
        //{
        //    squad.PointOfInterest = pointOfInterest;
        //}
        foreach (Unit unit in newUnits)
        {
            Squad squad = blackboard.squadManager.squadsOfUnits[unit];
            if (squad.UnitList.Count == 1)
                blackboard.squadManager.UnregisterSquad(squad);
            squad.Remove(unit);
        }

        List<Squad> newSquads = Squad.MakeSquadsDependingOnDistance(newUnits, 1000);

        blackboard.squadManager.RegisterSquads(newSquads);

        foreach (Squad squad in newSquads)
        {
            if (squad.IsPartiallyIdle)
            {
                squad.Stop();
            }

            squad.PointOfInterest = pointOfInterest;
        }

        if (nbUnitsBeingCreated != 0)
            yield return new WaitForSeconds(3f);

        yield break;
    }

    int EvaluateUnitToBuild(Factory factory, PointOfInterest poi)
    {
        // TODO:
        return 0;
    }
}
