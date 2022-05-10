using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilitySystem;

[RequireComponent(typeof(SquadManager))]
public class StrategyAI : MonoBehaviour
{
    public class Blackboard
    {
        public AIController controller;
        public SquadManager squadManager;

        public int nbUnits;
        public int nbEnemyUnits;
        public int nbBuildings;
        public int nbEnemyBuildings;

        public List<Unit> allyUnits;
        public List<Factory> allyFactories;
        public int nbBuildPoints;

        public List<Squad> idleGroups;
        public TargetBuilding[] allCapturePoints;
    }


    UtilitySystem.UtilitySystem objectif;
    UtilitySystem.UtilitySystem subjectif;

    public SquadManager squadManager;

    public AIController controller;

    private void Awake()
    {
        squadManager = GetComponent<SquadManager>();

        TargetBuilding[] allBuildings = FindObjectsOfType(typeof(TargetBuilding)) as TargetBuilding[];

        foreach (TargetBuilding targetBuilding in  allBuildings)
        {
            //squadTasks.Add(new );
        }
    }

    class MakeUnitsTactic : Tactic
    {
        public StrategyAI stratAI;

        public override void EvaluatePriority()
        {
            priority = 1f;
        }

        public override void Process()
        {
            stratAI.blackboard.allyFactories[0].RequestUnitBuild(0);
        }
    }

    class CapturePointTactic : SquadTactic
    {
        public StrategyAI stratAI;
        public TargetBuilding targetBuilding;

        public CapturePointTactic()
        {
            position = targetBuilding.Get2DPosition();
        }

        public override void EvaluatePriority()
        {
            // todo : change priority if enemies are close, etc
            if (targetBuilding.GetTeam() == stratAI.controller.GetTeam())
            {
                priority = -1f;
            }
            else if (targetBuilding.GetTeam() == ETeam.Neutral)
            {
                priority = 3f;
            }
            else // enemy team
            {
                priority = 6f;
            }
        }

        public override Squad TryShrink(List<Squad> totalSquads)
        {
            return null;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        AddTactic(new MakeUnitsTactic { stratAI = this });

        foreach (TargetBuilding targetBuilding in blackboard.allCapturePoints)
        {
            AddTactic(new CapturePointTactic { stratAI = this, targetBuilding = targetBuilding });
        }


    }

    void AddTactic(Tactic tactic)
    {
        alltacticsByEvaluationPriority.Add(tactic);
    }


    public TaskRunner taskRunner = new TaskRunner();
    public List<Tactic> alltacticsByEvaluationPriority = new List<Tactic>();
    public List<Tactic> allTacticsByPriority;
    public Blackboard blackboard;

    // Update is called once per frame
    void Update()
    {
        if (taskRunner.IsRunningTask())
            taskRunner.UpdateCurrentTask();
        else
        {
            alltacticsByEvaluationPriority.Sort((Tactic a, Tactic b) => a.GetEvaluationFrequencyScore().CompareTo(b.GetEvaluationFrequencyScore()));

            for (int i = 0; i < alltacticsByEvaluationPriority.Count && alltacticsByEvaluationPriority[i].GetEvaluationFrequencyScore() < 0f; i++)
            {
                alltacticsByEvaluationPriority[i].EvaluatePriority();
                alltacticsByEvaluationPriority[i].lastEvaluationTime = Time.time;
                // wait for seconds
            }

            // Sort by priority
            allTacticsByPriority = new List<Tactic>(alltacticsByEvaluationPriority);
            allTacticsByPriority.Sort((Tactic a, Tactic b) => a.priority.CompareTo(b.priority));

            // Will try to assign enough units for priority tasks
            foreach (Tactic tactic in allTacticsByPriority)
            {
                tactic.Process();
            }

            // TODO : do things with idle squads ?
        }
    }
}
