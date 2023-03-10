using System.Collections.Generic;
using FogOfWarPackage;
using InfluenceMapPackage;
using UnityEngine;
using UnityEngine.UI;

public class TargetBuilding : MonoBehaviour, IInfluencer, IFogOfWarEntity
{
    [SerializeField]
    float CaptureGaugeStart = 100f;
    [SerializeField]
    float CaptureGaugeSpeed = 1f;
    [SerializeField]
    int BuildPoints = 5;
    [SerializeField]
    Material BlueTeamMaterial = null;
    [SerializeField]
    Material RedTeamMaterial = null;

    MeshRenderer[] BuildingMeshRenderers = null;
    Image GaugeImage;
    int[] TeamScore;
    float CaptureGaugeValue;
    ETeam OwningTeam = ETeam.Neutral;
    ETeam CapturingTeam = ETeam.Neutral;
    public ETeam GetTeam() { return OwningTeam; }

    List<Unit> capturingUnits = new List<Unit>();

    #region MonoBehaviour methods
    void Start()
    {
        BuildingMeshRenderers = GetComponentsInChildren<MeshRenderer>();
        
        GaugeImage = GetComponentInChildren<Image>();
        if (GaugeImage)
            GaugeImage.fillAmount = 0f;
        CaptureGaugeValue = CaptureGaugeStart;
        TeamScore = new int[2];
        TeamScore[0] = 0;
        TeamScore[1] = 0;
    }
    void Update()
    {
        if (CapturingTeam == OwningTeam || CapturingTeam == ETeam.Neutral)
            return;

        if (CaptureGaugeSpeed == 0)
            ResetCapture();
        
        CaptureGaugeValue -= TeamScore[(int)CapturingTeam] * CaptureGaugeSpeed * Time.deltaTime;

        GaugeImage.fillAmount = 1f - CaptureGaugeValue / CaptureGaugeStart;

        if (CaptureGaugeValue <= 0f)
        {
            CaptureGaugeValue = 0f;
            OnCaptured(CapturingTeam);
        }
    }
    #endregion

    #region Capture methods
    public void StartCapture(Unit unit)
    {
        if (unit == null)
            return;

        capturingUnits.Add(unit);
        TeamScore[(int)unit.GetTeam()] += unit.Cost;

        if (CapturingTeam == ETeam.Neutral)
        {
            if (TeamScore[(int)GameServices.GetOpponent(unit.GetTeam())] == 0)
            {
                CapturingTeam = unit.GetTeam();
                GaugeImage.color = GameServices.GetTeamColor(CapturingTeam);
            }
        }
        else
        {
            if (TeamScore[(int)GameServices.GetOpponent(unit.GetTeam())] > 0)
                ResetCapture();
        }
    }
    public void StopCapture(Unit unit)
    {
        if (unit == null)
            return;

        capturingUnits.Remove(unit);
        TeamScore[(int)unit.GetTeam()] -= unit.Cost;
        if (TeamScore[(int)unit.GetTeam()] == 0)
        {
            ETeam opponentTeam = GameServices.GetOpponent(unit.GetTeam());
            if (TeamScore[(int)opponentTeam] == 0)
            {
                ResetCapture();
            }
            else
            {
                CapturingTeam = opponentTeam;
                GaugeImage.color = GameServices.GetTeamColor(CapturingTeam);
            }
        }
        else if (TeamScore[(int)unit.GetTeam()] < 0)
            TeamScore[(int)unit.GetTeam()] = 0; // TeamScore should NEVER be less than 0, but sometimes, a bug happens where it is, which prevents capturing the point
    }
    void ResetCapture()
    {
        CaptureGaugeValue = CaptureGaugeStart;
        CapturingTeam = ETeam.Neutral;
        GaugeImage.fillAmount = 0f;
    }
    void OnCaptured(ETeam newTeam)
    {
        Debug.Log("target captured by " + newTeam.ToString());
        if (OwningTeam != newTeam)
        {
            UnitController teamController = GameServices.GetControllerByTeam(newTeam);
            if (teamController != null)
                teamController.CaptureTarget(BuildPoints);

            if (OwningTeam != ETeam.Neutral)
            {
                GameServices.GetGameServices().UnregisterUnit(OwningTeam, this);

                // remove points to previously owning team
                teamController = GameServices.GetControllerByTeam(OwningTeam);
                if (teamController != null)
                    teamController.LoseTarget(BuildPoints);
            }
            GameServices.GetGameServices().RegisterUnit(newTeam, this);
        }
        
        ResetCapture();
        OwningTeam = newTeam;
        
        for (int i = 0; i < BuildingMeshRenderers.Length; i++)
        {
            BuildingMeshRenderers[i].material = newTeam == ETeam.Blue ? BlueTeamMaterial : RedTeamMaterial;

        }
    }
    #endregion

    public virtual Vector2 GetInfluencePosition()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }

    public virtual float GetInfluenceRadius()
    {
        return 6f;
    }

    public void OnDrawGizmos()
    {
#if UNITY_EDITOR
        GameServices gameServices = GameServices.GetGameServices();
        if (gameServices != null && gameServices.debug.targetAnalysis.drawTargetStatistic)
        {
            Gizmos.color = Color.red;
            // avoid z fighting with y up
            Gizmos.DrawWireSphere(transform.position + Vector3.up, gameServices.debug.targetAnalysis.targetStatisticRadius);
        }
#endif
    }

    public Vector2 GetVisibilityPosition()
    {
        return GetInfluencePosition();
    }

    public float GetVisibilityRadius()
    {
        return 40f;
    }

    public float GetPermanentVisibilityRadius()
    {
        return GetVisibilityRadius() * 1.5f;
    }
}
