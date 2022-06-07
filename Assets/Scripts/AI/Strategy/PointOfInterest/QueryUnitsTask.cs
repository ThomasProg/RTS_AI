using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

public class QueryUnitsTask : IPOITask<StrategyAI.Blackboard>
{
    public PointOfInterest pointOfInterest;

    public float strengthRequired = 3;
    // Removed from the travel time of a unit if the unit was already on this point of interest 
    public float persistency = 0f;

    public float timeToLeadSquadToPoI = 0.0f;
    public float currentStrength = 0f;

    public bool queryAllAvailableUnits = false;
    public bool queryIdleUnitsOnly = false;
    public bool queryRepairUnitsOnly = false;

    public IEnumerator Execute(StrategyAI.Blackboard blackboard)
    {
        int nbUnitsBeingCreated;
        do
        {
            if (currentStrength > strengthRequired && !queryAllAvailableUnits)
                yield break;

            //Debug.Log(Time.time + " : ========== Query Units ==========");

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
                if (squad.PointOfInterest == null || squad.PointOfInterest.priority == 0 || squad.PointOfInterest == pointOfInterest || (squad.PointOfInterest.priority <= pointOfInterest.priority && !queryIdleUnitsOnly))
                {
                    float time = (pointOfInterest.position - squad.GetAveragePosition()).magnitude / squad.GetSquadSpeed();
                    if (squad.PointOfInterest == pointOfInterest)
                        time -= persistency;

                    foreach (Unit unit in squad.UnitList)
                    {
                        if (!queryRepairUnitsOnly || unit.GetUnitData.CanRepair)
                        {
                            unitsSources.Add(time, new System.Tuple<Unit, Squad>(unit, squad));
                        }
                    }
                }
            }


            UnitController aiController = GameServices.GetAIController();
            Dictionary<Factory, int> factoryWithUnitToBuild = new Dictionary<Factory, int>();

            //if (!queryAllAvailableUnits)
            {
                foreach (Factory factory in blackboard.AllyFactories)
                {
                    int unitMenuIndex = EvaluateUnitToBuild(aiController, factory, pointOfInterest);

                    if (unitMenuIndex == -1)
                        continue;

                    factoryWithUnitToBuild.Add(factory, unitMenuIndex);

                    int cost = factory.GetUnitCost(unitMenuIndex);
                    if (factory.CanRequestUnit(cost))
                    {
                        UnitDataScriptable data = factory.GetBuildableUnitData(0); // TODO: Change it depending on unit
                        Vector2 factoryToPoI = (pointOfInterest.position - factory.GetInfluencePosition()).normalized;

                        // If we wan't create unit from factory that is the PoI, path is egale to 0 (and GetPathLength will be invalid)
                        if (pointOfInterest is FactoryPoI factoryPoI && factory == factoryPoI.factory)
                        {
                            unitsSources.Add(0f, factory);
                            continue;
                        }

                        float pathLength =
                            GameUtility.GetPathLength(factory.GetInfluencePosition() + factory.Size * factoryToPoI,
                                pointOfInterest.position - factory.Size * factoryToPoI) / data.Speed;

                        float previousUnitsBuildDuration = 0f;
                        for (int i = 0; i < factory.GetQueuedCount(0); i++) // TODO : Do that for each unit
                        {
                            previousUnitsBuildDuration += factory.GetBuildableUnitData(0).BuildDuration;
                        }

                        float time = pathLength + data.Cost + previousUnitsBuildDuration;
                        unitsSources.Add(time, factory);
                    }
                }
            }

            List<Unit> newUnits = new List<Unit>();
            nbUnitsBeingCreated = 0;

            if (unitsSources.Values.Count > 0)
            {
                foreach (System.Object unitsSource in unitsSources.Values)
                {
                    switch (unitsSource)
                    {
                        //case Squad squad:
                        //    newSquads.Add(squad);
                        //    currentStrength += squad.GetStrength();
                        //    break;
                        case System.Tuple<Unit, Squad> unitTuple:
                            newUnits.Add(unitTuple.Item1);
                            currentStrength += unitTuple.Item1.GetStrength();
                            unitTuple.Item2.RemoveUnit(unitTuple.Item1);
                            break;

                        case Factory factory:
                            bool isUnitBeingBuilt = blackboard.squadManager.RequestUnit(factory, factoryWithUnitToBuild[factory], pointOfInterest);

                            if (isUnitBeingBuilt)
                            {
                                currentStrength += factory.GetUnitCost(factoryWithUnitToBuild[factory]);
                                nbUnitsBeingCreated++;
                            }
                            break;
                    }

                    if (currentStrength >= strengthRequired && !queryAllAvailableUnits)
                        break;
                }
            }
            else
            {
                if (blackboard.AllyFactories.Length > 1 && aiController.TotalBuildPoints < 10)
                {
                    var randomIndexes = Enumerable.Range(0, blackboard.AllyFactories.Length)
                        .OrderBy(i => UnityEngine.Random.Range(0, 100)).ToList();
                    bool found = false;
                    int i = 0;
                    do
                    {
                        bool shouldDestroy = true;
                        Factory factoryToDestroy = blackboard.AllyFactories[randomIndexes[i]];
                        if (blackboard.squadManager.newUnitsPoI.ContainsKey(factoryToDestroy) && !factoryToDestroy.IsBuildingUnit)
                        {
                            foreach (PointOfInterest poi in blackboard.squadManager.newUnitsPoI[factoryToDestroy])
                            {
                                if (poi.priority > pointOfInterest.priority)
                                {
                                    shouldDestroy = false;
                                }
                            }
                            if (shouldDestroy)
                            {
                                factoryToDestroy.Destroy();
                                found = true;
                            }
                        }

                        i++;
                    } while (!found && i < blackboard.AllyFactories.Length);
                }
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
                squad.RemoveUnit(unit);
            }

            List<Squad> newSquads = Squad.MakeSquadsDependingOnDistance(newUnits, 50);

            blackboard.squadManager.RegisterSquads(newSquads);

            if (currentStrength < strengthRequired)
            {
                foreach (Squad squad in newSquads)
                {
                    if (squad.IsPartiallyIdle)
                    {
                        squad.Stop();
                    }

                    squad.PointOfInterest = null;
                }

                yield return null;
            }
            else
            {
                foreach (Squad squad in newSquads)
                {
                    if (squad.IsPartiallyIdle)
                    {
                        squad.Stop();
                    }

                    squad.PointOfInterest = pointOfInterest;
                }

                yield return new WaitForSeconds(timeToLeadSquadToPoI);
            }

            if (nbUnitsBeingCreated != 0)
                yield return null;
        } while (nbUnitsBeingCreated != 0);

        yield break;
    }

    static int GetIndexOfUnitWithMaxSpeed(List<UnitDataScriptable> unitDatas)
    {
        float maxSpeed = 0;
        int currentIndex = 0;
        for (int i = 0; i < unitDatas.Count; i++)
        {
            if (unitDatas[i].Speed > maxSpeed)
            {
                maxSpeed = unitDatas[i].Speed;
                currentIndex = i;
            }
        }
        return currentIndex;
    }

    static int GetIndexOfUnitWithMaxStrength(List<UnitDataScriptable> unitDatas)
    {
        float maxStrength = 0;
        int currentIndex = 0;
        for (int i = 0; i < unitDatas.Count; i++)
        {
            if (unitDatas[i].DPS > maxStrength)
            {
                maxStrength = unitDatas[i].DPS;
                currentIndex = i;
            }
        }
        return currentIndex;
    }

    int EvaluateUnitToBuild(UnitController controller, Factory factory, PointOfInterest poi)
    {
        List<UnitDataScriptable> unitDatas = new List<UnitDataScriptable>(factory.UnitPrefabsCount);

        for (int i = 0; i < factory.UnitPrefabsCount; i++)
        {
            UnitDataScriptable unitDataScriptable = factory.GetBuildableUnitData(i);
            if (!queryRepairUnitsOnly || unitDataScriptable.CanRepair)
            {
                unitDatas.Add(unitDataScriptable);
            }
        }

        // The first approach is to build random unity if we can
        int unitIndex = -1;
        do
        {
            if (unitIndex != -1)
            {
                unitDatas.RemoveAt(unitIndex);
                
                if (unitDatas.Count == 0)
                    return -1; // Error
            }

            //unitIndex = UnityEngine.Random.Range(0, unitDatas.Count);
            switch (poi)
            {
                case CapturePointPoI capturePointPoI:
                    unitIndex = GetIndexOfUnitWithMaxSpeed(unitDatas);
                    break;

                default:
                    unitIndex = GetIndexOfUnitWithMaxStrength(unitDatas);
                    break;
            }

        } while (unitDatas.Count != 0 && unitDatas[unitIndex].Cost > controller.TotalBuildPoints); // While we can't buy this unit, try another
        
        return unitIndex;
    }
}
