using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SquadTactic : Tactic
{
    public SquadManager squadManager;

    // squads doing the task or going to the task
    // squads will try to merge automatically if they are close enough
    public List<Squad> squads = new List<Squad>();

    public Vector2 position;

    public SquadTactic()
    {
        processTask = new ProcessTask { tactic = this };
    }

    public virtual void AddSquad(Squad squad)
    {
        squads.Add(squad);
    }

    public virtual void RemoveSquad(Squad squad)
    {
        squads.Remove(squad);
    }

    // Returns true if there are enough units for this tactic, false otherwise
    // This function should return true the moment a new squad is created etc
    //
    // new squads should be registered in the squadManager
    // totalSquads are squads that will be added to this tactic.
    // If you want don't want to add some squads or units, you can remove them from totalSquads.
    public abstract bool TryShrink(ref List<Squad> totalSquads);

    void RemoveAllSquads()
    {
        for (int i = squads.Count - 1; i > -1; i--)
        {
            squads[i].Tactic = null;
        }
    }

    class ProcessTask : InBetweenTask
    {
        public SquadTactic tactic;

        public override void OnStart()
        {
            if (tactic.priority < 0f)
            {
                tactic.RemoveAllSquads();
                RunNextTask();
                return;
            }

            bool isCurrentTaskDoable = false;

            List<Squad> tacticNewSquads = new List<Squad>(tactic.squads);

            List<Squad> allSquadsByDistance = new List<Squad>(tactic.squadManager.squads);
            // sort squads by distance to the task
            allSquadsByDistance.Sort((Squad a, Squad b) =>
            {
                // TODO : change into travel cost ? remove some points depending on if the squad is already doing a task ?
                float lengthA = (tactic.position - a.GetAveragePosition()).SqrMagnitude();
                float lengthB = (tactic.position - b.GetAveragePosition()).SqrMagnitude();

                return lengthA.CompareTo(lengthB);
            });

            // A squad that has been created after being shrinked
            List<Squad> newSquads = new List<Squad>();

            // search through all the squads, and will try to assign squads doing a tactic with less priority than the current one
            foreach (Squad squad in allSquadsByDistance)
            {
                // Wait for seconds
                if (squad.Tactic == null || squad.Tactic.priority < tactic.priority)
                {
                    tacticNewSquads.Add(squad);
                    if (tactic.TryShrink(ref tacticNewSquads))
                    {
                        isCurrentTaskDoable = true;
                        break;
                    }
                }
            }

            if (isCurrentTaskDoable)
            {
                foreach (Squad squad in tacticNewSquads)
                {
                    // Wait for seconds
                    squad.Tactic = tactic;
                }
            }
            else
            {
                tactic.RemoveAllSquads();
            }

            // TODO : wait for seconds

            RunNextTask();
        }
    }
}