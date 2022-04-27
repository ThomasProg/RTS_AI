using System;
using System.Collections.Generic;
using InfluenceMapPackage;
using UnityEngine;
using UnityEngine.UI;

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
    public struct DebugSettings
    {
        public TargetAnalysisDebug targetAnalysis;
        public BarycenterDebug barycenter;
    }
#endif
    
    [SerializeField, Tooltip("Generic material used for 3D models, in the following order : blue, red and green")]
    Material[] TeamMaterials = new Material[3];

    [SerializeField, Tooltip("Unplayable terrain border size")]
    float NonPlayableBorder = 100f;

    [SerializeField, Tooltip("Playable bounds size if no terrain is found")]
    float DefaultPlayableBoundsSize = 100f;

    static GameServices Instance = null;

    UnitController[] ControllersArray;
    TargetBuilding[] TargetBuildingArray;
    GameState CurrentGameState = null;
    [SerializeField] private TerrainInfluenceMap[] m_teamInfluenceMap = new TerrainInfluenceMap[(int) ETeam.TeamCount];

    Terrain CurrentTerrain = null;
    Bounds PlayableBounds;

#if UNITY_EDITOR
    public DebugSettings debug;
#endif

    #region Static methods
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
        if (Instance.ControllersArray.Length < (int)team)
            return null;
        return Instance.ControllersArray[(int)team];
    }
    public static Material GetTeamMaterial(ETeam team)
    {
        return Instance.TeamMaterials[(int)team];
    }
    public static ETeam GetOpponent(ETeam team)
    {
        return Instance.CurrentGameState.GetOpponent(team);
    }
    
    public TerrainInfluenceMap GetInfluenceMap(ETeam team)
    {
        return m_teamInfluenceMap[(int) team];
    }
    
    public static TargetBuilding[] GetTargetBuildings() { return Instance.TargetBuildingArray; }

    // return RGB color struct for each team
    public static Color GetTeamColor(ETeam team)
    {
        switch(team)
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
    public static float GetNonPlayableBorder { get { return Instance.NonPlayableBorder; } }
    public static Terrain GetTerrain { get { return Instance.CurrentTerrain; } }
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
    public void RegisterUnit(ETeam team, IInfluencer unit)
    {
        m_teamInfluenceMap[(int)team].RegisterEntity(unit);
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
    public void UnregisterUnit(ETeam team, IInfluencer unit)
    {
        m_teamInfluenceMap[(int)team].UnregisterEntity(unit);
    }

    #region MonoBehaviour methods
    void Awake()
    {
        Instance = this;

        // Retrieve controllers from scene for each team
        ControllersArray = new UnitController[2];
        foreach (UnitController controller in FindObjectsOfType<UnitController>())
        {
            ControllersArray[(int)controller.GetTeam()] = controller;
        }

        // Store TargetBuildings
        TargetBuildingArray = FindObjectsOfType<TargetBuilding>();

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
            Vector3 heightReduction = Vector3.up * 0.1f; // $$ hack : this is to prevent selectioning / building in high areas
            PlayableBounds.SetMinMax(PlayableBounds.min + clampedOne * NonPlayableBorder / 2f, PlayableBounds.max - clampedOne * NonPlayableBorder / 2f - heightReduction);
        }
        else
        {
            Debug.LogWarning("could not find terrain asset in scene, setting default PlayableBounds");
            Vector3 clampedOne = new Vector3(1f, 0f, 1f);
            PlayableBounds.SetMinMax(   new Vector3(-DefaultPlayableBoundsSize, -10.0f, -DefaultPlayableBoundsSize) + clampedOne * NonPlayableBorder / 2f,
                                        new Vector3(DefaultPlayableBoundsSize, 10.0f, DefaultPlayableBoundsSize) - clampedOne * NonPlayableBorder / 2f);
        }
    }
    #endregion
    
    private void Update()
    {
        
#if UNITY_EDITOR
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
            Vector2 globalBarycenter = Statistic.GetGlobalBarycenter();
            Vector2 blueBarycenter = Statistic.GetTeamBarycenter(ETeam.Blue);
            Vector2 redBarycenter = Statistic.GetTeamBarycenter(ETeam.Red);
            debug.barycenter.globalBarycenterInstance.transform.position = new Vector3(globalBarycenter.x,
                debug.barycenter.globalBarycenterInstance.transform.position.y, globalBarycenter.y);
            debug.barycenter.blueBarycenterInstance.transform.position = new Vector3(blueBarycenter.x,
                debug.barycenter.blueBarycenterInstance.transform.position.y, blueBarycenter.y);
            debug.barycenter.redBarycenterInstance.transform.position = new Vector3(redBarycenter.x,
                debug.barycenter.redBarycenterInstance.transform.position.y, redBarycenter.y);
        }
#endif
    }

    private void OnGUI()
    {
#if UNITY_EDITOR
        if (debug.targetAnalysis.drawTargetStatistic)
        {
            Statistic.TargetBuildingAnalysisData[] targetBuildingAnalysisData = Statistic.GetTargetBuildingAnalysisData(ETeam.Blue, debug.targetAnalysis.targetStatisticRadius);
            
            GUILayout.BeginVertical("box");
            GUILayout.Label("Target building analysis");

            foreach (Statistic.TargetBuildingAnalysisData buildingAnalysisData in targetBuildingAnalysisData)
            {
                GUILayout.BeginHorizontal("box");
                
                GUILayout.Label($"{buildingAnalysisData.target.name}:");
                GUILayout.Label($"Distance from blue team barycenter: {Mathf.Sqrt(buildingAnalysisData.sqrDistanceFromTeamBarycenter)}");
                GUILayout.Label($"Blue occupation: {buildingAnalysisData.balancing.occupationTeam1 * 100f}%");
                GUILayout.Label($"Red occupation: {buildingAnalysisData.balancing.occupationTeam2 * 100f}%");

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }
#endif
    }
}
