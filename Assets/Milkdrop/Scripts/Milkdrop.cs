using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System;

public class Milkdrop : MonoBehaviour
{
    public class Wave
    {
        public Dictionary<int, float> BaseVariables = new Dictionary<int, float>();
        public string InitEquation = "";
        public Action<Dictionary<int, float>> InitEquationCompiled;
        public string FrameEquation = "";
        public Action<Dictionary<int, float>> FrameEquationCompiled;
        public string PointEquation = "";
        public Action<Dictionary<int, float>> PointEquationCompiled;
        public Dictionary<int, float> Variables = new Dictionary<int, float>();
        public Dictionary<int, float> InitVariables = new Dictionary<int, float>();
        public Dictionary<int, float> FrameVariables = new Dictionary<int, float>();
        public Dictionary<int, float> PointVariables = new Dictionary<int, float>();
        public int[] UserKeys = new int[0];
        public Dictionary<int, float> FrameMap = new Dictionary<int, float>();
        public Dictionary<int, float> Inits = new Dictionary<int, float>();

        public float[] PointsDataL;
        public float[] PointsDataR;
        public Vector3[] Positions;
        public Color[] Colors;
        public Vector3[] SmoothedPositions;
        public Color[] SmoothedColors;
    }

    public class Shape
    {
        public Dictionary<int, float> BaseVariables = new Dictionary<int, float>();
        public string InitEquation = "";
        public Action<Dictionary<int, float>> InitEquationCompiled;
        public string FrameEquation = "";
        public Action<Dictionary<int, float>> FrameEquationCompiled;
        public Dictionary<int, float> Variables = new Dictionary<int, float>();
        public Dictionary<int, float> InitVariables = new Dictionary<int, float>();
        public Dictionary<int, float> FrameVariables = new Dictionary<int, float>();
        public int[] UserKeys = new int[0];
        public Dictionary<int, float> FrameMap = new Dictionary<int, float>();
        public Dictionary<int, float> Inits = new Dictionary<int, float>();

        public Vector3[] Positions;
        public Color[] Colors;
        public Vector2[] UVs;
        public Vector3[] BorderPositions;

        public Mesh ShapeMesh;
    }

    public class Preset
    {
        public Dictionary<int, float> BaseVariables = new Dictionary<int, float>();
        public string InitEquation = "";
        public Action<Dictionary<int, float>> InitEquationCompiled;
        public string FrameEquation = "";
        public Action<Dictionary<int, float>> FrameEquationCompiled;
        public string PixelEquation = "";
        public Action<Dictionary<int, float>> PixelEquationCompiled;
        public List<Wave> Waves = new List<Wave>();
        public List<Shape> Shapes = new List<Shape>();
        public string WarpEquation = "";
        public string CompEquation = "";
        public string Warp;
        public string Comp;

        public Dictionary<int, float> Variables = new Dictionary<int, float>();
        public Dictionary<int, float> InitVariables = new Dictionary<int, float>();
        public Dictionary<int, float> RegVariables = new Dictionary<int, float>();
        public Dictionary<int, float> FrameVariables = new Dictionary<int, float>();
        public Dictionary<int, float> PixelVariables = new Dictionary<int, float>();

        public int[] UserKeys = new int[0];
        public Dictionary<int, float> FrameMap = new Dictionary<int, float>();
        public Dictionary<int, float> AfterFrameVariables = new Dictionary<int, float>();

        public Material WarpMaterial;
        public Material DarkenCenterMaterial;
        public Material CompMaterial;
    }

    public Dictionary<string, Preset> LoadedPresets = new Dictionary<string, Preset>();

    public TextAsset[] PresetFiles;

    private Preset CurrentPreset;
    private Preset PrevPreset;

    public Vector2Int MeshSize = new Vector2Int(48, 36);
    public Vector2Int MeshSizeComp = new Vector2Int(32, 24);
    public Vector2Int MotionVectorsSize = new Vector2Int(64, 48);
    public Vector2Int Resolution = new Vector2Int(1200, 900);
    public int MaxShapeSides = 101;
    public int MaxSamples = 512;
    public float MaxFPS = 30f;

    public float ChangePresetIn = 5f;

    public float TransitionTime = 5.7f;

    [HideInInspector]
    public float presetChangeTimer = 0f;

    public float Bass;
    public float BassAtt;
    public float Mid;
    public float MidAtt;
    public float Treb;
    public float TrebAtt;

    public MeshFilter TargetMeshFilter;
    public MeshRenderer TargetMeshRenderer;

    public MeshFilter TargetMeshFilter2;
    public MeshRenderer TargetMeshRenderer2;

    public LineRenderer WaveformRenderer;
    public LineRenderer WaveformRenderer2;

    public Camera TargetCamera;

    public AudioSource TargetAudio;

    public Shader DefaultWarpShader;
    public Shader DarkenCenterShader;
    public Shader DefaultCompShader;

    public Material DoNothingMaterial;

    public Material BorderMaterial;

    public Material ShapeMaterial;

    public Transform BorderSideLeft;
    public Transform BorderSideRight;
    public Transform BorderSideTop;
    public Transform BorderSideBottom;
    public Transform BorderParent;

    public bool RandomOrder = true;

    private ulong CurrentFrame = 0;
    private float CurrentTime = 0f;

    private Vector3 baseDotScale;

    private Dictionary<string, string> VariableNameLookup = new Dictionary<string, string>
    {
        {"frating", "rating"},
        {"fgammaadj", "gammaadj"},
        {"fdecay", "decay"},
        {"fvideoechozoom", "echo_zoom"},
        {"fvideoechoalpha", "echo_alpha"},
        {"nvideoechoorientation", "echo_orient"},
        {"nwavemode", "wave_mode"},
        {"badditivewaves", "additivewave"},
        {"bwavedots", "wave_dots"},
        {"bwavethick", "wave_thick"},
        {"bmodwavealphabyvolume", "modwavealphabyvolume"},
        {"bmaximizewavecolor", "wave_brighten"},
        {"btexwrap", "wrap"},
        {"bdarkencenter", "darken_center"},
        {"bredbluestereo", "red_blue"},
        {"bbrighten", "brighten"},
        {"bdarken", "darken"},
        {"bsolarize", "solarize"},
        {"binvert", "invert"},
        {"fwavealpha", "wave_a"},
        {"fwavescale", "wave_scale"},
        {"fwavesmoothing", "wave_smoothing"},
        {"fwaveparam", "wave_mystery"},
        {"fmodwavealphastart", "modwavealphastart"},
        {"fmodwavealphaend", "modwavealphaend"},
        {"fwarpanimspeed", "warpanimspeed"},
        {"fwarpscale", "warpscale"},
        {"fzoomexponent", "zoomexp"},
        {"nmotionvectorsx", "mv_x"},
        {"nmotionvectorsy", "mv_y"},
        {"thick", "thickoutline"},
        {"instances", "num_inst"},
        {"num_instances", "num_inst"},
        {"badditive", "additive"},
        {"busedots", "usedots"},
        {"bspectrum", "spectrum"},
        {"bdrawthick", "thick"}
    };

    private int[] qs;

    private int[] ts;

    private int[] regs;

    private Vector2[] WarpUVs;
    private Color[] WarpColor;
    private Color[] CompColor;

    private RenderTexture PrevTempTexture;
    private RenderTexture TempTexture;
    [HideInInspector]
    public RenderTexture FinalTexture;

    private Mesh TargetMeshWarp;
    private Mesh TargetMeshDarkenCenter;
    private Mesh TargetMeshComp;

    private float[] timeArray;
    private float[] timeArrayL;
    private float[] timeArrayR;
    private float[] freqArrayL;
    private float[] freqArrayR;

    private Vector3[] BasicWaveFormPositions;
    private Vector3[] BasicWaveFormPositions2;
    private Vector3[] BasicWaveFormPositionsOld;
    private Vector3[] BasicWaveFormPositions2Old;
    private Vector3[] BasicWaveFormPositionsSmooth;
    private Vector3[] BasicWaveFormPositionsSmooth2;

    private Vector3[] MotionVectorsPositions;

    private float timeSinceLastFrame = 0f;

    private bool initialized = false;

    public float FPS;
    public string PresetName;

    public Transform DotParent;
    public GameObject DotPrefab;

    public Transform MotionVectorParent;
    public GameObject MotionVectorPrefab;

    private Transform[] Dots;
    private SpriteRenderer[] DotRenderers;

    private Transform[] MotionVectors;
    private SpriteRenderer[] MotionVectorRenderers;

    private float[] Stack = new float[2048];

    private int[] audioSampleStarts;
    private int[] audioSampleStops;

    private Dictionary<string, int> VariableNameTable = new Dictionary<string, int>();
    private int LatestVariableIndex = 0;

    private bool blending;
    private float blendStartTime;
    private float blendDuration;
    private float blendProgress;
    private float[] blendingVertInfoA;
    private float[] blendingVertInfoC;

    int[] mixedVariables;
    int[] snappedVariables;

    float[] imm = new float[3];
    float[] avg = new float[3];
    float[] longAvg = new float[3];
    float[] val = new float[3];
    float[] att = new float[3];

    int index = 0;

    Dictionary<int, float> Pick(Dictionary<int, float> source, int[] keys)
    {
        Dictionary<int, float> result = new Dictionary<int, float>();

        foreach (int key in source.Keys.Where(x => keys.Contains(x)))
        {
            result.Add(key, source[key]);
        }

        return result;
    }

    Dictionary<int, float> Omit(Dictionary<int, float> source, int[] keys)
    {
        Dictionary<int, float> result = new Dictionary<int, float>();

        foreach (int key in source.Keys.Where(x => !keys.Contains(x)))
        {
            result.Add(key, source[key]);
        }

        return result;
    }

    void RegisterVariable(string name)
    {
        if (VariableNameTable.ContainsKey(name))
        {
            return;
        }

        VariableNameTable.Add(name, LatestVariableIndex++);
    }

    void OnDestroy()
    {
        Destroy(TempTexture);
        Destroy(FinalTexture);
        Destroy(TargetMeshWarp);
        Destroy(TargetMeshDarkenCenter);
        Destroy(TargetMeshComp);

        UnloadPresets();
    }

    public void Initialize()
    {
        string[] _qs = new string[]
        {
            "q1", "q2", "q3", "q4", "q5", "q6", "q7", "q8",
            "q9", "q10", "q11","q12", "q13", "q14", "q15", "q16",
            "q17", "q18", "q19", "q20", "q21", "q22", "q23", "q24",
            "q25", "q26", "q27", "q28", "q29", "q30", "q31", "q32",
        };

        string[] _ts = new string[]
        {
            "t1", "t2", "t3", "t4", "t5", "t6", "t7", "t8"
        };

        string[] _regs = new string[99];

        for (int i = 0; i < _regs.Length; i++)
        {
            _regs[i] = i < 10 ? "reg0" + i : "reg" + i;
        }

        foreach (var v in _qs)
        {
            RegisterVariable(v);
        }

        foreach (var v in _ts)
        {
            RegisterVariable(v);
        }

        foreach (var v in _regs)
        {
            RegisterVariable(v);
        }

        qs = new int[_qs.Length];

        for (int i = 0; i < qs.Length; i++)
        {
            qs[i] = VariableNameTable[_qs[i]];
        }

        ts = new int[_ts.Length];

        for (int i = 0; i < ts.Length; i++)
        {
            ts[i] = VariableNameTable[_ts[i]];
        }

        regs = new int[_regs.Length];

        for (int i = 0; i < regs.Length; i++)
        {
            regs[i] = VariableNameTable[_regs[i]];
        }
        
        UnloadPresets();
        LoadPresets();

        blendingVertInfoA = new float[(MeshSize.x + 1) * (MeshSize.y + 1)];
        blendingVertInfoC = new float[(MeshSize.x + 1) * (MeshSize.y + 1)];

        string[] _mixedVariables = new string[]
        {
            "wave_a", "wave_r", "wave_g", "wave_b", "wave_x", "wave_y", "wave_mystery",
            "ob_size", "ob_r", "ob_g", "ob_b", "ob_a",
            "ib_size", "ib_r", "ib_g", "ib_b", "ib_a",
            "mv_x", "mv_y", "mv_dx", "mv_dy", "mv_l", "mv_r", "mv_g", "mv_b", "mv_a",
            "echo_zoom", "echo_alpha", "echo_orient",
            "b1n", "b2n", "b3n",
            "b1x", "b2x", "b3x",
            "b1ed", "b2ed", "b3ed"
        };

        string[] _snappedVariables = new string[]
        {
            "wave_dots", "wave_thick", "additivewave", "wave_brighten", "darken_center", "gammaadj", "wrap", "invert", "brighten", "darken", "solarize"
        };

        foreach (var v in _mixedVariables)
        {
            RegisterVariable(v);
        }

        foreach (var v in _snappedVariables)
        {
            RegisterVariable(v);
        }

        mixedVariables = new int[_mixedVariables.Length];

        for (int i = 0; i < mixedVariables.Length; i++)
        {
            mixedVariables[i] = VariableNameTable[_mixedVariables[i]];
        }

        snappedVariables = new int[_snappedVariables.Length];

        for (int i = 0; i < snappedVariables.Length; i++)
        {
            snappedVariables[i] = VariableNameTable[_snappedVariables[i]];
        }

        WarpUVs = new Vector2[(MeshSize.x + 1) * (MeshSize.y + 1)];
        WarpColor = new Color[(MeshSize.x + 1) * (MeshSize.y + 1)];
        CompColor = new Color[(MeshSizeComp.x + 1) * (MeshSizeComp.y + 1)];

        BasicWaveFormPositions = new Vector3[MaxSamples];
        BasicWaveFormPositions2 = new Vector3[MaxSamples];
        BasicWaveFormPositionsOld = new Vector3[MaxSamples];
        BasicWaveFormPositions2Old = new Vector3[MaxSamples];
        BasicWaveFormPositionsSmooth = new Vector3[MaxSamples * 2];
        BasicWaveFormPositionsSmooth2 = new Vector3[MaxSamples * 2];

        MotionVectorsPositions = new Vector3[MotionVectorsSize.x * MotionVectorsSize.y * 2];

        baseDotScale = DotPrefab.transform.localScale;

        Dots = new Transform[MaxSamples * 4];
        DotRenderers = new SpriteRenderer[MaxSamples * 4];

        for (int i = 0; i < Dots.Length; i++)
        {
            Dots[i] = Instantiate(DotPrefab, DotParent).transform;
            DotRenderers[i] = Dots[i].GetComponent<SpriteRenderer>();
        }

        MotionVectors = new Transform[MotionVectorsSize.x * MotionVectorsSize.y];
        MotionVectorRenderers = new SpriteRenderer[MotionVectorsSize.x * MotionVectorsSize.y];

        for (int i = 0; i < MotionVectors.Length; i++)
        {
            MotionVectors[i] = Instantiate(MotionVectorPrefab, MotionVectorParent).transform;
            MotionVectorRenderers[i] = MotionVectors[i].GetComponent<SpriteRenderer>();
        }

        timeArray = new float[MaxSamples * 2];
        timeArrayL = new float[MaxSamples];
        timeArrayR = new float[MaxSamples];

        freqArrayL = new float[MaxSamples];
        freqArrayR = new float[MaxSamples];

        PrevTempTexture = new RenderTexture(Resolution.x, Resolution.y, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
        TempTexture = new RenderTexture(Resolution.x, Resolution.y, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);
        FinalTexture = new RenderTexture(Resolution.x, Resolution.y, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);

        TargetMeshWarp = new Mesh();
        Vector3[] vertices = new Vector3[(MeshSize.x + 1) * (MeshSize.y + 1)];
        for (int i = 0, y = 0; y <= MeshSize.y; y++)
        {
            for (int x = 0; x <= MeshSize.x; x++, i++)
            {
                vertices[i] = new Vector3(x / (float)MeshSize.x * 2f - 1f, -(y / (float)MeshSize.y * 2f - 1f));
            }
        }
        TargetMeshWarp.vertices = vertices;
        int[] triangles = new int[MeshSize.x * MeshSize.y * 6];
        for (int ti = 0, vi = 0, y = 0; y < MeshSize.y; y++, vi++)
        {
            for (int x = 0; x < MeshSize.x; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + MeshSize.x + 1;
                triangles[ti + 5] = vi + MeshSize.x + 2;
            }
        }
        TargetMeshWarp.triangles = triangles;

        Vector2Int darkenMeshSize = new Vector2Int(2,2);
        TargetMeshDarkenCenter = new Mesh();
        vertices = new Vector3[(darkenMeshSize.x + 1) * (darkenMeshSize.y + 1)];
        Color[] colors = new Color[(darkenMeshSize.x + 1) * (darkenMeshSize.y + 1)];
        for (int i = 0, y = 0; y <= darkenMeshSize.y; y++)
        {
            for (int x = 0; x <= darkenMeshSize.x; x++, i++)
            {
                vertices[i] = new Vector3(x / (float)darkenMeshSize.x * 2f - 1f, -(y / (float)darkenMeshSize.y * 2f - 1f));
                colors[i] = new Color(0f, 0f, 0f, x == 1 && y == 1 ? 3f / 32f : 0f);
            }
        }
        TargetMeshDarkenCenter.vertices = vertices;
        TargetMeshDarkenCenter.colors = colors;
        triangles = new int[darkenMeshSize.x * darkenMeshSize.y * 6];
        for (int ti = 0, vi = 0, y = 0; y < darkenMeshSize.y; y++, vi++)
        {
            for (int x = 0; x < darkenMeshSize.x; x++, ti += 6, vi++)
            {
                if (vi % 2 == 0)
                {
                    triangles[ti] = vi;
                    triangles[ti + 1] = vi + darkenMeshSize.x + 1;
                    triangles[ti + 2] = vi + 1;
                    triangles[ti + 3] = vi + 1;
                    triangles[ti + 4] = vi + darkenMeshSize.x + 1;
                    triangles[ti + 5] = vi + darkenMeshSize.x + 2;
                }
                else
                {
                    triangles[ti] = vi;
                    triangles[ti + 1] = vi + darkenMeshSize.x + 2;
                    triangles[ti + 2] = vi + 1;
                    triangles[ti + 3] = vi;
                    triangles[ti + 4] = vi + darkenMeshSize.x + 1;
                    triangles[ti + 5] = vi + darkenMeshSize.x + 2;
                }
            }
        }
        TargetMeshDarkenCenter.triangles = triangles;

        TargetMeshComp = new Mesh();
        vertices = new Vector3[(MeshSizeComp.x + 1) * (MeshSizeComp.y + 1)];
        Vector2[] uvs = new Vector2[(MeshSizeComp.x + 1) * (MeshSizeComp.y + 1)];
        for (int i = 0, y = 0; y <= MeshSizeComp.y; y++)
        {
            for (int x = 0; x <= MeshSizeComp.x; x++, i++)
            {
                vertices[i] = new Vector3(x / (float)MeshSizeComp.x * 2f - 1f, -(y / (float)MeshSizeComp.y * 2f - 1f));
                uvs[i] = new Vector2(x / (float)MeshSizeComp.x, y / (float)MeshSizeComp.y);
            }
        }
        TargetMeshComp.vertices = vertices;
        TargetMeshComp.uv = uvs;
        triangles = new int[MeshSizeComp.x * MeshSizeComp.y * 6];
        for (int ti = 0, vi = 0, y = 0; y < MeshSizeComp.y; y++, vi++)
        {
            for (int x = 0; x < MeshSizeComp.x; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + MeshSizeComp.x + 1;
                triangles[ti + 5] = vi + MeshSizeComp.x + 2;
            }
        }
        TargetMeshComp.triangles = triangles;

        TargetMeshFilter.transform.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);
        WaveformRenderer.transform.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);
        WaveformRenderer2.transform.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);
        BorderParent.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);
        TargetMeshFilter2.transform.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);

        WaveformRenderer.enabled = false;
        WaveformRenderer2.enabled = false;

        BorderSideLeft.gameObject.SetActive(false);
        BorderSideRight.gameObject.SetActive(false);
        BorderSideTop.gameObject.SetActive(false);
        BorderSideBottom.gameObject.SetActive(false);

        int sampleRate = AudioSettings.outputSampleRate;
        float freqMultiplier = sampleRate * 0.5f;
        float bucketHz = freqMultiplier / MaxSamples;

        int bassLow = Mathf.Clamp(
            Mathf.RoundToInt(20f / bucketHz),
            0,
            MaxSamples - 1
        );

        int bassHigh = Mathf.Clamp(
            Mathf.RoundToInt(320f / bucketHz),
            0,
            MaxSamples - 1
        );

        int midHigh = Mathf.Clamp(
            Mathf.RoundToInt(2800f / bucketHz),
            0,
            MaxSamples - 1
        );

        int trebHigh = Mathf.Clamp(
            Mathf.RoundToInt(11025f / bucketHz),
            0,
            MaxSamples - 1
        );

        audioSampleStarts = new int[] { bassLow, bassHigh, midHigh };
        audioSampleStops = new int[] { bassHigh, midHigh, trebHigh };

        if (!string.IsNullOrEmpty(PresetName))
        {
            index = Array.IndexOf(LoadedPresets.Keys.ToArray(), PresetName);
        }

        PlayRandomPreset();

        initialized = true;
    }

    public void PlayRandomPreset()
    {
        var keys = LoadedPresets.Keys.ToArray();
        int ind;
        if (RandomOrder)
        {
            ind = UnityEngine.Random.Range(0, keys.Length);
        }
        else
        {
            ind = index++;
            if (index >= keys.Length)
            {
                index = 0;
            }
        }
        PlayPreset(keys[ind]);
    }

    void LateUpdate()
    {
        if (!initialized)
            return;

        if (Time.timeScale == 0f)
            return;
        
        if (!blending)
        {
            presetChangeTimer += Time.deltaTime;
        }

        if (presetChangeTimer >= ChangePresetIn)
        {
            presetChangeTimer -= ChangePresetIn;
            PlayRandomPreset();
        }
        
        timeSinceLastFrame += Time.deltaTime;

        if (timeSinceLastFrame >= 1f / MaxFPS)
        {
            timeSinceLastFrame -= 1f / MaxFPS;
            FPS = Mathf.Min(1f / Time.deltaTime, MaxFPS);
            Render();
        }
    }

    void Render()
    {
        CurrentTime += 1f / FPS;
        CurrentFrame++;

        if (blending)
        {
            blendProgress = (CurrentTime - blendStartTime) / blendDuration;

            if (blendProgress > 1f)
            {
                blending = false;
            }
        }

        UpdateAudioLevels();

        RunFrameEquations(CurrentPreset);
        RunPixelEquations(CurrentPreset, false);

        foreach (var v in Pick(CurrentPreset.PixelVariables, regs))
        {
            SetVariable(CurrentPreset.RegVariables, v.Key, v.Value);
        }

        // assing regs to global

        if (blending)
        {
            RunFrameEquations(PrevPreset);
            RunPixelEquations(PrevPreset, true);

            MixFrameEquations();
        }

        RenderImage();
    }

    void MixFrameEquations()
    {
        float mix = 0.5f - 0.5f * Mathf.Cos(blendProgress * Mathf.PI);
        float mix2 = 1f - mix;
        float snapPoint = 0.5f;
        
        foreach (var v in mixedVariables)
        {
            SetVariable(CurrentPreset.FrameVariables, v, mix * GetVariable(CurrentPreset.FrameVariables, v) + mix2 * GetVariable(PrevPreset.FrameVariables, v));
        }

        foreach (var v in snappedVariables)
        {
            SetVariable(CurrentPreset.FrameVariables, v, mix < snapPoint ? GetVariable(PrevPreset.FrameVariables, v) : GetVariable(CurrentPreset.FrameVariables, v));
        }
    }

    void UpdateAudioLevels()
    {
        if (TargetAudio.clip)
        {
            TargetAudio.clip.GetData(timeArray, TargetAudio.timeSamples);
            
            for (int i = 0; i < MaxSamples; i++)
            {
                timeArrayL[i] = timeArray[i * 2];
            }

            for (int i = 0; i < MaxSamples; i++)
            {
                timeArrayR[i] = timeArray[i * 2 + 1];
            }
        }

        TargetAudio.GetSpectrumData(freqArrayL, 0, FFTWindow.Rectangular);
        TargetAudio.GetSpectrumData(freqArrayR, 1, FFTWindow.Rectangular);

        for (int i = 0; i < MaxSamples; i++)
        {
            timeArrayL[i] *= 128f;
            timeArrayR[i] *= 128f;
        }

        for (int i = 0; i < MaxSamples; i++)
        {
            freqArrayL[i] *= 10000f;
            freqArrayR[i] *= 10000f;
        }

        float effectiveFPS = FPS;

        if (effectiveFPS < 15f)
        {
            effectiveFPS = 15f;
        }
        else if (effectiveFPS > 144f)
        {
            effectiveFPS = 144f;
        }

        for (int i = 0; i < 3; i++)
        {
            for (int j = audioSampleStarts[i]; j < audioSampleStops[i]; j++)
            {
                imm[i] += (freqArrayL[j] + freqArrayR[j]) * 0.5f;
            }
        }

        for (int i = 0; i < 3; i++)
        {
            float rate;
            if (imm[i] > avg[i])
            {
                rate = 0.2f;
            }
            else
            {
                rate = 0.5f;
            }
            rate = Mathf.Pow(rate, 30f / effectiveFPS);

            avg[i] = avg[i] * rate + imm[i] * (1 - rate);

            if (CurrentFrame < 50)
            {
                rate = 0.9f;
            } else
            {
                rate = 0.992f;
            }
            rate = Mathf.Pow(rate, 30f / effectiveFPS);
            longAvg[i] = longAvg[i] * rate + imm[i] * (1 - rate);

            if (this.longAvg[i] < 0.001f)
            {
                val[i] = 1.0f;
                att[i] = 1.0f;
            } else
            {
                val[i] = imm[i] / longAvg[i];
                att[i] = avg[i] / longAvg[i];
            }
        }

        Bass = val[0];
        BassAtt = att[0];
        Mid = val[1];
        MidAtt = att[1];
        Treb = val[2];
        TrebAtt = att[2];
    }

    void RunFrameEquations(Preset preset)
    {
        preset.FrameVariables = new Dictionary<int, float>(preset.Variables);

        foreach (var v in preset.InitVariables.Keys)
        {
            SetVariable(preset.FrameVariables, v, preset.InitVariables[v]);
        }

        foreach (var v in preset.FrameMap.Keys)
        {
            SetVariable(preset.FrameVariables, v, preset.FrameMap[v]);
        }

        SetVariable(preset.FrameVariables, "frame", CurrentFrame);
        SetVariable(preset.FrameVariables, "time", CurrentTime);
        SetVariable(preset.FrameVariables, "fps", FPS);
        SetVariable(preset.FrameVariables, "bass", Bass);
        SetVariable(preset.FrameVariables, "bass_att", BassAtt);
        SetVariable(preset.FrameVariables, "mid", Mid);
        SetVariable(preset.FrameVariables, "mid_att", MidAtt);
        SetVariable(preset.FrameVariables, "treb", Treb);
        SetVariable(preset.FrameVariables, "treb_att", TrebAtt);
        SetVariable(preset.FrameVariables, "meshx", MeshSize.x);
        SetVariable(preset.FrameVariables, "meshy", MeshSize.y);
        SetVariable(preset.FrameVariables, "aspectx", 1f);
        SetVariable(preset.FrameVariables, "aspecty", 1f);
        SetVariable(preset.FrameVariables, "pixelsx", Resolution.x);
        SetVariable(preset.FrameVariables, "pixelsy", Resolution.y);

        preset.FrameEquationCompiled(preset.FrameVariables);
    }

    void RunPixelEquations(Preset preset, bool blending)
    {
        int gridX = MeshSize.x;
        int gridZ = MeshSize.y;

        int gridX1 = gridX + 1;
        int gridZ1 = gridZ + 1;

        float warpTimeV = CurrentTime * GetVariable(preset.FrameVariables, "warpanimspeed");
        float warpScaleInv = 1f / GetVariable(preset.FrameVariables, "warpscale");

        float warpf0 = 11.68f + 4f * Mathf.Cos(warpTimeV * 1.413f + 1f);
        float warpf1 = 8.77f + 3f * Mathf.Cos(warpTimeV * 1.113f + 7f);
        float warpf2 = 10.54f + 3f * Mathf.Cos(warpTimeV * 1.233f + 3f);
        float warpf3 = 11.49f + 4f * Mathf.Cos(warpTimeV * 0.933f + 5f);

        float texelOffsetX = 0f;
        float texelOffsetY = 0f;

        float aspectx = 1f;
        float aspecty = 1f;

        int offset = 0;
        int offsetColor = 0;

        foreach (var v in preset.FrameVariables.Keys)
        {
            SetVariable(preset.PixelVariables, v, preset.FrameVariables[v]);
        }

        float warp = GetVariable(preset.PixelVariables, "warp");
        float zoom = GetVariable(preset.PixelVariables, "zoom");
        float zoomExp = GetVariable(preset.PixelVariables, "zoomexp");
        float cx = GetVariable(preset.PixelVariables, "cx");
        float cy = GetVariable(preset.PixelVariables, "cy");
        float sx = GetVariable(preset.PixelVariables, "sx");
        float sy = GetVariable(preset.PixelVariables, "sy");
        float dx = GetVariable(preset.PixelVariables, "dx");
        float dy = GetVariable(preset.PixelVariables, "dy");
        float rot = GetVariable(preset.PixelVariables, "rot");

        float frameZoom = zoom;
        float frameZoomExp = zoomExp;
        float frameRot = rot;
        float frameWarp = warp;
        float framecx = cx;
        float framecy = cy;
        float framesx = sx;
        float framesy = sy;
        float framedx = dx;
        float framedy = dy;

        for (int iz = 0; iz < gridZ1; iz++)
        {
            for (int ix = 0; ix < gridX1; ix++)
            {
                float x = (ix / (float)gridX) * 2f - 1f;
                float y = (iz / (float)gridZ) * 2f - 1f;
                float rad = Mathf.Sqrt(x * x * aspectx * aspectx + y * y * aspecty * aspecty);

                float ang;
                if (iz == gridZ / 2f && ix == gridX / 2f)
                {
                    ang = 0f;
                }
                else
                {
                    ang = Mathf.Atan2(y * aspecty, x * aspectx);
                    if (ang < 0f)
                    {
                        ang += 2f * Mathf.PI;
                    }
                }

                SetVariable(preset.PixelVariables, "x", x * 0.5f * aspectx + 0.5f);
                SetVariable(preset.PixelVariables, "y", y * -0.5f * aspecty + 0.5f);
                SetVariable(preset.PixelVariables, "rad", rad);
                SetVariable(preset.PixelVariables, "ang", ang);

                SetVariable(preset.PixelVariables, "zoom", frameZoom);
                SetVariable(preset.PixelVariables, "zoomexp", frameZoomExp);
                SetVariable(preset.PixelVariables, "rot", frameRot);
                SetVariable(preset.PixelVariables, "warp", frameWarp);
                SetVariable(preset.PixelVariables, "cx", framecx);
                SetVariable(preset.PixelVariables, "cy", framecy);
                SetVariable(preset.PixelVariables, "dx", framedx);
                SetVariable(preset.PixelVariables, "dy", framedy);
                SetVariable(preset.PixelVariables, "sx", framesx);
                SetVariable(preset.PixelVariables, "sy", framesy);

                preset.PixelEquationCompiled(preset.PixelVariables);

                warp = GetVariable(preset.PixelVariables, "warp");
                zoom = GetVariable(preset.PixelVariables, "zoom");
                zoomExp = GetVariable(preset.PixelVariables, "zoomexp");
                cx = GetVariable(preset.PixelVariables, "cx");
                cy = GetVariable(preset.PixelVariables, "cy");
                sx = GetVariable(preset.PixelVariables, "sx");
                sy = GetVariable(preset.PixelVariables, "sy");
                dx = GetVariable(preset.PixelVariables, "dx");
                dy = GetVariable(preset.PixelVariables, "dy");
                rot = GetVariable(preset.PixelVariables, "rot");

                float zoom2V = Mathf.Pow(zoom, Mathf.Pow(zoomExp, (rad * 2f - 1f)));
                float zoom2Inv = 1f / zoom2V;

                float u = x * 0.5f * aspectx * zoom2Inv + 0.5f;
                float v = -y * 0.5f * aspecty * zoom2Inv + 0.5f;

                u = (u - cx) / sx + cx;
                v = (v - cy) / sy + cy;

                if (warp != 0f)
                {
                    u +=
                        warp *
                        0.0035f *
                        Mathf.Sin(
                            warpTimeV * 0.333f + warpScaleInv * (x * warpf0 - y * warpf3)
                        );
                    v +=
                        warp *
                        0.0035f *
                        Mathf.Cos(
                            warpTimeV * 0.375f - warpScaleInv * (x * warpf2 + y * warpf1)
                        );
                    u +=
                        warp *
                        0.0035f *
                        Mathf.Cos(
                            warpTimeV * 0.753f - warpScaleInv * (x * warpf1 - y * warpf2)
                        );
                    v +=
                        warp *
                        0.0035f *
                        Mathf.Sin(
                            warpTimeV * 0.825f + warpScaleInv * (x * warpf0 + y * warpf3)
                        );
                }

                float u2 = u - cx;
                float v2 = v - cy;

                float cosRot = Mathf.Cos(rot);
                float sinRot = Mathf.Sin(rot);
                u = u2 * cosRot - v2 * sinRot + cx;
                v = u2 * sinRot + v2 * cosRot + cy;

                u -= dx;
                v -= dy;

                u = (u - 0.5f) / aspectx + 0.5f;
                v = (v - 0.5f) / aspecty + 0.5f;

                u += texelOffsetX;
                v += texelOffsetY;

                if (!blending)
                {
                    WarpUVs[offset] = new Vector2(u, v);
                    WarpColor[offsetColor] = Color.white;
                }
                else
                {
                    float mix2 = blendingVertInfoA[offset / 2] * blendProgress + blendingVertInfoC[offset / 2];
                    mix2 = Mathf.Clamp01(mix2);

                    WarpUVs[offset] = new Vector2(WarpUVs[offset].x * mix2 + u * (1 - mix2), WarpUVs[offset].y * mix2 + v * (1 - mix2));
                    WarpColor[offsetColor] = new Color(1f, 1f, 1f, mix2);
                }

                offset++;
                offsetColor++;
            }
        }
    }

    (float[], float[]) GetBlurValues(Dictionary<int, float> variables)
    {
        float blurMin1 = GetVariable(variables, "b1n");
        float blurMin2 = GetVariable(variables, "b2n");
        float blurMin3 = GetVariable(variables, "b3n");
        float blurMax1 = GetVariable(variables, "b1x");
        float blurMax2 = GetVariable(variables, "b2x");
        float blurMax3 = GetVariable(variables, "b3x");

        float fMinDist = 0.1f;
        if (blurMax1 - blurMin1 < fMinDist)
        {
            float avg = (blurMin1 + blurMax1) * 0.5f;
            blurMin1 = avg - fMinDist * 0.5f;
            blurMax1 = avg - fMinDist * 0.5f;
        }
        blurMax2 = Mathf.Min(blurMax1, blurMax2);
        blurMin2 = Mathf.Max(blurMin1, blurMin2);
        if (blurMax2 - blurMin2 < fMinDist)
        {
            float avg = (blurMin2 + blurMax2) * 0.5f;
            blurMin2 = avg - fMinDist * 0.5f;
            blurMax2 = avg - fMinDist * 0.5f;
        }
        blurMax3 = Mathf.Min(blurMax2, blurMax3);
        blurMin3 = Mathf.Max(blurMin2, blurMin3);
        if (blurMax3 - blurMin3 < fMinDist)
        {
            float avg = (blurMin3 + blurMax3) * 0.5f;
            blurMin3 = avg - fMinDist * 0.5f;
            blurMax3 = avg - fMinDist * 0.5f;
        }

        return (
            new float[] { blurMin1, blurMin2, blurMin3 },
            new float[] { blurMax1, blurMax2, blurMax3 }
        );
    }

    void RenderImage()
    {
        var swapTexture = TempTexture;
        TempTexture = PrevTempTexture;
        PrevTempTexture = swapTexture;

        TempTexture.wrapMode = GetVariable(CurrentPreset.FrameVariables, "wrap") == 0f ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;

        if (!blending)
        {
            DrawWarp(CurrentPreset, false);
        }
        else
        {
            DrawWarp(PrevPreset, false);
            DrawWarp(CurrentPreset, true);
        }

        // blur

        DrawMotionVectors();

        DrawShapes(CurrentPreset, blending ? blendProgress : 1f);

        DrawWaves(CurrentPreset, blending ? blendProgress : 1f);

        if (blending)
        {
            DrawShapes(PrevPreset, 1f - blendProgress);

            DrawWaves(PrevPreset, 1f - blendProgress);
        }

        DrawBasicWaveform();

        DrawDarkenCenter();

        DrawOuterBorder();

        DrawInnerBorder();

        // text

        if (!blending)
        {
            DrawComp(CurrentPreset, false);
        }
        else
        {
            DrawComp(PrevPreset, false);
            DrawComp(CurrentPreset, true);
        }
    }

    void DrawShapes(Preset preset, float blendProgress)
    {
        if (preset.Shapes.Count == 0)
        {
            return;
        }

        foreach (var CurrentShape in preset.Shapes)
        {
            if (GetVariable(CurrentShape.BaseVariables, "enabled") == 0f)
            {
                continue;
            }

            CurrentShape.FrameVariables = new Dictionary<int, float>(CurrentShape.Variables);

            foreach (var v in CurrentShape.Variables.Keys)
            {
                SetVariable(CurrentShape.FrameVariables, v, CurrentShape.Variables[v]);
            }

            foreach (var v in CurrentShape.FrameMap.Keys)
            {
                SetVariable(CurrentShape.FrameVariables, v, CurrentShape.FrameMap[v]);
            }

            if (string.IsNullOrEmpty(CurrentShape.FrameEquation))
            {
                foreach (var v in preset.AfterFrameVariables.Keys)
                {
                    SetVariable(CurrentShape.FrameVariables, v, preset.AfterFrameVariables[v]);
                }

                foreach (var v in CurrentShape.Inits.Keys)
                {
                    SetVariable(CurrentShape.FrameVariables, v, CurrentShape.Inits[v]);
                }
            }

            SetVariable(CurrentShape.FrameVariables, "frame", CurrentFrame);
            SetVariable(CurrentShape.FrameVariables, "time", CurrentTime);
            SetVariable(CurrentShape.FrameVariables, "fps", FPS);
            SetVariable(CurrentShape.FrameVariables, "bass", Bass);
            SetVariable(CurrentShape.FrameVariables, "bass_att", BassAtt);
            SetVariable(CurrentShape.FrameVariables, "mid", Mid);
            SetVariable(CurrentShape.FrameVariables, "mid_att", MidAtt);
            SetVariable(CurrentShape.FrameVariables, "treb", Treb);
            SetVariable(CurrentShape.FrameVariables, "treb_att", TrebAtt);
            SetVariable(CurrentShape.FrameVariables, "meshx", MeshSize.x);
            SetVariable(CurrentShape.FrameVariables, "meshy", MeshSize.y);
            SetVariable(CurrentShape.FrameVariables, "aspectx", 1f);
            SetVariable(CurrentShape.FrameVariables, "aspecty", 1f);
            SetVariable(CurrentShape.FrameVariables, "pixelsx", Resolution.x);
            SetVariable(CurrentShape.FrameVariables, "pixelsy", Resolution.y);

            int numInst = Mathf.FloorToInt(Mathf.Clamp(GetVariable(CurrentShape.BaseVariables, "num_inst"), 1f, 1024f));

            float baseX = GetVariable(CurrentShape.BaseVariables, "x");
            float baseY = GetVariable(CurrentShape.BaseVariables, "y");
            float baseRad = GetVariable(CurrentShape.BaseVariables, "rad");
            float baseAng = GetVariable(CurrentShape.BaseVariables, "ang");
            float baseR = GetVariable(CurrentShape.BaseVariables, "r");
            float baseG = GetVariable(CurrentShape.BaseVariables, "g");
            float baseB = GetVariable(CurrentShape.BaseVariables, "b");
            float baseA = GetVariable(CurrentShape.BaseVariables, "a");
            float baseR2 = GetVariable(CurrentShape.BaseVariables, "r2");
            float baseG2 = GetVariable(CurrentShape.BaseVariables, "g2");
            float baseB2 = GetVariable(CurrentShape.BaseVariables, "b2");
            float baseA2 = GetVariable(CurrentShape.BaseVariables, "a2");
            float baseBorderR = GetVariable(CurrentShape.BaseVariables, "border_r");
            float baseBorderG = GetVariable(CurrentShape.BaseVariables, "border_g");
            float baseBorderB = GetVariable(CurrentShape.BaseVariables, "border_b");
            float baseBorderA = GetVariable(CurrentShape.BaseVariables, "border_a");
            float baseThickOutline = GetVariable(CurrentShape.BaseVariables, "thickouline");
            float baseTextured = GetVariable(CurrentShape.BaseVariables, "textured");
            float baseTexZoom = GetVariable(CurrentShape.BaseVariables, "tex_zoom");
            float baseTexAng = GetVariable(CurrentShape.BaseVariables, "tex_ang");
            float baseAdditive = GetVariable(CurrentShape.BaseVariables, "additive");

            for (int j = 0; j < numInst; j++)
            {
                SetVariable(CurrentShape.FrameVariables, "instance", j);
                SetVariable(CurrentShape.FrameVariables, "x", baseX);
                SetVariable(CurrentShape.FrameVariables, "y", baseY);
                SetVariable(CurrentShape.FrameVariables, "rad", baseRad);
                SetVariable(CurrentShape.FrameVariables, "ang", baseAng);
                SetVariable(CurrentShape.FrameVariables, "r", baseR);
                SetVariable(CurrentShape.FrameVariables, "g", baseG);
                SetVariable(CurrentShape.FrameVariables, "b", baseB);
                SetVariable(CurrentShape.FrameVariables, "a", baseA);
                SetVariable(CurrentShape.FrameVariables, "r2", baseR2);
                SetVariable(CurrentShape.FrameVariables, "g2", baseG2);
                SetVariable(CurrentShape.FrameVariables, "b2", baseB2);
                SetVariable(CurrentShape.FrameVariables, "a2", baseA2);
                SetVariable(CurrentShape.FrameVariables, "border_r", baseBorderR);
                SetVariable(CurrentShape.FrameVariables, "border_g", baseBorderG);
                SetVariable(CurrentShape.FrameVariables, "border_b", baseBorderB);
                SetVariable(CurrentShape.FrameVariables, "border_a", baseBorderA);
                SetVariable(CurrentShape.FrameVariables, "thickouline", baseThickOutline);
                SetVariable(CurrentShape.FrameVariables, "textured", baseTextured);
                SetVariable(CurrentShape.FrameVariables, "tex_zoom", baseTexZoom);
                SetVariable(CurrentShape.FrameVariables, "tex_ang", baseTexAng);
                SetVariable(CurrentShape.FrameVariables, "additive", baseAdditive);

                if (!string.IsNullOrEmpty(CurrentShape.FrameEquation))
                {
                    foreach (var v in preset.AfterFrameVariables.Keys)
                    {
                        SetVariable(CurrentShape.FrameVariables, v, preset.AfterFrameVariables[v]);
                    }

                    foreach (var v in CurrentShape.Inits.Keys)
                    {
                        SetVariable(CurrentShape.FrameVariables, v, CurrentShape.Inits[v]);
                    }

                    CurrentShape.FrameEquationCompiled(CurrentShape.FrameVariables);
                }

                int sides = Mathf.Clamp(Mathf.FloorToInt(GetVariable(CurrentShape.FrameVariables, "sides")), 3, 100);

                float rad = GetVariable(CurrentShape.FrameVariables, "rad");
                float ang = GetVariable(CurrentShape.FrameVariables, "ang");

                float x = GetVariable(CurrentShape.FrameVariables, "x") * 2f - 1f;
                float y = GetVariable(CurrentShape.FrameVariables, "y") * 2f - 1f;

                float r = GetVariable(CurrentShape.FrameVariables, "r");
                float g = GetVariable(CurrentShape.FrameVariables, "g");
                float b = GetVariable(CurrentShape.FrameVariables, "b");
                float a = GetVariable(CurrentShape.FrameVariables, "a");
                float r2 = GetVariable(CurrentShape.FrameVariables, "r2");
                float g2 = GetVariable(CurrentShape.FrameVariables, "g2");
                float b2 = GetVariable(CurrentShape.FrameVariables, "b2");
                float a2 = GetVariable(CurrentShape.FrameVariables, "a2");

                float borderR = GetVariable(CurrentShape.FrameVariables, "border_r");
                float borderG = GetVariable(CurrentShape.FrameVariables, "border_g");
                float borderB = GetVariable(CurrentShape.FrameVariables, "border_b");
                float borderA = GetVariable(CurrentShape.FrameVariables, "border_a");

                Color borderColor = new Color
                (
                    borderR,
                    borderG,
                    borderB,
                    borderA * blendProgress
                );

                float thickoutline = GetVariable(CurrentShape.FrameVariables, "thickouline");
                
                float textured = GetVariable(CurrentShape.FrameVariables, "textured");
                float texZoom = GetVariable(CurrentShape.FrameVariables, "tex_zoom");
                float texAng = GetVariable(CurrentShape.FrameVariables, "tex_ang");

                float additive = GetVariable(CurrentShape.FrameVariables, "additive");

                bool hasBorder = borderColor.a > 0f;
                bool isTextured = Mathf.Abs(textured) >= 1f;
                bool isBorderThick = Mathf.Abs(thickoutline) >= 1f;
                bool isAdditive = Mathf.Abs(additive) >= 1f;

                CurrentShape.Positions[0] = new Vector3(x, y, 0f);

                CurrentShape.Colors[0] = new Color(r, g, b, a * blendProgress);

                if (isTextured)
                {
                    CurrentShape.UVs[0] = new Vector2(0.5f, 0.5f);
                }

                float quarterPi = Mathf.PI * 0.25f;

                for (int k = 1; k < sides + 1; k++)
                {
                    float p = (k - 1f) / sides;
                    float pTwoPi = p * 2f * Mathf.PI;

                    float angSum = pTwoPi + ang + quarterPi;
                    
                    CurrentShape.Positions[k] = new Vector3
                    (
                        x + rad * Mathf.Cos(angSum),
                        y + rad * Mathf.Sin(angSum),
                        0f
                    );

                    CurrentShape.Colors[k] = new Color(r2, g2, b2, a2 * blendProgress);

                    if (isTextured)
                    {
                        float texAngSum = pTwoPi + texAng + quarterPi;

                        CurrentShape.UVs[k] = new Vector2
                        (
                            0.5f + ((0.5f * Mathf.Cos(texAngSum)) / texZoom),
                            0.5f + (0.5f * Mathf.Sin(texAngSum)) / texZoom
                        );
                    }

                    if (hasBorder)
                    {
                        CurrentShape.BorderPositions[k - 1] = CurrentShape.Positions[k];
                    }
                }

                TargetMeshFilter2.gameObject.SetActive(true);

                CurrentShape.ShapeMesh.vertices = CurrentShape.Positions.Take(sides + 1).ToArray();
                CurrentShape.ShapeMesh.colors = CurrentShape.Colors.Take(sides + 1).ToArray();
                CurrentShape.ShapeMesh.uv = CurrentShape.UVs.Take(sides + 1).ToArray();

                int[] triangles = new int[sides * 3];

                for (int k = 0; k < sides; k++)
                {
                    triangles[k * 3 + 0] = 0;
                    triangles[k * 3 + 1] = k + 1;
                    triangles[k * 3 + 2] = (k + 2) >= (sides + 1) ? 1 : k + 2;
                }

                CurrentShape.ShapeMesh.triangles = triangles;

                TargetMeshFilter2.sharedMesh = CurrentShape.ShapeMesh;
                TargetMeshRenderer2.sharedMaterial = ShapeMaterial;

                ShapeMaterial.mainTexture = TempTexture;
                ShapeMaterial.SetFloat("uTextured", textured);
                ShapeMaterial.SetFloat("additive", additive);

                TargetMeshFilter.sharedMesh = TargetMeshWarp;
                TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

                DoNothingMaterial.mainTexture = TempTexture;

                TargetCamera.targetTexture = TempTexture;
                TargetCamera.Render();

                 TargetMeshFilter2.gameObject.SetActive(false);

                if (hasBorder)
                {
                    WaveformRenderer.enabled = true;
            
                    WaveformRenderer.loop = true;
                    WaveformRenderer.positionCount = sides;
                    WaveformRenderer.SetPositions(CurrentShape.BorderPositions);

                    if (isBorderThick)
                    {
                        WaveformRenderer.widthMultiplier = 2f;
                    }
                    else
                    {
                        WaveformRenderer.widthMultiplier = 0.5f;
                    }

                    WaveformRenderer.colorGradient = new Gradient()
                    {
                        colorKeys = new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                        alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                    };

                    WaveformRenderer.sharedMaterial.mainTexture = TempTexture;
                    WaveformRenderer.sharedMaterial.SetColor("waveColor", borderColor);
                    WaveformRenderer.sharedMaterial.SetFloat("additivewave", additive);
                    WaveformRenderer.sharedMaterial.SetFloat("aspect_ratio", Resolution.x / (float)Resolution.y);

                    TargetMeshFilter.sharedMesh = TargetMeshWarp;
                    TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

                    DoNothingMaterial.mainTexture = TempTexture;

                    TargetCamera.targetTexture = TempTexture;
                    TargetCamera.Render();

                    WaveformRenderer.loop = false;

                    WaveformRenderer.enabled = false;
                }
            }
        }
    }

    void DrawWaves(Preset preset, float blendProgress)
    {
        if (preset.Waves.Count == 0)
        {
            return;
        }

        foreach (var CurrentWave in preset.Waves)
        {
            if (GetVariable(CurrentWave.BaseVariables, "enabled") == 0f)
            {
                continue;
            }

            CurrentWave.FrameVariables = new Dictionary<int, float>(CurrentWave.Variables);

            foreach (var v in CurrentWave.Variables.Keys)
            {
                SetVariable(preset.FrameVariables, v, CurrentWave.Variables[v]);
            }

            foreach (var v in CurrentWave.FrameMap.Keys)
            {
                SetVariable(CurrentWave.FrameVariables, v, CurrentWave.FrameMap[v]);
            }

            foreach (var v in preset.AfterFrameVariables.Keys)
            {
                SetVariable(CurrentWave.FrameVariables, v, preset.AfterFrameVariables[v]);
            }

            foreach (var v in CurrentWave.Inits.Keys)
            {
                SetVariable(CurrentWave.FrameVariables, v, CurrentWave.Inits[v]);
            }

            SetVariable(CurrentWave.FrameVariables, "frame", CurrentFrame);
            SetVariable(CurrentWave.FrameVariables, "time", CurrentTime);
            SetVariable(CurrentWave.FrameVariables, "fps", FPS);
            SetVariable(CurrentWave.FrameVariables, "bass", Bass);
            SetVariable(CurrentWave.FrameVariables, "bass_att", BassAtt);
            SetVariable(CurrentWave.FrameVariables, "mid", Mid);
            SetVariable(CurrentWave.FrameVariables, "mid_att", MidAtt);
            SetVariable(CurrentWave.FrameVariables, "treb", Treb);
            SetVariable(CurrentWave.FrameVariables, "treb_att", TrebAtt);
            SetVariable(CurrentWave.FrameVariables, "meshx", MeshSize.x);
            SetVariable(CurrentWave.FrameVariables, "meshy", MeshSize.y);
            SetVariable(CurrentWave.FrameVariables, "aspectx", 1f);
            SetVariable(CurrentWave.FrameVariables, "aspecty", 1f);
            SetVariable(CurrentWave.FrameVariables, "pixelsx", Resolution.x);
            SetVariable(CurrentWave.FrameVariables, "pixelsy", Resolution.y);

            CurrentWave.FrameEquationCompiled(CurrentWave.FrameVariables);

            int maxSamples = 512;
            int samples = Mathf.FloorToInt(Mathf.Min(GetVariable(CurrentWave.FrameVariables, "samples", maxSamples), maxSamples));

            int sep = Mathf.FloorToInt(GetVariable(CurrentWave.FrameVariables, "sep"));
            float scaling = GetVariable(CurrentWave.FrameVariables, "scaling");
            float spectrum = GetVariable(CurrentWave.FrameVariables, "spectrum");
            float smoothing = GetVariable(CurrentWave.FrameVariables, "smoothing");
            float usedots = GetVariable(CurrentWave.BaseVariables, "usedots");

            float frameR = GetVariable(CurrentWave.FrameVariables, "r");
            float frameG = GetVariable(CurrentWave.FrameVariables, "g");
            float frameB = GetVariable(CurrentWave.FrameVariables, "b");
            float frameA = GetVariable(CurrentWave.FrameVariables, "a");

            float waveScale = GetVariable(CurrentWave.FrameVariables, "wave_scale");

            samples -= sep;

            if (!(samples >= 2 || usedots != 0f && samples >= 1))
            {
                continue;
            }

            bool useSpectrum = spectrum != 0f;
            float scale = (useSpectrum ? 0.15f : 0.004f) * scaling * waveScale;

            float[] pointsLeft = useSpectrum ? freqArrayL : timeArrayL;
            float[] pointsRight = useSpectrum ? freqArrayR : timeArrayR;

            int j0 = useSpectrum ? 0 : Mathf.FloorToInt((maxSamples - samples) / 2f - sep / 2f);
            int j1 = useSpectrum ? 0 : Mathf.FloorToInt((maxSamples - samples) / 2f + sep / 2f);
            float t = useSpectrum ? (maxSamples - sep) / (float)samples : 1f;

            float mix1 = Mathf.Pow(smoothing * 0.98f, 0.5f);
            float mix2 = 1f - mix1;

            CurrentWave.PointsDataL[0] = pointsLeft[j0];
            CurrentWave.PointsDataR[0] = pointsRight[j1];

            for (int j = 1; j < samples; j++)
            {
                float left = pointsLeft[Mathf.FloorToInt(j * t + j0)];
                float right = pointsRight[Mathf.FloorToInt(j * t + j1)];

                CurrentWave.PointsDataL[j] = left * mix2 + CurrentWave.PointsDataL[j - 1] * mix1;
                CurrentWave.PointsDataR[j] = right * mix2 + CurrentWave.PointsDataR[j - 1] * mix1;
            }

            for (int j = samples - 2; j >= 0; j--)
            {
                CurrentWave.PointsDataL[j] = CurrentWave.PointsDataL[j] * mix2 + CurrentWave.PointsDataL[j + 1] * mix1;
                CurrentWave.PointsDataR[j] = CurrentWave.PointsDataR[j] * mix2 + CurrentWave.PointsDataR[j + 1] * mix1;
            }

            for (int j = 0; j < samples; j++)
            {
                CurrentWave.PointsDataL[j] *= scale;
                CurrentWave.PointsDataR[j] *= scale;
            }

            for (int j = 0; j < samples; j++)
            {
                float value1 = CurrentWave.PointsDataL[j];
                float value2 = CurrentWave.PointsDataR[j];

                SetVariable(CurrentWave.FrameVariables, "sample", j / (samples - 1f));
                SetVariable(CurrentWave.FrameVariables, "value1", value1);
                SetVariable(CurrentWave.FrameVariables, "value2", value2);
                SetVariable(CurrentWave.FrameVariables, "x", 0.5f + value1);
                SetVariable(CurrentWave.FrameVariables, "y", 0.5f + value2);
                SetVariable(CurrentWave.FrameVariables, "r", frameR);
                SetVariable(CurrentWave.FrameVariables, "g", frameG);
                SetVariable(CurrentWave.FrameVariables, "b", frameB);
                SetVariable(CurrentWave.FrameVariables, "a", frameA);

                if (!string.IsNullOrEmpty(CurrentWave.PointEquation))
                {
                    CurrentWave.PointEquationCompiled(CurrentWave.FrameVariables);
                }

                float x = GetVariable(CurrentWave.FrameVariables, "x") * 2f - 1f;
                float y = GetVariable(CurrentWave.FrameVariables, "y") * 2f - 1f;

                float r = GetVariable(CurrentWave.FrameVariables, "r");
                float g = GetVariable(CurrentWave.FrameVariables, "g");
                float b = GetVariable(CurrentWave.FrameVariables, "b");
                float a = GetVariable(CurrentWave.FrameVariables, "a");

                CurrentWave.Positions[j] = new Vector3(x, y, 0f);
                CurrentWave.Colors[j] = new Color(r, g, b, a * blendProgress);
            }

            bool thick = GetVariable(CurrentWave.FrameVariables, "thick") != 0f;

            if (usedots != 0f)
            {
                DotParent.gameObject.SetActive(true);

                Vector3 outOfBounds = new Vector3(0f, 0f, -10f);

                float aspect_ratio = Resolution.x / (float)Resolution.y;

                for (int i = 0; i < MaxSamples * 4; i++)
                {
                    if (i < samples)
                    {
                        Dots[i].localPosition = new Vector3(CurrentWave.Positions[i].x * aspect_ratio, -CurrentWave.Positions[i].y, 0f);
                        DotRenderers[i].color = CurrentWave.Colors[i];
                        Dots[i].localScale = thick ? baseDotScale : baseDotScale * 0.5f;
                    }
                    else
                    {
                        Dots[i].localPosition = outOfBounds;
                    }
                }

                TargetMeshFilter.sharedMesh = TargetMeshWarp;
                TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

                DoNothingMaterial.mainTexture = TempTexture;

                TargetCamera.targetTexture = TempTexture;
                TargetCamera.Render();

                DotParent.gameObject.SetActive(false);
            }
            else
            {
                SmoothWaveAndColor(CurrentWave.Positions, CurrentWave.Colors, CurrentWave.SmoothedPositions, CurrentWave.SmoothedColors, samples);

                WaveformRenderer.enabled = true;

                if (thick)
                {
                    WaveformRenderer.widthMultiplier = 2f;
                }
                else
                {
                    WaveformRenderer.widthMultiplier = 0.5f;
                }

                WaveformRenderer.sharedMaterial.mainTexture = TempTexture;
                WaveformRenderer.sharedMaterial.SetColor("waveColor", Color.white);
                WaveformRenderer.sharedMaterial.SetFloat("additivewave", GetVariable(CurrentWave.FrameVariables, "additive"));
                WaveformRenderer.sharedMaterial.SetFloat("aspect_ratio", Resolution.x / (float)Resolution.y);

                TargetMeshFilter.sharedMesh = TargetMeshWarp;
                TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

                DoNothingMaterial.mainTexture = TempTexture;

                TargetCamera.targetTexture = TempTexture;

                samples = samples * 2 - 1;

                int iterations = Mathf.CeilToInt(samples / 8f);

                for (int i = 0; i < iterations; i++)
                {
                    int start = i * 8;
                    int end = Mathf.Min(samples, start + 8);

                    WaveformRenderer.positionCount = end - start;

                    for (int j = start; j < end; j++)
                    {
                        WaveformRenderer.SetPosition(j - start, CurrentWave.SmoothedPositions[j]);
                    }

                    var grad = new Gradient();

                    var colorKeys = new GradientColorKey[end - start];
                    var alphaKeys = new GradientAlphaKey[end - start];

                    for (int j = start; j < end; j++)
                    {
                        colorKeys[j - start] = new GradientColorKey(CurrentWave.SmoothedColors[j], j / (float)(end - start - 1));
                        alphaKeys[j - start] = new GradientAlphaKey(CurrentWave.SmoothedColors[j].a, j / (float)(end - start - 1));
                    }

                    grad.colorKeys = colorKeys;
                    grad.alphaKeys = alphaKeys;

                    WaveformRenderer.colorGradient = grad;

                    TargetCamera.Render();
                }

                WaveformRenderer.enabled = false;
            }
        }
    }

    void DrawWarp(Preset preset, bool blending)
    {
        if (preset.WarpMaterial == null)
        {
            return;
        }

        TargetMeshFilter.sharedMesh = TargetMeshWarp;
        TargetMeshWarp.SetUVs(0, WarpUVs);
        TargetMeshWarp.SetColors(WarpColor);

        //(float[], float[]) blurValues = GetBlurValues(preset.FrameVariables);

        TargetMeshRenderer.sharedMaterial = preset.WarpMaterial;

        preset.WarpMaterial.mainTexture = blending ? TempTexture : PrevTempTexture;

        /*preset.WarpMaterial.SetTexture("_MainTex2", FinalTexture);
        preset.WarpMaterial.SetTexture("_MainTex3", FinalTexture);
        preset.WarpMaterial.SetTexture("_MainTex4", FinalTexture);
        preset.WarpMaterial.SetTexture("_MainTex5", FinalTexture);*/

        // sampler_blur1
        // sampler_blur2
        // sampler_blur3

        // sampler_noise_lq
        // sampler_noise_mq
        // sampler_noise_hq
        // sampler_noise_lq_lite
        // sampler_pw_noise_lq
        // sampler_noisevol_lq
        // sampler_noisevol_hq

        // user textures

        preset.WarpMaterial.SetFloat("decay", GetVariable(preset.FrameVariables, "decay"));
        /*preset.WarpMaterial.SetVector("resolution", new Vector2(Resolution.x, Resolution.y));
        preset.WarpMaterial.SetVector("aspect", new Vector4(1f, 1f, 1f, 1f));
        preset.WarpMaterial.SetVector("texsize", new Vector4(Resolution.x, Resolution.y, 1f / Resolution.x, 1f / Resolution.y));
        preset.WarpMaterial.SetVector("texsize_noise_lq", new Vector4(256, 256, 1f / 256f, 1f / 256f));
        preset.WarpMaterial.SetVector("texsize_noise_mq", new Vector4(256, 256, 1f / 256f, 1f / 256));
        preset.WarpMaterial.SetVector("texsize_noise_hq", new Vector4(256, 256, 1f / 256f, 1f / 256f));
        preset.WarpMaterial.SetVector("texsize_noise_lq_lite", new Vector4(32, 32, 1f / 32f, 1f / 32f));
        preset.WarpMaterial.SetVector("texsize_noisevol_lq", new Vector4(32, 32, 1f / 32f, 1f / 32f));
        preset.WarpMaterial.SetVector("texsize_noisevol_hq", new Vector4(32, 32, 1f / 32f, 1f / 32f));
        preset.WarpMaterial.SetFloat("bass", Bass);
        preset.WarpMaterial.SetFloat("mid", Mid);
        preset.WarpMaterial.SetFloat("treb", Treb);
        preset.WarpMaterial.SetFloat("vol", (Bass + Mid + Treb) / 3f);
        preset.WarpMaterial.SetFloat("bass_att", BassAtt);
        preset.WarpMaterial.SetFloat("mid_att", MidAtt);
        preset.WarpMaterial.SetFloat("treb_att", TrebAtt);
        preset.WarpMaterial.SetFloat("vol_att", (BassAtt + MidAtt + TrebAtt) / 3f);
        preset.WarpMaterial.SetFloat("time", CurrentTime);
        preset.WarpMaterial.SetFloat("frame", FrameNum);
        preset.WarpMaterial.SetFloat("fps", FPS);
        preset.WarpMaterial.SetVector("rand_preset", 
            new Vector4(
                GetVariable(preset.FrameVariables, "rand_preset.x"),
                GetVariable(preset.FrameVariables, "rand_preset.y"),
                GetVariable(preset.FrameVariables, "rand_preset.z"),
                GetVariable(preset.FrameVariables, "rand_preset.w")
            )
        );
        preset.WarpMaterial.SetVector("rand_frame", 
            new Vector4(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f)
            )
        );
        preset.WarpMaterial.SetVector("_qa", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q1"),
                GetVariable(preset.AfterFrameVariables, "q2"),
                GetVariable(preset.AfterFrameVariables, "q3"),
                GetVariable(preset.AfterFrameVariables, "q4")
            )
        );
        preset.WarpMaterial.SetVector("_qb", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q5"),
                GetVariable(preset.AfterFrameVariables, "q6"),
                GetVariable(preset.AfterFrameVariables, "q7"),
                GetVariable(preset.AfterFrameVariables, "q8")
            )
        );
        preset.WarpMaterial.SetVector("_qc", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q9"),
                GetVariable(preset.AfterFrameVariables, "q10"),
                GetVariable(preset.AfterFrameVariables, "q11"),
                GetVariable(preset.AfterFrameVariables, "q12")
            )
        );
        preset.WarpMaterial.SetVector("_qd", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q13"),
                GetVariable(preset.AfterFrameVariables, "q14"),
                GetVariable(preset.AfterFrameVariables, "q15"),
                GetVariable(preset.AfterFrameVariables, "q16")
            )
        );
        preset.WarpMaterial.SetVector("_qe", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q17"),
                GetVariable(preset.AfterFrameVariables, "q18"),
                GetVariable(preset.AfterFrameVariables, "q19"),
                GetVariable(preset.AfterFrameVariables, "q20")
            )
        );
        preset.WarpMaterial.SetVector("_qf", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q21"),
                GetVariable(preset.AfterFrameVariables, "q22"),
                GetVariable(preset.AfterFrameVariables, "q23"),
                GetVariable(preset.AfterFrameVariables, "q24")
            )
        );
        preset.WarpMaterial.SetVector("_qg", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q25"),
                GetVariable(preset.AfterFrameVariables, "q26"),
                GetVariable(preset.AfterFrameVariables, "q27"),
                GetVariable(preset.AfterFrameVariables, "q28")
            )
        );
        preset.WarpMaterial.SetVector("_qh", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q29"),
                GetVariable(preset.AfterFrameVariables, "q30"),
                GetVariable(preset.AfterFrameVariables, "q31"),
                GetVariable(preset.AfterFrameVariables, "q32")
            )
        );
        preset.WarpMaterial.SetVector("slow_roam_cos", 
            new Vector4(
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.005f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.008f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.013f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.022f)
            )
        );
        preset.WarpMaterial.SetVector("roam_cos", 
            new Vector4(
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.3f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 1.3f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 5.0f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 20.0f)
            )
        );
        preset.WarpMaterial.SetVector("slow_roam_sin", 
            new Vector4(
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.005f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.008f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.013f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.022f)
            )
        );
        preset.WarpMaterial.SetVector("roam_sin", 
            new Vector4(
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.3f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 1.3f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 5.0f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 20.0f)
            )
        );

        float blurMin1 = blurValues.Item1[0];
        float blurMin2 = blurValues.Item1[1];
        float blurMin3 = blurValues.Item1[2];
        float blurMax1 = blurValues.Item2[0];
        float blurMax2 = blurValues.Item2[1];
        float blurMax3 = blurValues.Item2[2];

        float scale1 = blurMax1 - blurMin1;
        float bias1 = blurMin1;

        float scale2 = blurMax2 - blurMin2;
        float bias2 = blurMin2;

        float scale3 = blurMax3 - blurMin3;
        float bias3 = blurMin3;

        preset.WarpMaterial.SetFloat("blur1_min", blurMin1);
        preset.WarpMaterial.SetFloat("blur1_max", blurMax1);
        preset.WarpMaterial.SetFloat("blur2_min", blurMin2);
        preset.WarpMaterial.SetFloat("blur2_max", blurMax2);
        preset.WarpMaterial.SetFloat("blur3_min", blurMin3);
        preset.WarpMaterial.SetFloat("blur3_max", blurMax3);
        preset.WarpMaterial.SetFloat("scale1", scale1);
        preset.WarpMaterial.SetFloat("scale2", scale2);
        preset.WarpMaterial.SetFloat("scale3", scale3);
        preset.WarpMaterial.SetFloat("bias1", bias1);
        preset.WarpMaterial.SetFloat("bias2", bias2);
        preset.WarpMaterial.SetFloat("bias3", bias3);*/

        TargetCamera.targetTexture = TempTexture;
        TargetCamera.Render();
    }

    void DrawDarkenCenter()
    {
        if (CurrentPreset.DarkenCenterMaterial == null)
        {
            return;
        }

        if (GetVariable(CurrentPreset.FrameVariables, "darken_center") == 0f)
        {
            return;
        }

        TargetMeshFilter.sharedMesh = TargetMeshDarkenCenter;

        TargetMeshRenderer.sharedMaterial = CurrentPreset.DarkenCenterMaterial;

        CurrentPreset.DarkenCenterMaterial.mainTexture = TempTexture;

        TargetCamera.targetTexture = TempTexture;
        TargetCamera.Render();
    }

    void DrawOuterBorder()
    {
        Color outerColor = new Color
        (
            GetVariable(CurrentPreset.FrameVariables, "ob_r"),
            GetVariable(CurrentPreset.FrameVariables, "ob_g"),
            GetVariable(CurrentPreset.FrameVariables, "ob_b"),
            GetVariable(CurrentPreset.FrameVariables, "ob_a")
        );

        float borderSize = GetVariable(CurrentPreset.FrameVariables, "ob_size");

        DrawBorder(outerColor, borderSize, 0f);
    }

    void DrawInnerBorder()
    {
        Color innerColor = new Color
        (
            GetVariable(CurrentPreset.FrameVariables, "ib_r"),
            GetVariable(CurrentPreset.FrameVariables, "ib_g"),
            GetVariable(CurrentPreset.FrameVariables, "ib_b"),
            GetVariable(CurrentPreset.FrameVariables, "ib_a")
        );

        float borderSize = GetVariable(CurrentPreset.FrameVariables, "ib_size");
        float prevBorderSize = GetVariable(CurrentPreset.FrameVariables, "ob_size");

        DrawBorder(innerColor, borderSize, prevBorderSize);
    }

    void DrawBorder(Color borderColor, float borderSize, float prevBorderSize)
    {
        if (borderSize == 0f || borderColor.a == 0f)
        {
            return;
        }

        BorderSideLeft.gameObject.SetActive(true);
        BorderSideRight.gameObject.SetActive(true);
        BorderSideTop.gameObject.SetActive(true);
        BorderSideBottom.gameObject.SetActive(true);

        BorderSideLeft.localPosition = new Vector3(-1f + prevBorderSize + borderSize * 0.5f, 0f, 0f);
        BorderSideRight.localPosition = new Vector3(1f - prevBorderSize - borderSize * 0.5f, 0f, 0f);
        BorderSideTop.localPosition = new Vector3(0f, 1f - prevBorderSize - borderSize * 0.5f, 0f);
        BorderSideBottom.localPosition = new Vector3(0f, -1f + prevBorderSize + borderSize * 0.5f, 0f);

        BorderSideLeft.localScale = new Vector3(borderSize, 1f, 2f - (prevBorderSize + borderSize) * 2f) * 0.1f;
        BorderSideRight.localScale = new Vector3(borderSize, 1f, 2f - (prevBorderSize + borderSize) * 2f) * 0.1f;
        BorderSideTop.localScale = new Vector3(2f - prevBorderSize * 2f, 1f, borderSize) * 0.1f;
        BorderSideBottom.localScale = new Vector3(2f - prevBorderSize * 2f, 1f, borderSize) * 0.1f;

        BorderMaterial.SetColor("borderColor", borderColor);

        TargetMeshFilter.sharedMesh = TargetMeshWarp;
        TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

        DoNothingMaterial.mainTexture = TempTexture;

        TargetCamera.targetTexture = TempTexture;
        TargetCamera.Render();

        BorderSideLeft.gameObject.SetActive(false);
        BorderSideRight.gameObject.SetActive(false);
        BorderSideTop.gameObject.SetActive(false);
        BorderSideBottom.gameObject.SetActive(false);
    }

    Vector2 GetMotionDir(float fx, float fy)
    {
        int y0 = Mathf.FloorToInt(fy * MeshSize.y);
        float dy = fy * MeshSize.y - y0;

        int x0 = Mathf.FloorToInt(fx * MeshSize.x);
        float dx = fx * MeshSize.x - x0;

        int x1 = x0 + 1;
        int y1 = y0 + 1;

        int gridX1 = MeshSize.x + 1;

        float fx2;
        float fy2;
        fx2 = WarpUVs[y0 * gridX1 + x0].x * (1 - dx) * (1 - dy);
        fy2 = WarpUVs[y0 * gridX1 + x0].y * (1 - dx) * (1 - dy);
        fx2 += WarpUVs[y0 * gridX1 + x1].x * dx * (1 - dy);
        fy2 += WarpUVs[y0 * gridX1 + x1].y * dx * (1 - dy);
        fx2 += WarpUVs[y1 * gridX1 + x0].x * (1 - dx) * dy;
        fy2 += WarpUVs[y1 * gridX1 + x0].y * (1 - dx) * dy;
        fx2 += WarpUVs[y1 * gridX1 + x1].x * dx * dy;
        fy2 += WarpUVs[y1 * gridX1 + x1].y * dx * dy;

        return new Vector2(fx2, 1f - fy2);
    }

    void DrawMotionVectors()
    {
        float mvOn = GetVariable(CurrentPreset.FrameVariables, "bmotionvectorson");
        float mvA = mvOn == 0f ? 0f : GetVariable(CurrentPreset.FrameVariables, "mv_a");

        float mv_x = GetVariable(CurrentPreset.FrameVariables, "mv_x");
        float mv_y = GetVariable(CurrentPreset.FrameVariables, "mv_y");

        int nX = Mathf.FloorToInt(mv_x);
        int nY = Mathf.FloorToInt(mv_y);

        if (mvA <= 0.001f || nX <= 0f || nY <= 0f)
        {
            return;
        }

        float dx = mv_x - nX;
        float dy = mv_y - nY;

        if (nX > MotionVectorsSize.x)
        {
            nX = MotionVectorsSize.x;
            dx = 0f;
        }

        if (nY > MotionVectorsSize.y)
        {
            nY = MotionVectorsSize.y;
            dy = 0f;
        }

        float dx2 = GetVariable(CurrentPreset.FrameVariables, "mv_dx");
        float dy2 = GetVariable(CurrentPreset.FrameVariables, "mv_dy");

        float lenMult = GetVariable(CurrentPreset.FrameVariables, "mv_l");

        float minLen = 1f / Resolution.x;

        int numVecVerts = 0;

        for (int j = 0; j < nY; j++)
        {
            float fy = (j + 0.25f) / (nY + dy + 0.25f - 1.0f);
            fy -= dy2;

            if (fy > 0.0001f && fy < 0.9999f)
            {
                for (int i = 0; i < nX; i++)
                {
                    float fx = (i + 0.25f) / (nX + dx + 0.25f - 1.0f);
                    fx += dx2;

                    if (fx > 0.0001f && fx < 0.9999f)
                    {
                        Vector2 fx2arr = GetMotionDir(fx, fy);
                        float fx2 = fx2arr.x;
                        float fy2 = fx2arr.y;

                        float dxi = fx2 - fx;
                        float dyi = fy2 - fy;
                        dxi *= lenMult;
                        dyi *= lenMult;

                        float fdist = Mathf.Sqrt(dxi * dxi + dyi * dyi);

                        if (fdist < minLen && fdist > 0.00000001f)
                        {
                            fdist = minLen / fdist;
                            dxi *= fdist;
                            dyi *= fdist;
                        } else
                        {
                            dxi = minLen;
                            dxi = minLen;
                        }

                        fx2 = fx + dxi;
                        fy2 = fy + dyi;

                        float vx1 = 2.0f * fx - 1.0f;
                        float vy1 = 2.0f * fy - 1.0f;
                        float vx2 = 2.0f * fx2 - 1.0f;
                        float vy2 = 2.0f * fy2 - 1.0f;

                        MotionVectorsPositions[numVecVerts] = new Vector3(vx1, vy1, 0f);
                        MotionVectorsPositions[numVecVerts + 1] = new Vector3(vx2, vy2, 0f);

                        numVecVerts += 2;
                    }
                }
            }
        }

        if (numVecVerts == 0)
        {
            return;
        }

        Color color = new Color
        (
            GetVariable(CurrentPreset.FrameVariables, "mv_r"),
            GetVariable(CurrentPreset.FrameVariables, "mv_g"), 
            GetVariable(CurrentPreset.FrameVariables, "mv_b"),
            mvA
        );

        MotionVectorParent.gameObject.SetActive(true);

        float aspect_ratio = Resolution.x / (float)Resolution.y;

        Vector3 outOfBounds = new Vector3(0f, 0f, -10f);
        int half = numVecVerts / 2;

        for (int i = 0; i < MotionVectors.Length; i++)
        {
            if (i < half)
            {
                Vector3 pos1 = new Vector3(MotionVectorsPositions[i * 2].x * aspect_ratio, MotionVectorsPositions[i * 2].y, 0f);
                Vector3 pos2 = new Vector3(MotionVectorsPositions[i * 2 + 1].x * aspect_ratio, MotionVectorsPositions[i * 2 + 1].y, 0f);
                Vector3 dir = pos2 - pos1;
                float distance = dir.magnitude;
                MotionVectors[i].localPosition = Vector3.Lerp(pos1, pos2, 0.5f);
                MotionVectors[i].localScale = new Vector3(distance, 0.0017f, 0.0017f);
                MotionVectors[i].right = dir.normalized;
                MotionVectorRenderers[i].color = color;
            }
            else
            {
                MotionVectors[i].localPosition = outOfBounds;
            }
        }

        TargetMeshFilter.sharedMesh = TargetMeshWarp;
        TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

        DoNothingMaterial.mainTexture = TempTexture;

        TargetCamera.targetTexture = TempTexture;
        TargetCamera.Render();

        MotionVectorParent.gameObject.SetActive(false);
    }

    void DrawBasicWaveform()
    {
        float alpha = GetVariable(CurrentPreset.FrameVariables, "wave_a");

        float vol = (Bass + Mid + Treb) / 3f;

        if (vol <= -0.01f || alpha <= 0.001f || timeArrayL.Length == 0f)
        {
            return;
        }

        float scale = GetVariable(CurrentPreset.FrameVariables, "wave_scale") / 128f;
        float smooth = GetVariable(CurrentPreset.FrameVariables, "wave_smoothing");
        float smooth2 = scale * (1f - smooth);

        List<float> waveL = new List<float>();

        waveL.Add(timeArrayL[0] * scale);

        for (int i = 1; i < timeArrayL.Length; i++)
        {
            waveL.Add(timeArrayL[i] * smooth2 + waveL[i - 1] * smooth);
        }

        List<float> waveR = new List<float>();

        waveR.Add(timeArrayR[0] * scale);

        for (int i = 1; i < timeArrayR.Length; i++)
        {
            waveR.Add(timeArrayR[i] * smooth2 + waveR[i - 1] * smooth);
        }

        int newWaveMode = Mathf.FloorToInt(GetVariable(CurrentPreset.FrameVariables, "wave_mode")) % 8;
        int oldWaveMode = Mathf.FloorToInt(GetVariable(CurrentPreset.FrameVariables, "old_wave_mode")) % 8;

        float wavePosX = GetVariable(CurrentPreset.FrameVariables, "wave_x") * 2f - 1f;
        float wavePosY = GetVariable(CurrentPreset.FrameVariables, "wave_y") * 2f - 1f;

        int numVert = 0;
        int oldNumVert = 0;

        float globalAlpha = 0f;
        float globalAlphaOld = 0f;

        int its = blending && newWaveMode != oldWaveMode ? 2 : 1;

        for (int it = 0; it < its; it++)
        {
            int waveMode = (it == 0) ? newWaveMode : oldWaveMode;

            float fWaveParam2 = GetVariable(CurrentPreset.FrameVariables, "wave_mystery");

            if (
                (waveMode == 0 || waveMode == 1 || waveMode == 4) &&
                (fWaveParam2 < -1 || fWaveParam2 > 1)
            )
            {
                fWaveParam2 = fWaveParam2 * 0.5f + 0.5f;
                fWaveParam2 -= Mathf.Floor(fWaveParam2);
                fWaveParam2 = Mathf.Abs(fWaveParam2);
                fWaveParam2 = fWaveParam2 * 2f - 1f;
            }

            int localNumVert = 0;

            Vector3[] positions;
            Vector3[] positions2;

            if (it == 0)
            {
                positions = BasicWaveFormPositions;
                positions2 = BasicWaveFormPositions2;
            }
            else
            {
                positions = BasicWaveFormPositionsOld;
                positions2 = BasicWaveFormPositions2Old;
            }
            
            alpha = GetVariable(CurrentPreset.FrameVariables, "wave_a");

            if (waveMode == 0)
            {
                if (GetVariable(CurrentPreset.FrameVariables, "modwavealphabyvolume") > 0f)
                {
                    float alphaDiff = GetVariable(CurrentPreset.FrameVariables, "modwavealphaend") - GetVariable(CurrentPreset.FrameVariables, "modwavealphastart");
                    alpha *= (vol - GetVariable(CurrentPreset.FrameVariables, "modwavealphastart")) / alphaDiff;
                }
                alpha = Mathf.Clamp01(alpha);
                
                localNumVert = Mathf.FloorToInt(waveL.Count / 2f) + 1;
                float numVertInv = 1f / (localNumVert - 1f);
                int sampleOffset = Mathf.FloorToInt((waveL.Count - localNumVert) / 2f);

                for (int i = 0; i < localNumVert - 1; i++)
                {
                    float rad = 0.5f + 0.4f * waveR[i + sampleOffset] + fWaveParam2;
                    float ang = i * numVertInv * 2f * Mathf.PI + CurrentTime * 0.2f;

                    if (i < localNumVert / 10f)
                    {
                        float mix = i / (localNumVert * 0.1f);
                        mix = 0.5f - 0.5f * Mathf.Cos(mix * Mathf.PI);
                        float rad2 = 0.5f + 0.4f * waveR[i + localNumVert + sampleOffset] + fWaveParam2;
                        rad = (1f - mix) * rad2 + rad * mix;
                    }

                    positions[i] = new Vector3
                    (
                        rad * Mathf.Cos(ang) * 1f + wavePosX,
                        rad * Mathf.Sin(ang) * 1f + wavePosY,
                        0f
                    );
                }

                positions[localNumVert - 1] = positions[0];
            }
            else if (waveMode == 1)
            {
                alpha *= 1.25f;
                if (GetVariable(CurrentPreset.FrameVariables, "modwavealphabyvolume") > 0f)
                {
                    float alphaDiff = GetVariable(CurrentPreset.FrameVariables, "modwavealphaend") - GetVariable(CurrentPreset.FrameVariables, "modwavealphastart");
                    alpha *= (vol - GetVariable(CurrentPreset.FrameVariables, "modwavealphastart")) / alphaDiff;
                }
                alpha = Mathf.Clamp01(alpha);

                localNumVert = Mathf.FloorToInt(waveL.Count / 2f);

                for (int i = 0; i < localNumVert - 1; i++)
                {
                    float rad = 0.53f + 0.43f * waveR[i] + fWaveParam2;
                    float ang = waveL[i + 32] * 0.5f * Mathf.PI + CurrentTime * 2.3f;

                    positions[i] = new Vector3
                    (
                        rad * Mathf.Cos(ang) * 1f + wavePosX,
                        rad * Mathf.Sin(ang) * 1f + wavePosY,
                        0f
                    );
                }
            }
            else if (waveMode == 2)
            {
                if (Resolution.x < 1024)
                {
                    alpha *= 0.09f;
                }
                else if (Resolution.x >= 1024 && Resolution.x < 2048)
                {
                    alpha *= 0.11f;
                }
                else
                {
                    alpha *= 0.13f;
                }

                if (GetVariable(CurrentPreset.FrameVariables, "modwavealphabyvolume") > 0f)
                {
                    float alphaDiff = GetVariable(CurrentPreset.FrameVariables, "modwavealphaend") - GetVariable(CurrentPreset.FrameVariables, "modwavealphastart");
                    alpha *= (vol - GetVariable(CurrentPreset.FrameVariables, "modwavealphastart")) / alphaDiff;
                }
                alpha = Mathf.Clamp01(alpha);

                localNumVert = waveL.Count;

                for (int i = 0; i < waveL.Count; i++)
                {
                    positions[i] = new Vector3
                    (
                        waveR[i] * 1f + wavePosX,
                        waveL[(i + 32) % waveL.Count] * 1f + wavePosY,
                        0f
                    );
                }
            }
            else if (waveMode == 3)
            {
                if (Resolution.x < 1024)
                {
                    alpha *= 0.15f;
                }
                else if (Resolution.x >= 1024 && Resolution.x < 2048)
                {
                    alpha *= 0.22f;
                }
                else
                {
                    alpha *= 0.33f;
                }

                alpha *= 1.3f;
                alpha *= Treb * Treb;

                if (GetVariable(CurrentPreset.FrameVariables, "modwavealphabyvolume") > 0f)
                {
                    float alphaDiff = GetVariable(CurrentPreset.FrameVariables, "modwavealphaend") - GetVariable(CurrentPreset.FrameVariables, "modwavealphastart");
                    alpha *= (vol - GetVariable(CurrentPreset.FrameVariables, "modwavealphastart")) / alphaDiff;
                }
                alpha = Mathf.Clamp01(alpha);

                localNumVert = waveL.Count;

                for (int i = 0; i < waveL.Count; i++)
                {
                    positions[i] = new Vector3
                    (
                        waveR[i] * 1f + wavePosX,
                        waveL[(i + 32) % waveL.Count] * 1f + wavePosY,
                        0f
                    );
                }
            }
            else if (waveMode == 4)
            {
                if (GetVariable(CurrentPreset.FrameVariables, "modwavealphabyvolume") > 0f)
                {
                    float alphaDiff = GetVariable(CurrentPreset.FrameVariables, "modwavealphaend") - GetVariable(CurrentPreset.FrameVariables, "modwavealphastart");
                    alpha *= (vol - GetVariable(CurrentPreset.FrameVariables, "modwavealphastart")) / alphaDiff;
                }
                alpha = Mathf.Clamp01(alpha);

                localNumVert = waveL.Count;

                if (localNumVert > Resolution.x / 3f)
                {
                    localNumVert = Mathf.FloorToInt(Resolution.x / 3f);
                }

                float numVertInv = 1f / localNumVert;
                int sampleOffset = Mathf.FloorToInt((waveL.Count - localNumVert) / 2f);

                float w1 = 0.45f + 0.5f * (fWaveParam2 * 0.5f + 0.5f);
                float w2 = 1.0f - w1;

                for (int i = 0; i < localNumVert; i++)
                {
                    float x =
                        2.0f * i * numVertInv +
                        (wavePosX - 1) +
                        waveR[(i + 25 + sampleOffset) % waveL.Count] * 0.44f;
                    float y = waveL[i + sampleOffset] * 0.47f + wavePosY;

                    if (i > 1)
                    {
                        x =
                            x * w2 +
                            w1 *
                                (positions[i - 1].x * 2f -
                                positions[i - 2].x);
                        y =
                            y * w2 +
                            w1 *
                                (positions[i - 1].y * 2f -
                                positions[i - 2].y);
                    }

                    positions[i] = new Vector3(x, y, 0f);
                }
            }
            else if (waveMode == 5)
            {
                if (Resolution.x < 1024)
                {
                    alpha *= 0.09f;
                }
                else if (Resolution.x >= 1024 && Resolution.x < 2048)
                {
                    alpha *= 0.11f;
                }
                else
                {
                    alpha *= 0.13f;
                }

                if (GetVariable(CurrentPreset.FrameVariables, "modwavealphabyvolume") > 0f)
                {
                    float alphaDiff = GetVariable(CurrentPreset.FrameVariables, "modwavealphaend") - GetVariable(CurrentPreset.FrameVariables, "modwavealphastart");
                    alpha *= (vol - GetVariable(CurrentPreset.FrameVariables, "modwavealphastart")) / alphaDiff;
                }
                alpha = Mathf.Clamp01(alpha);

                float cosRot = Mathf.Cos(CurrentTime * 0.3f);
                float sinRot = Mathf.Sin(CurrentTime * 0.3f);

                localNumVert = waveL.Count;

                for (int i = 0; i < waveL.Count; i++)
                {
                    int ioff = (i + 32) % waveL.Count;
                    float x0 = waveR[i] * waveL[ioff] + waveL[i] * waveR[ioff];
                    float y0 = waveR[i] * waveR[i] - waveL[ioff] * waveL[ioff];

                    positions[i] = new Vector3
                    (
                        (x0 * cosRot - y0 * sinRot) * (1f + wavePosX),
                        (x0 * sinRot + y0 * cosRot) * (1f + wavePosY),
                        0f
                    );
                }
            }
            else if (waveMode == 6 || waveMode == 7)
            {
                if (GetVariable(CurrentPreset.FrameVariables, "modwavealphabyvolume") > 0f)
                {
                    float alphaDiff = GetVariable(CurrentPreset.FrameVariables, "modwavealphaend") - GetVariable(CurrentPreset.FrameVariables, "modwavealphastart");
                    alpha *= (vol - GetVariable(CurrentPreset.FrameVariables, "modwavealphastart")) / alphaDiff;
                }
                alpha = Mathf.Clamp01(alpha);

                localNumVert = Mathf.FloorToInt(waveL.Count / 2f);

                if (localNumVert > Resolution.x / 3f)
                {
                    localNumVert = Mathf.FloorToInt(Resolution.x / 3f);
                }

                int sampleOffset = Mathf.FloorToInt((waveL.Count - localNumVert) / 2f);
                float ang = Mathf.PI * 0.5f * fWaveParam2;
                float dx = Mathf.Cos(ang);
                float dy = Mathf.Sin(ang);

                float[] edgex = new float[]
                {
                    wavePosX * Mathf.Cos(ang + Mathf.PI * 0.5f) - dx * 3.0f,
                    wavePosX * Mathf.Cos(ang + Mathf.PI * 0.5f) + dx * 3.0f
                };

                float[] edgey = new float[]
                {
                    wavePosX * Mathf.Sin(ang + Mathf.PI * 0.5f) - dy * 3.0f,
                    wavePosX * Mathf.Sin(ang + Mathf.PI * 0.5f) + dy * 3.0f
                };

                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        float t = 0f;
                        bool bClip = false;

                        switch (j)
                        {
                            case 0:
                                if (edgex[i] > 1.1f)
                                {
                                    t = (1.1f - edgex[1 - i]) / (edgex[i] - edgex[1 - i]);
                                    bClip = true;
                                }
                                break;
                            case 1:
                                if (edgex[i] < -1.1f)
                                {
                                    t = (-1.1f - edgex[1 - i]) / (edgex[i] - edgex[1 - i]);
                                    bClip = true;
                                }
                                break;
                            case 2:
                                if (edgey[i] > 1.1f)
                                {
                                    t = (1.1f - edgey[1 - i]) / (edgey[i] - edgey[1 - i]);
                                    bClip = true;
                                }
                                break;
                            case 3:
                                if (edgey[i] < -1.1f)
                                {
                                    t = (-1.1f - edgey[1 - i]) / (edgey[i] - edgey[1 - i]);
                                    bClip = true;
                                }
                                break;
                        }

                        if (bClip)
                        {
                            float dxi = edgex[i] - edgex[1 - i];
                            float dyi = edgey[i] - edgey[1 - i];
                            edgex[i] = edgex[1 - i] + dxi * t;
                            edgey[i] = edgey[1 - i] + dyi * t;
                        }
                    }
                }

                dx = (edgex[1] - edgex[0]) / localNumVert;
                dy = (edgey[1] - edgey[0]) / localNumVert;

                float ang2 = Mathf.Atan2(dy, dx);
                float perpDx = Mathf.Cos(ang2 + Mathf.PI * 0.5f);
                float perpDy = Mathf.Sin(ang2 + Mathf.PI * 0.5f);

                if (waveMode == 6)
                {
                    for (int i = 0; i < localNumVert; i++)
                    {
                        float sample = waveL[i + sampleOffset];
                        positions[i] = new Vector3
                        (
                            edgex[0] + dx * i + perpDx * 0.25f * sample,
                            edgey[0] + dy * i + perpDy * 0.25f * sample,
                            0f
                        );
                    }
                }
                else
                {
                    float sep = Mathf.Pow(wavePosY * 0.5f + 0.5f, 2);

                    for (int i = 0; i < localNumVert; i++)
                    {
                        float sample = waveL[i + sampleOffset];
                        positions[i] = new Vector3
                        (
                            edgex[0] + dx * i + perpDx * (0.25f * sample + sep),
                            edgey[0] + dy * i + perpDy * (0.25f * sample + sep),
                            0f
                        );
                    }

                    for (int i = 0; i < localNumVert; i++)
                    {
                        float sample = waveR[i + sampleOffset];
                        positions2[i] = new Vector3
                        (
                            edgex[0] + dx * i + perpDx * (0.25f * sample - sep),
                            edgey[0] + dy * i + perpDy * (0.25f * sample - sep),
                            0f
                        );
                    }
                }
            }
            else
            {
                return;
            }

            if (it == 0)
            {
                BasicWaveFormPositions = positions;
                BasicWaveFormPositions2 = positions2;
                numVert = localNumVert;
                globalAlpha = alpha;
            }
            else
            {
                BasicWaveFormPositionsOld = positions;
                BasicWaveFormPositions2Old = positions2;
                oldNumVert = localNumVert;
                globalAlphaOld = alpha;
            }
        }

        if (numVert == 0)
        {
            throw new Exception("No waveform positions set");
        }

        float blendMix = 0.5f - 0.5f * Mathf.Cos(blendProgress * Mathf.PI);
        float blendMix2 = 1f - blendMix;

        if (oldNumVert > 0)
        {
            alpha = blendMix * globalAlpha + blendMix2 * globalAlphaOld;
        }

        float r = Mathf.Clamp01(GetVariable(CurrentPreset.FrameVariables, "wave_r"));
        float g = Mathf.Clamp01(GetVariable(CurrentPreset.FrameVariables, "wave_g"));
        float b = Mathf.Clamp01(GetVariable(CurrentPreset.FrameVariables, "wave_b"));

        if (GetVariable(CurrentPreset.FrameVariables, "wave_brighten") != 0f)
        {
            float maxc = Mathf.Max(r, g, b);

            if (maxc > 0.01f)
            {
                r /= maxc;
                g /= maxc;
                b /= maxc;
            }
        }

        Color color = new Color(r, g, b, alpha);

        if (oldNumVert > 0)
        {
            if (newWaveMode == 7)
            {
                float m = (oldNumVert - 1f) / (numVert * 2f);

                for (int i = 0; i < numVert; i++)
                {
                    float fIdx = i * m;
                    int nIdx = Mathf.FloorToInt(fIdx);
                    float t = fIdx - nIdx;

                    float x = BasicWaveFormPositionsOld[nIdx].x * (1 - t) + BasicWaveFormPositionsOld[nIdx + 1].x * t;
                    float y = BasicWaveFormPositionsOld[nIdx].y * (1 - t) + BasicWaveFormPositionsOld[nIdx + 1].y * t;

                    BasicWaveFormPositions[i] = new Vector3
                    (
                        BasicWaveFormPositions[i].x * blendMix + x * blendMix2,
                        BasicWaveFormPositions[i].y * blendMix + y * blendMix2,
                        0f
                    );
                }

                for (int i = 0; i < numVert; i++)
                {
                    float fIdx = (i + numVert) * m;
                    int nIdx = Mathf.FloorToInt(fIdx);
                    float t = fIdx - nIdx;

                    float x = BasicWaveFormPositionsOld[nIdx].x * (1 - t) + BasicWaveFormPositionsOld[nIdx + 1].x * t;
                    float y = BasicWaveFormPositionsOld[nIdx].y * (1 - t) + BasicWaveFormPositionsOld[nIdx + 1].y * t;

                    BasicWaveFormPositions2[i] = new Vector3
                    (
                        BasicWaveFormPositions2[i].x * blendMix + x * blendMix2,
                        BasicWaveFormPositions2[i].y * blendMix + y * blendMix2,
                        0f
                    );
                }
            }
            else if (oldWaveMode == 7)
            {
                float halfNumVert = numVert / 2f;
                float m = (oldNumVert - 1f) / halfNumVert;

                for (int i = 0; i < halfNumVert; i++)
                {
                    float fIdx = i * m;
                    int nIdx = Mathf.FloorToInt(fIdx);
                    float t = fIdx - nIdx;

                    float x = BasicWaveFormPositionsOld[nIdx].x * (1 - t) + BasicWaveFormPositionsOld[nIdx + 1].x * t;
                    float y = BasicWaveFormPositionsOld[nIdx].y * (1 - t) + BasicWaveFormPositionsOld[nIdx + 1].y * t;

                    BasicWaveFormPositions[i] = new Vector3
                    (
                        BasicWaveFormPositions[i].x * blendMix + x * blendMix2,
                        BasicWaveFormPositions[i].y * blendMix + y * blendMix2,
                        0f
                    );
                }

                for (int i = 0; i < halfNumVert; i++)
                {
                    float fIdx = i * m;
                    int nIdx = Mathf.FloorToInt(fIdx);
                    float t = fIdx - nIdx;

                    float x = BasicWaveFormPositions2Old[nIdx].x * (1 - t) + BasicWaveFormPositions2Old[nIdx + 1].x * t;
                    float y = BasicWaveFormPositions2Old[nIdx].y * (1 - t) + BasicWaveFormPositions2Old[nIdx + 1].y * t;

                    BasicWaveFormPositions2[i] = new Vector3
                    (
                        BasicWaveFormPositions2[i].x * blendMix + x * blendMix2,
                        BasicWaveFormPositions2[i].y * blendMix + y * blendMix2,
                        0f
                    );
                }
            }
            else
            {
                float m = (oldNumVert - 1f) / numVert;

                for (int i = 0; i < numVert; i++)
                {
                    float fIdx = i * m;
                    int nIdx = Mathf.FloorToInt(fIdx);
                    float t = fIdx - nIdx;

                    float x = BasicWaveFormPositionsOld[nIdx].x * (1 - t) + BasicWaveFormPositionsOld[nIdx + 1].x * t;
                    float y = BasicWaveFormPositionsOld[nIdx].y * (1 - t) + BasicWaveFormPositionsOld[nIdx + 1].y * t;

                    BasicWaveFormPositions[i] = new Vector3
                    (
                        BasicWaveFormPositions[i].x * blendMix + x * blendMix2,
                        BasicWaveFormPositions[i].y * blendMix + y * blendMix2,
                        0f
                    );
                }
            }
        }

        int smoothedNumVert = numVert * 2 - 1;

        SmoothWave(BasicWaveFormPositions, BasicWaveFormPositionsSmooth, numVert);

        if (newWaveMode == 7 || oldWaveMode == 7)
        {
            SmoothWave(BasicWaveFormPositions2, BasicWaveFormPositionsSmooth2, numVert);
        }

        if (GetVariable(CurrentPreset.FrameVariables, "wave_dots") != 0f)
        {
            DotParent.gameObject.SetActive(true);

            Vector3 outOfBounds = new Vector3(0f, 0f, -10f);

            float aspect_ratio = Resolution.x / (float)Resolution.y;

            for (int i = 0; i < MaxSamples * 2; i++)
            {
                if (i < smoothedNumVert)
                {
                    Dots[i].localPosition = new Vector3(BasicWaveFormPositionsSmooth[i].x * aspect_ratio, BasicWaveFormPositionsSmooth[i].y, 0f);
                    DotRenderers[i].color = color;
                    Dots[i].localScale = baseDotScale;
                }
                else
                {
                    Dots[i].localPosition = outOfBounds;
                }
            }

            if (newWaveMode != 7 && oldWaveMode != 7)
            {
                smoothedNumVert = 0;
            }

            for (int i = 0; i < MaxSamples * 2; i++)
            {
                if (i < smoothedNumVert)
                {
                    Dots[MaxSamples * 2 + i].localPosition = new Vector3(BasicWaveFormPositionsSmooth2[i].x * aspect_ratio, BasicWaveFormPositionsSmooth2[i].y, 0f);
                    DotRenderers[MaxSamples * 2 + i].color = color;
                    Dots[MaxSamples * 2 + i].localScale = baseDotScale;
                }
                else
                {
                    Dots[MaxSamples * 2 + i].localPosition = outOfBounds;
                }
            }

            TargetMeshFilter.sharedMesh = TargetMeshWarp;
            TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

            DoNothingMaterial.mainTexture = TempTexture;

            TargetCamera.targetTexture = TempTexture;
            TargetCamera.Render();

            DotParent.gameObject.SetActive(false);
        }
        else
        {
            WaveformRenderer.enabled = true;
            
            WaveformRenderer.positionCount = smoothedNumVert;
            WaveformRenderer.SetPositions(BasicWaveFormPositionsSmooth);

            if (GetVariable(CurrentPreset.FrameVariables, "wave_thick") != 0f)
            {
                WaveformRenderer.widthMultiplier = 2f;
            }
            else
            {
                WaveformRenderer.widthMultiplier = 0.5f;
            }

            if (newWaveMode == 7 || oldWaveMode == 7)
            {
                WaveformRenderer2.enabled = true;

                WaveformRenderer2.positionCount = smoothedNumVert;
                WaveformRenderer2.SetPositions(BasicWaveFormPositionsSmooth2);

                if (GetVariable(CurrentPreset.FrameVariables, "wave_thick") != 0f)
                {
                    WaveformRenderer2.widthMultiplier = 2f;
                }
                else
                {
                    WaveformRenderer2.widthMultiplier = 0.5f;
                }
            }

            WaveformRenderer.colorGradient = new Gradient()
            {
                colorKeys = new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            };

            WaveformRenderer.sharedMaterial.mainTexture = TempTexture;
            WaveformRenderer.sharedMaterial.SetColor("waveColor", color);
            WaveformRenderer.sharedMaterial.SetFloat("additivewave", GetVariable(CurrentPreset.FrameVariables, "additivewave"));
            WaveformRenderer.sharedMaterial.SetFloat("aspect_ratio", Resolution.x / (float)Resolution.y);

            TargetMeshFilter.sharedMesh = TargetMeshWarp;
            TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

            DoNothingMaterial.mainTexture = TempTexture;

            TargetCamera.targetTexture = TempTexture;
            TargetCamera.Render();

            WaveformRenderer.enabled = false;
            WaveformRenderer2.enabled = false;
        }
    }

    void SmoothWaveAndColor(Vector3[] positions, Color[] colors, Vector3[] positionsSmoothed, Color[] colorsSmoothed, int nVertsIn, bool zCoord = false)
    {
        float c1 = -0.15f;
        float c2 = 1.15f;
        float c3 = 1.15f;
        float c4 = -0.15f;
        float invSum = 1.0f / (c1 + c2 + c3 + c4);

        int j = 0;

        int iBelow = 0;
        int iAbove;
        int iAbove2 = 1;

        for (int i = 0; i < nVertsIn - 1; i++)
        {
            iAbove = iAbove2;
            iAbove2 = Mathf.Min(nVertsIn - 1, i + 2);

            positionsSmoothed[j] = new Vector3(positions[i].x, -positions[i].y, 0f);

            if (zCoord)
            {
                positionsSmoothed[j + 1] = new Vector3
                (
                    (c1 * positions[iBelow].x +
                    c2 * positions[i].x +
                    c3 * positions[iAbove].x +
                    c4 * positions[iAbove2].x) *
                    invSum,
                    -((c1 * positions[iBelow].y +
                    c2 * positions[i].y +
                    c3 * positions[iAbove].y +
                    c4 * positions[iAbove2].y) *
                    invSum),
                    (c1 * positions[iBelow].z +
                    c2 * positions[i].z +
                    c3 * positions[iAbove].z +
                    c4 * positions[iAbove2].z) *
                    invSum
                );
            }
            else
            {
                positionsSmoothed[j + 1] = new Vector3
                (
                    (c1 * positions[iBelow].x +
                    c2 * positions[i].x +
                    c3 * positions[iAbove].x +
                    c4 * positions[iAbove2].x) *
                    invSum,
                    -((c1 * positions[iBelow].y +
                    c2 * positions[i].y +
                    c3 * positions[iAbove].y +
                    c4 * positions[iAbove2].y) *
                    invSum),
                    0f
                );
            }

            colorsSmoothed[j] = colors[i];
            colorsSmoothed[j + 1] = colors[i];

            iBelow = i;
            j += 2;
        }

        positionsSmoothed[j] = new Vector3(positions[nVertsIn - 1].x, -positions[nVertsIn - 1].y, 0f);
        colorsSmoothed[j] = colors[nVertsIn - 1];
    }

    void SmoothWave(Vector3[] positions, Vector3[] positionsSmoothed, int nVertsIn, bool zCoord = false)
    {
        float c1 = -0.15f;
        float c2 = 1.15f;
        float c3 = 1.15f;
        float c4 = -0.15f;
        float invSum = 1.0f / (c1 + c2 + c3 + c4);

        int j = 0;

        int iBelow = 0;
        int iAbove;
        int iAbove2 = 1;

        for (int i = 0; i < nVertsIn - 1; i++)
        {
            iAbove = iAbove2;
            iAbove2 = Mathf.Min(nVertsIn - 1, i + 2);

            positionsSmoothed[j] = new Vector3(positions[i].x, -positions[i].y, 0f);

            if (zCoord)
            {
                positionsSmoothed[j + 1] = new Vector3
                (
                    (c1 * positions[iBelow].x +
                    c2 * positions[i].x +
                    c3 * positions[iAbove].x +
                    c4 * positions[iAbove2].x) *
                    invSum,
                    -((c1 * positions[iBelow].y +
                    c2 * positions[i].y +
                    c3 * positions[iAbove].y +
                    c4 * positions[iAbove2].y) *
                    invSum),
                    (c1 * positions[iBelow].z +
                    c2 * positions[i].z +
                    c3 * positions[iAbove].z +
                    c4 * positions[iAbove2].z) *
                    invSum
                );
            }
            else
            {
                positionsSmoothed[j + 1] = new Vector3
                (
                    (c1 * positions[iBelow].x +
                    c2 * positions[i].x +
                    c3 * positions[iAbove].x +
                    c4 * positions[iAbove2].x) *
                    invSum,
                    -((c1 * positions[iBelow].y +
                    c2 * positions[i].y +
                    c3 * positions[iAbove].y +
                    c4 * positions[iAbove2].y) *
                    invSum),
                    0f
                );
            }

            iBelow = i;
            j += 2;
        }

        positionsSmoothed[j] = new Vector3(positions[nVertsIn - 1].x, -positions[nVertsIn - 1].y, 0f);
    }

    void DrawComp(Preset preset, bool blending)
    {
        if (preset.CompMaterial == null)
        {
            return;
        }

        //(float[], float[]) blurValues = GetBlurValues(preset.FrameVariables);

        TargetMeshRenderer.sharedMaterial = preset.CompMaterial;

        float[] hueBase = new float[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

        for (int i = 0; i < 4; i++)
        {
            hueBase[i * 3] =
                0.6f +
                0.3f *
                Mathf.Sin(
                    CurrentTime * 30.0f * 0.0143f +
                    3f +
                    i * 21f +
                    GetVariable(preset.FrameVariables, "rand_start.w")
                );
            hueBase[i * 3 + 1] =
                0.6f +
                0.3f *
                Mathf.Sin(
                    CurrentTime * 30.0f * 0.0107f +
                    1f +
                    i * 13f +
                    GetVariable(preset.FrameVariables, "rand_start.y")
                );
            hueBase[i * 3 + 2] =
                0.6f +
                0.3f *
                Mathf.Sin(
                    CurrentTime * 30.0f * 0.0129f +
                    6f +
                    i * 9f +
                    GetVariable(preset.FrameVariables, "rand_start.z")
                );
            float maxShade = Mathf.Max(hueBase[i * 3], hueBase[i * 3 + 1], hueBase[i * 3 + 2]);
            for (int k = 0; k < 3; k++)
            {
                hueBase[i * 3 + k] = hueBase[i * 3 + k] / maxShade;
            }
        }

        int gridX1 = MeshSizeComp.x + 1;
        int gridY1 = MeshSizeComp.y + 1;

        int offsetColor = 0;

        for (int j = 0; j < gridY1; j++)
        {
            for (int i = 0; i < gridX1; i++)
            {
                float x = i / (float)MeshSizeComp.x;
                float y = j / (float)MeshSizeComp.y;

                float alpha = 1f;

                if (blending)
                {
                    x *= MeshSize.x + 1;
                    y *= MeshSize.y + 1;
                    x = Mathf.Clamp(x, 0, MeshSize.x - 1);
                    y = Mathf.Clamp(y, 0, MeshSize.y - 1);
                    int nx = Mathf.FloorToInt(x);
                    int ny = Mathf.FloorToInt(y);
                    float dx = x - nx;
                    float dy = y - ny;
                    float alpha00 = WarpColor[ny * (MeshSize.x + 1) + nx].a;
                    float alpha01 = WarpColor[ny * (MeshSize.x + 1) + (nx + 1)].a;
                    float alpha10 = WarpColor[(ny + 1) * (MeshSize.x + 1) + nx].a;
                    float alpha11 = WarpColor[(ny + 1) * (MeshSize.x + 1) + (nx + 1)].a;
                    alpha =
                        alpha00 * (1 - dx) * (1 - dy) +
                        alpha01 * dx * (1 - dy) +
                        alpha10 * (1 - dx) * dy +
                        alpha11 * dx * dy;
                }

                CompColor[offsetColor] = new Color
                (
                    hueBase[0] * x * y + hueBase[3] * (1f - x) * y + hueBase[6] * x * (1f - y) + hueBase[9] * (1f - x) * (1f - y),
                    hueBase[1] * x * y + hueBase[4] * (1f - x) * y + hueBase[7] * x * (1f - y) + hueBase[10] * (1f - x) * (1f - y),
                    hueBase[2] * x * y + hueBase[5] * (1f - x) * y + hueBase[8] * x * (1f - y) + hueBase[11] * (1f - x) * (1f - y),
                    alpha
                );

                offsetColor++;
            }
        }

        TargetMeshFilter.sharedMesh = TargetMeshComp;
        TargetMeshComp.SetColors(CompColor);

        preset.CompMaterial.mainTexture = TempTexture;

        /*preset.CompMaterial.SetTexture("_MainTex2", FinalTexture);
        preset.CompMaterial.SetTexture("_MainTex3", FinalTexture);
        preset.CompMaterial.SetTexture("_MainTex4", FinalTexture);
        preset.CompMaterial.SetTexture("_MainTex5", FinalTexture);*/

        // sampler_blur1
        // sampler_blur2
        // sampler_blur3

        // sampler_noise_lq
        // sampler_noise_mq
        // sampler_noise_hq
        // sampler_noise_lq_lite
        // sampler_pw_noise_lq
        // sampler_noisevol_lq
        // sampler_noisevol_hq

        // user textures

        //preset.CompMaterial.SetFloat("time", CurrentTime);
        preset.CompMaterial.SetFloat("gammaAdj", GetVariable(preset.FrameVariables, "gammaadj"));
        preset.CompMaterial.SetFloat("echo_zoom", GetVariable(preset.FrameVariables, "echo_zoom"));
        preset.CompMaterial.SetFloat("echo_alpha", GetVariable(preset.FrameVariables, "echo_alpha"));
        preset.CompMaterial.SetFloat("echo_orientation", GetVariable(preset.FrameVariables, "echo_orient"));
        preset.CompMaterial.SetFloat("invert", GetVariable(preset.FrameVariables, "invert"));
        preset.CompMaterial.SetFloat("brighten", GetVariable(preset.FrameVariables, "brighten"));
        preset.CompMaterial.SetFloat("_darken", GetVariable(preset.FrameVariables, "darken"));
        preset.CompMaterial.SetFloat("solarize", GetVariable(preset.FrameVariables, "solarize"));
        /*preset.CompMaterial.SetVector("resolution", new Vector2(Resolution.x, Resolution.y));
        preset.CompMaterial.SetVector("aspect", new Vector4(1f, 1f, 1f, 1f));
        preset.CompMaterial.SetVector("texsize", new Vector4(Resolution.x, Resolution.y, 1f / Resolution.x, 1f / Resolution.y));
        preset.CompMaterial.SetVector("texsize_noise_lq", new Vector4(256, 256, 1f / 256f, 1f / 256f));
        preset.CompMaterial.SetVector("texsize_noise_mq", new Vector4(256, 256, 1f / 256f, 1f / 256));
        preset.CompMaterial.SetVector("texsize_noise_hq", new Vector4(256, 256, 1f / 256f, 1f / 256f));
        preset.CompMaterial.SetVector("texsize_noise_lq_lite", new Vector4(32, 32, 1f / 32f, 1f / 32f));
        preset.CompMaterial.SetVector("texsize_noisevol_lq", new Vector4(32, 32, 1f / 32f, 1f / 32f));
        preset.CompMaterial.SetVector("texsize_noisevol_hq", new Vector4(32, 32, 1f / 32f, 1f / 32f));
        preset.CompMaterial.SetFloat("bass", Bass);
        preset.CompMaterial.SetFloat("mid", Mid);
        preset.CompMaterial.SetFloat("treb", Treb);
        preset.CompMaterial.SetFloat("vol", (Bass + Mid + Treb) / 3f);
        preset.CompMaterial.SetFloat("bass_att", BassAtt);
        preset.CompMaterial.SetFloat("mid_att", MidAtt);
        preset.CompMaterial.SetFloat("treb_att", TrebAtt);
        preset.CompMaterial.SetFloat("vol_att", (BassAtt + MidAtt + TrebAtt) / 3f);
        preset.CompMaterial.SetFloat("frame", FrameNum);
        preset.CompMaterial.SetFloat("fps", FPS);
        preset.CompMaterial.SetVector("rand_preset", 
            new Vector4(
                GetVariable(preset.FrameVariables, "rand_preset.x"),
                GetVariable(preset.FrameVariables, "rand_preset.y"),
                GetVariable(preset.FrameVariables, "rand_preset.z"),
                GetVariable(preset.FrameVariables, "rand_preset.w")
            )
        );
        preset.CompMaterial.SetVector("rand_frame", 
            new Vector4(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f)
            )
        );*/
        preset.CompMaterial.SetFloat("fShader", GetVariable(preset.FrameVariables, "fshader"));
        /*preset.CompMaterial.SetVector("_qa", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q1"),
                GetVariable(preset.AfterFrameVariables, "q2"),
                GetVariable(preset.AfterFrameVariables, "q3"),
                GetVariable(preset.AfterFrameVariables, "q4")
            )
        );
        preset.CompMaterial.SetVector("_qb", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q5"),
                GetVariable(preset.AfterFrameVariables, "q6"),
                GetVariable(preset.AfterFrameVariables, "q7"),
                GetVariable(preset.AfterFrameVariables, "q8")
            )
        );
        preset.CompMaterial.SetVector("_qc", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q9"),
                GetVariable(preset.AfterFrameVariables, "q10"),
                GetVariable(preset.AfterFrameVariables, "q11"),
                GetVariable(preset.AfterFrameVariables, "q12")
            )
        );
        preset.CompMaterial.SetVector("_qd", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q13"),
                GetVariable(preset.AfterFrameVariables, "q14"),
                GetVariable(preset.AfterFrameVariables, "q15"),
                GetVariable(preset.AfterFrameVariables, "q16")
            )
        );
        preset.CompMaterial.SetVector("_qe", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q17"),
                GetVariable(preset.AfterFrameVariables, "q18"),
                GetVariable(preset.AfterFrameVariables, "q19"),
                GetVariable(preset.AfterFrameVariables, "q20")
            )
        );
        preset.CompMaterial.SetVector("_qf", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q21"),
                GetVariable(preset.AfterFrameVariables, "q22"),
                GetVariable(preset.AfterFrameVariables, "q23"),
                GetVariable(preset.AfterFrameVariables, "q24")
            )
        );
        preset.CompMaterial.SetVector("_qg", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q25"),
                GetVariable(preset.AfterFrameVariables, "q26"),
                GetVariable(preset.AfterFrameVariables, "q27"),
                GetVariable(preset.AfterFrameVariables, "q28")
            )
        );
        preset.CompMaterial.SetVector("_qh", 
            new Vector4(
                GetVariable(preset.AfterFrameVariables, "q29"),
                GetVariable(preset.AfterFrameVariables, "q30"),
                GetVariable(preset.AfterFrameVariables, "q31"),
                GetVariable(preset.AfterFrameVariables, "q32")
            )
        );
        preset.CompMaterial.SetVector("slow_roam_cos", 
            new Vector4(
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.005f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.008f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.013f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.022f)
            )
        );
        preset.CompMaterial.SetVector("roam_cos", 
            new Vector4(
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.3f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 1.3f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 5.0f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 20.0f)
            )
        );
        preset.CompMaterial.SetVector("slow_roam_sin", 
            new Vector4(
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.005f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.008f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.013f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.022f)
            )
        );
        preset.CompMaterial.SetVector("roam_sin", 
            new Vector4(
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.3f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 1.3f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 5.0f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 20.0f)
            )
        );

        float blurMin1 = blurValues.Item1[0];
        float blurMin2 = blurValues.Item1[1];
        float blurMin3 = blurValues.Item1[2];
        float blurMax1 = blurValues.Item2[0];
        float blurMax2 = blurValues.Item2[1];
        float blurMax3 = blurValues.Item2[2];

        float scale1 = blurMax1 - blurMin1;
        float bias1 = blurMin1;

        float scale2 = blurMax2 - blurMin2;
        float bias2 = blurMin2;

        float scale3 = blurMax3 - blurMin3;
        float bias3 = blurMin3;

        preset.CompMaterial.SetFloat("blur1_min", blurMin1);
        preset.CompMaterial.SetFloat("blur1_max", blurMax1);
        preset.CompMaterial.SetFloat("blur2_min", blurMin2);
        preset.CompMaterial.SetFloat("blur2_max", blurMax2);
        preset.CompMaterial.SetFloat("blur3_min", blurMin3);
        preset.CompMaterial.SetFloat("blur3_max", blurMax3);
        preset.CompMaterial.SetFloat("scale1", scale1);
        preset.CompMaterial.SetFloat("scale2", scale2);
        preset.CompMaterial.SetFloat("scale3", scale3);
        preset.CompMaterial.SetFloat("bias1", bias1);
        preset.CompMaterial.SetFloat("bias2", bias2);
        preset.CompMaterial.SetFloat("bias3", bias3);*/

        TargetCamera.targetTexture = FinalTexture;
        TargetCamera.Render();
    }

    static float Func_Int(float x)
    {
        return Mathf.Floor(x);
    }

    static float Func_Abs(float x)
    {
        return Mathf.Abs(x);
    }

    static float Func_Sqr(float x)
    {
        return x * x;
    }

    static float Func_Sqrt(float x)
    {
        return Mathf.Sqrt(Mathf.Abs(x));
    }

    static float Func_Log(float x)
    {
        return Mathf.Log(x);
    }

    static float Func_Log10(float x)
    {
        return Mathf.Log10(x);
    }

    static float Func_Sign(float x)
    {
        return Mathf.Sign(x);
    }

    static float Func_Rand(float x)
    {
        return UnityEngine.Random.Range(0f, x);
    }

    static float Func_RandInt(float x)
    {
        return Mathf.Floor(UnityEngine.Random.Range(0, x));
    }

    static float Func_Bnot(float x)
    {
        return Mathf.Abs(x) < Mathf.Epsilon ? 1f : 0f;
    }

    static float Func_Pow(float x, float y)
    {
        float result = Mathf.Pow(x, y);

        if (!float.IsInfinity(result) && !float.IsNaN(result))
        {
            return result;
        }

        return 0f;
    }

    static float Func_Div(float x, float y)
    {
        if (y == 0f)
        {
            return 0f;
        }

        return x / y;
    }

    static float Func_Mod(float x, float y)
    {
        if (y == 0f)
        {
            return 0f;
        }

        return Mathf.FloorToInt(x) % Mathf.FloorToInt(y);
    }

    static float Func_Bitor(float x, float y)
    {
        return Mathf.FloorToInt(x) | Mathf.FloorToInt(y);
    }

    static float Func_Bitand(float x, float y)
    {
        return Mathf.FloorToInt(x) & Mathf.FloorToInt(y);
    }

    static float Func_Sigmoid(float x, float y)
    {
        float t = 1f + Mathf.Exp(-x * y);
        return Mathf.Abs(t) > Mathf.Epsilon ? 1f / t : 0f;
    }

    static float Func_Bor(float x, float y)
    {
        return Mathf.Abs(x) > Mathf.Epsilon || Mathf.Abs(y) > Mathf.Epsilon ? 1f : 0f;
    }

    static float Func_Band(float x, float y)
    {
        return Mathf.Abs(x) > Mathf.Epsilon && Mathf.Abs(y) > Mathf.Epsilon ? 1f : 0f;
    }

    static float Func_Equal(float x, float y)
    {
        return Mathf.Abs(x - y) < Mathf.Epsilon ? 1f : 0f;
    }

    static float Func_Above(float x, float y)
    {
        return x > y ? 1f : 0f;
    }

    static float Func_Below(float x, float y)
    {
        return x < y ? 1f : 0f;
    }

    static float Func_Min(float x, float y)
    {
        return Mathf.Min(x, y);
    }

    static float Func_Max(float x, float y)
    {
        return Mathf.Max(x, y);
    }

    static float Func_Ifcond(float x, float y, float z)
    {
        return Mathf.Abs(x) > Mathf.Epsilon ? y : z;
    }

    static float Func_Sin(float x)
    {
        return Mathf.Sin(x);
    }

    static float Func_Cos(float x)
    {
        return Mathf.Cos(x);
    }

    static float Func_Asin(float x)
    {
        return Mathf.Asin(x);
    }

    static float Func_Acos(float x)
    {
        return Mathf.Acos(x);
    }

    static float Func_Tan(float x)
    {
        return Mathf.Tan(x);
    }

    static float Func_Atan(float x)
    {
        return Mathf.Atan(x);
    }

    static float Func_Atan2(float x, float y)
    {
        return Mathf.Atan2(x, y);
    }

    static float Func_Exp(float x)
    {
        return Mathf.Exp(x);
    }

    delegate float Func1(float x);
    delegate float Func2(float x, float y);
    delegate float Func3(float x, float y, float z);

    Dictionary<string, Func1> Funcs1Arg = new Dictionary<string, Func1>
    {
        {"sqr", Func_Sqr},
        {"sqrt", Func_Sqrt},
        {"log", Func_Log},
        {"log10", Func_Log10},
        {"sign", Func_Sign},
        {"rand", Func_Rand},
        {"randint", Func_RandInt},
        {"bnot", Func_Bnot},
        {"sin", Func_Sin},
        {"cos", Func_Cos},
        {"abs", Func_Abs},
        {"tan", Func_Tan},
        {"int", Func_Int},
        {"asin", Func_Asin},
        {"acos", Func_Acos},
        {"atan", Func_Atan},
        {"exp", Func_Exp},
    };

    Dictionary<string, Func2> Funcs2Arg = new Dictionary<string, Func2>
    {
        {"pow", Func_Pow},
        {"div", Func_Div},
        {"mod", Func_Mod},
        {"bitor", Func_Bitor},
        {"bitand", Func_Bitand},
        {"sigmoid", Func_Sigmoid},
        {"bor", Func_Bor},
        {"band", Func_Band},
        {"equal", Func_Equal},
        {"above", Func_Above},
        {"below", Func_Below},
        {"min", Func_Min},
        {"max", Func_Max},
        {"atan2", Func_Atan2}
    };

    Dictionary<string, Func3> Funcs3Arg = new Dictionary<string, Func3>
    {
        {"if", Func_Ifcond}
    };

    List<string> TokenizeExpression(string Expression)
    {
        List<string> tokens = new List<string>();

        if (string.IsNullOrWhiteSpace(Expression))
        {
            return tokens;
        }

        int eqIndex = Expression.IndexOf('=');

        if (eqIndex < 0)
        {
            throw new Exception("no assignment in expression " + Expression);
        }

        tokens.Add(Expression.Substring(0, eqIndex));

        Expression = Expression.Substring(eqIndex + 1);

        string tokenBuffer = "";

        foreach (char c in Expression)
        {
            if (c == '+' || c == '-' || c == '*' || c == '/' || c == '(' || c == ')' || c == ',' || c == '%' || c == '|' || c == '&')
            {
                if (tokenBuffer != "")
                {
                    tokens.Add(tokenBuffer);
                    tokenBuffer = "";
                }

                tokens.Add(c.ToString());

                continue;
            }

            tokenBuffer += c;
        }

        if (tokenBuffer != "")
        {
            tokens.Add(tokenBuffer);
        }

        return tokens;
    }

    float GetVariable(Dictionary<int, float> Variables, string name, float defaultValue = 0f)
    {
        int key;

        if (!VariableNameTable.TryGetValue(name, out key))
        {
            return 0f;
        }

        if (Variables.TryGetValue(key, out float value))
        {
            return value;
        }

        return defaultValue;
    }

    void SetVariable(Dictionary<int, float> Variables, string name, float value)
    {
        RegisterVariable(name);

        int key = VariableNameTable[name];

        if (Variables.ContainsKey(key))
        {
            Variables[key] = value;
        }
        else
        {
            Variables.Add(key, value);
        }
    }

    float GetVariable(Dictionary<int, float> Variables, int key, float defaultValue = 0f)
    {
        if (Variables.TryGetValue(key, out float value))
        {
            return value;
        }
        return defaultValue;
    }

    void SetVariable(Dictionary<int, float> Variables, int key, float value)
    {
        if (Variables.ContainsKey(key))
        {
            Variables[key] = value;
        }
        else
        {
            Variables.Add(key, value);
        }
    }

    public void UnloadPresets()
    {
        foreach (var preset in LoadedPresets.Values)
        {
            Destroy(preset.WarpMaterial);
            Destroy(preset.CompMaterial);
            Destroy(preset.DarkenCenterMaterial);

            foreach (var shape in preset.Shapes)
            {
                Destroy(shape.ShapeMesh);
            }
        }

        LoadedPresets.Clear();
    }

    public void LoadPresets()
    {
        foreach (TextAsset file in PresetFiles)
        {
            LoadPreset(file.name, file.text);
        }
    }

    public void LoadPreset(string fileName, string file)
    {
        var preset = new Preset();

        string[] lines = file.Split('\n');

        bool acceptValues = false;

        foreach (string line in lines)
        {
            if (line.StartsWith("["))
            {
                acceptValues = !acceptValues;
                continue;
            }

            if (!acceptValues)
            {
                continue;
            }

            if (!line.Contains("="))
            {
                continue;
            }

            string arg, val;

            if (line.Trim().StartsWith("warp_") || line.Trim().StartsWith("comp_"))
            {
                int eqIndex = line.IndexOf('=');

                arg = line.Substring(0, eqIndex).Trim().ToLower();
                val = line.Substring(eqIndex + 2);
            }
            else
            {
                var lineTrimmed = line.Replace(" ", "").Replace("\r", "").ToLower();

                if (lineTrimmed.Contains("//"))
                {
                    lineTrimmed = lineTrimmed.Substring(0, lineTrimmed.IndexOf("//"));
                }

                int eqIndex = lineTrimmed.IndexOf('=');

                arg = lineTrimmed.Substring(0, eqIndex);
                val = lineTrimmed.Substring(eqIndex + 1);
            }


            if (string.IsNullOrEmpty(val))
            {
                continue;
            }

            if (arg.StartsWith("wave_") && char.IsDigit(arg[5]))
            {
                int num = int.Parse(arg.Split('_')[1]);

                while (num >= preset.Waves.Count)
                {
                    preset.Waves.Add(new Wave());
                }

                string codeName = arg.Split('_').Skip(2).Aggregate((a, b) => a + "_" + b);

                if (codeName.StartsWith("init"))
                {
                    preset.Waves[num].InitEquation += val;
                }
                else if (codeName.StartsWith("per_frame"))
                {
                    preset.Waves[num].FrameEquation += val;
                }
                else if (codeName.StartsWith("per_point"))
                {
                    preset.Waves[num].PointEquation += val;
                }
                else
                {
                    throw new Exception("Unknown wave code name " + codeName);
                }
            }
            else if (arg.StartsWith("wavecode_"))
            {
                int num = int.Parse(arg.Split('_')[1]);

                while (num >= preset.Waves.Count)
                {
                    preset.Waves.Add(new Wave());
                }

                string varName = arg.Split('_').Skip(2).Aggregate((a, b) => a + "_" + b);

                if (VariableNameLookup.ContainsKey(varName))
                {
                    SetVariable(preset.Waves[num].BaseVariables, VariableNameLookup[varName], float.Parse(val));
                }
                else
                {
                    SetVariable(preset.Waves[num].BaseVariables, varName, float.Parse(val));
                }
            }
            else if (arg.StartsWith("shape_") && char.IsDigit(arg[6]))
            {
                int num = int.Parse(arg.Split('_')[1]);

                while (num >= preset.Shapes.Count)
                {
                    preset.Shapes.Add(new Shape());
                }

                string codeName = arg.Split('_').Skip(2).Aggregate((a, b) => a + "_" + b);

                if (codeName.StartsWith("init"))
                {
                    preset.Shapes[num].InitEquation += val;
                }
                else if (codeName.StartsWith("per_frame"))
                {
                    preset.Shapes[num].FrameEquation += val;
                }
                else
                {
                    throw new Exception("Unknown shape code name " + codeName);
                }
            }
            else if (arg.StartsWith("shapecode_"))
            {
                int num = int.Parse(arg.Split('_')[1]);

                while (num >= preset.Shapes.Count)
                {
                    preset.Shapes.Add(new Shape());
                }

                string varName = arg.Split('_').Skip(2).Aggregate((a, b) => a + "_" + b);

                if (VariableNameLookup.ContainsKey(varName))
                {
                    SetVariable(preset.Shapes[num].BaseVariables, VariableNameLookup[varName], float.Parse(val));
                }
                else
                {
                    SetVariable(preset.Shapes[num].BaseVariables, varName, float.Parse(val));
                }
            }
            else if (arg.StartsWith("per_frame_"))
            {
                preset.FrameEquation += val;
            }
            else if (arg.StartsWith("per_pixel_"))
            {
                preset.PixelEquation += val;
            }
            else if (arg.StartsWith("per_frame_init_"))
            {
                preset.InitEquation += val;
            }
            else if (arg.StartsWith("warp_"))
            {
                preset.Warp += val + "\n";
            }
            else if (arg.StartsWith("comp_"))
            {
                preset.Comp += val + "\n";
            }
            else
            {
                if (VariableNameLookup.ContainsKey(arg))
                {
                    SetVariable(preset.BaseVariables, VariableNameLookup[arg], float.Parse(val));
                }
                else
                {
                    SetVariable(preset.BaseVariables, arg, float.Parse(val));
                }
            }
        }

        preset.InitEquationCompiled = CompileEquation(preset.InitEquation.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries).Select(x => TokenizeExpression(x)).ToList());
        preset.FrameEquationCompiled = CompileEquation(preset.FrameEquation.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries).Select(x => TokenizeExpression(x)).ToList());
        preset.PixelEquationCompiled = CompileEquation(preset.PixelEquation.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries).Select(x => TokenizeExpression(x)).ToList());

        foreach (var wave in preset.Waves)
        {
            wave.InitEquationCompiled = CompileEquation(wave.InitEquation.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries).Select(x => TokenizeExpression(x)).ToList());
            wave.FrameEquationCompiled = CompileEquation(wave.FrameEquation.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries).Select(x => TokenizeExpression(x)).ToList());
            wave.PointEquationCompiled = CompileEquation(wave.PointEquation.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries).Select(x => TokenizeExpression(x)).ToList());
        }

        foreach (var shape in preset.Shapes)
        {
            shape.InitEquationCompiled = CompileEquation(shape.InitEquation.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries).Select(x => TokenizeExpression(x)).ToList());
            shape.FrameEquationCompiled = CompileEquation(shape.FrameEquation.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries).Select(x => TokenizeExpression(x)).ToList());
        }

        LoadedPresets.Add(fileName, preset);
    }

    Action<Dictionary<int, float>> CompileEquation(List<List<string>> Equation)
    {
        Action<Dictionary<int, float>> result = null;
        
        foreach (var line in Equation)
        {
            if (line.Count == 0)
                continue;
            
            string varName = line[0];

            RegisterVariable(varName);
            int varIndex = VariableNameTable[varName];

            int stackIndex = 0;

            Func<Dictionary<int, float>, float> compiledLine = CompileExpression(line.Skip(1).ToList(), ref stackIndex);

            result += (Dictionary<int, float> Variables) =>
            {
                SetVariable(Variables, varIndex, compiledLine(Variables));
            };
        }

        if (result == null)
        {
            result = (Dictionary<int, float> Variables) => { };
        }

        return result;
    }

    Func<Dictionary<int, float>, float> CompileVariable(string token)
    {
        if (token[0] == '#')
        {
            int stackIndex = int.Parse(token.Substring(1));

            return (Dictionary<int, float> Variables) => Stack[stackIndex];
        }

        if (token[0] == '.' || token[0] == '-' || char.IsDigit(token[0]))
        {
            var result = float.Parse(token);
            return (Dictionary<int, float> Variables) => result;
        }

        RegisterVariable(token);
        int varIndex = VariableNameTable[token];

        return (Dictionary<int, float> Variables) => GetVariable(Variables, varIndex);
    }

    Func<Dictionary<int, float>, float> CompileExpression(List<string> Tokens, ref int stackIndex)
    {
        List<Action<Dictionary<int, float>>> innerActions = new List<Action<Dictionary<int, float>>>();

        string debugOut = "";

        for (int tokenNum = 0; tokenNum < Tokens.Count; tokenNum++)
        {
            debugOut += Tokens[tokenNum] + ", ";
        }

        for (int tokenNum = 0; tokenNum < Tokens.Count; tokenNum++)
        {
            string token = Tokens[tokenNum];

            if (token == ")")
            {
                throw new System.Exception("Unmatched closing parenthesis: " + debugOut);
            }
            
            if (token == "(")
            {
                bool success = false;
                bool isFunction = false;

                if (tokenNum > 0)
                {
                    string prev = Tokens[tokenNum - 1];

                    if (prev != "*" && prev != "/" && prev != "+" && prev != "-" && prev != "%" && prev != "|" && prev != "&")
                    {
                        isFunction = true;
                    }
                }

                int depth = 0;

                for (int tokenNum2 = tokenNum + 1; tokenNum2 < Tokens.Count; tokenNum2++)
                {
                    string token2 = Tokens[tokenNum2];

                    if (token2 == "(")
                    {
                        depth++;

                        continue;
                    }

                    if (token2 == ")")
                    {
                        if (depth == 0)
                        {
                            if (isFunction)
                            {
                                List<List<string>> arguments = new List<List<string>>();

                                arguments.Add(new List<string>());

                                int depth2 = 0;

                                for (int i = tokenNum + 1; i < tokenNum2; i++)
                                {
                                    string token3 = Tokens[i];

                                    if (token3 == "(")
                                    {
                                        depth2++;
                                    }
                                    else if (token3 == ")")
                                    {
                                        depth2--;
                                    }

                                    if (depth2 == 0 && token3 == ",")
                                    {
                                        arguments.Add(new List<string>());

                                        continue;
                                    }

                                    arguments[arguments.Count - 1].Add(token3);
                                }

                                List<Func<Dictionary<int, float>, float>> argumentValues = new List<Func<Dictionary<int, float>, float>>();

                                foreach (List<string> argument in arguments)
                                {
                                    argumentValues.Add(CompileExpression(argument, ref stackIndex));
                                }

                                string functionName = Tokens[tokenNum - 1];

                                int funcIndex = stackIndex++;
                                string funcId = "#" + funcIndex;

                                Action<Dictionary<int, float>> compiledFunction = (Dictionary<int, float> Variables) =>
                                {
                                    throw new Exception("Error compiling function " + functionName + ": " + debugOut);
                                };
                                
                                switch (arguments.Count)
                                {
                                    case 1:
                                        compiledFunction = (Dictionary<int, float> Variables) =>
                                        {
                                            Stack[funcIndex] = Funcs1Arg[functionName](argumentValues[0](Variables));
                                        };
                                        break;
                                    case 2:
                                        compiledFunction = (Dictionary<int, float> Variables) =>
                                        {
                                            Stack[funcIndex] = Funcs2Arg[functionName](argumentValues[0](Variables), argumentValues[1](Variables));
                                        };
                                        break;
                                    case 3:
                                        compiledFunction = (Dictionary<int, float> Variables) =>
                                        {
                                            Stack[funcIndex] = Funcs3Arg[functionName](argumentValues[0](Variables), argumentValues[1](Variables), argumentValues[2](Variables));
                                        };
                                        break;
                                }

                                innerActions.Add(compiledFunction);

                                Tokens.RemoveRange(tokenNum - 1, tokenNum2 - tokenNum + 1);

                                Tokens[tokenNum - 1] = funcId;
                                
                                tokenNum--;
                            }
                            else
                            {
                                int funcIndex = stackIndex++;
                                string funcId = "#" + funcIndex;

                                Func<Dictionary<int, float>, float> exp = CompileExpression(Tokens.Skip(tokenNum + 1).Take(tokenNum2 - tokenNum - 1).ToList(), ref stackIndex);

                                innerActions.Add((Dictionary<int, float> Variables) =>
                                {
                                    Stack[funcIndex] = exp(Variables);
                                });

                                Tokens.RemoveRange(tokenNum, tokenNum2 - tokenNum);

                                Tokens[tokenNum] = funcId;
                            }

                            success = true;

                            break;
                        }
                        else
                        {
                            depth--;

                            continue;
                        }
                    }
                }

                if (!success)
                {
                    throw new System.Exception("Unmatched opening parenthesis: " + debugOut);
                }

                continue;
            }
        }

        for (int tokenNum = 0; tokenNum < Tokens.Count; tokenNum++)
        {
            string token = Tokens[tokenNum];

            if (token == "+")
            {
                if (tokenNum == 0 || Tokens[tokenNum - 1] == "*" || Tokens[tokenNum - 1] == "/" || Tokens[tokenNum - 1] == "+" || Tokens[tokenNum - 1] == "-" || Tokens[tokenNum - 1] == "%" || Tokens[tokenNum - 1] == "|" || Tokens[tokenNum - 1] == "&")
                {
                    string next = Tokens[tokenNum + 1];

                    int funcIndex = stackIndex++;
                    string funcId = "#" + funcIndex;

                    Func<Dictionary<int, float>, float> exp = CompileVariable(next);

                    innerActions.Add((Dictionary<int, float> Variables) =>
                    {
                        Stack[funcIndex] = +exp(Variables);
                    });

                    Tokens.RemoveRange(tokenNum, 1);

                    Tokens[tokenNum] = funcId;
                }
            }

            if (token == "-")
            {
                if (tokenNum == 0 || Tokens[tokenNum - 1] == "*" || Tokens[tokenNum - 1] == "/" || Tokens[tokenNum - 1] == "+" || Tokens[tokenNum - 1] == "-" || Tokens[tokenNum - 1] == "%" || Tokens[tokenNum - 1] == "|" || Tokens[tokenNum - 1] == "&")
                {
                    string next = Tokens[tokenNum + 1];

                    int funcIndex = stackIndex++;
                    string funcId = "#" + funcIndex;

                    Func<Dictionary<int, float>, float> exp = CompileVariable(next);

                    innerActions.Add((Dictionary<int, float> Variables) =>
                    {
                        Stack[funcIndex] = -exp(Variables);
                    });

                    Tokens.RemoveRange(tokenNum, 1);

                    Tokens[tokenNum] = funcId;
                }
            }
        }

        for (int tokenNum = 0; tokenNum < Tokens.Count; tokenNum++)
        {
            string token = Tokens[tokenNum];

            if (token == "*")
            {
                string prev = Tokens[tokenNum - 1];
                string next = Tokens[tokenNum + 1];

                int funcIndex = stackIndex++;
                string funcId = "#" + funcIndex;

                Func<Dictionary<int, float>, float> prevValue = CompileVariable(prev);
                Func<Dictionary<int, float>, float> nextValue = CompileVariable(next);

                innerActions.Add((Dictionary<int, float> Variables) =>
                {
                    Stack[funcIndex] = prevValue(Variables) * nextValue(Variables);
                });

                Tokens.RemoveRange(tokenNum - 1, 2);

                Tokens[tokenNum - 1] = funcId;

                tokenNum--;
            }

            if (token == "/")
            {
                string prev = Tokens[tokenNum - 1];
                string next = Tokens[tokenNum + 1];

                int funcIndex = stackIndex++;
                string funcId = "#" + funcIndex;

                Func<Dictionary<int, float>, float> prevValue = CompileVariable(prev);
                Func<Dictionary<int, float>, float> nextValue = CompileVariable(next);

                innerActions.Add((Dictionary<int, float> Variables) =>
                {
                    Stack[funcIndex] = prevValue(Variables) / nextValue(Variables);
                });

                Tokens.RemoveRange(tokenNum - 1, 2);

                Tokens[tokenNum - 1] = funcId;

                tokenNum--;
            }

            if (token == "%")
            {
                string prev = Tokens[tokenNum - 1];
                string next = Tokens[tokenNum + 1];

                int funcIndex = stackIndex++;
                string funcId = "#" + funcIndex;

                Func<Dictionary<int, float>, float> prevValue = CompileVariable(prev);
                Func<Dictionary<int, float>, float> nextValue = CompileVariable(next);

                innerActions.Add((Dictionary<int, float> Variables) =>
                {
                    float divider = nextValue(Variables);

                    if (divider == 0f)
                    {
                        Stack[funcIndex] = 0f;
                    }
                    else
                    {
                        Stack[funcIndex] = (int)prevValue(Variables) % (int)divider;
                    }
                });

                Tokens.RemoveRange(tokenNum - 1, 2);

                Tokens[tokenNum - 1] = funcId;

                tokenNum--;
            }
        }

        for (int tokenNum = 0; tokenNum < Tokens.Count; tokenNum++)
        {
            string token = Tokens[tokenNum];

            if (token == "+")
            {
                string prev = Tokens[tokenNum - 1];
                string next = Tokens[tokenNum + 1];

                int funcIndex = stackIndex++;
                string funcId = "#" + funcIndex;

                Func<Dictionary<int, float>, float> prevValue = CompileVariable(prev);
                Func<Dictionary<int, float>, float> nextValue = CompileVariable(next);

                innerActions.Add((Dictionary<int, float> Variables) =>
                {
                    Stack[funcIndex] = prevValue(Variables) + nextValue(Variables);
                });

                Tokens.RemoveRange(tokenNum - 1, 2);

                Tokens[tokenNum - 1] = funcId;

                tokenNum--;
            }

            if (token == "-")
            {
                string prev = Tokens[tokenNum - 1];
                string next = Tokens[tokenNum + 1];

                int funcIndex = stackIndex++;
                string funcId = "#" + funcIndex;

                Func<Dictionary<int, float>, float> prevValue = CompileVariable(prev);
                Func<Dictionary<int, float>, float> nextValue = CompileVariable(next);

                innerActions.Add((Dictionary<int, float> Variables) =>
                {
                    Stack[funcIndex] = prevValue(Variables) - nextValue(Variables);
                });

                Tokens.RemoveRange(tokenNum - 1, 2);

                Tokens[tokenNum - 1] = funcId;

                tokenNum--;
            }
        }

        for (int tokenNum = 0; tokenNum < Tokens.Count; tokenNum++)
        {
            string token = Tokens[tokenNum];

            if (token == "&")
            {
                string prev = Tokens[tokenNum - 1];
                string next = Tokens[tokenNum + 1];

                int funcIndex = stackIndex++;
                string funcId = "#" + funcIndex;

                Func<Dictionary<int, float>, float> prevValue = CompileVariable(prev);
                Func<Dictionary<int, float>, float> nextValue = CompileVariable(next);

                innerActions.Add((Dictionary<int, float> Variables) =>
                {
                    Stack[funcIndex] = (int)prevValue(Variables) & (int)nextValue(Variables);
                });

                Tokens.RemoveRange(tokenNum - 1, 2);

                Tokens[tokenNum - 1] = funcId;

                tokenNum--;
            }
        }

        for (int tokenNum = 0; tokenNum < Tokens.Count; tokenNum++)
        {
            string token = Tokens[tokenNum];

            if (token == "|")
            {
                string prev = Tokens[tokenNum - 1];
                string next = Tokens[tokenNum + 1];

                int funcIndex = stackIndex++;
                string funcId = "#" + funcIndex;

                Func<Dictionary<int, float>, float> prevValue = CompileVariable(prev);
                Func<Dictionary<int, float>, float> nextValue = CompileVariable(next);

                innerActions.Add((Dictionary<int, float> Variables) =>
                {
                    Stack[funcIndex] = (int)prevValue(Variables) | (int)nextValue(Variables);
                });

                Tokens.RemoveRange(tokenNum - 1, 2);

                Tokens[tokenNum - 1] = funcId;

                tokenNum--;
            }
        }

        if (Tokens.Count != 1)
        {
            string a = "";
            foreach (var token in Tokens)
            {
                a += token + ", ";
            }
            throw new System.Exception("evaluation failed: " + debugOut + " => " + a);
        }

        Func<Dictionary<int, float>, float> finalValue = CompileVariable(Tokens[0]);

        Func<Dictionary<int, float>, float> result = (Dictionary<int, float> Variables) =>
        {
            for (int i = 0; i < innerActions.Count; i++)
            {
                innerActions[i](Variables);
            }

            return finalValue(Variables);
        };

        return result;
    }

    void GenPlasma(int x0, int x1, int y0, int y1, float dt)
    {
        int midx = Mathf.FloorToInt((x0 + x1) / 2f);
        int midy = Mathf.FloorToInt((y0 + y1) / 2f);

        float t00 = blendingVertInfoC[y0 * (MeshSize.x + 1) + x0];
        float t01 = blendingVertInfoC[y0 * (MeshSize.x + 1) + x1];
        float t10 = blendingVertInfoC[y1 * (MeshSize.x + 1) + x0];
        float t11 = blendingVertInfoC[y1 * (MeshSize.x + 1) + x1];

        if (y1 - y0 >= 2)
        {
            if (x0 == 0)
            {
                blendingVertInfoC[midy * (MeshSize.x + 1) + x0] = 0.5f * (t00 + t10) + (UnityEngine.Random.Range(0f, 2f) - 1f) * dt;
            }

            blendingVertInfoC[midy * (MeshSize.x + 1) + x1] = 0.5f * (t01 + t11) + (UnityEngine.Random.Range(0f, 2f) - 1f) * dt;
        }

        if (x1 - x0 >= 2)
        {
            if (y0 == 0)
            {
                blendingVertInfoC[y0 * (MeshSize.x + 1) + midx] = 0.5f * (t00 + t01) + (UnityEngine.Random.Range(0f, 2f) - 1f) * dt;
            }

            blendingVertInfoC[y1 * (MeshSize.x + 1) + midx] = 0.5f * (t10 + t11) + (UnityEngine.Random.Range(0f, 2f) - 1f) * dt;
        }

        if (y1 - y0 >= 2 && x1 - x0 >= 2)
        {
            t00 = blendingVertInfoC[midy * (MeshSize.x + 1) + x0];
            t01 = blendingVertInfoC[midy * (MeshSize.x + 1) + x1];
            t10 = blendingVertInfoC[y0 * (MeshSize.x + 1) + midx];
            t11 = blendingVertInfoC[y1 * (MeshSize.x + 1) + midx];
            blendingVertInfoC[midy * (MeshSize.x + 1) + midx] = 0.25f * (t10 + t11 + t00 + t01) + (UnityEngine.Random.Range(0f, 2f) - 1f) * dt;

            GenPlasma(x0, midx, y0, midy, dt * 0.5f);
            GenPlasma(midx, x1, y0, midy, dt * 0.5f);
            GenPlasma(x0, midx, midy, y1, dt * 0.5f);
            GenPlasma(midx, x1, midy, y1, dt * 0.5f);
        }
    }

    void CreateBlendPattern()
    {
        int mixType = UnityEngine.Random.Range(1, 4);

        if (mixType == 0)
        {
            int nVert = 0;
            for (int y = 0; y <= MeshSize.y; y++)
            {
                for (int x = 0; x <= MeshSize.x; x++)
                {
                    blendingVertInfoA[nVert] = 1;
                    blendingVertInfoC[nVert] = 0;
                    nVert += 1;
                }
            }
        }
        else if (mixType == 1)
        {
            float ang = UnityEngine.Random.Range(0f, 6.28f);
            float vx = Mathf.Cos(ang);
            float vy = Mathf.Sin(ang);
            float band = 0.1f + UnityEngine.Random.Range(0f, 0.2f);
            float invBand = 1.0f / band;

            int nVert = 0;
            for (int y = 0; y <= MeshSize.y; y++)
            {
                float fy = y / (float)MeshSize.y;

                for (int x = 0; x <= MeshSize.x; x++)
                {
                    float fx = x / (float)MeshSize.x;

                    float t = (fx - 0.5f) * vx + (fy - 0.5f) * vy + 0.5f;
                    t = (t - 0.5f) / Mathf.Sqrt(2f) + 0.5f;

                    blendingVertInfoA[nVert] = invBand * (1f + band);
                    blendingVertInfoC[nVert] = -invBand + invBand * t;
                    nVert += 1;
                }
            }
        }
        else if (mixType == 2)
        {
            float band = 0.12f + UnityEngine.Random.Range(0f, 0.13f);
            float invBand = 1.0f / band;

            blendingVertInfoC[0] = UnityEngine.Random.Range(0f, 1f);
            blendingVertInfoC[MeshSize.x] = UnityEngine.Random.Range(0f, 1f);
            blendingVertInfoC[MeshSize.y * (MeshSize.x + 1)] = UnityEngine.Random.Range(0f, 1f);
            blendingVertInfoC[MeshSize.y * (MeshSize.x + 1) + MeshSize.x] = UnityEngine.Random.Range(0f, 1f);
            GenPlasma(0, MeshSize.x, 0, MeshSize.y, 0.25f);

            float minc = blendingVertInfoC[0];
            float maxc = blendingVertInfoC[0];

            int nVert = 0;
            for (int y = 0; y <= MeshSize.y; y++)
            {
                for (int x = 0; x <= MeshSize.x; x++)
                {
                    if (minc > blendingVertInfoC[nVert])
                    {
                        minc = blendingVertInfoC[nVert];
                    }
                    if (maxc < blendingVertInfoC[nVert])
                    {
                        maxc = blendingVertInfoC[nVert];
                    }
                    nVert += 1;
                }
            }

            float mult = 1.0f / (maxc - minc);
            nVert = 0;
            for (int y = 0; y <= MeshSize.y; y++)
            {
                for (int x = 0; x <= MeshSize.x; x++)
                {
                    float t = (blendingVertInfoC[nVert] - minc) * mult;
                    blendingVertInfoA[nVert] = invBand * (1 + band);
                    blendingVertInfoC[nVert] = -invBand + invBand * t;
                    nVert += 1;
                }
            }
        }
        else if (mixType == 3)
        {
            float band = 0.02f + UnityEngine.Random.Range(0f, 0.14f) + UnityEngine.Random.Range(0f, 0.34f);
            float invBand = 1.0f / band;
            int dir = UnityEngine.Random.Range(0, 2) * 2 - 1;

            int nVert = 0;
            for (int y = 0; y <= MeshSize.y; y++)
            {
                float dy = (y / (float)MeshSize.y - 0.5f);
                for (int x = 0; x <= MeshSize.x; x++)
                {
                    float dx = (x / (float)MeshSize.x - 0.5f);

                    float t = Mathf.Sqrt(dx * dx + dy * dy) * 1.41421f;

                    if (dir == -1)
                    {
                        t = 1f - t;
                    }

                    blendingVertInfoA[nVert] = invBand * (1 + band);
                    blendingVertInfoC[nVert] = -invBand + invBand * t;
                    nVert += 1;
                }
            }
        }
    }

    public void PlayPreset(string preset)
    {
        if (CurrentPreset != null)
        {
            CreateBlendPattern();

            blending = true;
            blendStartTime = CurrentTime;
            blendDuration = TransitionTime;
            blendProgress = 0f;

            SetVariable(LoadedPresets[preset].BaseVariables, "old_wave_mode", GetVariable(CurrentPreset.BaseVariables, "wave_mode"));
        }

        PresetName = preset;

        PrevPreset = CurrentPreset;
        CurrentPreset = LoadedPresets[preset];

        CurrentPreset.Variables = new Dictionary<int, float>();

        foreach (var v in CurrentPreset.BaseVariables.Keys)
        {
            SetVariable(CurrentPreset.Variables, v, CurrentPreset.BaseVariables[v]);
        }

        SetVariable(CurrentPreset.Variables, "frame", CurrentFrame);
        SetVariable(CurrentPreset.Variables, "time", CurrentTime);
        SetVariable(CurrentPreset.Variables, "fps", FPS == 0f ? 30f : FPS);
        SetVariable(CurrentPreset.Variables, "bass", Bass);
        SetVariable(CurrentPreset.Variables, "bass_att", BassAtt);
        SetVariable(CurrentPreset.Variables, "mid", Mid);
        SetVariable(CurrentPreset.Variables, "mid_att", MidAtt);
        SetVariable(CurrentPreset.Variables, "treb", Treb);
        SetVariable(CurrentPreset.Variables, "treb_att", TrebAtt);
        SetVariable(CurrentPreset.Variables, "meshx", MeshSize.x);
        SetVariable(CurrentPreset.Variables, "meshy", MeshSize.y);
        SetVariable(CurrentPreset.Variables, "aspectx", 1f);
        SetVariable(CurrentPreset.Variables, "aspecty", 1f);
        SetVariable(CurrentPreset.Variables, "pixelsx", Resolution.x);
        SetVariable(CurrentPreset.Variables, "pixelsy", Resolution.y);
        SetVariable(CurrentPreset.Variables, "rand_start.x", UnityEngine.Random.Range(0f, 1f));
        SetVariable(CurrentPreset.Variables, "rand_start.y", UnityEngine.Random.Range(0f, 1f));
        SetVariable(CurrentPreset.Variables, "rand_start.z", UnityEngine.Random.Range(0f, 1f));
        SetVariable(CurrentPreset.Variables, "rand_start.w", UnityEngine.Random.Range(0f, 1f));
        SetVariable(CurrentPreset.Variables, "rand_preset.x", UnityEngine.Random.Range(0f, 1f));
        SetVariable(CurrentPreset.Variables, "rand_preset.y", UnityEngine.Random.Range(0f, 1f));
        SetVariable(CurrentPreset.Variables, "rand_preset.z", UnityEngine.Random.Range(0f, 1f));
        SetVariable(CurrentPreset.Variables, "rand_preset.w", UnityEngine.Random.Range(0f, 1f));

        List<int> nonUserKeys = CurrentPreset.Variables.Keys.ToList();
        nonUserKeys.AddRange(regs);

        var afterInit = new Dictionary<int, float>(CurrentPreset.Variables);

        CurrentPreset.InitEquationCompiled(afterInit);

        CurrentPreset.InitVariables = Pick(afterInit, qs);
        CurrentPreset.RegVariables = Pick(afterInit, regs);
        var initUserVars = Pick(afterInit, nonUserKeys.ToArray());

        CurrentPreset.FrameVariables = new Dictionary<int, float>(CurrentPreset.Variables);

        foreach (var v in CurrentPreset.InitVariables.Keys)
        {
            SetVariable(CurrentPreset.FrameVariables, v, CurrentPreset.InitVariables[v]);
        }

        foreach (var v in CurrentPreset.RegVariables.Keys)
        {
            SetVariable(CurrentPreset.FrameVariables, v, CurrentPreset.RegVariables[v]);
        }

        CurrentPreset.FrameEquationCompiled(CurrentPreset.FrameVariables);

        CurrentPreset.UserKeys = Omit(CurrentPreset.FrameVariables, nonUserKeys.ToArray()).Keys.ToArray();
        CurrentPreset.FrameMap = Pick(CurrentPreset.FrameVariables, CurrentPreset.UserKeys);
        CurrentPreset.AfterFrameVariables = Pick(CurrentPreset.FrameVariables, qs);
        CurrentPreset.RegVariables = Pick(CurrentPreset.FrameVariables, regs);

        if (CurrentPreset.Waves.Count > 0)
        {
            foreach (var CurrentWave in CurrentPreset.Waves)
            {
                CurrentWave.PointsDataL = new float[MaxSamples];
                CurrentWave.PointsDataR = new float[MaxSamples];

                CurrentWave.Positions = new Vector3[MaxSamples];
                CurrentWave.Colors = new Color[MaxSamples];
                CurrentWave.SmoothedPositions = new Vector3[MaxSamples * 2 - 1];
                CurrentWave.SmoothedColors = new Color[MaxSamples * 2 - 1];

                if (GetVariable(CurrentWave.BaseVariables, "enabled") != 0f)
                {
                    CurrentWave.Variables = new Dictionary<int, float>();

                    foreach (var v in CurrentWave.BaseVariables.Keys)
                    {
                        SetVariable(CurrentWave.Variables, v, CurrentWave.BaseVariables[v]);
                    }

                    SetVariable(CurrentWave.Variables, "frame", CurrentFrame);
                    SetVariable(CurrentWave.Variables, "time", CurrentTime);
                    SetVariable(CurrentWave.Variables, "fps", FPS);
                    SetVariable(CurrentWave.Variables, "bass", Bass);
                    SetVariable(CurrentWave.Variables, "bass_att", BassAtt);
                    SetVariable(CurrentWave.Variables, "mid", Mid);
                    SetVariable(CurrentWave.Variables, "mid_att", MidAtt);
                    SetVariable(CurrentWave.Variables, "treb", Treb);
                    SetVariable(CurrentWave.Variables, "treb_att", TrebAtt);
                    SetVariable(CurrentWave.Variables, "meshx", MeshSize.x);
                    SetVariable(CurrentWave.Variables, "meshy", MeshSize.y);
                    SetVariable(CurrentWave.Variables, "aspectx", 1f);
                    SetVariable(CurrentWave.Variables, "aspecty", 1f);
                    SetVariable(CurrentWave.Variables, "pixelsx", Resolution.x);
                    SetVariable(CurrentWave.Variables, "pixelsy", Resolution.y);
                    SetVariable(CurrentWave.Variables, "rand_start.x", GetVariable(CurrentWave.BaseVariables, "rand_start.x"));
                    SetVariable(CurrentWave.Variables, "rand_start.y", GetVariable(CurrentWave.BaseVariables, "rand_start.y"));
                    SetVariable(CurrentWave.Variables, "rand_start.z", GetVariable(CurrentWave.BaseVariables, "rand_start.z"));
                    SetVariable(CurrentWave.Variables, "rand_start.w", GetVariable(CurrentWave.BaseVariables, "rand_start.w"));
                    SetVariable(CurrentWave.Variables, "rand_preset.x", GetVariable(CurrentWave.BaseVariables, "rand_preset.x"));
                    SetVariable(CurrentWave.Variables, "rand_preset.y", GetVariable(CurrentWave.BaseVariables, "rand_preset.y"));
                    SetVariable(CurrentWave.Variables, "rand_preset.z", GetVariable(CurrentWave.BaseVariables, "rand_preset.z"));
                    SetVariable(CurrentWave.Variables, "rand_preset.w", GetVariable(CurrentWave.BaseVariables, "rand_preset.w"));

                    List<int> nonUserWaveKeys = CurrentWave.Variables.Keys.ToList();
                    nonUserWaveKeys.AddRange(regs);
                    nonUserWaveKeys.AddRange(ts);

                    foreach (var v in CurrentPreset.AfterFrameVariables.Keys)
                    {
                        SetVariable(CurrentWave.Variables, v, CurrentPreset.AfterFrameVariables[v]);
                    }

                    foreach (var v in CurrentPreset.RegVariables.Keys)
                    {
                        SetVariable(CurrentWave.Variables, v, CurrentPreset.RegVariables[v]);
                    }

                    CurrentWave.InitEquationCompiled(CurrentWave.Variables);
                    
                    CurrentPreset.RegVariables = Pick(CurrentWave.Variables, regs);

                    foreach (var v in CurrentWave.BaseVariables.Keys)
                    {
                        SetVariable(CurrentWave.Variables, v, CurrentWave.BaseVariables[v]);
                    }

                    CurrentWave.Inits = Pick(CurrentWave.Variables, ts);
                    CurrentWave.UserKeys = Omit(CurrentWave.Variables, nonUserWaveKeys.ToArray()).Keys.ToArray();
                    CurrentWave.FrameMap = Pick(CurrentWave.Variables, CurrentWave.UserKeys);
                }
            }
        }

        if (CurrentPreset.Shapes.Count > 0)
        {
            foreach (var CurrentShape in CurrentPreset.Shapes)
            {
                CurrentShape.ShapeMesh = new Mesh();

                CurrentShape.Positions = new Vector3[MaxShapeSides + 2];
                CurrentShape.Colors = new Color[MaxShapeSides + 2];
                CurrentShape.UVs = new Vector2[MaxShapeSides + 2];
                CurrentShape.BorderPositions = new Vector3[MaxShapeSides + 1];

                if (GetVariable(CurrentShape.BaseVariables, "enabled") != 0f)
                {
                    CurrentShape.Variables = new Dictionary<int, float>();

                    foreach (var v in CurrentShape.BaseVariables.Keys)
                    {
                        SetVariable(CurrentShape.Variables, v, CurrentShape.BaseVariables[v]);
                    }

                    SetVariable(CurrentShape.Variables, "frame", CurrentFrame);
                    SetVariable(CurrentShape.Variables, "time", CurrentTime);
                    SetVariable(CurrentShape.Variables, "fps", FPS);
                    SetVariable(CurrentShape.Variables, "bass", Bass);
                    SetVariable(CurrentShape.Variables, "bass_att", BassAtt);
                    SetVariable(CurrentShape.Variables, "mid", Mid);
                    SetVariable(CurrentShape.Variables, "mid_att", MidAtt);
                    SetVariable(CurrentShape.Variables, "treb", Treb);
                    SetVariable(CurrentShape.Variables, "treb_att", TrebAtt);
                    SetVariable(CurrentShape.Variables, "meshx", MeshSize.x);
                    SetVariable(CurrentShape.Variables, "meshy", MeshSize.y);
                    SetVariable(CurrentShape.Variables, "aspectx", 1f);
                    SetVariable(CurrentShape.Variables, "aspecty", 1f);
                    SetVariable(CurrentShape.Variables, "pixelsx", Resolution.x);
                    SetVariable(CurrentShape.Variables, "pixelsy", Resolution.y);
                    SetVariable(CurrentShape.Variables, "rand_start.x", GetVariable(CurrentShape.BaseVariables, "rand_start.x"));
                    SetVariable(CurrentShape.Variables, "rand_start.y", GetVariable(CurrentShape.BaseVariables, "rand_start.y"));
                    SetVariable(CurrentShape.Variables, "rand_start.z", GetVariable(CurrentShape.BaseVariables, "rand_start.z"));
                    SetVariable(CurrentShape.Variables, "rand_start.w", GetVariable(CurrentShape.BaseVariables, "rand_start.w"));
                    SetVariable(CurrentShape.Variables, "rand_preset.x", GetVariable(CurrentShape.BaseVariables, "rand_preset.x"));
                    SetVariable(CurrentShape.Variables, "rand_preset.y", GetVariable(CurrentShape.BaseVariables, "rand_preset.y"));
                    SetVariable(CurrentShape.Variables, "rand_preset.z", GetVariable(CurrentShape.BaseVariables, "rand_preset.z"));
                    SetVariable(CurrentShape.Variables, "rand_preset.w", GetVariable(CurrentShape.BaseVariables, "rand_preset.w"));

                    List<int> nonUserShapeKeys = CurrentShape.Variables.Keys.ToList();
                    nonUserShapeKeys.AddRange(regs);
                    nonUserShapeKeys.AddRange(ts);

                    foreach (var v in CurrentPreset.AfterFrameVariables.Keys)
                    {
                        SetVariable(CurrentShape.Variables, v, CurrentPreset.AfterFrameVariables[v]);
                    }

                    foreach (var v in CurrentPreset.RegVariables.Keys)
                    {
                        SetVariable(CurrentShape.Variables, v, CurrentPreset.RegVariables[v]);
                    }

                    CurrentShape.InitEquationCompiled(CurrentShape.Variables);

                    CurrentPreset.RegVariables = Pick(CurrentShape.Variables, regs);

                    foreach (var v in CurrentShape.BaseVariables.Keys)
                    {
                        SetVariable(CurrentShape.Variables, v, CurrentShape.BaseVariables[v]);
                    }

                    CurrentShape.Inits = Pick(CurrentShape.Variables, ts);
                    CurrentShape.UserKeys = Omit(CurrentShape.Variables, nonUserShapeKeys.ToArray()).Keys.ToArray();
                    CurrentShape.FrameMap = Pick(CurrentShape.Variables, CurrentShape.UserKeys);
                }
            }
        }

        if (string.IsNullOrEmpty(CurrentPreset.Warp))
        {
            CurrentPreset.WarpMaterial = new Material(DefaultWarpShader);
        }
        else
        {
            throw new System.NotImplementedException("Compiling shaders is not supported yet");
        }

        if (string.IsNullOrEmpty(CurrentPreset.Comp))
        {
            CurrentPreset.CompMaterial = new Material(DefaultCompShader);
        }
        else
        {
            throw new System.NotImplementedException("Compiling shaders is not supported yet");
        }

        CurrentPreset.DarkenCenterMaterial = new Material(DarkenCenterShader);
    }
}
