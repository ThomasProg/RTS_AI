using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using InfluenceMapPackage;
using UnityEngine;
using UnityEngine.AI;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public static class GameUtility
{
    /// <summary>
    /// Get barycenter of all unity in a specific team
    /// </summary>
    /// <param name="team"></param>
    /// <returns></returns>
    public static Vector2 GetTeamBarycenter(ETeam team)
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

        return posSum / radiusSum;
    }

    /// <summary>
    /// Get the barycenter of all unity in the map (ally or enemy)
    /// </summary>
    /// <returns></returns>
    public static Vector2 GetGlobalBarycenter()
    {
        TerrainInfluenceMap influenceMapBlue = GameServices.GetGameServices().GetInfluenceMap(ETeam.Blue);
        TerrainInfluenceMap influenceMapRed = GameServices.GetGameServices().GetInfluenceMap(ETeam.Red);

        IInfluencer[] blueInfluencers = influenceMapBlue.Influencers;
        IInfluencer[] redInfluencers = influenceMapRed.Influencers;

        if (blueInfluencers.Length == 0 || redInfluencers.Length == 0)
            return Vector3.zero;

        Vector2 posSum = Vector2.zero;
        float radiusSum = 0f;

        foreach (IInfluencer influencer in blueInfluencers)
        {
            posSum += influencer.GetInfluencePosition() * influencer.GetInfluenceRadius();
            radiusSum += influencer.GetInfluenceRadius();
        }

        foreach (IInfluencer influencer in redInfluencers)
        {
            posSum += influencer.GetInfluencePosition() * influencer.GetInfluenceRadius();
            radiusSum += influencer.GetInfluenceRadius();
        }

        return posSum / radiusSum;
    }

    public struct Balancing
    {
        public float localTeam1; // [0, 1] self occupation (team only)
        public float localTeam2; // [0, 1] self occupation (team only)
        public float occupationTeam1; // [0, 1] percentage of occupation
        public float occupationTeam2; // [0, 1] percentage of occupation
    }

    /// <summary>
    /// Get global balancing depending on the influence map
    /// </summary>
    /// <returns></returns>
    static public Balancing GetBalancing()
    {
        Balancing balancing = new Balancing();

        Color[] blueData = GameServices.GetGameServices().GetInfluenceMap(ETeam.Blue).GetDatas();
        Color[] redData = GameServices.GetGameServices().GetInfluenceMap(ETeam.Red).GetDatas();

        int blueDataLength = blueData.Length;
        
        if (blueDataLength != redData.Length)
        {
            Debug.LogError("Influence map with different size");
            return balancing;
        }
        
        for (int i = 0; i < blueDataLength; i++)
        {
            balancing.localTeam1 += blueData[i].r;
            balancing.localTeam2 += redData[i].r;
            
            // Get highest between team 1 and 2
            balancing.occupationTeam1 += (blueData[i].r >= redData[i].r) ? blueData[i].r : 0;
            balancing.occupationTeam2 += (redData[i].r > blueData[i].r) ? redData[i].r : 0;
        }

        balancing.localTeam1 /= blueDataLength;
        balancing.localTeam2 /= blueDataLength;
        balancing.occupationTeam1 /= blueDataLength;
        balancing.occupationTeam2 /= blueDataLength;
        
        return balancing;
    }
    
    /// <summary>
    /// Return the balancing of power in a specific zone depending on the influence map
    /// </summary>
    /// <param name="center"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    static public Balancing GetBalancingInZone(Vector2 center, float radius)
    {
        Balancing balancing = new Balancing();

        Rect wolrdRect = new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
        Color[] blueData = GameServices.GetGameServices().GetInfluenceMap(ETeam.Blue).GetDatasInWorld(wolrdRect);
        Color[] redData = GameServices.GetGameServices().GetInfluenceMap(ETeam.Red).GetDatasInWorld(wolrdRect);

        int blueDataLength = blueData.Length;

        if (blueDataLength != redData.Length)
        {
            Debug.LogError("Influence map with different size");
            return balancing;
        }

        float pixelSize = radius / blueDataLength;

        int pixelProcessed = 0;
        for (int i = 0; i < blueDataLength; i++)
        {
            Vector2 pixelLocalPos = new Vector2(i % radius, i / (int)radius) / pixelSize;
            
            if (pixelLocalPos.SqrMagnitude() > radius * radius)
                continue;

            pixelProcessed++;
            
            balancing.localTeam1 += blueData[i].r;
            balancing.localTeam2 += redData[i].r;
            
            // Get highest between team 1 and 2
            balancing.occupationTeam1 += (blueData[i].r >= redData[i].r) ? blueData[i].r : 0;
            balancing.occupationTeam2 += (redData[i].r > blueData[i].r) ? redData[i].r : 0;
        }

        balancing.localTeam1 /= pixelProcessed;
        balancing.localTeam2 /= pixelProcessed;
        balancing.occupationTeam1 /= pixelProcessed;
        balancing.occupationTeam2 /= pixelProcessed;
        
        return balancing;
    }

    public struct TargetBuildingAnalysisData
    {
        public TargetBuilding target;
        public float sqrDistanceFromTeamBarycenter;
        public Balancing balancing;
    }
    
    /// <summary>
    /// Get the balance of power depending on the influence map arround all targetBuidling
    /// </summary>
    /// <param name="team"></param>
    /// <param name="influenceRadiusAnalysis"></param>
    /// <returns></returns>
    public static TargetBuildingAnalysisData[] GetTargetBuildingAnalysisData(ETeam team, float influenceRadiusAnalysis)
    {
        TargetBuilding[] targetBuildings = GameServices.GetTargetBuildings();
        TargetBuildingAnalysisData[] targetBuildingAnalysisDatas =
            new TargetBuildingAnalysisData[targetBuildings.Length];
        
        Vector2 teamBarycenter = GetTeamBarycenter(team);
        
        for (int i = 0; i < targetBuildings.Length; i++)
        {
            TargetBuilding targ = targetBuildings[i];
            Vector3 position = targ.transform.position;
            Vector2 target2DPos = new Vector2(position.x, position.z);
            
            targetBuildingAnalysisDatas[i].target = targ;
            targetBuildingAnalysisDatas[i].sqrDistanceFromTeamBarycenter = (teamBarycenter - target2DPos).SqrMagnitude();
            targetBuildingAnalysisDatas[i].balancing = GetBalancingInZone(targ.GetInfluencePosition(), influenceRadiusAnalysis);
        }

        return targetBuildingAnalysisDatas;
    }

    public struct BalanceOfPower
    {
        public float playerStrength;
        public float aiStrength;
    }

    /// <summary>
    /// Return the balance of power between squad on ai and player in a specific zone.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    public static BalanceOfPower EvaluateBalanceOfPower(Vector2 position, float radius)
    {
        BalanceOfPower rst = new BalanceOfPower();

        AIController aiController = GameServices.GetAIController();
        rst.playerStrength = EvaluateSquadsStrengthInZone(aiController.PlayerSquads, position, radius);
        rst.aiStrength = EvaluateSquadsStrengthInZone(aiController.Squads, position, radius);

        return rst;
    }
    
    /// <summary>
    /// Return the strength of a squad in a zone.
    /// </summary>
    /// <param name="squads"></param>
    /// <param name="position"></param>
    /// <param name="sqrRadius"></param>
    /// <returns></returns>
    public static float EvaluateSquadsStrengthInZone(IEnumerable squads, Vector2 position, float sqrRadius)
    {
        float strength = 0f;
        foreach (Squad squad in squads)
        {
            if ((squad.GetAveragePosition() - position).sqrMagnitude + squad.GetSqrInfluenceRadius() < sqrRadius)
                strength += squad.GetStrength();
        }

        return strength;
    }

    public struct POITargetByEnemySquad
    {
        public PointOfInterest poi;
        public Squad enemy;
        public float priority; //[0, 1], 0 is very low priority, 1 very high priority
    }
    
    /// <summary>
    /// Get all enemy squad that target a POI. This function return an approximation of all squad that could attack this POI.
    /// </summary>
    /// <param name="poi"></param>
    /// <param name="enemyTeam"></param>
    /// <param name="groupDistance"></param>
    /// <param name="radiusErrorCoef"></param>
    /// <param name="objectifFilterPrirotiy">This value is used to filter priority of the current objectif</param>
    /// <returns></returns>
    public static List<POITargetByEnemySquad> GetPOITargetByEnemySquad(PointOfInterest poi, ETeam enemyTeam, float groupDistance, float radiusErrorCoef, float objectifFilterPrirotiy)
    {
        List<POITargetByEnemySquad> rst = new List<POITargetByEnemySquad>();
        List<EnemySquadPotentialObjectives> enemySquadsObjectives = EvaluateEnemySquadObjective(enemyTeam, groupDistance, radiusErrorCoef);

        foreach (EnemySquadPotentialObjectives enemySquadObjectives in enemySquadsObjectives)
        {
            float efficiencyTotal = 0f;
            foreach (SquadObjective objective in enemySquadObjectives.objectives)
            {
                efficiencyTotal += objective.GetStrategyEffectivity();
            }
            
            foreach (SquadObjective objective in enemySquadObjectives.objectives)
            {
                float priority = objective.GetStrategyEffectivity() / efficiencyTotal;
                if (priority > objectifFilterPrirotiy)
                {
                    rst.Add(new POITargetByEnemySquad
                    {
                        poi = poi,
                        enemy = enemySquadObjectives.current,
                        priority = priority
                    });
                }
            }
        }

        return rst;
    }
    
    public enum EObjectiveType
    {
        CaptureTargetBuilding,
        ProtectFactory,
        AttackFactory,
        AttackSquad
    }
    
    public struct SquadObjective
    {
        public Vector2 position;
        public float sqrtDistanceFromSquad; // including error marge
        public EObjectiveType type;
        
        public float allyStrength;
        public float enemyStrength;
        public float directionWeight; // Dot product between squad dir and objectif pos. This can be used to estimate priority of a squad in movement

        public float GetStrategyEffectivity()
        {
            // Add coefficient to direction to give him more or less importance in the equation
            return allyStrength / (Mathf.Max(enemyStrength, 1) * sqrtDistanceFromSquad) + directionWeight * 0.001f;
        }
    }
    
    public struct EnemySquadPotentialObjectives
    {
        public Squad current;
        public List<SquadObjective> objectives;
    }

    /// <summary>
    /// This function test all enemy to evaluate its squad objectif depending on POI in the map. It's an approximation
    /// </summary>
    /// <param name="enemyTeam"></param>
    /// <param name="groupDistance">The distance used to evaluate squads</param>
    /// <param name="radiusErrorCoef"></param>
    /// <returns></returns>
    public static List<EnemySquadPotentialObjectives> EvaluateEnemySquadObjective(ETeam enemyTeam, float groupDistance, float radiusErrorCoef)
    {
        UnitController controllerCurrent = GameServices.GetControllerByTeam(enemyTeam);
        UnitController controllerEnemy = GameServices.GetControllerByTeam(enemyTeam == ETeam.Blue ? ETeam.Red : ETeam.Blue);
        
        List<Squad> squadsCurrent = Squad.MakeSquadsDependingOnDistance(controllerCurrent.Units, groupDistance);
        List<Squad> squadsEnemy = Squad.MakeSquadsDependingOnDistance(controllerEnemy.Units, groupDistance);
        
        TargetBuilding[] targetBuildings = GameServices.GetTargetBuildings();
        
        Factory[] factoriesCurrent = controllerCurrent.Factories;
        Factory[] factoriesEnemy = controllerEnemy.Factories;

        List<EnemySquadPotentialObjectives> squadsObjective = new List<EnemySquadPotentialObjectives>();
        
        // Need to compare squad distance with enemy squad, building and target building. 
        foreach (Squad squad in squadsCurrent)
        {
            EnemySquadPotentialObjectives squadPotentialObjectives = new EnemySquadPotentialObjectives();
            squadPotentialObjectives.objectives = new List<SquadObjective>();
            squadPotentialObjectives.current = squad;
            
            float squadSqrInfluenceRadius = squad.GetSqrInfluenceRadius();

            ProcessObjective(radiusErrorCoef, targetBuildings, squad, squadSqrInfluenceRadius, squadsCurrent, squadsEnemy, ref squadPotentialObjectives, EObjectiveType.CaptureTargetBuilding);
            ProcessObjective(radiusErrorCoef, factoriesCurrent, squad, squadSqrInfluenceRadius, squadsCurrent, squadsEnemy, ref squadPotentialObjectives, EObjectiveType.ProtectFactory);
            ProcessObjective(radiusErrorCoef, factoriesEnemy, squad, squadSqrInfluenceRadius, squadsCurrent, squadsEnemy, ref squadPotentialObjectives, EObjectiveType.AttackFactory);
            ProcessObjective(radiusErrorCoef, squadsEnemy, squad, squadSqrInfluenceRadius, squadsCurrent, squadsEnemy, ref squadPotentialObjectives, EObjectiveType.AttackSquad);
            
            squadsObjective.Add(squadPotentialObjectives);
        }

        return squadsObjective;
    }

    internal static void ProcessObjective<T>(float radiusErrorCoef, IEnumerable<T> influencers, Squad squad,
        float squadSqrInfluenceRadius, List<Squad> squadsCurrent, List<Squad> squadsEnemy, ref EnemySquadPotentialObjectives squadPotentialObjectives, EObjectiveType type) where T : IInfluencer
    {
        Vector2 squadPos = squad.GetAveragePosition();
        Vector2 squadDir = squad.GetUnormalizedDirection().normalized;

        foreach (T influencer in influencers)
        {
            float squadCurrentDefendingPoint = 0f;
            float squadEnemyAttackingPoint = 0f;
            Vector2 squadToInfluencer = (influencer.GetInfluencePosition() - squadPos);
            float sqrDistSquadTarget =
                squadToInfluencer.sqrMagnitude + squadSqrInfluenceRadius;
            sqrDistSquadTarget *= radiusErrorCoef; // multiply by error coef to scale the radius and anticipate squad movement

            foreach (Squad squadAlly in squadsCurrent)
            {
                float sqrDistAllySquadTarget = (influencer.GetInfluencePosition() - squadAlly.GetInfluencePosition()).sqrMagnitude + squadAlly.GetInfluenceRadius();
                if (sqrDistAllySquadTarget < sqrDistSquadTarget)
                    squadCurrentDefendingPoint += squadAlly.GetStrength();
            }

            foreach (Squad enemySquad in squadsEnemy)
            {
                float sqrDistEnemySquadTarget = (influencer.GetInfluencePosition() - enemySquad.GetInfluencePosition()).sqrMagnitude + enemySquad.GetInfluenceRadius();
                if (sqrDistEnemySquadTarget < sqrDistSquadTarget)
                    squadEnemyAttackingPoint += enemySquad.GetStrength();
            }

            SquadObjective objective = new SquadObjective();
            objective.position = influencer.GetInfluencePosition();
            objective.sqrtDistanceFromSquad = sqrDistSquadTarget;
            objective.type = type;
            objective.allyStrength = squadCurrentDefendingPoint;
            objective.enemyStrength = squadEnemyAttackingPoint;
            objective.directionWeight = Vector2.Dot(squadDir, squadToInfluencer.normalized); // 1 if in direction, 0 if perpendicular -1 if in opposition

            squadPotentialObjectives.objectives.Add(objective);
        }
    }
    
    public static NavMeshPath GetPath(Vector3 fromPos, Vector3 toPos, int passableMask = NavMesh.AllAreas)
    {
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(fromPos, toPos, passableMask, path))
            return path;
       
        return null;
    }
       
    public static float GetPathLength( NavMeshPath path )
    {
        if (path == null)
        {
            Debug.LogWarning("Target is not accessible. Max value returned");
            return float.MaxValue;
        }

        float lng = 0.0f;
       
        if (( path.status != NavMeshPathStatus.PathInvalid ) && ( path.corners.Length > 1 ))
        {
            for ( int i = 1; i < path.corners.Length; ++i )
            {
                lng += Vector3.Distance( path.corners[i-1], path.corners[i] );
            }
        }
       
        return lng;
    }

    public static Vector2 ToVec2(Vector3 vec)
    {
        return new Vector2(vec.x, vec.z);
    }
    
    public static Vector3 ToVec3(Vector2 vec)
    {
        return new Vector3(vec.x, 0f, vec.y);
    }
    
    public static float GetPathLength(Vector2 fromPos, Vector2 toPos, int passableMask = NavMesh.AllAreas)
    {
        return GetPathLength(GetPath(ToVec3(fromPos), ToVec3(toPos), passableMask));
    }

    public static Color GetGoldenRatioColorWithIndex(int index, float s = 0.8f, float v = 0.95f)
    {
        // use golden ratio
        const float golden_ratio_conjugate = 0.381966f;
        return Color.HSVToRGB((index * golden_ratio_conjugate) % 1 , s, v);
    }
}
