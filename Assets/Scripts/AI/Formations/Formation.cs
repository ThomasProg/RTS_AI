using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class Formation
{
    protected List<Vector3> relativePositions = new List<Vector3>();
    protected Squad attachedSquad;

    public Vector3 Position = Vector3.zero;
    public Vector3 formationBarycenter = Vector3.zero;
    public float Rotation = 0f;
    public float Scale = 1f;
    
    protected int unitCount;

    protected Formation(Squad squad)
    {
        relativePositions = new List<Vector3>();
        attachedSquad = squad;
    }
    
    public Vector3 GetFormationCenterPosition()
    {
        return Position;
    }

    public Dictionary<Unit, Vector3> GetUnitsPosition(Vector3 target)
    {
        if (attachedSquad.Units.Count != relativePositions.Count)
        {
            unitCount = attachedSquad.Units.Count;
            UpdateRelativePositions();
        }


        Dictionary<Unit, Vector3> retSet = new Dictionary<Unit, Vector3>();

        int idx = 0;
        foreach (var unit in attachedSquad.Units)
        {
            retSet.Add(unit, target + Quaternion.AngleAxis(Rotation, Vector3.up) * GetScaledRelativePosition(idx));
            idx++;
        }

        return retSet;
    }

    private Vector3 GetScaledRelativePosition(int idx)
    {
        return (relativePositions[idx] - formationBarycenter) * Scale;
    }

    protected abstract void UpdateRelativePositions();
}