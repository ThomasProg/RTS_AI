using System;
using InfluenceMapPackage;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseEntity : MonoBehaviour, ISelectable, IDamageable, IRepairable, IInfluencer
{
    [SerializeField]
    protected ETeam Team;

    protected int HP = 0;
    protected Action OnHpUpdated;
    protected GameObject SelectedSprite = null;
    protected Text HPText = null;
    protected bool IsInitialized = false;

    public Action<BaseEntity> OnDeadEvent;
    public bool IsSelected { get; protected set; }
    public bool IsAlive { get; protected set; }
    virtual public void Init(ETeam _team)
    {
        if (IsInitialized)
            return;

        Team = _team;
        GameServices.GetGameServices().RegisterUnit(Team,this);
        
        IsInitialized = true;
    }
    public Color GetColor()
    {
        return GameServices.GetTeamColor(GetTeam());
    }
    void UpdateHpUI()
    {
        if (HPText != null)
            HPText.text = "HP : " + HP.ToString();
    }

    #region ISelectable
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        SelectedSprite?.SetActive(IsSelected);
    }
    public ETeam GetTeam()
    {
        return Team;
    }
    #endregion

    #region IDamageable
    public void AddDamage(int damageAmount)
    {
        if (IsAlive == false)
            return;

        HP -= damageAmount;

        OnHpUpdated?.Invoke();

        if (HP <= 0)
        {
            IsAlive = false;
            OnDeadEvent?.Invoke(this);
            Debug.Log("Entity " + gameObject.name + " died");
        }
    }
    public void Destroy()
    {
        AddDamage(HP);
    }
    #endregion

    #region IRepairable
    virtual public bool NeedsRepairing()
    {
        return true;
    }
    virtual public void Repair(int amount)
    {
        OnHpUpdated?.Invoke();
    }
    virtual public void FullRepair()
    {
    }
    #endregion

    #region MonoBehaviour methods
    virtual protected void Awake()
    {
        IsAlive = true;

        SelectedSprite = transform.Find("SelectedSprite")?.gameObject;
        SelectedSprite?.SetActive(false);

        Transform hpTransform = transform.Find("Canvas/HPText");
        if (hpTransform)
            HPText = hpTransform.GetComponent<Text>();

        OnHpUpdated += UpdateHpUI;
    }
    virtual protected void Start()
    {
        UpdateHpUI();
    }
    
    private void OnDisable()
    {
        GameServices.GetGameServices().UnregisterUnit(Team,this);
    }

    virtual protected void Update()
    {
        if (!IsInitialized)
        {
            GameServices.GetGameServices().RegisterUnit(Team, this);
            IsInitialized = true;
        }
    }
    #endregion

    public virtual Vector2 GetInfluencePosition()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }

    public virtual float GetInfluenceRadius()
    {
        return 10f;
    }
}
