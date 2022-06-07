using System;
using System.Collections.Generic;
using System.Linq;
using FogOfWarPackage;
using InfluenceMapPackage;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UtilitySystemPackage;
using Random = System.Random;

public enum ETeam
{
    Blue = 0,
    Red = 1,
    //Green,

    Neutral = 2,
    TeamCount = 3
}

[RequireComponent(typeof(GameState))]
public class GameServices : MonoBehaviour
{
#if UNITY_EDITOR
    [System.Serializable]
    public struct TargetAnalysisDebug
    {
        public bool drawTargetStatistic;
        public float targetStatisticRadius;
    }

    [System.Serializable]
    public struct BarycenterDebug
    {
        [HideInInspector] public bool prevDrawBarycenters;
        [HideInInspector] public GameObject globalBarycenterInstance;
        [HideInInspector] public GameObject redBarycenterInstance;
        [HideInInspector] public GameObject blueBarycenterInstance;

        public bool drawBarycenters;
        public GameObject globalBarycenterPrefab;
        public GameObject redBarycenterPrefab;
        public GameObject blueBarycenterPrefab;
    }

    [System.Serializable]
    public struct AISquadDecisionPrevision
    {
        public bool displayStatistic;
        public bool display3MainObjectif;
    }

    [System.Serializable]
    public struct AISquadDebug
    {
        public bool displayAISquad;
        public bool useDifferentColors;
    }

    [System.Serializable]
    public struct TimeScaleDebugger
    {
        public bool displayTimeScale;
    }

    [System.Serializable]
    public struct POIDebugger
    {
        public bool displayPriorities;
        public bool displayStrengthRequired;
    }

    [Serializable]
    public struct UtilitySystemDebug
    {
        public bool displayUtilitySystem;
    }
    
    [Serializable]
    public struct FogOfWarDebug
    {
        [HideInInspector] public bool fowPreviousSetting;
        [HideInInspector] public bool debugButtonPrevious;
        public bool useFogOfWar;
    }

    [Serializable]
    public struct ResourcePointDebug
    {
        public bool displayAIDebug;
        public bool displayPlayerDebug;
    }

    [System.Serializable]
    public struct DebugSettings
    {
        public TargetAnalysisDebug targetAnalysis;
        public BarycenterDebug barycenter;
        public AISquadDecisionPrevision aiSquadDecisionPrevision;
        public POIDebugger poiDebugger;
        public TimeScaleDebugger timeScaleDebugger;
        public AISquadDebug aiSquadDebug;
        public UtilitySystemDebug utilitySystemDebug;
        public ResourcePointDebug ResourcePointDebug;
        public FogOfWarDebug fogOfWarDebug;
    }

#endif
    
    [SerializeField, Tooltip("Unplayable terrain border size")]
    float NonPlayableBorder = 100f;

    [SerializeField, Tooltip("Playable bounds size if no terrain is found")]
    float DefaultPlayableBoundsSize = 100f;

    static GameServices Instance = null;

    UnitController[] ControllersArray;
    PlayerController playerController;
    TargetBuilding[] TargetBuildingArray;
    GameState CurrentGameState = null;

    private TerrainInfluenceMap[] m_teamInfluenceMap = new TerrainInfluenceMap[(int) ETeam.TeamCount];
    private TerrainFogOfWar m_teamPlayerFogOfWar;
    private Dictionary<BaseEntity, Tuple<Renderer[], Canvas>>[] m_teamsUnitsRenderer = new Dictionary<BaseEntity, Tuple<Renderer[], Canvas>>[(int) ETeam.TeamCount];

    [SerializeField] ForwardRendererData m_rendererData;
    private PostProcessFogOfWarFeature m_fowFeature;
    

    Terrain CurrentTerrain = null;
    Bounds PlayableBounds;

#if UNITY_EDITOR
    public DebugSettings debug;
#endif

    #region Static methods

    public static AIController GetAIController()
    {
        return Instance.ControllersArray.OfType<AIController>().First();
    }

    public static PlayerController GetPlayerController()
    {
        return Instance.ControllersArray.OfType<PlayerController>().First();
    }

    public static GameServices GetGameServices()
    {
        return Instance;
    }

    public static GameState GetGameState()
    {
        return Instance.CurrentGameState;
    }

    public static UnitController GetControllerByTeam(ETeam team)
    {
        if (Instance.ControllersArray.Length < (int) team)
            return null;
        return Instance.ControllersArray[(int) team];
    }

    public static ETeam GetOpponent(ETeam team)
    {
        return Instance.CurrentGameState.GetOpponent(team);
    }

    public TerrainInfluenceMap GetInfluenceMap(ETeam team)
    {
        return m_teamInfluenceMap[(int) team];
    }

    public static TargetBuilding[] GetTargetBuildings()
    {
        return Instance.TargetBuildingArray;
    }

    // return RGB color struct for each team
    public static Color GetTeamColor(ETeam team)
    {
        switch (team)
        {
            case ETeam.Blue:
                return Color.blue;
            case ETeam.Red:
                return Color.red;
            //case Team.Green:
            //    return Color.green;
            default:
                return Color.grey;
        }
    }

    public static float GetNonPlayableBorder
    {
        get { return Instance.NonPlayableBorder; }
    }

    public static Terrain GetTerrain
    {
        get { return Instance.CurrentTerrain; }
    }

    public static Bounds GetPlayableBounds()
    {
        return Instance.PlayableBounds;
    }

    public static Vector3 GetTerrainSize()
    {
        return Instance.TerrainSize;
    }

    public static bool IsPosInPlayableBounds(Vector3 pos)
    {
        if (GetPlayableBounds().Contains(pos))
            return true;

        return false;
    }

    public Vector3 TerrainSize
    {
        get
        {
            if (CurrentTerrain)
                return CurrentTerrain.terrainData.bounds.size;
            return new Vector3(DefaultPlayableBoundsSize, 10.0f, DefaultPlayableBoundsSize);
        }
    }

    #endregion

    public void SwapTeam()
    {
        foreach (UnitController controller in ControllersArray)
        {
            controller.Team = GetOpponent(controller.Team);
        }
    }

    /// <summary>
    /// Need to be called in OnEnable
    /// </summary>
    /// <example>
    ///private void OnEnable()
    ///{
    ///    GameManager.Instance.RegisterUnit(team, this);
    ///}
    /// </example>
    /// <param name="team"></param>
    public void RegisterUnit(ETeam team, BaseEntity entity)
    {
        if (m_teamInfluenceMap[(int) team] != null)
        {
            m_teamInfluenceMap[(int) team].RegisterEntity(entity);
        }
        else
        {
            Debug.LogWarning($"Influence map {(int) team} does not exist");
        }
        
        if (team == GetPlayerController().Team)
        {
            m_teamPlayerFogOfWar.RegisterEntity(entity);
        }
        
        m_teamsUnitsRenderer[(int) team].Add(entity, new Tuple<Renderer[], Canvas>(entity.GetComponentsInChildren<Renderer>(), entity.GetComponentInChildren<Canvas>()));
    }
    
    public void RegisterUnit(ETeam team, TargetBuilding entity)
    {
        if (m_teamInfluenceMap[(int) team] != null)
        {
            m_teamInfluenceMap[(int) team].RegisterEntity(entity);
        }
        else
        {
            Debug.LogWarning($"Influence map {(int) team} does not exist");
        }
        
        if (team == GetPlayerController().Team)
        {
            m_teamPlayerFogOfWar.RegisterEntity(entity);
        }
    }

    /// <summary>
    /// Need to be called in OnDisable
    /// </summary>
    /// <example>
    ///private void OnDisable()
    ///{
    ///    if(gameObject.scene.isLoaded)
    ///        GameManager.Instance.UnregisterUnit(team, this);
    ///}
    /// </example>
    /// <param name="team"></param>
    public void UnregisterUnit(ETeam team, BaseEntity entity)
    {
        m_teamInfluenceMap[(int) team]?.UnregisterEntity(entity);
        
        if (team == GetPlayerController().Team)
        {
            m_teamPlayerFogOfWar?.UnregisterEntity(entity);
        }
        m_teamsUnitsRenderer[(int) team].Remove(entity);
    }
    
    public void UnregisterUnit(ETeam team, TargetBuilding entity)
    {
        m_teamInfluenceMap[(int) team]?.UnregisterEntity(entity);
        
        if (team == GetPlayerController().Team)
        {
            m_teamPlayerFogOfWar?.UnregisterEntity(entity);
        }
    }

    #region MonoBehaviour methods

    void OnEnable()
    {
        Instance = this;

        // Retrieve controllers from scene for each team
        ControllersArray = new UnitController[2];
        foreach (UnitController controller in FindObjectsOfType<UnitController>())
        {
            ControllersArray[(int) controller.GetTeam()] = controller;

            if (controller is PlayerController pController)
                this.playerController = pController;
        }

        // Store TargetBuildings
        TargetBuildingArray = FindObjectsOfType<TargetBuilding>();
        m_teamInfluenceMap = FindObjectsOfType<TerrainInfluenceMap>();
        m_teamPlayerFogOfWar = FindObjectOfType<TerrainFogOfWar>();

        // Store GameState ref
        if (CurrentGameState == null)
            CurrentGameState = GetComponent<GameState>();

        // Assign first found terrain
        foreach (Terrain terrain in FindObjectsOfType<Terrain>())
        {
            CurrentTerrain = terrain;
            //Debug.Log("terrainData " + CurrentTerrain.terrainData.bounds.ToString());
            break;
        }

        if (CurrentTerrain)
        {
            PlayableBounds = CurrentTerrain.terrainData.bounds;
            Vector3 clampedOne = new Vector3(1f, 0f, 1f);
            Vector3
                heightReduction =
                    Vector3.up * 0.1f; // $$ hack : this is to prevent selectioning / building in high areas
            PlayableBounds.SetMinMax(PlayableBounds.min + clampedOne * NonPlayableBorder / 2f,
                PlayableBounds.max - clampedOne * NonPlayableBorder / 2f - heightReduction);
        }
        else
        {
            Debug.LogWarning("could not find terrain asset in scene, setting default PlayableBounds");
            Vector3 clampedOne = new Vector3(1f, 0f, 1f);
            PlayableBounds.SetMinMax(
                new Vector3(-DefaultPlayableBoundsSize, -10.0f, -DefaultPlayableBoundsSize) +
                clampedOne * NonPlayableBorder / 2f,
                new Vector3(DefaultPlayableBoundsSize, 10.0f, DefaultPlayableBoundsSize) -
                clampedOne * NonPlayableBorder / 2f);
        }
        
        m_fowFeature = m_rendererData.rendererFeatures.OfType<PostProcessFogOfWarFeature>().FirstOrDefault();
     
        if (m_fowFeature == null)
            return;
         
        m_fowFeature.settings.terrainFogOfWars = new []{m_teamPlayerFogOfWar};
        
#if UNITY_EDITOR
        m_fowFeature.settings.IsEnabled = debug.fogOfWarDebug.useFogOfWar;
        debug.fogOfWarDebug.useFogOfWar = debug.fogOfWarDebug.debugButtonPrevious;
#endif
        
        m_rendererData.SetDirty();

        m_teamsUnitsRenderer[0] = new Dictionary<BaseEntity, Tuple<Renderer[], Canvas>>();
        m_teamsUnitsRenderer[1] = new Dictionary<BaseEntity, Tuple<Renderer[], Canvas>>();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (debug.fogOfWarDebug.debugButtonPrevious != debug.fogOfWarDebug.useFogOfWar)
        {
            m_fowFeature.settings.IsEnabled = debug.fogOfWarDebug.useFogOfWar;
            m_rendererData.SetDirty();
            debug.fogOfWarDebug.debugButtonPrevious = debug.fogOfWarDebug.useFogOfWar;
        } 
#endif
        if (m_fowFeature.settings.IsEnabled)
            UpdateHiddenObject();
        
#if UNITY_EDITOR
        else if (debug.fogOfWarDebug.fowPreviousSetting != m_fowFeature.settings.IsEnabled)
        {
            ShowAllEntities();
        }
        debug.fogOfWarDebug.fowPreviousSetting = m_fowFeature.settings.IsEnabled;

        
        if (debug.barycenter.prevDrawBarycenters != debug.barycenter.drawBarycenters)
        {
            debug.barycenter.prevDrawBarycenters = debug.barycenter.drawBarycenters;

            if (debug.barycenter.drawBarycenters)
            {
                debug.barycenter.globalBarycenterInstance = Instantiate(debug.barycenter.globalBarycenterPrefab);
                debug.barycenter.blueBarycenterInstance = Instantiate(debug.barycenter.blueBarycenterPrefab);
                debug.barycenter.redBarycenterInstance = Instantiate(debug.barycenter.redBarycenterPrefab);
            }
            else
            {
                Destroy(debug.barycenter.globalBarycenterInstance);
                Destroy(debug.barycenter.blueBarycenterInstance);
                Destroy(debug.barycenter.redBarycenterInstance);
            }
        }

        if (debug.barycenter.drawBarycenters)
        {
            Vector2 globalBarycenter = GameUtility.GetGlobalBarycenter();
            Vector2 blueBarycenter = GameUtility.GetTeamBarycenter(ETeam.Blue);
            Vector2 redBarycenter = GameUtility.GetTeamBarycenter(ETeam.Red);
            debug.barycenter.globalBarycenterInstance.transform.position = new Vector3(globalBarycenter.x,
                debug.barycenter.globalBarycenterInstance.transform.position.y, globalBarycenter.y);
            debug.barycenter.blueBarycenterInstance.transform.position = new Vector3(blueBarycenter.x,
                debug.barycenter.blueBarycenterInstance.transform.position.y, blueBarycenter.y);
            debug.barycenter.redBarycenterInstance.transform.position = new Vector3(redBarycenter.x,
                debug.barycenter.redBarycenterInstance.transform.position.y, redBarycenter.y);
        }
#endif
    }

    void UpdateHiddenObject()
    {
        Color[] colors1 = m_teamPlayerFogOfWar.GetDatas();
        
        foreach (KeyValuePair<BaseEntity, Tuple<Renderer[], Canvas>> unitsRenderers in m_teamsUnitsRenderer[(int) GetAIController().Team])
        {
            Vector3 position = unitsRenderers.Key.transform.position;
            float x = (position.x - m_teamPlayerFogOfWar.Terrain.GetPosition().x) / (float)m_teamPlayerFogOfWar.Terrain
                .terrainData.size.x * (m_teamPlayerFogOfWar.RenderTexture.width - 1);
            float y = (position.z - m_teamPlayerFogOfWar.Terrain.GetPosition().z) / (float)m_teamPlayerFogOfWar.Terrain
                .terrainData.size.z * (m_teamPlayerFogOfWar.RenderTexture.height - 1);

            bool shouldBeDisplay = colors1[((int) x + (int) y * m_teamPlayerFogOfWar.RenderTexture.width)].r > 0.5f;

            foreach (Renderer unitsRenderer in unitsRenderers.Value.Item1)
            {
                unitsRenderer.enabled = shouldBeDisplay;
            }

            if (unitsRenderers.Value.Item2 != null)
                unitsRenderers.Value.Item2.enabled = shouldBeDisplay;
        }
    }
    
    void ShowAllEntities()
    {
        foreach (KeyValuePair<BaseEntity, Tuple<Renderer[], Canvas>> unitsRenderers in m_teamsUnitsRenderer[(int) GetAIController().Team])
        {
            foreach (Renderer unitsRenderer in unitsRenderers.Value.Item1)
            {
                unitsRenderer.enabled = true;
            }

            if (unitsRenderers.Value.Item2 != null)
                unitsRenderers.Value.Item2.enabled = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (Instance != null)
        {
            if (debug.aiSquadDebug.displayAISquad)
            {
                var squads = GetAIController().Squads;
                for (int index = 0; index < squads.Length; index++)
                {
                    Squad squad = squads[index];
                    Handles.color = debug.aiSquadDebug.useDifferentColors
                        ? GameUtility
                            .GetGoldenRatioColorWithIndex(
                                index) /*Color.HSVToRGB(index / (float)squads.Length, 1f, 1f)*/
                        : Color.blue;

                    Vector3 center = GameUtility.ToVec3(squad.GetAveragePosition());
                    float radius = Mathf.Sqrt(squad.GetInfluenceRadius());

                    Handles.DrawWireDisc(center, Vector3.up, radius, 2f);
                }
            }

            if (debug.poiDebugger.displayPriorities)
            {
                foreach (var poi in GetAIController().strategyAI.AllPointOfInterests)
                {
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = Color.yellow;
                    Handles.Label(new Vector3(poi.position.x, 20f, poi.position.y), $"{poi.priority}", style);
                }
            }

            if (debug.poiDebugger.displayStrengthRequired)
            {
                foreach (var poi in GetAIController().strategyAI.AllPointOfInterests)
                {
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = Color.red;
                    Handles.Label(new Vector3(poi.position.x, 20f, poi.position.y + 15f), $"{poi.strengthRequired}",
                        style);
                }
            }

            if (debug.aiSquadDecisionPrevision.display3MainObjectif)
            {
                List<GameUtility.EnemySquadPotentialObjectives> squadsObjective =
                    GameUtility.EvaluateEnemySquadObjective(GetAIController(), GetPlayerController(),
                        GetAIController().PlayerSquads, GetAIController().Squads, 1.1f);

                foreach (GameUtility.EnemySquadPotentialObjectives squadObjective in squadsObjective)
                {
                    squadObjective.objectives.Sort(delegate(GameUtility.SquadObjective objective,
                        GameUtility.SquadObjective objective1)
                    {
                        return objective.GetStrategyEffectivity().CompareTo(objective1.GetStrategyEffectivity());
                    });

                    Vector2 influencePosition = squadObjective.current.GetInfluencePosition();
                    float influenceRadius = Mathf.Sqrt(squadObjective.current.GetInfluenceRadius());

                    float efficiencyTotal = 0f;
                    for (int i = squadObjective.objectives.Count - 1; i >= Mathf.Max(0, squadObjective.objectives.Count - 3); i--)
                    {
                        efficiencyTotal += squadObjective.objectives[i].GetStrategyEffectivity();
                    }

                    // display the 3 main objectives
                    float thickness = 4f;
                    for (int i = squadObjective.objectives.Count - 1; i >= Mathf.Max(0, squadObjective.objectives.Count - 3); i--)
                    {
                        GameUtility.SquadObjective lastObjective = squadObjective.objectives[i];
                        Color color = Color.blue;

                        Vector3 p1 = new Vector3(influencePosition.x, 1f, influencePosition.y);
                        Vector3 p2 = new Vector3(lastObjective.poi.position.x, 1f, lastObjective.poi.position.y);
                        Handles.DrawBezier(p1, p2, p1, p2, color, null, thickness);
                        thickness -= 1;

                        Handles.Label((p2 + p1) / 2f,
                            $"{lastObjective.GetStrategyEffectivity() / efficiencyTotal * 100f}%");

                        Handles.color = Color.blue;
                        Handles.DrawWireDisc(p1, Vector3.up, influenceRadius, 2f);
                    }
                }
            }
        }
    }

    private void OnGUI()
    {
#if UNITY_EDITOR
        if (debug.timeScaleDebugger.displayTimeScale)
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label("Time scale");
            Time.timeScale = GUILayout.HorizontalSlider(Time.timeScale, 0f, 10f, GUILayout.Width(Screen.width / 6f));
            GUILayout.EndHorizontal();
        }

        if (debug.targetAnalysis.drawTargetStatistic)
        {
            GameUtility.TargetBuildingAnalysisData[] targetBuildingAnalysisData =
                GameUtility.GetTargetBuildingAnalysisData(playerController.GetTeam(),
                    debug.targetAnalysis.targetStatisticRadius);

            GUILayout.BeginVertical("box");
            GUILayout.Label("Target building analysis");

            foreach (GameUtility.TargetBuildingAnalysisData buildingAnalysisData in targetBuildingAnalysisData)
            {
                GUILayout.BeginHorizontal("box");

                GUILayout.Label($"{buildingAnalysisData.target.name}:");
                GUILayout.Label(
                    $"Distance from blue team barycenter: {Mathf.Sqrt(buildingAnalysisData.sqrDistanceFromTeamBarycenter)}");
                GUILayout.Label($"Blue occupation: {buildingAnalysisData.balancing.occupationTeam1 * 100f}%");
                GUILayout.Label($"Red occupation: {buildingAnalysisData.balancing.occupationTeam2 * 100f}%");

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }
        else if (debug.aiSquadDecisionPrevision.displayStatistic)
        {
            List<GameUtility.EnemySquadPotentialObjectives> squadsObjective =
                GameUtility.EvaluateEnemySquadObjective(GetAIController(), GetPlayerController(),
                    GetAIController().PlayerSquads, GetAIController().Squads, 1.1f);

            GUILayout.BeginVertical("box");
            GUILayout.Label("AI squad decision prevision");

            int squadID = 0;
            foreach (GameUtility.EnemySquadPotentialObjectives squadObjective in squadsObjective)
            {
                squadObjective.objectives.Sort((delegate(GameUtility.SquadObjective objective,
                    GameUtility.SquadObjective objective1)
                {
                    return objective.GetStrategyEffectivity().CompareTo(objective1.GetStrategyEffectivity());
                }));

                GUILayout.BeginVertical("box");
                GUILayout.Label(
                    $"Squad {squadID} | Units = {squadObjective.current.UnitList.Count} | Strength = {squadObjective.current.GetStrength()}:");

                GUILayout.BeginHorizontal("box");

                GameUtility.SquadObjective lastObjective = squadObjective.objectives.Last();
                GUILayout.Label(
                    $"Main objective: {lastObjective.poi} | Enemy strength {lastObjective.aiStrength} | Efficiency {lastObjective.GetStrategyEffectivity()} | Distance {Mathf.Sqrt(lastObjective.sqrtDistanceFromSquad)}");

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                ++squadID;

                Vector2 influencePosition = squadObjective.current.GetInfluencePosition();
                Debug.DrawLine(new Vector3(influencePosition.x, 10f, influencePosition.y),
                    new Vector3(lastObjective.poi.position.x, 10f, lastObjective.poi.position.y),
                    Color.blue);
            }

            GUILayout.EndVertical();
        }
        else if (debug.utilitySystemDebug.displayUtilitySystem)
        {
            StrategyAI strategyAI = GetAIController().strategyAI;
            GUILayout.BeginVertical();
            DisplayUtilitySystem(strategyAI.objectiveUtilitySystem, "objective");
            DisplayUtilitySystem(strategyAI.subjectiveUtilitySystem, "subjective");
            GUILayout.EndVertical();
        }

        if (debug.ResourcePointDebug.displayAIDebug)
        {
            DrawResourcePointDebug(GetAIController(), "Player");
        }

        if (debug.ResourcePointDebug.displayPlayerDebug)
        {
            DrawResourcePointDebug(GetPlayerController(), "AI");
        }
#endif
    }

    private static void DrawResourcePointDebug(UnitController Controller, string name)
    {
        GUILayout.Label($"{name}'s Resources");
        GUILayout.BeginHorizontal("box");

        int totalUnitCost = 0;
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"Units Cost");
            GUILayout.BeginVertical("box");
            foreach (var unit in Controller.Units)
            {
                totalUnitCost += unit.Cost;
                GUILayout.Label($"{unit.name} : {unit.Cost}");
            }

            GUILayout.EndVertical();

            GUILayout.Label($"Total Unit Cost: {totalUnitCost}");
            GUILayout.EndVertical();
        }

        int totalBuildingCost = 0;
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"Factories Cost");
            GUILayout.BeginVertical("box");

            foreach (Factory factory in Controller.Factories)
            {
                totalBuildingCost += factory.Cost;
                GUILayout.Label($"{factory.name} : {factory.Cost}");
            }

            GUILayout.EndVertical();

            GUILayout.Label($"Total Factory Cost: {totalBuildingCost}");
            GUILayout.EndVertical();
        }

        int PendingCost = 0;
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"Factories Pending Cost");
            GUILayout.BeginVertical("box");

            foreach (Factory factory in Controller.Factories)
            {
                PendingCost += factory.GetPendingCost();
                GUILayout.Label($"{factory.name} : {factory.GetPendingCost()}");
            }

            GUILayout.EndVertical();

            GUILayout.Label($"Total Pending Cost: {PendingCost}");
            GUILayout.EndVertical();
        }

        int totalSpent = totalBuildingCost + totalUnitCost;
        int maximumPoints = Controller.StartingBuildPoints + Controller.CapturedTargets * 5 + 10;
        {
            GUILayout.BeginVertical("box");

            GUILayout.Label($"Maximum Points: {maximumPoints}");
            GUILayout.Label($"Available Points: {Controller.TotalBuildPoints}");
            GUILayout.Label($"Total actually spent : {totalSpent}");
            GUILayout.Label(
                $"Missing points : {maximumPoints - (totalSpent + Controller.TotalBuildPoints + PendingCost)}");

            GUILayout.EndVertical();
        }

        GUILayout.EndHorizontal();
    }

    public void FixMissingMoney()
    {
        AIController Controller = GetAIController();
        int totalUnitCost = 0;
        int totalBuildingCost = 0;
        int PendingCost = 0;

        foreach (var unit in Controller.Units)
        {
            totalUnitCost += unit.Cost;
        }
        
        foreach (Factory factory in Controller.Factories)
        {
            PendingCost += factory.GetPendingCost();
            totalBuildingCost += factory.Cost;
        }
        
        int totalSpent = totalBuildingCost + totalUnitCost;
        int maximumPoints = Controller.StartingBuildPoints + Controller.CapturedTargets * 5 + 10;

        Controller.TotalBuildPoints += maximumPoints - (totalSpent + Controller.TotalBuildPoints + PendingCost);
    }

#if UNITY_EDITOR
    private void DisplayUtilitySystem(UtilitySystem system, string name)
    {
        GUILayout.Label(name);
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical("box");
        foreach (var stat in system.GetInputs().Values)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(stat.Name);
            stat.Value = GUILayout.HorizontalSlider(stat.Value, 0f, 1f, GUILayout.Width(100f));
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();

        GUILayout.BeginVertical("box");
        foreach (var utility in system.GetUtilities().Values)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(utility.Name);
            GUILayout.HorizontalSlider(utility.Value, 0f, 1f, GUILayout.Width(100f));
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }
#endif

    #endregion
}