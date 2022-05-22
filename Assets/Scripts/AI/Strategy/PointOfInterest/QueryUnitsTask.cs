using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QueryUnitsTask : IPOITask<StrategyAI.Blackboard>
{
    public List<Squad> squads = new List<Squad>();
    public PointOfInterest pointOfInterest;

    public float strengthRequired = 3;

    public IEnumerator Execute(StrategyAI.Blackboard blackboard)
    {
        //yield return new WaitForSeconds(2f);
        //blackboard.squadManager.QueryUnit(pointOfInterest, blackboard);

        //List<Squad> allSquadsByDistance = new List<Squad>(pointOfInterest.squadManager.squads);
        //// sort squads by distance to the task
        //allSquadsByDistance.Sort((Squad a, Squad b) =>
        //{
        //    // TODO : change into travel cost ? remove some points depending on if the squad is already doing a task ?
        //    float lengthA = (pointOfInterest.position - a.GetAveragePosition()).SqrMagnitude();
        //    float lengthB = (pointOfInterest.position - b.GetAveragePosition()).SqrMagnitude();

        //    return lengthA.CompareTo(lengthB);
        //});

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
                    time -= 30f;
                unitsSources.Add(time, squad);

            }
        }

        foreach (Factory factory in blackboard.AllyFactories)
        {
            float time = (pointOfInterest.position - new Vector2(factory.transform.position.x, factory.transform.position.z)).magnitude;
            unitsSources.Add(time, factory);
        }

        List<Squad> newSquads = new List<Squad>();
        float currentStrength = 0;
        int nbUnitsBeingCreated = 0;
        foreach (System.Object unitsSource in unitsSources.Values)
        {
            switch (unitsSource)
            {
                case Squad squad:
                    newSquads.Add(squad);
                    currentStrength += squad.GetStrength();
                    break;

                case Factory factory:
                    factory.RequestUnitBuild(0);
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
        squads = newSquads;
        foreach (Squad squad in squads)
        {
            squad.PointOfInterest = pointOfInterest;
        }

        yield return null;
    }

}
