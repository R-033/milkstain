using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using System;

public class Milkdrop : MonoBehaviour
{
    public class Wave
    {
        public Dictionary<string, float> BaseVariables = new Dictionary<string, float>();
        public string InitEquation = "";
        public string FrameEquation = "";
        public string PointEquation = "";
    }

    public class Shape
    {
        public Dictionary<string, float> BaseVariables = new Dictionary<string, float>();
        public string InitEquation = "";
        public string FrameEquation = "";
    }

    public class Preset
    {
        public Dictionary<string, float> BaseVariables = new Dictionary<string, float>();
        public string InitEquation = "";
        public Action<Dictionary<string, float>> InitEquationCompiled;
        public string FrameEquation = "";
        public Action<Dictionary<string, float>> FrameEquationCompiled;
        public string PixelEquation = "";
        public Action<Dictionary<string, float>> PixelEquationCompiled;
        public List<Wave> Waves = new List<Wave>();
        public List<Shape> Shapes = new List<Shape>();
        public string WarpEquation = "";
        public string CompEquation = "";
        public string Warp;
        public string Comp;

        public Dictionary<string, float> Variables = new Dictionary<string, float>();
        public Dictionary<string, float> InitVariables = new Dictionary<string, float>();
        public Dictionary<string, float> RegVariables = new Dictionary<string, float>();
        public Dictionary<string, float> FrameVariables = new Dictionary<string, float>();
        public Dictionary<string, float> PixelVariables = new Dictionary<string, float>();

        public string[] UserKeys = new string[0];
        public Dictionary<string, float> FrameMap = new Dictionary<string, float>();
        public Dictionary<string, float> AfterFrameVariables = new Dictionary<string, float>();

        public Material WarpMaterial;
        public Material DarkenCenterMaterial;
        public Material CompMaterial;
    }

    public Dictionary<string, Preset> LoadedPresets = new Dictionary<string, Preset>();

    public string PresetPath;

    private Preset CurrentPreset;

    public Vector2Int MeshSize = new Vector2Int(48, 36);
    public Vector2Int MeshSizeComp = new Vector2Int(32, 24);
    public Vector2Int Resolution = new Vector2Int(1200, 900);
    public int BasicWaveformNumAudioSamples = 512;
    public float MaxFPS = 30f;

    public float ChangePresetIn = 5f;

    private float presetChangeTimer = 0f;

    private float Bass;
    private float BassAtt;
    private float Mid;
    private float MidAtt;
    private float Treb;
    private float TrebAtt;

    public bool ResetBG;

    public bool EnableWarp = true;
    public bool EnableDarkenCenter = true;
    public bool EnableBasicWaveform = true;
    public bool EnableComp = true;

    public RawImage TargetGraphic;

    public MeshFilter TargetMeshFilter;
    public MeshRenderer TargetMeshRenderer;

    public LineRenderer WaveformRenderer;
    public LineRenderer WaveformRenderer2;

    public Camera TargetCamera;

    public AudioSource TargetAudio;

    public Shader DefaultWarpShader;
    public Shader DarkenCenterShader;
    public Shader DefaultCompShader;

    public Material DoNothingMaterial;

    public Texture TestBackground;

    private ulong FrameNum = 0;
    private float CurrentTime = 0f;

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

    private string[] qs = new string[]
    {
        "q1", "q2", "q3", "q4", "q5", "q6", "q7", "q8",
        "q9", "q10", "q11","q12", "q13", "q14", "q15", "q16",
        "q17", "q18", "q19", "q20", "q21", "q22", "q23", "q24",
        "q25", "q26", "q27", "q28", "q29", "q30", "q31", "q32",
    };

    private string[] ts = new string[]
    {
        "t1", "t2", "t3", "t4", "t5", "t6", "t7", "t8"
    };

    private string[] regs = new string[99];

    private Vector2[] WarpUVs;
    private Color[] WarpColor;
    private Color[] CompColor;

    private RenderTexture FinalTexture;

    private Mesh TargetMeshWarp;
    private Mesh TargetMeshDarkenCenter;
    private Mesh TargetMeshComp;

    private float[] timeArrayL;
    private float[] timeArrayR;

    private Vector3[] BasicWaveFormPositions;
    private Vector3[] BasicWaveFormPositions2;

    private float sampleRate;

    private float bassLow;
    private float bassHigh;
    private float midHigh;
    private float trebHigh;

    private float timeSinceLastFrame = 0f;

    public float FPS;

    Dictionary<string, float> Pick(Dictionary<string, float> source, string[] keys)
    {
        Dictionary<string, float> result = new Dictionary<string, float>();

        foreach (string key in source.Keys.Where(x => keys.Contains(x)))
        {
            result.Add(key, source[key]);
        }

        return result;
    }

    Dictionary<string, float> Omit(Dictionary<string, float> source, string[] keys)
    {
        Dictionary<string, float> result = new Dictionary<string, float>();

        foreach (string key in source.Keys.Where(x => !keys.Contains(x)))
        {
            result.Add(key, source[key]);
        }

        return result;
    }

    void Start()
    {
        for (int i = 0; i < regs.Length; i++)
        {
            regs[i] = i < 10 ? "reg0" + i : "reg" + i;
        }
        
        UnloadPresets();
        LoadPresetsFolder(PresetPath);

        WarpUVs = new Vector2[(MeshSize.x + 1) * (MeshSize.y + 1)];
        WarpColor = new Color[(MeshSize.x + 1) * (MeshSize.y + 1)];
        CompColor = new Color[(MeshSizeComp.x + 1) * (MeshSizeComp.y + 1)];

        BasicWaveFormPositions = new Vector3[BasicWaveformNumAudioSamples];
        BasicWaveFormPositions2 = new Vector3[BasicWaveformNumAudioSamples];

        timeArrayL = new float[BasicWaveformNumAudioSamples];
        timeArrayR = new float[BasicWaveformNumAudioSamples];

        FinalTexture = new RenderTexture(Resolution.x, Resolution.y, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm);

        TargetGraphic.texture = FinalTexture;

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

        WaveformRenderer.enabled = false;
        WaveformRenderer2.enabled = false;

        sampleRate = AudioSettings.outputSampleRate * 0.5f;

        bassLow = Mathf.Clamp(
            0,
            0,
            BasicWaveformNumAudioSamples - 1
        );

        bassHigh = Mathf.Clamp(
            BasicWaveformNumAudioSamples / 3f,
            0,
            BasicWaveformNumAudioSamples - 1
        );

        midHigh = Mathf.Clamp(
            BasicWaveformNumAudioSamples / 3f * 2f,
            0,
            BasicWaveformNumAudioSamples - 1
        );

        trebHigh = BasicWaveformNumAudioSamples - 1;

        PlayRandomPreset();
    }

    int index = 0;

    public void PlayRandomPreset()
    {
        var keys = LoadedPresets.Keys.ToArray();
        //int ind = UnityEngine.Random.Range(0, keys.Length);
        int ind = index++;
        if (index >= keys.Length)
        {
            index = 0;
        }
        PlayPreset(keys[ind]);
    }

    void Update()
    {
        if (Time.timeScale == 0f)
            return;
        
        presetChangeTimer += Time.deltaTime;

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
        FrameNum++;

        TargetAudio.GetSpectrumData(timeArrayL, 0, FFTWindow.Rectangular);
        TargetAudio.GetSpectrumData(timeArrayR, 1, FFTWindow.Rectangular);

        Bass = 0f;
        BassAtt = 0f;
        Mid = 0f;
        MidAtt = 0f;
        Treb = 0f;
        TrebAtt = 0f;

        for (int i = 0; i < BasicWaveformNumAudioSamples; i++)
        {
            timeArrayL[i] *= 8000f;
            timeArrayR[i] *= 8000f;
        }

        for (int i = 0; i < BasicWaveformNumAudioSamples; i++)
        {
            if (i >= bassLow && i < bassHigh)
            {
                Bass += timeArrayL[i] + timeArrayR[i];
            }
            else if (i >= bassHigh && i < midHigh)
            {
                Mid += timeArrayL[i] + timeArrayR[i];
            }
            else if (i >= midHigh && i < trebHigh)
            {
                Treb += timeArrayL[i] + timeArrayR[i];
            }
        }

        Bass /= (bassHigh - bassLow) * 2f;
        Mid /= (midHigh - bassHigh) * 2f;
        Treb /= (trebHigh - midHigh) * 2f;

        // todo average
        BassAtt = Bass;
        MidAtt = Mid;
        TrebAtt = Treb;

        RunFrameEquations();
        RunPixelEquations();

        // todo blending

        RenderImage();
    }

    void RunFrameEquations()
    {
        CurrentPreset.FrameVariables = new Dictionary<string, float>(CurrentPreset.Variables);

        foreach (var v in CurrentPreset.InitVariables.Keys)
        {
            SetVariable(CurrentPreset.FrameVariables, v, CurrentPreset.InitVariables[v]);
        }

        foreach (var v in CurrentPreset.FrameMap.Keys)
        {
            SetVariable(CurrentPreset.FrameVariables, v, CurrentPreset.FrameMap[v]);
        }

        SetVariable(CurrentPreset.FrameVariables, "frame", FrameNum);
        SetVariable(CurrentPreset.FrameVariables, "time", CurrentTime);
        SetVariable(CurrentPreset.FrameVariables, "fps", FPS);
        SetVariable(CurrentPreset.FrameVariables, "bass", Bass);
        SetVariable(CurrentPreset.FrameVariables, "bass_att", BassAtt);
        SetVariable(CurrentPreset.FrameVariables, "mid", Mid);
        SetVariable(CurrentPreset.FrameVariables, "mid_att", MidAtt);
        SetVariable(CurrentPreset.FrameVariables, "treb", Treb);
        SetVariable(CurrentPreset.FrameVariables, "treb_att", TrebAtt);
        SetVariable(CurrentPreset.FrameVariables, "meshx", MeshSize.x);
        SetVariable(CurrentPreset.FrameVariables, "meshy", MeshSize.y);
        SetVariable(CurrentPreset.FrameVariables, "aspectx", 1f);
        SetVariable(CurrentPreset.FrameVariables, "aspecty", 1f);
        SetVariable(CurrentPreset.FrameVariables, "pixelsx", Resolution.x);
        SetVariable(CurrentPreset.FrameVariables, "pixelsy", Resolution.y);

        CurrentPreset.FrameEquationCompiled(CurrentPreset.FrameVariables);
    }

    void RunPixelEquations()
    {
        int gridX = MeshSize.x;
        int gridZ = MeshSize.y;

        int gridX1 = gridX + 1;
        int gridZ1 = gridZ + 1;

        float warpTimeV = CurrentTime * GetVariable(CurrentPreset.FrameVariables, "warpanimspeed");
        float warpScaleInv = 1f / GetVariable(CurrentPreset.FrameVariables, "warpscale");

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

        foreach (var v in CurrentPreset.FrameVariables.Keys)
        {
            SetVariable(CurrentPreset.PixelVariables, v, CurrentPreset.FrameVariables[v]);
        }

        float warp = GetVariable(CurrentPreset.PixelVariables, "warp");
        float zoom = GetVariable(CurrentPreset.PixelVariables, "zoom");
        float zoomExp = GetVariable(CurrentPreset.PixelVariables, "zoomexp");
        float cx = GetVariable(CurrentPreset.PixelVariables, "cx");
        float cy = GetVariable(CurrentPreset.PixelVariables, "cy");
        float sx = GetVariable(CurrentPreset.PixelVariables, "sx");
        float sy = GetVariable(CurrentPreset.PixelVariables, "sy");
        float dx = GetVariable(CurrentPreset.PixelVariables, "dx");
        float dy = GetVariable(CurrentPreset.PixelVariables, "dy");
        float rot = GetVariable(CurrentPreset.PixelVariables, "rot");

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

                SetVariable(CurrentPreset.PixelVariables, "x", x * 0.5f * aspectx + 0.5f);
                SetVariable(CurrentPreset.PixelVariables, "y", y * -0.5f * aspecty + 0.5f);
                SetVariable(CurrentPreset.PixelVariables, "rad", rad);
                SetVariable(CurrentPreset.PixelVariables, "ang", ang);

                SetVariable(CurrentPreset.PixelVariables, "zoom", frameZoom);
                SetVariable(CurrentPreset.PixelVariables, "zoomexp", frameZoomExp);
                SetVariable(CurrentPreset.PixelVariables, "rot", frameRot);
                SetVariable(CurrentPreset.PixelVariables, "warp", frameWarp);
                SetVariable(CurrentPreset.PixelVariables, "cx", framecx);
                SetVariable(CurrentPreset.PixelVariables, "cy", framecy);
                SetVariable(CurrentPreset.PixelVariables, "dx", framedx);
                SetVariable(CurrentPreset.PixelVariables, "dy", framedy);
                SetVariable(CurrentPreset.PixelVariables, "sx", framesx);
                SetVariable(CurrentPreset.PixelVariables, "sy", framesy);

                CurrentPreset.PixelEquationCompiled(CurrentPreset.PixelVariables);

                warp = GetVariable(CurrentPreset.PixelVariables, "warp");
                zoom = GetVariable(CurrentPreset.PixelVariables, "zoom");
                zoomExp = GetVariable(CurrentPreset.PixelVariables, "zoomexp");
                cx = GetVariable(CurrentPreset.PixelVariables, "cx");
                cy = GetVariable(CurrentPreset.PixelVariables, "cy");
                sx = GetVariable(CurrentPreset.PixelVariables, "sx");
                sy = GetVariable(CurrentPreset.PixelVariables, "sy");
                dx = GetVariable(CurrentPreset.PixelVariables, "dx");
                dy = GetVariable(CurrentPreset.PixelVariables, "dy");
                rot = GetVariable(CurrentPreset.PixelVariables, "rot");

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

                bool blending = false; // todo

                if (!blending)
                {
                    WarpUVs[offset] = new Vector2(u, v);
                    WarpColor[offsetColor] = Color.white;
                }
                else
                {
                    // todo blending
                }

                offset++;
                offsetColor++;
            }
        }
    }

    (float[], float[]) GetBlurValues(Dictionary<string, float> variables)
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
        FinalTexture.wrapMode = GetVariable(CurrentPreset.FrameVariables, "wrap") == 0f ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;

        if (EnableWarp)
        {
            DrawWarp();
        }

        // todo blending support

        // todo blur

        // todo motion vectors

        // todo shapes

        // todo waves

        // todo shapes & waves blending

        if (EnableBasicWaveform)
        {
            DrawBasicWaveform();
        }

        if (EnableDarkenCenter)
        {
            DrawDarkenCenter();
        }

        // outer border

        // inner border

        // text

        if (EnableComp)
        {
            DrawComp();
        }
    }

    void DrawWarp()
    {
        if (CurrentPreset.WarpMaterial == null)
        {
            return;
        }

        TargetMeshFilter.sharedMesh = TargetMeshWarp;
        TargetMeshWarp.SetUVs(0, WarpUVs);
        TargetMeshWarp.SetColors(WarpColor);

        //(float[], float[]) blurValues = GetBlurValues(CurrentPreset.FrameVariables);

        TargetMeshRenderer.sharedMaterial = CurrentPreset.WarpMaterial;

        if (ResetBG)
        {
            ResetBG = false;
            CurrentPreset.WarpMaterial.mainTexture = TestBackground;
        }
        else
        {
            CurrentPreset.WarpMaterial.mainTexture = FinalTexture;
        }

        /*CurrentPreset.WarpMaterial.SetTexture("_MainTex2", FinalTexture);
        CurrentPreset.WarpMaterial.SetTexture("_MainTex3", FinalTexture);
        CurrentPreset.WarpMaterial.SetTexture("_MainTex4", FinalTexture);
        CurrentPreset.WarpMaterial.SetTexture("_MainTex5", FinalTexture);*/

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

        CurrentPreset.WarpMaterial.SetFloat("decay", GetVariable(CurrentPreset.FrameVariables, "decay"));
        /*CurrentPreset.WarpMaterial.SetVector("resolution", new Vector2(Resolution.x, Resolution.y));
        CurrentPreset.WarpMaterial.SetVector("aspect", new Vector4(1f, 1f, 1f, 1f));
        CurrentPreset.WarpMaterial.SetVector("texsize", new Vector4(Resolution.x, Resolution.y, 1f / Resolution.x, 1f / Resolution.y));
        CurrentPreset.WarpMaterial.SetVector("texsize_noise_lq", new Vector4(256, 256, 1f / 256f, 1f / 256f));
        CurrentPreset.WarpMaterial.SetVector("texsize_noise_mq", new Vector4(256, 256, 1f / 256f, 1f / 256));
        CurrentPreset.WarpMaterial.SetVector("texsize_noise_hq", new Vector4(256, 256, 1f / 256f, 1f / 256f));
        CurrentPreset.WarpMaterial.SetVector("texsize_noise_lq_lite", new Vector4(32, 32, 1f / 32f, 1f / 32f));
        CurrentPreset.WarpMaterial.SetVector("texsize_noisevol_lq", new Vector4(32, 32, 1f / 32f, 1f / 32f));
        CurrentPreset.WarpMaterial.SetVector("texsize_noisevol_hq", new Vector4(32, 32, 1f / 32f, 1f / 32f));
        CurrentPreset.WarpMaterial.SetFloat("bass", Bass);
        CurrentPreset.WarpMaterial.SetFloat("mid", Mid);
        CurrentPreset.WarpMaterial.SetFloat("treb", Treb);
        CurrentPreset.WarpMaterial.SetFloat("vol", (Bass + Mid + Treb) / 3f);
        CurrentPreset.WarpMaterial.SetFloat("bass_att", BassAtt);
        CurrentPreset.WarpMaterial.SetFloat("mid_att", MidAtt);
        CurrentPreset.WarpMaterial.SetFloat("treb_att", TrebAtt);
        CurrentPreset.WarpMaterial.SetFloat("vol_att", (BassAtt + MidAtt + TrebAtt) / 3f);
        CurrentPreset.WarpMaterial.SetFloat("time", CurrentTime);
        CurrentPreset.WarpMaterial.SetFloat("frame", FrameNum);
        CurrentPreset.WarpMaterial.SetFloat("fps", FPS);
        CurrentPreset.WarpMaterial.SetVector("rand_preset", 
            new Vector4(
                GetVariable(CurrentPreset.FrameVariables, "rand_preset.x"),
                GetVariable(CurrentPreset.FrameVariables, "rand_preset.y"),
                GetVariable(CurrentPreset.FrameVariables, "rand_preset.z"),
                GetVariable(CurrentPreset.FrameVariables, "rand_preset.w")
            )
        );
        CurrentPreset.WarpMaterial.SetVector("rand_frame", 
            new Vector4(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f)
            )
        );
        CurrentPreset.WarpMaterial.SetVector("_qa", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q1"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q2"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q3"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q4")
            )
        );
        CurrentPreset.WarpMaterial.SetVector("_qb", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q5"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q6"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q7"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q8")
            )
        );
        CurrentPreset.WarpMaterial.SetVector("_qc", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q9"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q10"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q11"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q12")
            )
        );
        CurrentPreset.WarpMaterial.SetVector("_qd", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q13"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q14"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q15"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q16")
            )
        );
        CurrentPreset.WarpMaterial.SetVector("_qe", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q17"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q18"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q19"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q20")
            )
        );
        CurrentPreset.WarpMaterial.SetVector("_qf", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q21"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q22"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q23"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q24")
            )
        );
        CurrentPreset.WarpMaterial.SetVector("_qg", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q25"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q26"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q27"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q28")
            )
        );
        CurrentPreset.WarpMaterial.SetVector("_qh", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q29"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q30"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q31"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q32")
            )
        );
        CurrentPreset.WarpMaterial.SetVector("slow_roam_cos", 
            new Vector4(
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.005f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.008f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.013f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.022f)
            )
        );
        CurrentPreset.WarpMaterial.SetVector("roam_cos", 
            new Vector4(
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.3f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 1.3f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 5.0f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 20.0f)
            )
        );
        CurrentPreset.WarpMaterial.SetVector("slow_roam_sin", 
            new Vector4(
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.005f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.008f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.013f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.022f)
            )
        );
        CurrentPreset.WarpMaterial.SetVector("roam_sin", 
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

        CurrentPreset.WarpMaterial.SetFloat("blur1_min", blurMin1);
        CurrentPreset.WarpMaterial.SetFloat("blur1_max", blurMax1);
        CurrentPreset.WarpMaterial.SetFloat("blur2_min", blurMin2);
        CurrentPreset.WarpMaterial.SetFloat("blur2_max", blurMax2);
        CurrentPreset.WarpMaterial.SetFloat("blur3_min", blurMin3);
        CurrentPreset.WarpMaterial.SetFloat("blur3_max", blurMax3);
        CurrentPreset.WarpMaterial.SetFloat("scale1", scale1);
        CurrentPreset.WarpMaterial.SetFloat("scale2", scale2);
        CurrentPreset.WarpMaterial.SetFloat("scale3", scale3);
        CurrentPreset.WarpMaterial.SetFloat("bias1", bias1);
        CurrentPreset.WarpMaterial.SetFloat("bias2", bias2);
        CurrentPreset.WarpMaterial.SetFloat("bias3", bias3);*/

        TargetCamera.targetTexture = FinalTexture;
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

        if (ResetBG)
        {
            ResetBG = false;
            CurrentPreset.DarkenCenterMaterial.mainTexture = TestBackground;
        }
        else
        {
            CurrentPreset.DarkenCenterMaterial.mainTexture = FinalTexture;
        }

        TargetCamera.targetTexture = FinalTexture;
        TargetCamera.Render();
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

        float newWaveMode = GetVariable(CurrentPreset.FrameVariables, "wave_mode") % 8;
        float oldWaveMode = GetVariable(CurrentPreset.FrameVariables, "old_wave_mode") % 8;

        float wavePosX = GetVariable(CurrentPreset.FrameVariables, "wave_x") * 2f - 1f;
        float wavePosY = GetVariable(CurrentPreset.FrameVariables, "wave_y") * 2f - 1f;

        int numVert = 0;
        //int oldNumVert = 0;

        int its = 1; // if blending 2

        for (int it = 0; it < its; it++)
        {
            float waveMode = (it == 0) ? newWaveMode : oldWaveMode;

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

            //if (it == 0)
            {
                positions = BasicWaveFormPositions;
                positions2 = BasicWaveFormPositions2;
            }
            //else
            //{
                // old positions
            //
            
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
                else if (waveMode == 7)
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

            //if (it == 0)
            //{
                BasicWaveFormPositions = positions;
                BasicWaveFormPositions2 = positions2;
                numVert = localNumVert;
            //}
            //else
            //{
                // old
            //}
        }

        //float blendMix = 0.5f - 0.5f * Mathf.Cos(0f * Mathf.PI);
        //float blendMix2 = 1f - blendMix;

        //if (oldNumVert > 0)
        //{
        //    alpha = blendMix * alpha + blendMix2 * oldAlpha;
        //}

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

        Vector4 color = new Vector4(r, g, b, alpha);

        // if oldNumVert stuff

        int smoothedNumVert = numVert * 2 - 1;

        // smooth positions

        if (newWaveMode == 7 || oldWaveMode == 7)
        {
            // smooth positions2
        }

        WaveformRenderer.enabled = true;
        
        WaveformRenderer.positionCount = numVert;
        WaveformRenderer.SetPositions(BasicWaveFormPositions);

        if (GetVariable(CurrentPreset.FrameVariables, "wave_thick") != 0f || GetVariable(CurrentPreset.FrameVariables, "wave_dots") != 0f)
        {
            WaveformRenderer.widthMultiplier = 2f;
        }
        else
        {
            WaveformRenderer.widthMultiplier = 1f;
        }

        if (newWaveMode == 7 || oldWaveMode == 7)
        {
            WaveformRenderer2.enabled = true;

            WaveformRenderer2.positionCount = numVert;
            WaveformRenderer2.SetPositions(BasicWaveFormPositions2);

            if (GetVariable(CurrentPreset.FrameVariables, "wave_thick") != 0f || GetVariable(CurrentPreset.FrameVariables, "wave_dots") != 0f)
            {
                WaveformRenderer2.widthMultiplier = 2f;
            }
            else
            {
                WaveformRenderer2.widthMultiplier = 1f;
            }
        }

        WaveformRenderer.sharedMaterial.mainTexture = FinalTexture;
        WaveformRenderer.sharedMaterial.SetVector("waveColor", color);
        WaveformRenderer.sharedMaterial.SetFloat("additivewave", GetVariable(CurrentPreset.FrameVariables, "additivewave"));
        WaveformRenderer.sharedMaterial.SetFloat("wave_dots", GetVariable(CurrentPreset.FrameVariables, "wave_dots"));
        WaveformRenderer.sharedMaterial.SetFloat("aspect_ratio", Resolution.x / (float)Resolution.y);

        TargetMeshFilter.sharedMesh = TargetMeshWarp;
        TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

        DoNothingMaterial.mainTexture = FinalTexture;

        TargetCamera.targetTexture = FinalTexture;
        TargetCamera.Render();

        WaveformRenderer.enabled = false;
        WaveformRenderer2.enabled = false;
    }

    void DrawComp()
    {
        if (CurrentPreset.CompMaterial == null)
        {
            return;
        }

        //(float[], float[]) blurValues = GetBlurValues(CurrentPreset.FrameVariables);

        TargetMeshRenderer.sharedMaterial = CurrentPreset.CompMaterial;

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
                    GetVariable(CurrentPreset.FrameVariables, "rand_start.w")
                );
            hueBase[i * 3 + 1] =
                0.6f +
                0.3f *
                Mathf.Sin(
                    CurrentTime * 30.0f * 0.0107f +
                    1f +
                    i * 13f +
                    GetVariable(CurrentPreset.FrameVariables, "rand_start.y")
                );
            hueBase[i * 3 + 2] =
                0.6f +
                0.3f *
                Mathf.Sin(
                    CurrentTime * 30.0f * 0.0129f +
                    6f +
                    i * 9f +
                    GetVariable(CurrentPreset.FrameVariables, "rand_start.z")
                );
            float maxShade = Mathf.Max(hueBase[i * 3], hueBase[i * 3 + 1], hueBase[i * 3 + 2]);
            for (int k = 0; k < 3; k++)
            {
                hueBase[i * 3 + k] = hueBase[i * 3 + k] / maxShade;
                hueBase[i * 3 + k] = 0.5f + 0.5f * hueBase[i * 3 + k];
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

                // todo blending

                CompColor[offsetColor] = new Color
                (
                    0.5f + (hueBase[0] * x * y + hueBase[3] * (1f - x) * y + hueBase[6] * x * (1f - y) + hueBase[9] * (1f - x) * (1f - y))* 0.5f,
                    0.5f + (hueBase[1] * x * y + hueBase[4] * (1f - x) * y + hueBase[7] * x * (1f - y) + hueBase[10] * (1f - x) * (1f - y)) * 0.5f,
                    0.5f + (hueBase[2] * x * y + hueBase[5] * (1f - x) * y + hueBase[8] * x * (1f - y) + hueBase[11] * (1f - x) * (1f - y)) * 0.5f,
                    0.5f + alpha * 0.5f
                );

                offsetColor++;
            }
        }

        TargetMeshFilter.sharedMesh = TargetMeshComp;
        TargetMeshComp.SetColors(CompColor);

        if (ResetBG)
        {
            ResetBG = false;
            CurrentPreset.CompMaterial.mainTexture = TestBackground;
        }
        else
        {
            CurrentPreset.CompMaterial.mainTexture = FinalTexture;
        }

        /*CurrentPreset.CompMaterial.SetTexture("_MainTex2", FinalTexture);
        CurrentPreset.CompMaterial.SetTexture("_MainTex3", FinalTexture);
        CurrentPreset.CompMaterial.SetTexture("_MainTex4", FinalTexture);
        CurrentPreset.CompMaterial.SetTexture("_MainTex5", FinalTexture);*/

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

        //CurrentPreset.CompMaterial.SetFloat("time", CurrentTime);
        CurrentPreset.CompMaterial.SetFloat("gammaAdj", GetVariable(CurrentPreset.FrameVariables, "gammaadj"));
        CurrentPreset.CompMaterial.SetFloat("echo_zoom", GetVariable(CurrentPreset.FrameVariables, "echo_zoom"));
        CurrentPreset.CompMaterial.SetFloat("echo_alpha", GetVariable(CurrentPreset.FrameVariables, "echo_alpha"));
        CurrentPreset.CompMaterial.SetFloat("echo_orientation", GetVariable(CurrentPreset.FrameVariables, "echo_orient"));
        CurrentPreset.CompMaterial.SetFloat("invert", GetVariable(CurrentPreset.FrameVariables, "invert"));
        CurrentPreset.CompMaterial.SetFloat("brighten", GetVariable(CurrentPreset.FrameVariables, "brighten"));
        CurrentPreset.CompMaterial.SetFloat("_darken", GetVariable(CurrentPreset.FrameVariables, "darken"));
        CurrentPreset.CompMaterial.SetFloat("solarize", GetVariable(CurrentPreset.FrameVariables, "solarize"));
        /*CurrentPreset.CompMaterial.SetVector("resolution", new Vector2(Resolution.x, Resolution.y));
        CurrentPreset.CompMaterial.SetVector("aspect", new Vector4(1f, 1f, 1f, 1f));
        CurrentPreset.CompMaterial.SetVector("texsize", new Vector4(Resolution.x, Resolution.y, 1f / Resolution.x, 1f / Resolution.y));
        CurrentPreset.CompMaterial.SetVector("texsize_noise_lq", new Vector4(256, 256, 1f / 256f, 1f / 256f));
        CurrentPreset.CompMaterial.SetVector("texsize_noise_mq", new Vector4(256, 256, 1f / 256f, 1f / 256));
        CurrentPreset.CompMaterial.SetVector("texsize_noise_hq", new Vector4(256, 256, 1f / 256f, 1f / 256f));
        CurrentPreset.CompMaterial.SetVector("texsize_noise_lq_lite", new Vector4(32, 32, 1f / 32f, 1f / 32f));
        CurrentPreset.CompMaterial.SetVector("texsize_noisevol_lq", new Vector4(32, 32, 1f / 32f, 1f / 32f));
        CurrentPreset.CompMaterial.SetVector("texsize_noisevol_hq", new Vector4(32, 32, 1f / 32f, 1f / 32f));
        CurrentPreset.CompMaterial.SetFloat("bass", Bass);
        CurrentPreset.CompMaterial.SetFloat("mid", Mid);
        CurrentPreset.CompMaterial.SetFloat("treb", Treb);
        CurrentPreset.CompMaterial.SetFloat("vol", (Bass + Mid + Treb) / 3f);
        CurrentPreset.CompMaterial.SetFloat("bass_att", BassAtt);
        CurrentPreset.CompMaterial.SetFloat("mid_att", MidAtt);
        CurrentPreset.CompMaterial.SetFloat("treb_att", TrebAtt);
        CurrentPreset.CompMaterial.SetFloat("vol_att", (BassAtt + MidAtt + TrebAtt) / 3f);
        CurrentPreset.CompMaterial.SetFloat("frame", FrameNum);
        CurrentPreset.CompMaterial.SetFloat("fps", FPS);
        CurrentPreset.CompMaterial.SetVector("rand_preset", 
            new Vector4(
                GetVariable(CurrentPreset.FrameVariables, "rand_preset.x"),
                GetVariable(CurrentPreset.FrameVariables, "rand_preset.y"),
                GetVariable(CurrentPreset.FrameVariables, "rand_preset.z"),
                GetVariable(CurrentPreset.FrameVariables, "rand_preset.w")
            )
        );
        CurrentPreset.CompMaterial.SetVector("rand_frame", 
            new Vector4(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f)
            )
        );*/
        CurrentPreset.CompMaterial.SetFloat("fShader", GetVariable(CurrentPreset.FrameVariables, "fshader"));
        /*CurrentPreset.CompMaterial.SetVector("_qa", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q1"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q2"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q3"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q4")
            )
        );
        CurrentPreset.CompMaterial.SetVector("_qb", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q5"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q6"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q7"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q8")
            )
        );
        CurrentPreset.CompMaterial.SetVector("_qc", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q9"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q10"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q11"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q12")
            )
        );
        CurrentPreset.CompMaterial.SetVector("_qd", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q13"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q14"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q15"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q16")
            )
        );
        CurrentPreset.CompMaterial.SetVector("_qe", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q17"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q18"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q19"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q20")
            )
        );
        CurrentPreset.CompMaterial.SetVector("_qf", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q21"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q22"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q23"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q24")
            )
        );
        CurrentPreset.CompMaterial.SetVector("_qg", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q25"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q26"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q27"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q28")
            )
        );
        CurrentPreset.CompMaterial.SetVector("_qh", 
            new Vector4(
                GetVariable(CurrentPreset.AfterFrameVariables, "q29"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q30"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q31"),
                GetVariable(CurrentPreset.AfterFrameVariables, "q32")
            )
        );
        CurrentPreset.CompMaterial.SetVector("slow_roam_cos", 
            new Vector4(
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.005f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.008f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.013f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.022f)
            )
        );
        CurrentPreset.CompMaterial.SetVector("roam_cos", 
            new Vector4(
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 0.3f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 1.3f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 5.0f),
                0.5f + 0.5f * Mathf.Cos(CurrentTime * 20.0f)
            )
        );
        CurrentPreset.CompMaterial.SetVector("slow_roam_sin", 
            new Vector4(
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.005f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.008f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.013f),
                0.5f + 0.5f * Mathf.Sin(CurrentTime * 0.022f)
            )
        );
        CurrentPreset.CompMaterial.SetVector("roam_sin", 
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

        CurrentPreset.CompMaterial.SetFloat("blur1_min", blurMin1);
        CurrentPreset.CompMaterial.SetFloat("blur1_max", blurMax1);
        CurrentPreset.CompMaterial.SetFloat("blur2_min", blurMin2);
        CurrentPreset.CompMaterial.SetFloat("blur2_max", blurMax2);
        CurrentPreset.CompMaterial.SetFloat("blur3_min", blurMin3);
        CurrentPreset.CompMaterial.SetFloat("blur3_max", blurMax3);
        CurrentPreset.CompMaterial.SetFloat("scale1", scale1);
        CurrentPreset.CompMaterial.SetFloat("scale2", scale2);
        CurrentPreset.CompMaterial.SetFloat("scale3", scale3);
        CurrentPreset.CompMaterial.SetFloat("bias1", bias1);
        CurrentPreset.CompMaterial.SetFloat("bias2", bias2);
        CurrentPreset.CompMaterial.SetFloat("bias3", bias3);*/

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

        if (float.IsFinite(result) && !float.IsNaN(result))
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
        {"atan", Func_Atan}
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

        if (string.IsNullOrEmpty(Expression))
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

    float GetVariable(Dictionary<string, float> Variables, string name)
    {
        if (Variables.TryGetValue(name, out float value))
        {
            return value;
        }
        return 0f;
    }

    void SetVariable(Dictionary<string, float> Variables, string name, float value)
    {
        if (Variables.ContainsKey(name))
        {
            Variables[name] = value;
        }
        else
        {
            Variables.Add(name, value);
        }
    }

    public void UnloadPresets()
    {
        LoadedPresets.Clear();
    }

    public void LoadPresetsFolder(string folder)
    {
        foreach (string file in Directory.GetFiles(folder))
        {
            if (file.EndsWith(".milk"))
            {
                LoadPreset(file);
            }
        }
    }

    public void LoadPreset(string file)
    {
        //Debug.Log("Loading " + Path.GetFileNameWithoutExtension(file));

        var preset = new Preset();

        string[] lines = File.ReadAllLines(file);

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
                var lineTrimmed = line.Replace(" ", "").ToLower();

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

            if (arg.StartsWith("wave_"))
            {
                // todo
            }
            else if (arg.StartsWith("wavecode_"))
            {
                // todo
            }
            else if (arg.StartsWith("shape_"))
            {
                // todo
            }
            else if (arg.StartsWith("shapecode_"))
            {
                // todo
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

        preset.InitEquationCompiled = CompileEquation(preset.InitEquation.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(x => TokenizeExpression(x)).ToList());
        preset.FrameEquationCompiled = CompileEquation(preset.FrameEquation.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(x => TokenizeExpression(x)).ToList());
        preset.PixelEquationCompiled = CompileEquation(preset.PixelEquation.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(x => TokenizeExpression(x)).ToList());

        LoadedPresets.Add(Path.GetFileNameWithoutExtension(file), preset);
    }

    Action<Dictionary<string, float>> CompileEquation(List<List<string>> Equation)
    {
        Action<Dictionary<string, float>> result = null;
        
        foreach (var line in Equation)
        {
            if (line.Count == 0)
                continue;
            
            string varName = line[0];

            Func<Dictionary<string, float>, float> compiledLine = CompileExpression(line.Skip(1).ToList());

            result += (Dictionary<string, float> Variables) =>
            {
                SetVariable(Variables, varName, compiledLine(Variables));
            };
        }

        if (result == null)
        {
            result = (Dictionary<string, float> Variables) => { };
        }

        return result;
    }

    Func<Dictionary<string, float>, float> CompileVariable(string token)
    {
        if (token[0] == '.' || token[0] == '-' || char.IsDigit(token[0]))
        {
            var result = float.Parse(token);
            return (Dictionary<string, float> Variables) => result;
        }

        return (Dictionary<string, float> Variables) => GetVariable(Variables, token);
    }

    Func<Dictionary<string, float>, float> CompileExpression(List<string> Tokens)
    {
        List<Action<Dictionary<string, float>>> innerActions = new List<Action<Dictionary<string, float>>>();

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

                                List<Func<Dictionary<string, float>, float>> argumentValues = new List<Func<Dictionary<string, float>, float>>();

                                foreach (List<string> argument in arguments)
                                {
                                    argumentValues.Add(CompileExpression(argument));
                                }

                                string functionName = Tokens[tokenNum - 1];

                                string funcId = "#" + System.Guid.NewGuid().ToString();

                                Action<Dictionary<string, float>> compiledFunction = (Dictionary<string, float> Variables) =>
                                {
                                    throw new Exception("Error compiling function " + functionName + ": " + debugOut);
                                };
                                
                                switch (arguments.Count)
                                {
                                    case 1:
                                        compiledFunction = (Dictionary<string, float> Variables) =>
                                        {
                                            SetVariable(Variables, funcId, Funcs1Arg[functionName](argumentValues[0](Variables)));
                                        };
                                        break;
                                    case 2:
                                        compiledFunction = (Dictionary<string, float> Variables) =>
                                        {
                                            SetVariable(Variables, funcId, Funcs2Arg[functionName](argumentValues[0](Variables), argumentValues[1](Variables)));
                                        };
                                        break;
                                    case 3:
                                        compiledFunction = (Dictionary<string, float> Variables) =>
                                        {
                                            SetVariable(Variables, funcId, Funcs3Arg[functionName](argumentValues[0](Variables), argumentValues[1](Variables), argumentValues[2](Variables)));
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
                                string funcId = "#" + System.Guid.NewGuid().ToString();

                                Func<Dictionary<string, float>, float> exp = CompileExpression(Tokens.Skip(tokenNum + 1).Take(tokenNum2 - tokenNum - 1).ToList());

                                innerActions.Add((Dictionary<string, float> Variables) =>
                                {
                                    SetVariable(Variables, funcId, exp(Variables));
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

                    string funcId = "#" + System.Guid.NewGuid().ToString();

                    Func<Dictionary<string, float>, float> exp = CompileVariable(next);

                    innerActions.Add((Dictionary<string, float> Variables) =>
                    {
                        SetVariable(Variables, funcId, +exp(Variables));
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

                    string funcId = "#" + System.Guid.NewGuid().ToString();

                    Func<Dictionary<string, float>, float> exp = CompileVariable(next);

                    innerActions.Add((Dictionary<string, float> Variables) =>
                    {
                        SetVariable(Variables, funcId, -exp(Variables));
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

                string funcId = "#" + System.Guid.NewGuid().ToString();

                Func<Dictionary<string, float>, float> prevValue = CompileVariable(prev);
                Func<Dictionary<string, float>, float> nextValue = CompileVariable(next);

                innerActions.Add((Dictionary<string, float> Variables) =>
                {
                    SetVariable(Variables, funcId, prevValue(Variables) * nextValue(Variables));
                });

                Tokens.RemoveRange(tokenNum - 1, 2);

                Tokens[tokenNum - 1] = funcId;

                tokenNum--;
            }

            if (token == "/")
            {
                string prev = Tokens[tokenNum - 1];
                string next = Tokens[tokenNum + 1];

                string funcId = "#" + System.Guid.NewGuid().ToString();

                Func<Dictionary<string, float>, float> prevValue = CompileVariable(prev);
                Func<Dictionary<string, float>, float> nextValue = CompileVariable(next);

                innerActions.Add((Dictionary<string, float> Variables) =>
                {
                    SetVariable(Variables, funcId, prevValue(Variables) / nextValue(Variables));
                });

                Tokens.RemoveRange(tokenNum - 1, 2);

                Tokens[tokenNum - 1] = funcId;

                tokenNum--;
            }

            if (token == "%")
            {
                string prev = Tokens[tokenNum - 1];
                string next = Tokens[tokenNum + 1];

                string funcId = "#" + System.Guid.NewGuid().ToString();

                Func<Dictionary<string, float>, float> prevValue = CompileVariable(prev);
                Func<Dictionary<string, float>, float> nextValue = CompileVariable(next);

                innerActions.Add((Dictionary<string, float> Variables) =>
                {
                    SetVariable(Variables, funcId, (int)prevValue(Variables) % (int)nextValue(Variables));
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

                string funcId = "#" + System.Guid.NewGuid().ToString();

                Func<Dictionary<string, float>, float> prevValue = CompileVariable(prev);
                Func<Dictionary<string, float>, float> nextValue = CompileVariable(next);

                innerActions.Add((Dictionary<string, float> Variables) =>
                {
                    SetVariable(Variables, funcId, prevValue(Variables) + nextValue(Variables));
                });

                Tokens.RemoveRange(tokenNum - 1, 2);

                Tokens[tokenNum - 1] = funcId;

                tokenNum--;
            }

            if (token == "-")
            {
                string prev = Tokens[tokenNum - 1];
                string next = Tokens[tokenNum + 1];

                string funcId = "#" + System.Guid.NewGuid().ToString();

                Func<Dictionary<string, float>, float> prevValue = CompileVariable(prev);
                Func<Dictionary<string, float>, float> nextValue = CompileVariable(next);

                innerActions.Add((Dictionary<string, float> Variables) =>
                {
                    SetVariable(Variables, funcId, prevValue(Variables) - nextValue(Variables));
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

                string funcId = "#" + System.Guid.NewGuid().ToString();

                Func<Dictionary<string, float>, float> prevValue = CompileVariable(prev);
                Func<Dictionary<string, float>, float> nextValue = CompileVariable(next);

                innerActions.Add((Dictionary<string, float> Variables) =>
                {
                    SetVariable(Variables, funcId, (int)prevValue(Variables) & (int)nextValue(Variables));
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

                string funcId = "#" + System.Guid.NewGuid().ToString();

                Func<Dictionary<string, float>, float> prevValue = CompileVariable(prev);
                Func<Dictionary<string, float>, float> nextValue = CompileVariable(next);

                innerActions.Add((Dictionary<string, float> Variables) =>
                {
                    SetVariable(Variables, funcId, (int)prevValue(Variables) | (int)nextValue(Variables));
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

        Func<Dictionary<string, float>, float> finalValue = CompileVariable(Tokens[0]);

        Func<Dictionary<string, float>, float> result = (Dictionary<string, float> Variables) =>
        {
            for (int i = 0; i < innerActions.Count; i++)
            {
                innerActions[i](Variables);
            }

            return finalValue(Variables);
        };

        return result;
    }

    public void PlayPreset(string preset)
    {
        Debug.Log("Playing " + preset);

        CurrentPreset = LoadedPresets[preset];

        CurrentPreset.Variables = new Dictionary<string, float>();

        foreach (var v in CurrentPreset.BaseVariables.Keys)
        {
            SetVariable(CurrentPreset.Variables, v, CurrentPreset.BaseVariables[v]);
        }

        SetVariable(CurrentPreset.Variables, "frame", FrameNum);
        SetVariable(CurrentPreset.Variables, "time", CurrentTime);
        SetVariable(CurrentPreset.Variables, "fps", FPS);
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

        List<string> nonUserKeys = CurrentPreset.Variables.Keys.ToList();
        nonUserKeys.AddRange(regs);

        var afterInit = new Dictionary<string, float>(CurrentPreset.Variables);

        CurrentPreset.InitEquationCompiled(afterInit);

        CurrentPreset.InitVariables = Pick(afterInit, qs);
        CurrentPreset.RegVariables = Pick(afterInit, regs);
        var initUserVars = Pick(afterInit, nonUserKeys.ToArray());

        CurrentPreset.FrameVariables = new Dictionary<string, float>(CurrentPreset.Variables);

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

        // todo setup waves

        // todo setup shapes

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
