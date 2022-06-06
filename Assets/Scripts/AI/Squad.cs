using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InfluenceMapPackage;
using UnityEngine;

public class Squad : IInfluencer
{
    public HashSet<Unit> Units { get; private set; } = new HashSet<Unit>();
    public List<Unit> UnitList => Units.ToList();
    TargetBuilding targetCapturePoint;
    public Formation formation;

    PointOfInterest _pointOfInterest;

    public Action<Squad> OnSquadEmpty;

    private int previousFramePosUpdated;
    private Vector2 currentPosition;

    public PointOfInterest PointOfInterest
    {
        get => _pointOfInterest;
        set
        {
            if (_pointOfInterest != value)
            {
                // remove squad from previous Tactic
                if (_pointOfInterest != null)
                    _pointOfInterest.RemoveSquad(this);

                _pointOfInterest = value;

                // add squad to new tactic
                if (_pointOfInterest != null)
                    _pointOfInterest.AddSquad(this);
            }
        }
    }

    public void AddUnit(Unit unit)
    {
        Units.Add(unit);
        unit.OnDeadEvent += unitToRemove => RemoveUnit(unitToRemove as Unit);
    }

    public void RemoveUnit(Unit unit)
    {
        Units.Remove(unit);
        unit.OnDeadEvent -= unitToRemove => RemoveUnit(unitToRemove as Unit);
        
        if (Units.Count == 0)
            OnSquadEmpty?.Invoke(this);
    }

    public bool IsEmpty => Units.Count == 0;

    public Squad()
    {
    }

    public Squad(Unit squadUnit)
    {
        Units = new HashSet<Unit>();
        AddUnit(squadUnit);
    }

    public Squad(HashSet<Unit> squadUnits)
    {
        Units = new HashSet<Unit>();
        
        foreach (Unit unit in squadUnits)
        {
            AddUnit(unit);
            unit.OnDeadEvent += unitToRemove => RemoveUnit(unitToRemove as Unit);
        }
    }

    public void NormalizeSquadSpeed()
    {
        float maxSpeed = GetSquadSpeed();
        foreach (var unit in Units)
        {
            unit.SetSpeed(maxSpeed);
        }
    }

    public Squad(List<Unit> squadUnits)
    {
        Units = new HashSet<Unit>();

        foreach (var unit in squadUnits)
        {
            AddUnit(unit);
        } 
    }

    /// <summary>
    /// Get the average position of the units of the squad
    /// </summary>
    public Vector2 GetAveragePosition()
    {
        if (previousFramePosUpdated == Time.frameCount)
            return currentPosition;

        previousFramePosUpdated = Time.frameCount;
      
        Vector2 averagePosition = Vector2.zero;
        foreach (Unit unit in Units)
        {
            averagePosition += unit.GetInfluencePosition();
        }

        currentPosition = averagePosition / Units.Count;
        return currentPosition;
    }

    public Vector2 GetUnormalizedDirection()
    {
        Vector2 groupDir = Vector2.zero;
        foreach (Unit unit in Units)
        {
            groupDir += unit.GetDirection();
        }
        return groupDir;
    }

    public float GetStrength()
    {
        float strength = 0;
        foreach (Unit unit in Units)
        {
            strength += unit.GetStrength();
        }

        return strength;
    }
    
    public float GetSqrInfluenceRadius()
    {
        float sqrInfluenceRadius = 0;
        Vector2 squadPos = GetAveragePosition();
        
        foreach (Unit unit in Units)
        {
            float unitInflRad = unit.GetInfluenceRadius();
            sqrInfluenceRadius = Mathf.Max(sqrInfluenceRadius,
                (squadPos - unit.GetInfluencePosition()).sqrMagnitude + unitInflRad * unitInflRad);
        }

        return sqrInfluenceRadius;
    }
    
    public Vector2 GetInfluencePosition()
    {
        return GetAveragePosition();
    }

    public float GetInfluenceRadius()
    {
        return GetSqrInfluenceRadius();
    }

    // Modifiy in place
    /// <summary>
    /// Get the maximal squad speed, to adjust speed according to the slowest unit in the squad
    /// </summary>
    public float GetSquadSpeed()
    {
        if (IsEmpty) return 0;
        return Units.Select(unit => unit.GetUnitData.Speed).Min();
    }

    /// <summary>
    /// Merge the squads together
    /// </summary>
    /// <param name="other">The other squad to merge with</param>
    public void Merge(Squad other)
    {
        Units.UnionWith(other.Units);
        other.Units.Clear();
    }

    /// <summary>
    /// Merge the squads according to a unit-wise predicate
    /// </summary>
    /// <param name="other">The squad to merge into this</param>
    /// <param name="predicate">If the unit from `other` satisfies the predicate, add it to `this` squad</param>
    /// <returns>The squad containing units from `other` that did not satisfy the `predicate`</returns>
    public void MergeIf(Squad other, Func<Squad, Unit, bool> predicate)
    {
        foreach (var unit in other.Units)
        {
            if (predicate(this, unit) && !Units.Contains(unit))
            {
                AddUnit(unit);
            }
        }

        other.Units.ExceptWith(Units);
    }

    /// <summary>
    /// Split the squad according to a specific set of unit
    /// </summary>
    /// <param name="unitsToRemove">Set of unit to remove from the squad</param>
    /// <returns>The removed units</returns>
    /// <remarks>
    /// The returned squad might not contain all `unitsToRemove` if some of them weren't in the original squad
    /// </remarks>
    public Squad Split(HashSet<Unit> unitsToRemove)
    {
        unitsToRemove.UnionWith(Units);
        Units.ExceptWith(unitsToRemove);
        return new Squad(unitsToRemove); // TODO : Register new squad ?
    }

    public Squad Split(int nbUnitsInNewSquad)
    {
        List<Unit> units = new List<Unit>(Units.Take(1));
        return new Squad(units); // TODO : Register new squad ?
    }

    /// <summary>
    /// Split the squad according to a ratio
    /// </summary>
    /// <param name="ratio">Percentage of unit to go to the new squad (floored)</param>
    /// <returns>The new squads composed of `ratio` units at maximum</returns>
    public Squad SplitRatio(float ratio)
    {
        int i = 0;
        int count = Mathf.FloorToInt(Units.Count * ratio);
        return SplitIf((squad, unit) => (i++ < count));
    }

    /// <summary>
    /// Split the squad according to a unit-wise predicate
    /// </summary>
    /// <param name="predicate">
    /// If the unit from `this` squad satisfies the predicate, remove it from `this` squad and add it to the returned squad
    /// </param>
    /// <returns>The squad containing units from `this` squad that did satisfy the `predicate`</returns>
    public Squad SplitIf(Func<Squad, Unit, bool> predicate)
    {
        HashSet<Unit> newUnits = new HashSet<Unit>();

        foreach (var unit in Units)
        {
            if (predicate(this, unit))
                newUnits.Add(unit);
        }

        Units.ExceptWith(newUnits);

        return new Squad(newUnits);
    }

    /// <summary>
    /// Merge a list of squads according to a unit-wise predicate
    /// </summary>
    /// <param name="squads">List of squads to merge</param>
    /// <param name="predicate">
    /// If the unit from `this` squad satisfies the predicate, add it to `this` squad and remove it from the other squad
    /// </param>
    /// <returns>A list containing all the merged squads</returns>
    public static List<Squad> MergeSquadsIf(List<Squad> squads, Func<Squad, Unit, bool> predicate)
    {
        for (int i = 0; i < squads.Count; i++)
        {
            for (int j = 0; j < squads.Count; j++)
            {
                if (i == j) continue;
                squads[i].MergeIf(squads[j], predicate);
            }
        }

        List<Squad> newSquadList = new List<Squad>();
        for (int i = 0; i < squads.Count; i++)
        {
            if (!squads[i].IsEmpty)
                newSquadList.Add(squads[i]);
        }

        return newSquadList;
    }

    /// <summary>
    /// Split a list of squads according to a unit-wise predicate
    /// </summary>
    /// <param name="squads">List of squads to split</param>
    /// <param name="predicate">
    /// If the unit from `this` squad satisfies the predicate, remove it from `this` squad and add it to the new squad
    /// </param>
    /// <returns>A list containing all the pairs of original and split squads</returns>
    public static List<Squad> SplitSquadsIf(List<Squad> squads, Func<Squad, Unit, bool> predicate)
    {
        List<Squad> newSquadList = new List<Squad>();
        for (int i = 0; i < squads.Count; i++)
        {
            Squad splitSquad = squads[i].SplitIf(predicate);
            if (!splitSquad.IsEmpty)
                newSquadList.Add(splitSquad);
        }

        for (int i = 0; i < squads.Count; i++)
        {
            if (!squads[i].IsEmpty)
                newSquadList.Add(squads[i]);
        }

        return newSquadList;
    }
    
    // TODO: Move this to a squad manager
    /// <summary>
    /// Merge the squads together if they are close enough
    /// </summary>
    /// <param name="squads">List of squads to be merged</param>
    /// <param name="dist">Maximum distance for the squads to be merged</param>
    public static void FuseSquadsDependingOnDistance(List<Squad> squads, float dist = 50)
    {
        for (int i = 0; i < squads.Count; i++)
        {
            for (int j = 0; j < squads.Count; j++)
            {
                if (i == j) continue;

                squads[i].MergeIf(squads[j], (squad, unit) =>
                    (squad.GetAveragePosition() - unit.GetInfluencePosition()).sqrMagnitude < dist * dist);
            }
        }

        for (int i = 0; i < squads.Count; i++)
        {
            if (squads[i].IsEmpty)
            {
                squads.RemoveAt(i);
                i--;
            }
        }
    }

    /// <summary>
    /// Split all the units between squads depending on their positions
    /// </summary>
    /// <param name="units">Units to share between squads</param>
    /// <param name="dist">Maximum distance for the squads to be merged</param>
    /// <returns>The list of merged squads</returns>
    public static List<Squad> MakeSquadsDependingOnDistance(IEnumerable<Unit> units, float dist = 50)
    {
        List<Squad> squads = new List<Squad>();
        foreach (Unit unit in units)
        {
            Squad squad = new Squad(new HashSet<Unit> {unit});
            squads.Add(squad);
        }

        FuseSquadsDependingOnDistance(squads, dist);
        return squads;
    }

    public void Stop()
    {
        foreach (Unit unit in Units)
        {
            unit.Stop();
        }

        PointOfInterest = null;
    }

    public void GoCaptureTarget(TargetBuilding targetCapturePoint)
    {
        foreach (Unit unit in Units)
        {
            unit.SetTaskGoTo(targetCapturePoint.transform.position, unit.UnitData.CaptureDistanceMax);
            unit.AddTaskCaptureTarget(targetCapturePoint);
        }
    }

    public void AttackTarget(BaseEntity attackedEntity)
    {
        foreach (Unit unit in Units)
        {
            unit.SetTaskGoTo(attackedEntity.transform.position, unit.UnitData.AttackDistanceMax);
            unit.AddTaskAttackTarget(attackedEntity);
        }
    }

    public void RepairTarget(BaseEntity repairedEntity)
    {
        foreach (Unit unit in Units)
        {
            unit.SetTaskGoTo(repairedEntity.transform.position, unit.UnitData.RepairDistanceMax);
            unit.AddTaskRepairTarget(repairedEntity);
        }
    }

    public void Goto(Vector3 target, float stoppingDistance = 1f)
    {
        CheckAndUpdateFormation(target);

        Dictionary<Unit, Vector3> formationTargetPositions = formation.GetUnitsPosition(target);

        foreach (Unit unit in Units)
        {
            unit.SetTaskGoTo(formationTargetPositions[unit], stoppingDistance);
        }
    }
    
    public void Regroup(Vector3 target)
    {
        CheckAndUpdateFormation(target);

        Vector3 averagePosition3D = GameUtility.ToVec3(GetAveragePosition());
        Dictionary<Unit, Vector3> formationAveragePosition = formation.GetUnitsPosition(averagePosition3D);

        foreach (Unit unit in Units)
        {
            unit.SetTaskGoTo(formationAveragePosition[unit]);
        }
    }

    private void CheckAndUpdateFormation(Vector3 target)
    {
        NormalizeSquadSpeed();

        if (formation == null)
        {
            formation = new CircleFormation(this);
            formation.Scale = 2;
        }

        Vector3 position = GameUtility.ToVec3(GetAveragePosition());

        formation.Position = target;
        formation.Scale = Mathf.Log(Units.Count) + 1;

        Vector3 diff = formation.GetFormationCenterPosition() - position;
        formation.Rotation = Quaternion.LookRotation(-diff).eulerAngles.y;
    }

    public bool IsPartiallyIdle
    {
        get
        {
            bool rst = false;

            foreach (Unit unit in Units)
            {
                rst |= unit.IsIdle;
            }
    
            return rst;
        }
    }

    public bool IsIdle
    {
        get
        {
            bool rst = true;
            
            foreach (Unit unit in Units)
            {
                rst &= unit.IsIdle;
            }

            return rst;
        }
    }

    public ETeam GetTeam()
    {
        return Units.First().GetTeam();
    }

    private Vector3 ComputeActionRangeOffset(Vector3 StartPos, Vector3 TargetPos, float maxRange)
    {
        Vector3 targetDir = TargetPos - StartPos;
        return targetDir.normalized * maxRange;
    }

    // private float GetSquadAttackRange()
    // {
    //     Units.Select(unit => unit.da)
    // }
}