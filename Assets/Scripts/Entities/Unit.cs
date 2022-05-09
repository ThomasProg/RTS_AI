using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : BaseEntity
{
    [SerializeField] public UnitDataScriptable UnitData = null;

    public Transform BulletSlot;

    private PullTaskRunner m_taskRunner = new PullTaskRunner();

    public Formation formation;

    NavMeshAgent NavMeshAgent;
    public bool IsIdle => !m_taskRunner.IsRunningTask();

    public UnitDataScriptable GetUnitData
    {
        get { return UnitData; }
    }

    public int Cost
    {
        get { return UnitData.Cost; }
    }

    public int GetTypeId
    {
        get { return UnitData.TypeId; }
    }

    override public void Init(ETeam _team)
    {
        if (IsInitialized)
            return;

        base.Init(_team);

        HP = UnitData.MaxHP;
        OnDeadEvent += Unit_OnDead;
    }

    void Unit_OnDead()
    {
        m_taskRunner.StopCurrentTask();

        if (GetUnitData.DeathFXPrefab)
        {
            GameObject fx = Instantiate(GetUnitData.DeathFXPrefab, transform);
            fx.transform.parent = null;
        }

        Destroy(gameObject);
    }

    #region MonoBehaviour methods

    override protected void Awake()
    {
        base.Awake();

        NavMeshAgent = GetComponent<NavMeshAgent>();
        BulletSlot = transform.Find("BulletSlot");

        // fill NavMeshAgent parameters
        NavMeshAgent.speed = GetUnitData.Speed;
        NavMeshAgent.angularSpeed = GetUnitData.AngularSpeed;
        NavMeshAgent.acceleration = GetUnitData.Acceleration;
    }

    override protected void Start()
    {
        // Needed for non factory spawned units (debug)
        if (!IsInitialized)
            Init(Team);

        base.Start();
    }

    override protected void Update()
    {
        m_taskRunner.UpdateCurrentTask();

        // TODO: Can be checked each 3, 4 frames...
        TryProcessAutoAttack();
        TryProcessAutoRepair();
        TryProcessAutoCapture();
    }

    private void TryProcessAutoAttack()
    {
        if (IsIdle)
        {
            if (UnitData.IsAutoAttack)
            {
                Unit[] opponentUnits = GameServices.GetControllerByTeam(GameServices.GetOpponent(GetTeam())).Units;
                Unit target = null;
                float targetDistance = float.MaxValue;

                foreach (Unit unit in opponentUnits)
                {
                    float sqrtDist = (unit.GetInfluencePosition() - GetInfluencePosition()).sqrMagnitude;
                    if (sqrtDist < UnitData.AttackDistanceMax * UnitData.AttackDistanceMax &&
                        (target == null || sqrtDist < targetDistance))
                    {
                        targetDistance = sqrtDist;
                        target = unit;
                    }
                }

                if (target != null)
                    SetTaskAttackTarget(target);
            }
        }
    }

    private void TryProcessAutoRepair()
    {
        // Check if is always IsIdle
        if (IsIdle)
        {
            if (UnitData.IsAutoRepair && UnitData.CanRepair)
            {
                Unit[] units = GameServices.GetControllerByTeam(GetTeam()).Units;
                Unit target = null;
                float targetDistance = float.MaxValue;

                foreach (Unit unit in units)
                {
                    float sqrtDist = (unit.GetInfluencePosition() - GetInfluencePosition()).sqrMagnitude;
                    if (unit.NeedsRepairing() && sqrtDist < UnitData.RepairDistanceMax * UnitData.RepairDistanceMax &&
                        (target == null || sqrtDist < targetDistance))
                    {
                        targetDistance = sqrtDist;
                        target = unit;
                    }
                }

                if (target != null)
                    SetTaskRepairTarget(target);
            }
        }
    }

    private void TryProcessAutoCapture()
    {
        // Check if is always IsIdle
        if (IsIdle)
        {
            if (UnitData.IsAutoCapture && UnitData.IsAutoCapture)
            {
                TargetBuilding[] buildings = GameServices.GetTargetBuildings();
                TargetBuilding target = null;
                float targetDistance = float.MaxValue;

                foreach (TargetBuilding building in buildings)
                {
                    float sqrtDist = (building.GetInfluencePosition() - GetInfluencePosition()).sqrMagnitude;
                    if (building.GetTeam() != GetTeam() &&
                        sqrtDist < UnitData.CaptureDistanceMax * UnitData.CaptureDistanceMax &&
                        (target == null || sqrtDist < targetDistance))
                    {
                        targetDistance = sqrtDist;
                        target = building;
                    }
                }

                if (target != null)
                    SetTaskCaptureTarget(target);
            }
        }
    }

    #endregion

    #region IRepairable

    override public bool NeedsRepairing()
    {
        return HP < GetUnitData.MaxHP;
    }

    override public void Repair(int amount)
    {
        HP = Mathf.Min(HP + amount, GetUnitData.MaxHP);
        base.Repair(amount);
    }

    override public void FullRepair()
    {
        Repair(GetUnitData.MaxHP);
    }

    #endregion

    #region Tasks methods : Moving, Capturing, Targeting, Attacking, Repairing ...

    public void ProcessNextTask()
    {
        m_taskRunner.ProcessNextTask();
    }

    public void GoTo(Vector2 pos)
    {
        if (!NavMeshAgent.isOnNavMesh)
            return;

        NavMeshAgent.SetDestination(new Vector3(pos.x, 0f, pos.y));
        NavMeshAgent.isStopped = false;
    }

    public bool IsDestinationReached()
    {
        return NavMeshAgent.isOnNavMesh && !NavMeshAgent.pathPending && NavMeshAgent.remainingDistance <= NavMeshAgent.stoppingDistance &&
               (!NavMeshAgent.hasPath || NavMeshAgent.velocity.sqrMagnitude == 0f);
    }

    public void StopMovement()
    {
        NavMeshAgent.isStopped = true;
    }

    // $$$ To be updated for AI implementation $$$

    // Moving Task
    public void AddTaskGoTo(Vector3 pos)
    {
        m_taskRunner.AddNewTask(new GoTo(this, new Vector2(pos.x, pos.z)));
    }

    // Targetting Task - attack
    public void AddTaskAttackTarget(BaseEntity target)
    {
        if (target.GetTeam() != GetTeam())
        {
            m_taskRunner.AddNewTask(new Attack(this, target));
        }
    }

    // Targetting Task - capture
    public void AddTaskCaptureTarget(TargetBuilding target)
    {
        if (target.GetTeam() != GetTeam())
        {
            m_taskRunner.AddNewTask(new CaptureTarget(this, target));
        }
    }

    // Targetting Task - repairing
    public void AddTaskRepairTarget(BaseEntity target)
    {
        if (target.GetTeam() == GetTeam())
        {
            m_taskRunner.AddNewTask(new Repair(this, target));
        }
    }
    
    
    // Moving Task
    public void SetTaskGoTo(Vector3 pos)
    {
        m_taskRunner.AssignNewTask(new GoTo(this, new Vector2(pos.x, pos.z)));
    }

    // Targetting Task - attack
    public void SetTaskAttackTarget(BaseEntity target)
    {
        if (target.GetTeam() != GetTeam())
        {
            m_taskRunner.AssignNewTask(new Attack(this, target));
        }
    }

    // Targetting Task - capture
    public void SetTaskCaptureTarget(TargetBuilding target)
    {
        if (target.GetTeam() != GetTeam())
        {
            m_taskRunner.AssignNewTask(new CaptureTarget(this, target));
        }
    }

    // Targetting Task - repairing
    public void SetTaskRepairTarget(BaseEntity target)
    {
        if (target.GetTeam() == GetTeam())
        {
            m_taskRunner.AssignNewTask(new Repair(this, target));
        }
    }

    #endregion
}