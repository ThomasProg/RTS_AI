using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaptureTarget : Task
{
    private Unit m_unit;
    private TargetBuilding m_target;

    public CaptureTarget(Unit unit, TargetBuilding target)
    {
        m_unit = unit;
        m_target = target;
    }

    public override void OnStart()
    {
        if (CanCapture())
        {
            StartCapture();
        }
        else
        {
            OnEnd();
        }
    }

    public override void OnStop()
    {
        StopCapture();
    }

    public override void OnUpdate()
    {
        if (m_target.GetTeam() == m_unit.GetTeam())
        {
            OnEnd();
        }
    }

    public override void OnEnd()
    {
        m_unit.ProcessNextTask();
    }
    
    bool CanCapture()
    {
        // distance check
        float sqrDist = (m_target.GetInfluencePosition() - m_unit.GetInfluencePosition()).sqrMagnitude;
        return sqrDist <= m_unit.GetUnitData.CaptureDistanceMax * m_unit.GetUnitData.CaptureDistanceMax && m_target.GetTeam() != m_unit.GetTeam();
    }
    
    void StartCapture()
    {
        if (CanCapture() == false)
            return;
        
        m_target.StartCapture(m_unit);
    }
    
    void StopCapture()
    {
        m_target.StopCapture(m_unit);
    }
}