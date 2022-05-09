using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class Unit : BaseEntity
{
    [SerializeField]
    public UnitDataScriptable UnitData = null;

    public Transform BulletSlot;

    private PullTaskRunner m_taskRunner = new PullTaskRunner();

    public Formation formation;

    NavMeshAgent NavMeshAgent;
    public bool IsIdle => !m_taskRunner.IsRunningTask();
    
    public UnitDataScriptable GetUnitData { get { return UnitData; } }
    public int Cost { get { return UnitData.Cost; } }
    public int GetTypeId { get { return UnitData.TypeId; } }
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
        NavMeshAgent.SetDestination(new Vector3(pos.x, 0f, pos.y));
        NavMeshAgent.isStopped = false;
    }

    public bool IsDestinationReached()
    {
        return !NavMeshAgent.pathPending && NavMeshAgent.remainingDistance <= NavMeshAgent.stoppingDistance &&
               (!NavMeshAgent.hasPath || NavMeshAgent.velocity.sqrMagnitude == 0f);
    }

    public void StopMovement()
    {
        NavMeshAgent.isStopped = true;
    }
    
    // $$$ To be updated for AI implementation $$$

    // Moving Task
    public void SetTargetPos(Vector3 pos)
    {
        m_taskRunner.AssignNewTask(new GoTo(this, new Vector2(pos.x, pos.z)));
    }

    // Targetting Task - attack
    public void SetAttackTarget(BaseEntity target)
    {
        if (target.GetTeam() != GetTeam())
            m_taskRunner.AssignNewTask(new Attack(this, target));
    }

    // Targetting Task - capture
    public void SetCaptureTarget(TargetBuilding target)
    {
        if (target.GetTeam() != GetTeam())
        {
            SetTargetPos(target.transform.position);
            m_taskRunner.AssignNewTask(new CaptureTarget(this, target));
        }
    }

    // Targetting Task - repairing
    public void SetRepairTarget(BaseEntity target)
    {
        if (target.GetTeam() == GetTeam())
            m_taskRunner.AssignNewTask(new Repair(this, target));
    }
    #endregion
}
