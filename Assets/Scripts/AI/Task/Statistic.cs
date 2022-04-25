using System.Collections;
using System.Collections.Generic;
using InfluenceMapPackage;
using UnityEngine;

public static class Statistic
{
    public static Vector3 GetTeamBarycenter(ETeam team)
    {
        TerrainInfluenceMap influenceMap = GameServices.GetGameServices().GetInfluenceMap(team);

        IInfluencer[] influencers = influenceMap.Influencers;
        
        if (influencers.Length == 0)
            return Vector3.zero;
        
        Vector2 posSum = Vector2.zero;
        float radiusSum = 0f;
        
        foreach (IInfluencer influencer in influencers)
        {
            posSum += influencer.GetInfluencePosition() * influencer.GetInfluenceRadius();
            radiusSum += influencer.GetInfluenceRadius();
        }
        
        return  posSum / radiusSum;
    }
    
    struct TargetBuildingAnalysisData
    {
        public TargetBuilding target;
        public float distanceFromPoint;
        public float influence;
    }
    
    static TargetBuildingAnalysisData[] GetTargetBuildingAnalysisData()
    {
        return new TargetBuildingAnalysisData[0];
    }
}
