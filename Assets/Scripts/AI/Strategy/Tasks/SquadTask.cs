using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SquadTask : Task
{


    public abstract float GetPriorityScoreForSquad(Squad squad);

    public abstract float GetTravelCostForSquad(Squad squad);
}
