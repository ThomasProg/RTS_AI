using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QueryUnitsTask : IPOITask<StrategyAI.Blackboard>
{
    struct UnitsSource
    {
        public Object unitsSource; // Squad or Factory
        public float timeToGoAtLocation;
    }


    public PointOfInterest pointOfInterest;

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
        SortedList<float, System.Object> unitsSources = new SortedList<float, System.Object>();
        foreach (Squad squad in blackboard.squadManager.squads)
        {
            if (squad.PointOfInterest == null || squad.PointOfInterest.priority < pointOfInterest.priority)
            {
                float time = (pointOfInterest.position - squad.GetAveragePosition()).magnitude / squad.GetSquadSpeed();
                unitsSources.Add(time, squad);

            }
        }

        foreach (Factory factory in blackboard.AllyFactories)
        {
            float time = (pointOfInterest.position - new Vector2(factory.transform.position.x, factory.transform.position.z)).magnitude;
            unitsSources.Add(time, factory);
        }

        float currentStrength = 0;
        float strengthRequired = 3;
        int nbUnitsBeingCreated = 0;
        foreach (System.Object unitsSource in unitsSources.Values)
        {
            switch (unitsSource)
            {
                case Squad squad:
                    squad.PointOfInterest = pointOfInterest;
                    currentStrength += squad.GetStrength();
                    break;

                case Factory factory:
                    factory.RequestUnitBuild(0);
                    currentStrength += 1;
                    nbUnitsBeingCreated++;
                    break;
            }

            if (strengthRequired >= currentStrength)
                break;
        }

        //yield return new WaitUntil();

        yield return null;
    }

}
