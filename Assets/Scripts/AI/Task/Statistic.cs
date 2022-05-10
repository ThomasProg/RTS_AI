using System.Collections;
using System.Collections.Generic;
using InfluenceMapPackage;
using UnityEngine;

public static class Statistic
{
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

    public struct PointOfInterestEvaluation
    {
        public Vector2 position;
        public float sqrtDistanceFromGroup;
        public float strength;
    }
    
    public static List<PointOfInterestEvaluation> EvaluatePointOfInterest(Vector2 groupPosition, ETeam team)
    {
        UnitController controller = GameServices.GetControllerByTeam(team);
        Unit[] units = controller.Units;
        Factory[] factories = controller.Factories;
        // TODO: Target buidling

        List<PointOfInterestEvaluation> pointOfInterestEvaluations = new List<PointOfInterestEvaluation>();
        
        // Iterate on all building and evaluate unit around depending on distance from group.
        foreach (Factory factory in factories)
        {
            PointOfInterestEvaluation pointOfInterestEvaluation = new PointOfInterestEvaluation();

            pointOfInterestEvaluation.position = factory.GetInfluencePosition();
            
            float sqrDistGroupFactory = (factory.GetInfluencePosition() - groupPosition).sqrMagnitude;
            pointOfInterestEvaluation.sqrtDistanceFromGroup = sqrDistGroupFactory;
            
            // Get units in radius factory/group radius
            float strength = 0;
            foreach (Unit unit in units)
            {
                float sqrDistUnityFactory = (factory.GetInfluencePosition() - unit.GetInfluencePosition()).sqrMagnitude;
                strength += (sqrDistUnityFactory < sqrDistGroupFactory) ? unit.Cost : 0;
            }

            pointOfInterestEvaluation.strength = strength;
            pointOfInterestEvaluations.Add(pointOfInterestEvaluation);
        }

        // Iterate on groups
        // TODO: get group and do same things 
        
        return pointOfInterestEvaluations;
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

        public float GetStrategyEffectivity()
        {
            return allyStrength / (Mathf.Max(enemyStrength, 1) * Mathf.Sqrt(sqrtDistanceFromSquad));
        }
    }
    
    public struct EnemySquadObjectiveEvaluation
    {
        public Squad current;
        public List<SquadObjective> objectives;
    }
    
    public static List<EnemySquadObjectiveEvaluation> EvaluateEnemySquadObjective(ETeam enemyTeam, float groupDistance, float radiusErrorCoef)
    {
        UnitController controllerCurrent = GameServices.GetControllerByTeam(enemyTeam);
        UnitController controllerEnemy = GameServices.GetControllerByTeam(enemyTeam == ETeam.Blue ? ETeam.Red : ETeam.Blue);
        
        List<Squad> squadsCurrent = Squad.MakeSquadsDependingOnDistance(controllerCurrent.Units, groupDistance);
        List<Squad> squadsEnemy = Squad.MakeSquadsDependingOnDistance(controllerEnemy.Units, groupDistance);
        
        TargetBuilding[] targetBuildings = GameServices.GetTargetBuildings();
        
        Factory[] factoriesCurrent = controllerCurrent.Factories;
        Factory[] factoriesEnemy = controllerEnemy.Factories;

        List<EnemySquadObjectiveEvaluation> squadsObjective = new List<EnemySquadObjectiveEvaluation>();
        
        // Need to compare squad distance with enemy squad, building and target building. 
        foreach (Squad squad in squadsCurrent)
        {
            EnemySquadObjectiveEvaluation squadObjective = new EnemySquadObjectiveEvaluation();
            squadObjective.objectives = new List<SquadObjective>();
            squadObjective.current = squad;
            
            float squadSqrInfluenceRadius = squad.GetSqrInfluenceRadius();
            Vector2 squadPos = squad.GetAveragePosition();

            ProcessObjective(radiusErrorCoef, targetBuildings, squadPos, squadSqrInfluenceRadius, squadsCurrent, squadsEnemy, ref squadObjective, EObjectiveType.CaptureTargetBuilding);
            ProcessObjective(radiusErrorCoef, factoriesCurrent, squadPos, squadSqrInfluenceRadius, squadsCurrent, squadsEnemy, ref squadObjective, EObjectiveType.ProtectFactory);
            ProcessObjective(radiusErrorCoef, factoriesEnemy, squadPos, squadSqrInfluenceRadius, squadsCurrent, squadsEnemy, ref squadObjective, EObjectiveType.AttackFactory);
            ProcessObjective(radiusErrorCoef, squadsEnemy, squadPos, squadSqrInfluenceRadius, squadsCurrent, squadsEnemy, ref squadObjective, EObjectiveType.AttackSquad);
            
            squadsObjective.Add(squadObjective);
        }

        return squadsObjective;
    }

    private static void ProcessObjective<T>(float radiusErrorCoef, IEnumerable<T> influencers, Vector2 squadPos,
        float squadSqrInfluenceRadius, List<Squad> squadsCurrent, List<Squad> squadsEnemy, ref EnemySquadObjectiveEvaluation squadObjective, EObjectiveType type) where T : IInfluencer
    {
        foreach (T influencer in influencers)
        {
            float squadCurrentDefendingPoint = 0f;
            float squadEnemyAttackingPoint = 0f;
            float sqrDistSquadTarget =
                (influencer.GetInfluencePosition() - squadPos).sqrMagnitude + squadSqrInfluenceRadius;
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

            squadObjective.objectives.Add(objective);
        }
    }
}
