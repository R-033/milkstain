using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System;
using System.Runtime.CompilerServices;
using System.Globalization;

namespace Milkstain
{
    public class Milkdrop : MonoBehaviour
    {
        private struct LineQueue
        {
            public Vector3[] LinePositions;
            public Color[] LineColors;
            public Material LineMaterial;
            public bool ShouldLoop;
            public bool IsThick;
        }

        public Preset CurrentPreset;

        public TextAsset[] PresetFiles;

        public Texture[] PresetTextures;
        public Shader[] PresetWarpShaders;
        public Shader[] PresetCompShaders;

        private Preset PrevPreset;

        public Vector2Int MeshSize = new Vector2Int(48, 36);
        public Vector2Int MeshSizeComp = new Vector2Int(32, 24);
        public Vector2Int MotionVectorsSize = new Vector2Int(64, 48);
        public Vector2Int Resolution = new Vector2Int(1200, 900);
        public float Scale = 1f;
        public int MaxShapeSides = 101;
        public int MaxSamples = 512;
        public float MaxFPS = 30f;

        public float ChangePresetIn = 5f;

        public float TransitionTime = 5.7f;

        [HideInInspector]
        public float presetChangeTimer = 0f;

        [HideInInspector]
        public float Bass;
        [HideInInspector]
        public float BassAtt;
        [HideInInspector]
        public float Mid;
        [HideInInspector]
        public float MidAtt;
        [HideInInspector]
        public float Treb;
        [HideInInspector]
        public float TrebAtt;

        public MeshFilter TargetMeshFilter;
        public MeshRenderer TargetMeshRenderer;
        
        public Camera TargetCamera;

        public AudioSource TargetAudio;

        public Shader DefaultWarpShader;
        public Shader DefaultCompShader;

        public Material DoNothingMaterial;

        public Material BlurMaterialHorizontal;
        public Material BlurMaterialVertical;

        public Material BorderMaterial;

        public Material ShapeMaterial;

        public Material LineMaterial;

        public Material DarkenCenterMaterial;

        public Transform BorderSideLeft;
        public Transform BorderSideRight;
        public Transform BorderSideTop;
        public Transform BorderSideBottom;
        public Transform BorderParent;

        public TextMesh SuperText;

        public bool RandomOrder = true;
        public bool ConsiderRating = true;

        public bool SkipCustomShaded = true;

        public string SuperTextString;
        public bool RenderSuperText;

        public bool OscilloscopeMode = false;

        private ulong CurrentFrame = 0;
        private float CurrentTime = 0f;

        private Vector3 baseDotScale;
        private Vector3 baseSquareScale;

        private Vector2[] WarpUVs;
        private Color[] WarpColor;
        private Color[] CompColor;

        [HideInInspector]
        public RenderTexture FinalTexture;

        private RenderTexture PrevTempTexture;

        private RenderTexture TempTextureFW;
        private RenderTexture TempTextureFC;
        private RenderTexture TempTexturePW;
        private RenderTexture TempTexturePC;

        private RenderTexture TempTexture;

        private RenderTexture Blur1Texture;
        private RenderTexture Blur2Texture;
        private RenderTexture Blur3Texture;

        private Texture2D TextureNoiseLQ;
        private Texture2D TextureNoiseMQ;
        private Texture2D TextureNoiseHQ;
        private Texture2D TextureNoiseLQLite;
        private Texture2D TexturePWNoiseLQ;
        private Texture3D TextureNoiseVolLQ;
        private Texture3D TextureNoiseVolHQ;

        private Mesh TargetMeshWarp;
        private Mesh TargetMeshDarkenCenter;
        private Mesh TargetMeshComp;

        private List<LineQueue> LinesToDraw = new List<LineQueue>();

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

        [HideInInspector]
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

        private int[] audioSampleStarts;
        private int[] audioSampleStops;

        private bool blending;
        private float presetStartTime;
        private float blendDuration;
        private float blendProgress;
        private float[] blendingVertInfoA;
        private float[] blendingVertInfoC;

        private float[] imm = new float[3];
        private float[] avg = new float[3];
        private float[] longAvg = new float[3];
        private float[] val = new float[3];
        private float[] att = new float[3];

        private int index = 0;

        private (float[], float[]) blurValues;

        private Vector2 AspectRatio;

        private Vector3 shapeScale;
        private Vector3 waveScale;

        private List<int> weightedPresetSelection;

        public void UpdateResolution(Vector2Int res)
        {
            Destroy(TempTexture);
            Destroy(PrevTempTexture);
            Destroy(TempTextureFW);
            Destroy(TempTextureFC);
            Destroy(TempTexturePW);
            Destroy(TempTexturePC);
            Destroy(FinalTexture);
            Destroy(Blur1Texture);
            Destroy(Blur2Texture);
            Destroy(Blur3Texture);

            Resolution = res;

            AspectRatio = new Vector2
            (
                Resolution.x > Resolution.y ? 1f : (float)Resolution.x / Resolution.y,
                Resolution.x > Resolution.y ? (float)Resolution.y / Resolution.x : 1f
            );

            baseDotScale = Vector3.one * (4f / Resolution.y * Scale);
            baseSquareScale = Vector3.one * (1f / Resolution.y * Scale);

            PrevTempTexture = new RenderTexture(Resolution.x, Resolution.y, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm);

            TempTextureFW = new RenderTexture(Resolution.x, Resolution.y, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm);
            TempTextureFW.filterMode = FilterMode.Bilinear;
            TempTextureFW.wrapMode = TextureWrapMode.Repeat;
            TempTextureFC = new RenderTexture(Resolution.x, Resolution.y, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm);
            TempTextureFC.filterMode = FilterMode.Bilinear;
            TempTextureFC.wrapMode = TextureWrapMode.Clamp;
            TempTexturePW = new RenderTexture(Resolution.x, Resolution.y, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm);
            TempTexturePW.filterMode = FilterMode.Point;
            TempTexturePW.wrapMode = TextureWrapMode.Repeat;
            TempTexturePC = new RenderTexture(Resolution.x, Resolution.y, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm);
            TempTexturePC.filterMode = FilterMode.Point;
            TempTexturePC.wrapMode = TextureWrapMode.Clamp;

            TempTexture = new RenderTexture(Resolution.x, Resolution.y, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm);
            FinalTexture = new RenderTexture(Resolution.x, Resolution.y, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm);

            Blur1Texture = new RenderTexture(Mathf.RoundToInt(Resolution.x * 0.5f), Mathf.RoundToInt(Resolution.y * 0.5f), 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm);
            Blur2Texture = new RenderTexture(Mathf.RoundToInt(Resolution.x * 0.125f), Mathf.RoundToInt(Resolution.y * 0.125f), 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm);
            Blur3Texture = new RenderTexture(Mathf.RoundToInt(Resolution.x * 0.0625f), Mathf.RoundToInt(Resolution.y * 0.0625f), 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm);

            TargetMeshFilter.transform.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);
            BorderParent.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);

            shapeScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);
            waveScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);
        }

        void Awake()
        {
            DoNothingMaterial = new Material(DoNothingMaterial);
            BlurMaterialHorizontal = new Material(BlurMaterialHorizontal);
            BlurMaterialVertical = new Material(BlurMaterialVertical);
            BorderMaterial = new Material(BorderMaterial);
            ShapeMaterial = new Material(ShapeMaterial);
            LineMaterial = new Material(LineMaterial);
            DarkenCenterMaterial = new Material(DarkenCenterMaterial);

            BorderSideLeft.GetComponent<MeshRenderer>().sharedMaterial = BorderMaterial;
            BorderSideRight.GetComponent<MeshRenderer>().sharedMaterial = BorderMaterial;
            BorderSideTop.GetComponent<MeshRenderer>().sharedMaterial = BorderMaterial;
            BorderSideBottom.GetComponent<MeshRenderer>().sharedMaterial = BorderMaterial;
        }

        void OnDestroy()
        {
            Destroy(DoNothingMaterial);
            Destroy(BlurMaterialHorizontal);
            Destroy(BlurMaterialVertical);
            Destroy(BorderMaterial);
            Destroy(ShapeMaterial);
            Destroy(LineMaterial);
            Destroy(DarkenCenterMaterial);
            Destroy(TempTexture);
            Destroy(PrevTempTexture);
            Destroy(TempTextureFW);
            Destroy(TempTextureFC);
            Destroy(TempTexturePW);
            Destroy(TempTexturePC);
            Destroy(FinalTexture);
            Destroy(TargetMeshWarp);
            Destroy(TargetMeshDarkenCenter);
            Destroy(TargetMeshComp);
            Destroy(Blur1Texture);
            Destroy(Blur2Texture);
            Destroy(Blur3Texture);
            Destroy(TextureNoiseLQ);
            Destroy(TextureNoiseMQ);
            Destroy(TextureNoiseHQ);
            Destroy(TextureNoiseLQLite);
            Destroy(TexturePWNoiseLQ);
            Destroy(TextureNoiseVolLQ);
            Destroy(TextureNoiseVolHQ);

            UnloadPresets();
        }

        public void Initialize()
        {
            UnloadPresets();

            blendingVertInfoA = new float[(MeshSize.x + 1) * (MeshSize.y + 1)];
            blendingVertInfoC = new float[(MeshSize.x + 1) * (MeshSize.y + 1)];

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

            var dotSprite = DotPrefab.GetComponent<SpriteRenderer>().sprite;
            var squareSprite = MotionVectorPrefab.GetComponent<SpriteRenderer>().sprite;

            UpdateResolution(Resolution);

            TextureNoiseLQ = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            TextureNoiseLQLite = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            TextureNoiseMQ = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            TextureNoiseHQ = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            TexturePWNoiseLQ = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            TextureNoiseVolLQ = new Texture3D(32, 32, 32, TextureFormat.RGBA32, false);
            TextureNoiseVolHQ = new Texture3D(32, 32, 32, TextureFormat.RGBA32, false);

            CreateNoiseTex(TextureNoiseLQ, 256, 1);
            CreateNoiseTex(TextureNoiseLQLite, 32, 1);
            CreateNoiseTex(TextureNoiseMQ, 256, 4);
            CreateNoiseTex(TextureNoiseHQ, 256, 8);
            CreateNoiseVolTex(TextureNoiseVolLQ, 32, 1);
            CreateNoiseVolTex(TextureNoiseVolHQ, 32, 4);

            TextureNoiseLQ.wrapMode = TextureWrapMode.Repeat;
            TextureNoiseLQLite.wrapMode = TextureWrapMode.Repeat;
            TextureNoiseMQ.wrapMode = TextureWrapMode.Repeat;
            TextureNoiseHQ.wrapMode = TextureWrapMode.Repeat;
            TextureNoiseVolLQ.wrapMode = TextureWrapMode.Repeat;
            TextureNoiseVolHQ.wrapMode = TextureWrapMode.Repeat;
            TexturePWNoiseLQ.wrapMode = TextureWrapMode.Repeat;
            TexturePWNoiseLQ.filterMode = FilterMode.Point;
            TexturePWNoiseLQ.SetPixels32(TextureNoiseLQ.GetPixels32());
            TexturePWNoiseLQ.Apply();

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
                for (int i = 0; i < PresetFiles.Length; i++)
                {
                    if (PresetFiles[i].name == PresetName)
                    {
                        index = i;
                        break;
                    }
                }
            }

            PlayRandomPreset(0f);

            initialized = true;
        }

        float fCubicInterpolate(float y0, float y1, float y2, float y3, float t)
        {
            float t2 = t * t;
            float t3 = t * t2;
            float a0 = y3 - y2 - y0 + y1;
            float a1 = y0 - y1 - a0;
            float a2 = y2 - y0;
            float a3 = y1;

            return a0 * t3 + a1 * t2 + a2 * t + a3;
        }

        Color32 dwCubicInterpolate(Color32 y0, Color32 y1, Color32 y2, Color32 y3, float t)
        {
            return new Color32
            (
                (byte)Mathf.FloorToInt(Mathf.Clamp01(fCubicInterpolate(y0.r / 255f, y1.r / 255f, y2.r / 255f, y3.r / 255f, t)) * 255f),
                (byte)Mathf.FloorToInt(Mathf.Clamp01(fCubicInterpolate(y0.g / 255f, y1.g / 255f, y2.g / 255f, y3.g / 255f, t)) * 255f),
                (byte)Mathf.FloorToInt(Mathf.Clamp01(fCubicInterpolate(y0.b / 255f, y1.b / 255f, y2.b / 255f, y3.b / 255f, t)) * 255f),
                (byte)Mathf.FloorToInt(Mathf.Clamp01(fCubicInterpolate(y0.a / 255f, y1.a / 255f, y2.a / 255f, y3.a / 255f, t)) * 255f)
            );
        }

        void CreateNoiseTex(Texture2D target, int noiseSize, int zoom)
        {
            int nsize = noiseSize * noiseSize;
            int texRange = zoom > 1 ? 216 : 256;
            float halfTexRange = texRange * 0.5f;
            
            Color32[] texArr = new Color32[nsize];

            for (int i = 0; i < nsize; i++)
            {
                texArr[i] = new Color32
                (
                    (byte)Mathf.FloorToInt(UnityEngine.Random.Range(0f, 1f) * texRange + halfTexRange),
                    (byte)Mathf.FloorToInt(UnityEngine.Random.Range(0f, 1f) * texRange + halfTexRange),
                    (byte)Mathf.FloorToInt(UnityEngine.Random.Range(0f, 1f) * texRange + halfTexRange),
                    (byte)Mathf.FloorToInt(UnityEngine.Random.Range(0f, 1f) * texRange + halfTexRange)
                );
            }

            if (zoom > 1)
            {
                for (int y = 0; y < noiseSize; y += zoom)
                {
                    for (int x = 0; x < noiseSize; x++)
                    {
                        if (x % zoom != 0)
                        {
                            int baseX = Mathf.FloorToInt(x / (float)zoom) * zoom + noiseSize;
                            int baseY = y * noiseSize;

                            Color32 y0 = texArr[baseY + ((baseX - zoom) % noiseSize)];
                            Color32 y1 = texArr[baseY + (baseX % noiseSize)];
                            Color32 y2 = texArr[baseY + ((baseX + zoom) % noiseSize)];
                            Color32 y3 = texArr[baseY + ((baseX + zoom * 2) % noiseSize)];

                            float t = (x % zoom) / (float)zoom;

                            texArr[y * noiseSize + x] = dwCubicInterpolate(y0, y1, y2, y3, t);
                        }
                    }
                }

                for (int x = 0; x < noiseSize; x++)
                {
                    for (int y = 0; y < noiseSize; y++)
                    {
                        if (y % zoom != 0)
                        {
                            int baseY = Mathf.FloorToInt(y / (float)zoom) * zoom + noiseSize;

                            Color32 y0 = texArr[((baseY - zoom) % noiseSize) * noiseSize + x];
                            Color32 y1 = texArr[(baseY % noiseSize) * noiseSize + x];
                            Color32 y2 = texArr[((baseY + zoom) % noiseSize) * noiseSize + x];
                            Color32 y3 = texArr[((baseY + zoom * 2) % noiseSize) * noiseSize + x];

                            float t = (y % zoom) / (float)zoom;

                            texArr[y * noiseSize + x] = dwCubicInterpolate(y0, y1, y2, y3, t);
                        }
                    }
                }
            }

            target.SetPixels32(texArr);
            target.Apply();
        }

        void CreateNoiseVolTex(Texture3D target, int noiseSize, int zoom)
        {
            int nsize = noiseSize * noiseSize * noiseSize;
            int texRange = zoom > 1 ? 216 : 256;
            float halfTexRange = texRange * 0.5f;

            Color32[] texArr = new Color32[nsize];

            for (int i = 0; i < nsize; i++)
            {
                texArr[i] = new Color32
                (
                    (byte)Mathf.FloorToInt(UnityEngine.Random.Range(0f, 1f) * texRange + halfTexRange),
                    (byte)Mathf.FloorToInt(UnityEngine.Random.Range(0f, 1f) * texRange + halfTexRange),
                    (byte)Mathf.FloorToInt(UnityEngine.Random.Range(0f, 1f) * texRange + halfTexRange),
                    (byte)Mathf.FloorToInt(UnityEngine.Random.Range(0f, 1f) * texRange + halfTexRange)
                );
            }

            int wordsPerSlice = noiseSize * noiseSize;
            int wordsPerLine = noiseSize;

            if (zoom > 1)
            {
                for (int z = 0; z < noiseSize; z += zoom)
                {
                    for (int y = 0; y < noiseSize; y += zoom)
                    {
                        for (int x = 0; x < noiseSize; x++)
                        {
                            if (x % zoom != 0)
                            {
                                int baseX = Mathf.FloorToInt(x / (float)zoom) * zoom + noiseSize;
                                int baseY = z * wordsPerSlice + y * wordsPerLine;

                                Color32 y0 = texArr[baseY + ((baseX - zoom) % noiseSize)];
                                Color32 y1 = texArr[baseY + (baseX % noiseSize)];
                                Color32 y2 = texArr[baseY + ((baseX + zoom) % noiseSize)];
                                Color32 y3 = texArr[baseY + ((baseX + zoom * 2) % noiseSize)];

                                float t = (x % zoom) / (float)zoom;

                                texArr[z * wordsPerSlice + y * wordsPerLine + x] = dwCubicInterpolate(y0, y1, y2, y3, t);
                            }
                        }
                    }
                }

                for (int z = 0; z < noiseSize; z += zoom)
                {
                    for (int x = 0; x < noiseSize; x++)
                    {
                        for (int y = 0; y < noiseSize; y++)
                        {
                            if (y % zoom != 0)
                            {
                                int baseY = Mathf.FloorToInt(y / (float)zoom) * zoom + noiseSize;
                                int baseZ = z * wordsPerSlice;

                                Color32 y0 = texArr[((baseY - zoom) % noiseSize) * wordsPerLine + x + baseZ];
                                Color32 y1 = texArr[(baseY % noiseSize) * wordsPerLine + x + baseZ];
                                Color32 y2 = texArr[((baseY + zoom) % noiseSize) * wordsPerLine + x + baseZ];
                                Color32 y3 = texArr[((baseY + zoom * 2) % noiseSize) * wordsPerLine + x + baseZ];

                                float t = (y % zoom) / (float)zoom;

                                texArr[y * wordsPerLine + x + baseZ] = dwCubicInterpolate(y0, y1, y2, y3, t);
                            }
                        }
                    }
                }

                for (int x = 0; x < noiseSize; x++)
                {
                    for (int y = 0; y < noiseSize; y++)
                    {
                        for (int z = 0; z < noiseSize; z++)
                        {
                            if (z % zoom != 0)
                            {
                                int baseY = y * wordsPerLine;
                                int baseZ = Mathf.FloorToInt(z / (float)zoom) * zoom + noiseSize;

                                Color32 y0 = texArr[((baseZ - zoom) % noiseSize) * wordsPerSlice + x + baseY];
                                Color32 y1 = texArr[(baseZ % noiseSize) * wordsPerSlice + x + baseY];
                                Color32 y2 = texArr[((baseZ + zoom) % noiseSize) * wordsPerSlice + x + baseY];
                                Color32 y3 = texArr[((baseZ + zoom * 2) % noiseSize) * wordsPerSlice + x + baseY];

                                float t = (y % zoom) / (float)zoom;

                                texArr[z * wordsPerSlice + x + baseY] = dwCubicInterpolate(y0, y1, y2, y3, t);
                            }
                        }
                    }
                }
            }

            target.SetPixels32(texArr);
            target.Apply();
        }

        public void PlayRandomPreset(float transitionDuration)
        {
            if (blending)
            {
                return;
            }

            bool result;
            int fallbackCounter = 0;

            do
            {
                if (fallbackCounter > 100)
                {
                    return;
                }

                int ind;
                if (RandomOrder)
                {
                    if (ConsiderRating)
                    {
                        if (weightedPresetSelection == null)
                        {
                            weightedPresetSelection = new List<int>();

                            for (int i = 0; i < PresetFiles.Length; i++)
                            {
                                var rating = Mathf.RoundToInt(GetRating(PresetFiles[i].text));

                                for (int j = 0; j < rating; j++)
                                {
                                    weightedPresetSelection.Add(i);
                                }
                            }
                        }

                        ind = weightedPresetSelection[UnityEngine.Random.Range(0, weightedPresetSelection.Count)];
                    }
                    else
                    {
                        ind = UnityEngine.Random.Range(0, PresetFiles.Length);
                    }
                }
                else
                {
                    ind = index++;
                    if (index >= PresetFiles.Length)
                    {
                        index = 0;
                    }
                }

                result = PlayPreset(ind, transitionDuration);

                fallbackCounter++;
            }
            while (!result);
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
                PlayRandomPreset(TransitionTime);
            }
            
            timeSinceLastFrame += Time.deltaTime;

            if (timeSinceLastFrame >= 1f / MaxFPS)
            {
                FPS = Mathf.Min(1f / Time.deltaTime, MaxFPS);
                timeSinceLastFrame -= 1f / FPS;
                Render();
            }
        }

        void OnRenderObject()
        {
            GL.PushMatrix();
            GL.MultMatrix(transform.localToWorldMatrix);

            foreach (var line in LinesToDraw)
            {
                if (line.IsThick)
                {
                    float lineScale = 2f / Resolution.y * Scale;

                    line.LineMaterial.SetPass(0);

                    for (int j = 0; j < 4; j++)
                    {
                        GL.Begin(GL.LINE_STRIP);

                        for (int i = 0; i < line.LinePositions.Length; i++)
                        {
                            Vector3 pos0 = i > 0 ? line.LinePositions[i - 1] : line.LinePositions[i];
                            Vector3 pos1 = i > 0 ? line.LinePositions[i] : line.LinePositions[i + 1];
                            Vector3 right = (pos1 - pos0).normalized;
                            right *= (j - 1.5f) * lineScale;
                            GL.Color(line.LineColors.Length == 1 ? line.LineColors[0] : line.LineColors[i]);
                            GL.Vertex3(line.LinePositions[i].x * waveScale.x + right.x, line.LinePositions[i].y * waveScale.y + right.y, line.LinePositions[i].z);
                        }

                        if (line.ShouldLoop)
                        {
                            Vector3 pos0 = line.LinePositions[0];
                            Vector3 pos1 = line.LinePositions[1];
                            Vector3 right = (pos1 - pos0).normalized;
                            right *= (j - 1.5f) * lineScale;
                            GL.Color(line.LineColors[0]);
                            GL.Vertex3(line.LinePositions[0].x * waveScale.x + right.x, line.LinePositions[0].y * waveScale.y + right.y, line.LinePositions[0].z);
                        }

                        GL.End();
                    }
                }
                else
                {
                    GL.Begin(GL.LINE_STRIP);

                    line.LineMaterial.SetPass(0);

                    for (int i = 0; i < line.LinePositions.Length; i++)
                    {
                        GL.Color(line.LineColors.Length == 1 ? line.LineColors[0] : line.LineColors[i]);
                        GL.Vertex3(line.LinePositions[i].x * waveScale.x, line.LinePositions[i].y * waveScale.y, line.LinePositions[i].z);
                    }

                    if (line.ShouldLoop)
                    {
                        GL.Color(line.LineColors[0]);
                        GL.Vertex3(line.LinePositions[0].x * waveScale.x, line.LinePositions[0].y * waveScale.y, line.LinePositions[0].z);
                    }

                    GL.End();
                }
            }

            GL.PopMatrix();

            LinesToDraw.Clear();
        }

        void Render()
        {
            CurrentTime += Mathf.Max(Time.deltaTime, 1f / MaxFPS);
            CurrentFrame++;

            if (blending)
            {
                blendProgress = (CurrentTime - presetStartTime) / blendDuration;

                if (blendProgress > 1f)
                {
                    blending = false;
                }
            }

            UpdateAudioLevels();

            RenderImage();
        }

        void MixFrameEquations()
        {
            float mix = 0.5f - 0.5f * Mathf.Cos(blendProgress * Mathf.PI);
            float mix2 = 1f - mix;
            float snapPoint = 0.5f;
            
            for (int i = 0; i < State.MixedVariables.Length; i++)
            {
                var v = State.MixedVariables[i];
                State.SetVariable(CurrentPreset.FrameVariables, v, mix * CurrentPreset.FrameVariables.Heap[(int)v] + mix2 * PrevPreset.FrameVariables.Heap[(int)v]);
            }

            for (int i = 0; i < State.MixedVariables.Length; i++)
            {
                var v = State.MixedVariables[i];
                State.SetVariable(CurrentPreset.FrameVariables, v, mix < snapPoint ? PrevPreset.FrameVariables.Heap[(int)v] : CurrentPreset.FrameVariables.Heap[(int)v]);
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
                imm[i] = 0f;
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

        void SetGlobalVars(State state)
        {
            State.SetVariable(state, Var.frame, CurrentFrame);
            State.SetVariable(state, Var.time, CurrentTime);
            State.SetVariable(state, Var.fps, FPS == 0f ? 30f : FPS);
            State.SetVariable(state, Var.progress, Mathf.Clamp01((CurrentTime - presetStartTime) / ChangePresetIn));
            State.SetVariable(state, Var.bass, Bass);
            State.SetVariable(state, Var.bass_att, BassAtt);
            State.SetVariable(state, Var.mid, Mid);
            State.SetVariable(state, Var.mid_att, MidAtt);
            State.SetVariable(state, Var.treb, Treb);
            State.SetVariable(state, Var.treb_att, TrebAtt);
            State.SetVariable(state, Var.meshx, MeshSize.x);
            State.SetVariable(state, Var.meshy, MeshSize.y);
            State.SetVariable(state, Var.aspectx, AspectRatio.x);
            State.SetVariable(state, Var.aspecty, AspectRatio.y);
            State.SetVariable(state, Var.pixelsx, Resolution.x);
            State.SetVariable(state, Var.pixelsy, Resolution.y);
        }

        void RunFrameEquations(Preset preset)
        {
            UnityEngine.Profiling.Profiler.BeginSample("RunFrameEquations");

            foreach (var v in preset.Variables.Keys)
            {
                State.SetVariable(preset.FrameVariables, v, preset.Variables.Heap[(int)v]);
            }

            foreach (var v in preset.InitVariables.Keys)
            {
                State.SetVariable(preset.FrameVariables, v, preset.InitVariables.Heap[(int)v]);
            }

            foreach (var v in preset.FrameMap.Keys)
            {
                State.SetVariable(preset.FrameVariables, v, preset.FrameMap.Heap[(int)v]);
            }

            SetGlobalVars(preset.FrameVariables);

            preset.FrameEquationCompiled(preset.FrameVariables);

            CurrentPreset.AfterFrameVariables = State.PickQs(CurrentPreset.FrameVariables);

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void RunPixelEquations(Preset preset, bool blending)
        {
            UnityEngine.Profiling.Profiler.BeginSample("RunPixelEquations");

            int gridX = MeshSize.x;
            int gridZ = MeshSize.y;

            int gridX1 = gridX + 1;
            int gridZ1 = gridZ + 1;

            float warpTimeV = CurrentTime * State.GetVariable(preset.FrameVariables, Var.warpanimspeed);
            float warpScaleInv = 1f / State.GetVariable(preset.FrameVariables, Var.warpscale);

            float warpf0 = 11.68f + 4f * Mathf.Cos(warpTimeV * 1.413f + 1f);
            float warpf1 = 8.77f + 3f * Mathf.Cos(warpTimeV * 1.113f + 7f);
            float warpf2 = 10.54f + 3f * Mathf.Cos(warpTimeV * 1.233f + 3f);
            float warpf3 = 11.49f + 4f * Mathf.Cos(warpTimeV * 0.933f + 5f);

            float texelOffsetX = 0f;
            float texelOffsetY = 0f;

            float aspectx = AspectRatio.x;
            float aspecty = AspectRatio.y;

            int offset = 0;
            int offsetColor = 0;

            var pixelVariables = preset.PixelVariables;

            foreach (var vv in preset.FrameVariables.Keys)
            {
                State.SetVariable(pixelVariables, vv, preset.FrameVariables.Heap[(int)vv]);
            }

            float warp = State.GetVariable(pixelVariables, Var.warp);
            float zoom = State.GetVariable(pixelVariables, Var.zoom);
            float zoomExp = State.GetVariable(pixelVariables, Var.zoomexp);
            float cx = State.GetVariable(pixelVariables, Var.cx);
            float cy = State.GetVariable(pixelVariables, Var.cy);
            float sx = State.GetVariable(pixelVariables, Var.sx);
            float sy = State.GetVariable(pixelVariables, Var.sy);
            float dx = State.GetVariable(pixelVariables, Var.dx);
            float dy = State.GetVariable(pixelVariables, Var.dy);
            float rot = State.GetVariable(pixelVariables, Var.rot);

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

            float x;
            float y;
            float rad;
            float ang;
            float zoom2V;
            float zoom2Inv;
            float u;
            float v;

            for (int iz = 0; iz < gridZ1; iz++)
            {
                for (int ix = 0; ix < gridX1; ix++)
                {
                    x = (ix / (float)gridX) * 2f - 1f;
                    y = (iz / (float)gridZ) * 2f - 1f;
                    rad = Mathf.Sqrt(x * x * aspectx * aspectx + y * y * aspecty * aspecty);

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

                    State.SetVariable(pixelVariables, Var.x, x * 0.5f * aspectx + 0.5f);
                    State.SetVariable(pixelVariables, Var.y, y * -0.5f * aspecty + 0.5f);
                    State.SetVariable(pixelVariables, Var.rad, rad);
                    State.SetVariable(pixelVariables, Var.ang, ang);

                    State.SetVariable(pixelVariables, Var.zoom, frameZoom);
                    State.SetVariable(pixelVariables, Var.zoomexp, frameZoomExp);
                    State.SetVariable(pixelVariables, Var.rot, frameRot);
                    State.SetVariable(pixelVariables, Var.warp, frameWarp);
                    State.SetVariable(pixelVariables, Var.cx, framecx);
                    State.SetVariable(pixelVariables, Var.cy, framecy);
                    State.SetVariable(pixelVariables, Var.dx, framedx);
                    State.SetVariable(pixelVariables, Var.dy, framedy);
                    State.SetVariable(pixelVariables, Var.sx, framesx);
                    State.SetVariable(pixelVariables, Var.sy, framesy);

                    preset.PixelEquationCompiled(pixelVariables);

                    warp = State.GetVariable(pixelVariables, Var.warp);
                    zoom = State.GetVariable(pixelVariables, Var.zoom);
                    zoomExp = State.GetVariable(pixelVariables, Var.zoomexp);
                    cx = State.GetVariable(pixelVariables, Var.cx);
                    cy = State.GetVariable(pixelVariables, Var.cy);
                    sx = State.GetVariable(pixelVariables, Var.sx);
                    sy = State.GetVariable(pixelVariables, Var.sy);
                    dx = State.GetVariable(pixelVariables, Var.dx);
                    dy = State.GetVariable(pixelVariables, Var.dy);
                    rot = State.GetVariable(pixelVariables, Var.rot);

                    zoom2V = Mathf.Pow(zoom, Mathf.Pow(zoomExp, (rad * 2f - 1f)));
                    zoom2Inv = 1f / zoom2V;

                    u = x * 0.5f * aspectx * zoom2Inv + 0.5f;
                    v = -y * 0.5f * aspecty * zoom2Inv + 0.5f;

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

            UnityEngine.Profiling.Profiler.EndSample();
        }

        (float[], float[]) GetBlurValues(State variables)
        {
            float blurMin1 = State.GetVariable(variables, Var.b1n);
            float blurMin2 = State.GetVariable(variables, Var.b2n);
            float blurMin3 = State.GetVariable(variables, Var.b3n);
            float blurMax1 = State.GetVariable(variables, Var.b1x);
            float blurMax2 = State.GetVariable(variables, Var.b2x);
            float blurMax3 = State.GetVariable(variables, Var.b3x);

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
            RunFrameEquations(CurrentPreset);
            RunPixelEquations(CurrentPreset, false);

            var pick = State.PickRegs(CurrentPreset.PixelVariables);

            foreach (var v in pick.Keys)
            {
                State.SetVariable(CurrentPreset.RegVariables, v, pick.Heap[(int)v]);
            }

            // assign regs to global

            if (blending)
            {
                RunFrameEquations(PrevPreset);
                RunPixelEquations(PrevPreset, true);

                MixFrameEquations();
            }

            if (!SkipCustomShaded)
            {
                blurValues = GetBlurValues(CurrentPreset.FrameVariables);
            }

            var swapTexture = TempTexture;
            TempTexture = PrevTempTexture;
            PrevTempTexture = swapTexture;

            TempTexture.wrapMode = State.GetVariable(CurrentPreset.FrameVariables, Var.wrap) == 0f ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;

            Blur1Texture.wrapMode = TempTexture.wrapMode;
            Blur2Texture.wrapMode = TempTexture.wrapMode;
            Blur3Texture.wrapMode = TempTexture.wrapMode;

            if (!blending)
            {
                DrawWarp(CurrentPreset, false);
            }
            else
            {
                DrawWarp(PrevPreset, false);
                DrawWarp(CurrentPreset, true);
            }

            if (!SkipCustomShaded)
            {
                DrawBlur();
            }

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

            DrawSuperText();

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

        void DrawBlur()
        {
            if (CurrentPreset.MaxBlurLevel > 0)
            {
                DrawBlurPass(PrevTempTexture, Blur1Texture, 0);
                
                if (CurrentPreset.MaxBlurLevel > 1)
                {
                    DrawBlurPass(Blur1Texture, Blur2Texture, 1);

                    if (CurrentPreset.MaxBlurLevel > 2)
                    {
                        DrawBlurPass(Blur2Texture, Blur3Texture, 2);
                    }
                }
            }
        }

        Vector2 GetBlurScaleAndBias(int blurLevel)
        {
            float[] scale = new float[] {1f, 1f, 1f};
            float[] bias = new float[] {0f, 0f, 0f};

            float[] blurMins = blurValues.Item1;
            float[] blurMaxs = blurValues.Item2;

            float tempMin;
            float tempMax;
            scale[0] = 1f / (blurMaxs[0] - blurMins[0]);
            bias[0] = -blurMins[0] * scale[0];
            tempMin = (blurMins[1] - blurMins[0]) / (blurMaxs[0] - blurMins[0]);
            tempMax = (blurMaxs[1] - blurMins[0]) / (blurMaxs[0] - blurMins[0]);
            scale[1] = 1f / (tempMax - tempMin);
            bias[1] = -tempMin * scale[1];
            tempMin = (blurMins[2] - blurMins[1]) / (blurMaxs[1] - blurMins[1]);
            tempMax = (blurMaxs[2] - blurMins[1]) / (blurMaxs[1] - blurMins[1]);
            scale[2] = 1f / (tempMax - tempMin);
            bias[2] = -tempMin * scale[2];

            return new Vector2(scale[blurLevel], bias[blurLevel]);
        }

        void DrawBlurPass(RenderTexture source, RenderTexture target, int blurLevel)
        {
            UnityEngine.Profiling.Profiler.BeginSample("DrawBlurPass");

            Vector2 scaleAndBias = GetBlurScaleAndBias(blurLevel);

            BlurMaterialHorizontal.SetVector("texsize", new Vector4(source.width, source.height, 1f / source.width, 1f / source.height));
            BlurMaterialHorizontal.SetFloat("scale", scaleAndBias.x);
            BlurMaterialHorizontal.SetFloat("bias", scaleAndBias.y);
            BlurMaterialHorizontal.SetVector("ws", new Vector4(4f + 3.8f, 3.5f + 2.9f, 1.9f + 1.2f, 0.7f + 0.3f));
            BlurMaterialHorizontal.SetVector("ds", new Vector4(0f + (2f * 3.8f) / (4f + 3.8f), 2f + (2f * 2.9f) / (3.5f + 2.9f), 4f + (2f * 1.2f) / (1.9f + 1.2f), 6f + (2f * 0.3f) / (0.7f + 0.3f)));
            BlurMaterialHorizontal.SetFloat("wdiv", 0.5f / (4f + 3.8f + 3.5f + 2.9f + 1.9f + 1.2f + 0.7f + 0.3f));

            float b1ed = blurLevel == 0 ? State.GetVariable(CurrentPreset.FrameVariables, Var.b1ed) : 0f;

            BlurMaterialVertical.SetVector("texsize", new Vector4(target.width, target.height, 1f / target.width, 1f / target.height));
            BlurMaterialVertical.SetFloat("ed1", 1f - b1ed);
            BlurMaterialVertical.SetFloat("ed2", b1ed);
            BlurMaterialVertical.SetFloat("ed3", 5f);
            BlurMaterialVertical.SetVector("wds", new Vector4(4f + 3.8f + 3.5f + 2.9f, 1.9f + 1.2f + 0.7f + 0.3f, 0f + 2f * ((3.5f + 2.9f) / (4f + 3.8f + 3.5f + 2.9f)), 2f + 2f * ((0.7f + 0.3f) / (1.9f + 1.2f + 0.7f + 0.3f))));
            BlurMaterialVertical.SetFloat("wdiv", 0.5f / (4f + 3.8f + 3.5f + 2.9f + 1.9f + 1.2f + 0.7f + 0.3f));

            TargetMeshFilter.sharedMesh = TargetMeshWarp;
            TargetMeshRenderer.sharedMaterial = BlurMaterialHorizontal;

            BlurMaterialHorizontal.mainTexture = source;

            TargetCamera.targetTexture = target;
            TargetCamera.Render();

            TargetMeshRenderer.sharedMaterial = BlurMaterialVertical;

            BlurMaterialVertical.mainTexture = target;

            TargetCamera.targetTexture = target;
            TargetCamera.Render();

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void DrawSuperText()
        {
            if (!RenderSuperText)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("DrawSuperText");

            SuperText.gameObject.SetActive(true);

            if (SuperText.text != SuperTextString)
            {
                SuperText.text = SuperTextString;
            }

            TargetMeshFilter.sharedMesh = TargetMeshWarp;
            TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

            DoNothingMaterial.mainTexture = TempTexture;

            TargetCamera.targetTexture = TempTexture;
            TargetCamera.Render();

            SuperText.gameObject.SetActive(false);

            UnityEngine.Profiling.Profiler.EndSample();

        }

        void DrawShapes(Preset preset, float blendProgress)
        {
            if (preset.Shapes.Count == 0)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("DrawShapes");

            int unDrawnShapes = 0;

            foreach (var CurrentShape in preset.Shapes)
            {
                if (State.GetVariable(CurrentShape.BaseVariables, Var.enabled) == 0f)
                {
                    continue;
                }

                foreach (var v in CurrentShape.Variables.Keys)
                {
                    State.SetVariable(CurrentShape.FrameVariables, v, CurrentShape.Variables.Heap[(int)v]);
                }

                foreach (var v in CurrentShape.FrameMap.Keys)
                {
                    State.SetVariable(CurrentShape.FrameVariables, v, CurrentShape.FrameMap.Heap[(int)v]);
                }

                if (string.IsNullOrEmpty(CurrentShape.FrameEquationSource))
                {
                    foreach (var v in preset.AfterFrameVariables.Keys)
                    {
                        State.SetVariable(CurrentShape.FrameVariables, v, preset.AfterFrameVariables.Heap[(int)v]);
                    }

                    foreach (var v in CurrentShape.Inits.Keys)
                    {
                        State.SetVariable(CurrentShape.FrameVariables, v, CurrentShape.Inits.Heap[(int)v]);
                    }
                }

                SetGlobalVars(CurrentShape.FrameVariables);

                int numInst = Mathf.FloorToInt(Mathf.Clamp(State.GetVariable(CurrentShape.BaseVariables, Var.num_inst), 1f, 1024f));

                float baseX = State.GetVariable(CurrentShape.BaseVariables, Var.x);
                float baseY = State.GetVariable(CurrentShape.BaseVariables, Var.y);
                float baseRad = State.GetVariable(CurrentShape.BaseVariables, Var.rad);
                float baseAng = State.GetVariable(CurrentShape.BaseVariables, Var.ang);
                float baseR = State.GetVariable(CurrentShape.BaseVariables, Var.r);
                float baseG = State.GetVariable(CurrentShape.BaseVariables, Var.g);
                float baseB = State.GetVariable(CurrentShape.BaseVariables, Var.b);
                float baseA = State.GetVariable(CurrentShape.BaseVariables, Var.a);
                float baseR2 = State.GetVariable(CurrentShape.BaseVariables, Var.r2);
                float baseG2 = State.GetVariable(CurrentShape.BaseVariables, Var.g2);
                float baseB2 = State.GetVariable(CurrentShape.BaseVariables, Var.b2);
                float baseA2 = State.GetVariable(CurrentShape.BaseVariables, Var.a2);
                float baseBorderR = State.GetVariable(CurrentShape.BaseVariables, Var.border_r);
                float baseBorderG = State.GetVariable(CurrentShape.BaseVariables, Var.border_g);
                float baseBorderB = State.GetVariable(CurrentShape.BaseVariables, Var.border_b);
                float baseBorderA = State.GetVariable(CurrentShape.BaseVariables, Var.border_a);
                float baseThickOutline = State.GetVariable(CurrentShape.BaseVariables, Var.thickoutline);
                float baseTextured = State.GetVariable(CurrentShape.BaseVariables, Var.textured);
                float baseTexZoom = State.GetVariable(CurrentShape.BaseVariables, Var.tex_zoom);
                float baseTexAng = State.GetVariable(CurrentShape.BaseVariables, Var.tex_ang);
                float baseAdditive = State.GetVariable(CurrentShape.BaseVariables, Var.additive);

                for (int j = 0; j < numInst; j++)
                {
                    State.SetVariable(CurrentShape.FrameVariables, Var.instance, j);
                    State.SetVariable(CurrentShape.FrameVariables, Var.x, baseX);
                    State.SetVariable(CurrentShape.FrameVariables, Var.y, baseY);
                    State.SetVariable(CurrentShape.FrameVariables, Var.rad, baseRad);
                    State.SetVariable(CurrentShape.FrameVariables, Var.ang, baseAng);
                    State.SetVariable(CurrentShape.FrameVariables, Var.r, baseR);
                    State.SetVariable(CurrentShape.FrameVariables, Var.g, baseG);
                    State.SetVariable(CurrentShape.FrameVariables, Var.b, baseB);
                    State.SetVariable(CurrentShape.FrameVariables, Var.a, baseA);
                    State.SetVariable(CurrentShape.FrameVariables, Var.r2, baseR2);
                    State.SetVariable(CurrentShape.FrameVariables, Var.g2, baseG2);
                    State.SetVariable(CurrentShape.FrameVariables, Var.b2, baseB2);
                    State.SetVariable(CurrentShape.FrameVariables, Var.a2, baseA2);
                    State.SetVariable(CurrentShape.FrameVariables, Var.border_r, baseBorderR);
                    State.SetVariable(CurrentShape.FrameVariables, Var.border_g, baseBorderG);
                    State.SetVariable(CurrentShape.FrameVariables, Var.border_b, baseBorderB);
                    State.SetVariable(CurrentShape.FrameVariables, Var.border_a, baseBorderA);
                    State.SetVariable(CurrentShape.FrameVariables, Var.thickoutline, baseThickOutline);
                    State.SetVariable(CurrentShape.FrameVariables, Var.textured, baseTextured);
                    State.SetVariable(CurrentShape.FrameVariables, Var.tex_zoom, baseTexZoom);
                    State.SetVariable(CurrentShape.FrameVariables, Var.tex_ang, baseTexAng);
                    State.SetVariable(CurrentShape.FrameVariables, Var.additive, baseAdditive);

                    if (!string.IsNullOrEmpty(CurrentShape.FrameEquationSource))
                    {
                        foreach (var v in preset.AfterFrameVariables.Keys)
                        {
                            State.SetVariable(CurrentShape.FrameVariables, v, preset.AfterFrameVariables.Heap[(int)v]);
                        }

                        foreach (var v in CurrentShape.Inits.Keys)
                        {
                            State.SetVariable(CurrentShape.FrameVariables, v, CurrentShape.Inits.Heap[(int)v]);
                        }

                        CurrentShape.FrameEquationCompiled(CurrentShape.FrameVariables);
                    }

                    int sides = Mathf.Clamp(Mathf.FloorToInt(State.GetVariable(CurrentShape.FrameVariables, Var.sides)), 3, 100);

                    float rad = State.GetVariable(CurrentShape.FrameVariables, Var.rad);
                    float ang = State.GetVariable(CurrentShape.FrameVariables, Var.ang);

                    float x = State.GetVariable(CurrentShape.FrameVariables, Var.x) * 2f - 1f;
                    float y = State.GetVariable(CurrentShape.FrameVariables, Var.y) * 2f - 1f;

                    float r = State.GetVariable(CurrentShape.FrameVariables, Var.r);
                    float g = State.GetVariable(CurrentShape.FrameVariables, Var.g);
                    float b = State.GetVariable(CurrentShape.FrameVariables, Var.b);
                    float a = State.GetVariable(CurrentShape.FrameVariables, Var.a);
                    float r2 = State.GetVariable(CurrentShape.FrameVariables, Var.r2);
                    float g2 = State.GetVariable(CurrentShape.FrameVariables, Var.g2);
                    float b2 = State.GetVariable(CurrentShape.FrameVariables, Var.b2);
                    float a2 = State.GetVariable(CurrentShape.FrameVariables, Var.a2);

                    float borderR = State.GetVariable(CurrentShape.FrameVariables, Var.border_r);
                    float borderG = State.GetVariable(CurrentShape.FrameVariables, Var.border_g);
                    float borderB = State.GetVariable(CurrentShape.FrameVariables, Var.border_b);
                    float borderA = State.GetVariable(CurrentShape.FrameVariables, Var.border_a);

                    Color borderColor = new Color
                    (
                        borderR,
                        borderG,
                        borderB,
                        borderA * blendProgress
                    );

                    float thickoutline = State.GetVariable(CurrentShape.FrameVariables, Var.thickoutline);

                    float textured = State.GetVariable(CurrentShape.FrameVariables, Var.textured);
                    float texZoom = State.GetVariable(CurrentShape.FrameVariables, Var.tex_zoom);
                    float texAng = State.GetVariable(CurrentShape.FrameVariables, Var.tex_ang);

                    float additive = State.GetVariable(CurrentShape.FrameVariables, Var.additive);

                    bool hasBorder = borderColor.a > 0f;
                    bool isTextured = Mathf.Abs(textured) >= 1f;
                    bool isBorderThick = Mathf.Abs(thickoutline) >= 1f;
                    bool isAdditive = Mathf.Abs(additive) >= 1f;

                    CurrentShape.Positions[0] = new Vector3(x, -y, 0f);

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
                            x + rad * Mathf.Cos(angSum) * AspectRatio.y,
                            -(y + rad * Mathf.Sin(angSum)),
                            0f
                        );

                        CurrentShape.Colors[k] = new Color(r2, g2, b2, a2 * blendProgress);

                        if (isTextured)
                        {
                            float texAngSum = pTwoPi + texAng + quarterPi;

                            CurrentShape.UVs[k] = new Vector2
                            (
                                0.5f + ((0.5f * Mathf.Cos(texAngSum)) / texZoom) * AspectRatio.y,
                                0.5f + (0.5f * Mathf.Sin(texAngSum)) / texZoom
                            );
                        }

                        if (hasBorder)
                        {
                            CurrentShape.BorderPositions[k - 1] = CurrentShape.Positions[k];
                        }
                    }

                    CurrentShape.ShapeMeshes[j].Clear();

                    CurrentShape.ShapeMeshes[j].vertices = CurrentShape.Positions.Take(sides + 1).ToArray();
                    CurrentShape.ShapeMeshes[j].colors = CurrentShape.Colors.Take(sides + 1).ToArray();
                    CurrentShape.ShapeMeshes[j].uv = CurrentShape.UVs.Take(sides + 1).ToArray();

                    int[] triangles = new int[sides * 3];

                    for (int k = 0; k < sides; k++)
                    {
                        triangles[k * 3 + 0] = 0;
                        triangles[k * 3 + 1] = k + 1;
                        triangles[k * 3 + 2] = (k + 2) >= (sides + 1) ? 1 : k + 2;
                    }

                    CurrentShape.ShapeMeshes[j].triangles = triangles;

                    CurrentShape.ShapeMaterials[j].mainTexture = TempTexture;
                    CurrentShape.ShapeMaterials[j].SetTexture("_MainTexPrev", CurrentShape.Texture == null ? PrevTempTexture : CurrentShape.Texture);
                    CurrentShape.ShapeMaterials[j].SetFloat("uTextured", textured);
                    CurrentShape.ShapeMaterials[j].SetFloat("additive", additive);

                    TargetMeshFilter.sharedMesh = TargetMeshWarp;
                    TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

                    DoNothingMaterial.mainTexture = TempTexture;

                    var matrix = Matrix4x4.TRS(transform.TransformPoint(new Vector3(0f, 0f, -0.5f)), transform.rotation, shapeScale);

                    Graphics.DrawMesh(CurrentShape.ShapeMeshes[j], matrix, CurrentShape.ShapeMaterials[j], 31, TargetCamera, 0);
                    unDrawnShapes++;

                    if (hasBorder)
                    {
                        CurrentShape.BorderMaterials[j].mainTexture = TempTexture;
                        CurrentShape.BorderMaterials[j].SetColor("waveColor", borderColor);
                        CurrentShape.BorderMaterials[j].SetFloat("additivewave", additive);
                        CurrentShape.BorderMaterials[j].SetFloat("aspect_ratio", Resolution.x / (float)Resolution.y);

                        var line = new LineQueue();
                        line.ShouldLoop = true;
                        line.LinePositions = CurrentShape.BorderPositions.Take(sides).ToArray();
                        line.IsThick = isBorderThick;
                        line.LineColors = new Color[] { Color.white };
                        line.LineMaterial = CurrentShape.BorderMaterials[j];
                        LinesToDraw.Add(line);
                    }
                }
            }

            if (unDrawnShapes > 0)
            {
                TargetCamera.targetTexture = TempTexture;
                TargetCamera.Render();
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void DrawWaves(Preset preset, float blendProgress)
        {
            if (preset.Waves.Count == 0)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("DrawWaves");

            int undrawnLines = 0;

            foreach (var CurrentWave in preset.Waves)
            {
                if (State.GetVariable(CurrentWave.BaseVariables, Var.enabled) == 0f)
                {
                    continue;
                }

                foreach (var v in CurrentWave.Variables.Keys)
                {
                    State.SetVariable(CurrentWave.FrameVariables, v, CurrentWave.Variables.Heap[(int)v]);
                }

                foreach (var v in CurrentWave.FrameMap.Keys)
                {
                    State.SetVariable(CurrentWave.FrameVariables, v, CurrentWave.FrameMap.Heap[(int)v]);
                }

                foreach (var v in preset.AfterFrameVariables.Keys)
                {
                    State.SetVariable(CurrentWave.FrameVariables, v, preset.AfterFrameVariables.Heap[(int)v]);
                }

                foreach (var v in CurrentWave.Inits.Keys)
                {
                    State.SetVariable(CurrentWave.FrameVariables, v, CurrentWave.Inits.Heap[(int)v]);
                }

                SetGlobalVars(CurrentWave.FrameVariables);

                CurrentWave.FrameEquationCompiled(CurrentWave.FrameVariables);

                int samples = Mathf.FloorToInt(Mathf.Min(State.GetVariable(CurrentWave.FrameVariables, Var.samples), MaxSamples));

                if (samples == 0)
                {
                    samples = MaxSamples;
                }

                int sep = Mathf.FloorToInt(State.GetVariable(CurrentWave.FrameVariables, Var.sep));
                float scaling = State.GetVariable(CurrentWave.FrameVariables, Var.scaling);
                float spectrum = State.GetVariable(CurrentWave.FrameVariables, Var.spectrum);
                float smoothing = State.GetVariable(CurrentWave.FrameVariables, Var.smoothing);
                float usedots = State.GetVariable(CurrentWave.BaseVariables, Var.usedots);

                float frameR = State.GetVariable(CurrentWave.FrameVariables, Var.r);
                float frameG = State.GetVariable(CurrentWave.FrameVariables, Var.g);
                float frameB = State.GetVariable(CurrentWave.FrameVariables, Var.b);
                float frameA = State.GetVariable(CurrentWave.FrameVariables, Var.a);

                float waveScale = State.GetVariable(CurrentPreset.BaseVariables, Var.wave_scale);

                samples -= sep;

                if (!(samples >= 2 || usedots != 0f && samples >= 1))
                {
                    continue;
                }

                bool useSpectrum = spectrum != 0f;
                float scale = (useSpectrum ? 0.15f : 0.004f) * scaling * waveScale;

                float[] pointsLeft = useSpectrum ? freqArrayL : timeArrayL;
                float[] pointsRight = useSpectrum ? freqArrayR : timeArrayR;

                int j0 = useSpectrum ? 0 : Mathf.FloorToInt((MaxSamples - samples) / 2f - sep / 2f);
                int j1 = useSpectrum ? 0 : Mathf.FloorToInt((MaxSamples - samples) / 2f + sep / 2f);
                float t = useSpectrum ? (MaxSamples - sep) / (float)samples : 1f;

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

                    State.SetVariable(CurrentWave.FrameVariables, Var.sample, j / (samples - 1f));
                    State.SetVariable(CurrentWave.FrameVariables, Var.value1, value1);
                    State.SetVariable(CurrentWave.FrameVariables, Var.value2, value2);
                    State.SetVariable(CurrentWave.FrameVariables, Var.x, 0.5f + value1);
                    State.SetVariable(CurrentWave.FrameVariables, Var.y, 0.5f + value2);
                    State.SetVariable(CurrentWave.FrameVariables, Var.r, frameR);
                    State.SetVariable(CurrentWave.FrameVariables, Var.g, frameG);
                    State.SetVariable(CurrentWave.FrameVariables, Var.b, frameB);
                    State.SetVariable(CurrentWave.FrameVariables, Var.a, frameA);

                    if (!string.IsNullOrEmpty(CurrentWave.PointEquationSource))
                    {
                        CurrentWave.PointEquationCompiled(CurrentWave.FrameVariables);
                    }

                    float x = (State.GetVariable(CurrentWave.FrameVariables, Var.x) * 2f - 1f) * (1f / AspectRatio.x);
                    float y = (State.GetVariable(CurrentWave.FrameVariables, Var.y) * 2f - 1f) * (1f / AspectRatio.y);

                    float r = State.GetVariable(CurrentWave.FrameVariables, Var.r);
                    float g = State.GetVariable(CurrentWave.FrameVariables, Var.g);
                    float b = State.GetVariable(CurrentWave.FrameVariables, Var.b);
                    float a = State.GetVariable(CurrentWave.FrameVariables, Var.a);

                    CurrentWave.Positions[j] = new Vector3(x, y, 0f);
                    CurrentWave.Colors[j] = new Color(r, g, b, a * blendProgress);
                }

                bool thick = State.GetVariable(CurrentWave.FrameVariables, Var.thick) != 0f;

                if (usedots != 0f)
                {
                    DotParent.localPosition = new Vector3(0f, 0f, -0.5f);

                    Vector3 outOfBounds = new Vector3(0f, 0f, -10f);

                    float aspect_ratio = Resolution.x / (float)Resolution.y;

                    for (int i = 0; i < MaxSamples * 4; i++)
                    {
                        if (i < samples)
                        {
                            Dots[i].localPosition = ValidatePosition(new Vector3(CurrentWave.Positions[i].x * aspect_ratio, -CurrentWave.Positions[i].y, 0f));
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

                    undrawnLines = 0;

                    DotParent.localPosition = new Vector3(0f, 0f, -10f);
                }
                else
                {
                    SmoothWaveAndColor(CurrentWave.Positions, CurrentWave.Colors, CurrentWave.SmoothedPositions, CurrentWave.SmoothedColors, samples);

                    CurrentWave.LineMaterial.mainTexture = TempTexture;
                    CurrentWave.LineMaterial.SetColor("waveColor", Color.white);
                    CurrentWave.LineMaterial.SetFloat("additivewave", State.GetVariable(CurrentWave.FrameVariables, Var.additive));
                    CurrentWave.LineMaterial.SetFloat("aspect_ratio", Resolution.x / (float)Resolution.y);

                    var line = new LineQueue();
                    line.ShouldLoop = true;
                    line.LinePositions = CurrentWave.SmoothedPositions.Take(samples * 2 - 1).ToArray();
                    line.IsThick = thick;
                    line.LineColors = CurrentWave.SmoothedColors.Take(samples * 2 - 1).ToArray();
                    line.LineMaterial = CurrentWave.LineMaterial;
                    LinesToDraw.Add(line);

                    undrawnLines++;
                }
            }

            if (undrawnLines > 0)
            {
                TargetMeshFilter.sharedMesh = TargetMeshWarp;
                TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

                DoNothingMaterial.mainTexture = TempTexture;

                TargetCamera.targetTexture = TempTexture;

                TargetCamera.Render();
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void DrawWarp(Preset preset, bool blending)
        {
            if (preset.WarpMaterial == null)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("DrawWarp");

            TargetMeshFilter.sharedMesh = TargetMeshWarp;
            TargetMeshWarp.SetUVs(0, WarpUVs);
            TargetMeshWarp.SetColors(WarpColor);

            TargetMeshRenderer.sharedMaterial = preset.WarpMaterial;

            preset.WarpMaterial.mainTexture = TempTexture;

            preset.WarpMaterial.SetTexture("_MainTexPrev", PrevTempTexture);

            preset.WarpMaterial.SetFloat("decay", State.GetVariable(preset.FrameVariables, Var.decay));
            preset.WarpMaterial.SetFloat("blending", blending ? 1f : 0f);

            if (!SkipCustomShaded)
            {
                Graphics.Blit(PrevTempTexture, TempTextureFW);
                Graphics.Blit(PrevTempTexture, TempTextureFC);
                Graphics.Blit(PrevTempTexture, TempTexturePW);
                Graphics.Blit(PrevTempTexture, TempTexturePC);

                preset.WarpMaterial.SetTexture("_MainTex2", TempTextureFW);
                preset.WarpMaterial.SetTexture("_MainTex3", TempTextureFC);
                preset.WarpMaterial.SetTexture("_MainTex4", TempTexturePW);
                preset.WarpMaterial.SetTexture("_MainTex5", TempTexturePC);

                preset.WarpMaterial.SetTexture("_MainTex6", Blur1Texture);
                preset.WarpMaterial.SetTexture("_MainTex7", Blur2Texture);
                preset.WarpMaterial.SetTexture("_MainTex8", Blur3Texture);

                preset.CompMaterial.SetTexture("_MainTex9", TextureNoiseLQ);
                preset.CompMaterial.SetTexture("_MainTex10", TextureNoiseLQLite);
                preset.CompMaterial.SetTexture("_MainTex11", TextureNoiseMQ);
                preset.CompMaterial.SetTexture("_MainTex12", TextureNoiseHQ);
                preset.CompMaterial.SetTexture("_MainTex13", TexturePWNoiseLQ);
                preset.CompMaterial.SetTexture("_MainTex14", TextureNoiseVolLQ);
                preset.CompMaterial.SetTexture("_MainTex15", TextureNoiseVolHQ);

                preset.WarpMaterial.SetVector("resolution", new Vector2(Resolution.x, Resolution.y));
                preset.WarpMaterial.SetVector("aspect", new Vector4(AspectRatio.x, AspectRatio.y, 1f / AspectRatio.x, 1f / AspectRatio.y));
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
                preset.WarpMaterial.SetFloat("frame", CurrentFrame);
                preset.WarpMaterial.SetFloat("fps", FPS);
                preset.WarpMaterial.SetVector("rand_preset", 
                    new Vector4(
                        State.GetVariable(preset.FrameVariables, Var.rand_preset_x),
                        State.GetVariable(preset.FrameVariables, Var.rand_preset_y),
                        State.GetVariable(preset.FrameVariables, Var.rand_preset_z),
                        State.GetVariable(preset.FrameVariables, Var.rand_preset_w)
                    )
                );
                preset.WarpMaterial.SetVector("rand_frame", 
                    new Vector4(
                        UnityEngine.Random.Range(0f, 1f),
                        UnityEngine.Random.Range(0f, 1f),
                        UnityEngine.Random.Range(0f, 1f),
                        UnityEngine.Random.Range(0f, 1f)
                    )
                );
                preset.WarpMaterial.SetVector("_qa", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q1),
                        State.GetVariable(preset.AfterFrameVariables, Var.q2),
                        State.GetVariable(preset.AfterFrameVariables, Var.q3),
                        State.GetVariable(preset.AfterFrameVariables, Var.q4)
                    )
                );
                preset.WarpMaterial.SetVector("_qb", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q5),
                        State.GetVariable(preset.AfterFrameVariables, Var.q6),
                        State.GetVariable(preset.AfterFrameVariables, Var.q7),
                        State.GetVariable(preset.AfterFrameVariables, Var.q8)
                    )
                );
                preset.WarpMaterial.SetVector("_qc", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q9),
                        State.GetVariable(preset.AfterFrameVariables, Var.q10),
                        State.GetVariable(preset.AfterFrameVariables, Var.q11),
                        State.GetVariable(preset.AfterFrameVariables, Var.q12)
                    )
                );
                preset.WarpMaterial.SetVector("_qd", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q13),
                        State.GetVariable(preset.AfterFrameVariables, Var.q14),
                        State.GetVariable(preset.AfterFrameVariables, Var.q15),
                        State.GetVariable(preset.AfterFrameVariables, Var.q16)
                    )
                );
                preset.WarpMaterial.SetVector("_qe", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q17),
                        State.GetVariable(preset.AfterFrameVariables, Var.q18),
                        State.GetVariable(preset.AfterFrameVariables, Var.q19),
                        State.GetVariable(preset.AfterFrameVariables, Var.q20)
                    )
                );
                preset.WarpMaterial.SetVector("_qf", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q21),
                        State.GetVariable(preset.AfterFrameVariables, Var.q22),
                        State.GetVariable(preset.AfterFrameVariables, Var.q23),
                        State.GetVariable(preset.AfterFrameVariables, Var.q24)
                    )
                );
                preset.WarpMaterial.SetVector("_qg", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q25),
                        State.GetVariable(preset.AfterFrameVariables, Var.q26),
                        State.GetVariable(preset.AfterFrameVariables, Var.q27),
                        State.GetVariable(preset.AfterFrameVariables, Var.q28)
                    )
                );
                preset.WarpMaterial.SetVector("_qh", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q29),
                        State.GetVariable(preset.AfterFrameVariables, Var.q30),
                        State.GetVariable(preset.AfterFrameVariables, Var.q31),
                        State.GetVariable(preset.AfterFrameVariables, Var.q32)
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
                preset.WarpMaterial.SetFloat("bias3", bias3);

                // todo rotations
            }

            TargetCamera.targetTexture = TempTexture;
            TargetCamera.Render();

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void DrawDarkenCenter()
        {
            if (State.GetVariable(CurrentPreset.FrameVariables, Var.darken_center) == 0f)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("DrawDarkenCenter");

            TargetMeshFilter.sharedMesh = TargetMeshDarkenCenter;

            TargetMeshRenderer.sharedMaterial = DarkenCenterMaterial;

            DarkenCenterMaterial.mainTexture = TempTexture;

            TargetCamera.targetTexture = TempTexture;
            TargetCamera.Render();

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void DrawOuterBorder()
        {
            UnityEngine.Profiling.Profiler.BeginSample("DrawOuterBorder");

            Color outerColor = new Color
            (
                State.GetVariable(CurrentPreset.FrameVariables, Var.ob_r),
                State.GetVariable(CurrentPreset.FrameVariables, Var.ob_g),
                State.GetVariable(CurrentPreset.FrameVariables, Var.ob_b),
                State.GetVariable(CurrentPreset.FrameVariables, Var.ob_a)
            );

            float borderSize = State.GetVariable(CurrentPreset.FrameVariables, Var.ob_size);

            DrawBorder(outerColor, borderSize, 0f);

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void DrawInnerBorder()
        {
            UnityEngine.Profiling.Profiler.BeginSample("DrawInnerBorder");

            Color innerColor = new Color
            (
                State.GetVariable(CurrentPreset.FrameVariables, Var.ib_r),
                State.GetVariable(CurrentPreset.FrameVariables, Var.ib_g),
                State.GetVariable(CurrentPreset.FrameVariables, Var.ib_b),
                State.GetVariable(CurrentPreset.FrameVariables, Var.ib_a)
            );

            float borderSize = State.GetVariable(CurrentPreset.FrameVariables, Var.ib_size);
            float prevBorderSize = State.GetVariable(CurrentPreset.FrameVariables, Var.ob_size);

            DrawBorder(innerColor, borderSize, prevBorderSize);

            UnityEngine.Profiling.Profiler.EndSample();
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
            float mvOn = State.GetVariable(CurrentPreset.FrameVariables, Var.motionvectorson);
            float mvA = mvOn == 0f ? 0f : State.GetVariable(CurrentPreset.FrameVariables, Var.mv_a);

            float mv_x = State.GetVariable(CurrentPreset.FrameVariables, Var.mv_x);
            float mv_y = State.GetVariable(CurrentPreset.FrameVariables, Var.mv_y);

            int nX = Mathf.FloorToInt(mv_x);
            int nY = Mathf.FloorToInt(mv_y);

            if (mvA <= 0.001f || nX <= 0f || nY <= 0f)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("DrawMotionVectors");

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

            float dx2 = State.GetVariable(CurrentPreset.FrameVariables, Var.mv_dx);
            float dy2 = State.GetVariable(CurrentPreset.FrameVariables, Var.mv_dy);

            float lenMult = State.GetVariable(CurrentPreset.FrameVariables, Var.mv_l);

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
                UnityEngine.Profiling.Profiler.EndSample();
                return;
            }

            Color color = new Color
            (
                State.GetVariable(CurrentPreset.FrameVariables, Var.mv_r),
                State.GetVariable(CurrentPreset.FrameVariables, Var.mv_g), 
                State.GetVariable(CurrentPreset.FrameVariables, Var.mv_b),
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
                    MotionVectors[i].localScale = new Vector3(distance, baseSquareScale.y, baseSquareScale.z);
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

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void DrawBasicWaveform()
        {
            float alpha = State.GetVariable(CurrentPreset.FrameVariables, Var.wave_a);

            float vol = (Bass + Mid + Treb) / 3f;

            if (vol <= -0.01f || alpha <= 0.001f || timeArrayL.Length == 0f)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("DrawBasicWaveform");

            float scale = State.GetVariable(CurrentPreset.FrameVariables, Var.wave_scale) / 128f;
            float smooth = State.GetVariable(CurrentPreset.FrameVariables, Var.wave_smoothing);
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

            int newWaveMode = Mathf.FloorToInt(State.GetVariable(CurrentPreset.FrameVariables, Var.wave_mode)) % 8;
            int oldWaveMode = PrevPreset == null ? 0 : Mathf.FloorToInt(State.GetVariable(PrevPreset.FrameVariables, Var.wave_mode)) % 8;

            float wavePosX = State.GetVariable(CurrentPreset.FrameVariables, Var.wave_x) * 2f - 1f;
            float wavePosY = State.GetVariable(CurrentPreset.FrameVariables, Var.wave_y) * 2f - 1f;

            if (OscilloscopeMode)
            {
                newWaveMode = 8;
                oldWaveMode = 8;
                wavePosX = 0f;
                wavePosY = 0f;
            }

            int numVert = 0;
            int oldNumVert = 0;

            float globalAlpha = 0f;
            float globalAlphaOld = 0f;

            int its = blending && newWaveMode != oldWaveMode ? 2 : 1;

            for (int it = 0; it < its; it++)
            {
                int waveMode = (it == 0) ? newWaveMode : oldWaveMode;

                float fWaveParam2 = State.GetVariable(CurrentPreset.FrameVariables, Var.wave_mystery);

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

                int localNumVert;

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
                
                alpha = State.GetVariable(CurrentPreset.FrameVariables, Var.wave_a);

                float ang;
                int sampleOffset;
                float numVertInv;

                switch (waveMode)
                {
                    case 0:
                        if (State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphabyvolume) > 0f)
                        {
                            float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphaend) - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart);
                            alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart)) / alphaDiff;
                        }
                        alpha = Mathf.Clamp01(alpha);
                        
                        localNumVert = Mathf.FloorToInt(waveL.Count / 2f) + 1;
                        numVertInv = 1f / (localNumVert - 1f);
                        sampleOffset = Mathf.FloorToInt((waveL.Count - localNumVert) / 2f);

                        for (int i = 0; i < localNumVert - 1; i++)
                        {
                            float rad = 0.5f + 0.4f * waveR[i + sampleOffset] + fWaveParam2;
                            ang = i * numVertInv * 2f * Mathf.PI + CurrentTime * 0.2f;

                            if (i < localNumVert / 10f)
                            {
                                float mix = i / (localNumVert * 0.1f);
                                mix = 0.5f - 0.5f * Mathf.Cos(mix * Mathf.PI);
                                float rad2 = 0.5f + 0.4f * waveR[i + localNumVert + sampleOffset] + fWaveParam2;
                                rad = (1f - mix) * rad2 + rad * mix;
                            }

                            positions[i] = new Vector3
                            (
                                rad * Mathf.Cos(ang) * AspectRatio.y + wavePosX,
                                rad * Mathf.Sin(ang) * AspectRatio.x + wavePosY,
                                0f
                            );
                        }

                        positions[localNumVert - 1] = positions[0];
                        break;
                    
                    case 1:
                        alpha *= 1.25f;
                        if (State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphabyvolume) > 0f)
                        {
                            float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphaend) - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart);
                            alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart)) / alphaDiff;
                        }
                        alpha = Mathf.Clamp01(alpha);

                        localNumVert = Mathf.FloorToInt(waveL.Count / 2f);

                        for (int i = 0; i < localNumVert - 1; i++)
                        {
                            float rad = 0.53f + 0.43f * waveR[i] + fWaveParam2;
                            ang = waveL[i + 32] * 0.5f * Mathf.PI + CurrentTime * 2.3f;

                            positions[i] = new Vector3
                            (
                                rad * Mathf.Cos(ang) * AspectRatio.y + wavePosX,
                                rad * Mathf.Sin(ang) * AspectRatio.x + wavePosY,
                                0f
                            );
                        }
                        break;

                    case 2:
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

                        if (State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphabyvolume) > 0f)
                        {
                            float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphaend) - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart);
                            alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart)) / alphaDiff;
                        }
                        alpha = Mathf.Clamp01(alpha);

                        localNumVert = waveL.Count;

                        for (int i = 0; i < waveL.Count; i++)
                        {
                            positions[i] = new Vector3
                            (
                                waveR[i] * AspectRatio.y + wavePosX,
                                waveL[(i + 32) % waveL.Count] * AspectRatio.x + wavePosY,
                                0f
                            );
                        }
                        break;
                    
                    case 3:
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

                        if (State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphabyvolume) > 0f)
                        {
                            float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphaend) - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart);
                            alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart)) / alphaDiff;
                        }
                        alpha = Mathf.Clamp01(alpha);

                        localNumVert = waveL.Count;

                        for (int i = 0; i < waveL.Count; i++)
                        {
                            positions[i] = new Vector3
                            (
                                waveR[i] * AspectRatio.y + wavePosX,
                                waveL[(i + 32) % waveL.Count] * AspectRatio.x + wavePosY,
                                0f
                            );
                        }
                        break;
                    
                    case 4:
                        if (State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphabyvolume) > 0f)
                        {
                            float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphaend) - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart);
                            alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart)) / alphaDiff;
                        }
                        alpha = Mathf.Clamp01(alpha);

                        localNumVert = waveL.Count;

                        if (localNumVert > Resolution.x / 3f)
                        {
                            localNumVert = Mathf.FloorToInt(Resolution.x / 3f);
                        }

                        numVertInv = 1f / localNumVert;
                        sampleOffset = Mathf.FloorToInt((waveL.Count - localNumVert) / 2f);

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
                        break;
                    
                    case 5:
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

                        if (State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphabyvolume) > 0f)
                        {
                            float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphaend) - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart);
                            alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart)) / alphaDiff;
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
                                (x0 * cosRot - y0 * sinRot) * (AspectRatio.y + wavePosX),
                                (x0 * sinRot + y0 * cosRot) * (AspectRatio.x + wavePosY),
                                0f
                            );
                        }
                        break;
                    
                    case 6:
                    case 7:
                        if (State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphabyvolume) > 0f)
                        {
                            float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphaend) - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart);
                            alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart)) / alphaDiff;
                        }
                        alpha = Mathf.Clamp01(alpha);

                        localNumVert = Mathf.FloorToInt(waveL.Count / 2f);

                        if (localNumVert > Resolution.x / 3f)
                        {
                            localNumVert = Mathf.FloorToInt(Resolution.x / 3f);
                        }

                        sampleOffset = Mathf.FloorToInt((waveL.Count - localNumVert) / 2f);
                        ang = Mathf.PI * 0.5f * fWaveParam2;
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
                        break;
                    
                    case 8:
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

                        if (State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphabyvolume) > 0f)
                        {
                            float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphaend) - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart);
                            alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, Var.modwavealphastart)) / alphaDiff;
                        }
                        alpha = Mathf.Clamp01(alpha);

                        localNumVert = waveL.Count;

                        for (int i = 0; i < waveL.Count; i++)
                        {
                            positions[i] = new Vector3
                            (
                                waveL[i] * AspectRatio.y + wavePosX,
                                -waveR[i] * AspectRatio.x + wavePosY,
                                0f
                            );
                        }
                        break;
                    
                    default:
                        UnityEngine.Profiling.Profiler.EndSample();
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
                UnityEngine.Profiling.Profiler.EndSample();

                Debug.LogError("No waveform positions set");
                return;
            }

            float blendMix = 0.5f - 0.5f * Mathf.Cos(blendProgress * Mathf.PI);
            float blendMix2 = 1f - blendMix;

            if (oldNumVert > 0)
            {
                alpha = blendMix * globalAlpha + blendMix2 * globalAlphaOld;
            }

            float r = Mathf.Clamp01(State.GetVariable(CurrentPreset.FrameVariables, Var.wave_r));
            float g = Mathf.Clamp01(State.GetVariable(CurrentPreset.FrameVariables, Var.wave_g));
            float b = Mathf.Clamp01(State.GetVariable(CurrentPreset.FrameVariables, Var.wave_b));

            if (State.GetVariable(CurrentPreset.FrameVariables, Var.wave_brighten) != 0f)
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

            int smoothedNumVert;

            if (OscilloscopeMode)
            {
                smoothedNumVert = numVert;
                Array.Copy(BasicWaveFormPositions, BasicWaveFormPositionsSmooth, numVert);
            }
            else
            {
                smoothedNumVert = numVert * 2 - 1;
                SmoothWave(BasicWaveFormPositions, BasicWaveFormPositionsSmooth, numVert);

                if (newWaveMode == 7 || oldWaveMode == 7)
                {
                    SmoothWave(BasicWaveFormPositions2, BasicWaveFormPositionsSmooth2, numVert);
                }
            }

            if (State.GetVariable(CurrentPreset.FrameVariables, Var.wave_dots) != 0f)
            {
                DotParent.localPosition = new Vector3(0f, 0f, -0.5f);

                Vector3 outOfBounds = new Vector3(0f, 0f, -10f);

                float aspect_ratio = Resolution.x / (float)Resolution.y;

                for (int i = 0; i < MaxSamples * 2; i++)
                {
                    if (i < smoothedNumVert)
                    {
                        Dots[i].localPosition = ValidatePosition(new Vector3(BasicWaveFormPositionsSmooth[i].x * aspect_ratio, BasicWaveFormPositionsSmooth[i].y, 0f));
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
                        Dots[MaxSamples * 2 + i].localPosition = ValidatePosition(new Vector3(BasicWaveFormPositionsSmooth2[i].x * aspect_ratio, BasicWaveFormPositionsSmooth2[i].y, 0f));
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

                DotParent.localPosition = new Vector3(0f, 0f, -10f);
            }
            else
            {
                LineMaterial.mainTexture = TempTexture;
                LineMaterial.SetColor("waveColor", color);
                LineMaterial.SetFloat("additivewave", State.GetVariable(CurrentPreset.FrameVariables, Var.additivewave));
                LineMaterial.SetFloat("aspect_ratio", Resolution.x / (float)Resolution.y);

                var line = new LineQueue();
                line.LinePositions = BasicWaveFormPositionsSmooth.Take(smoothedNumVert).ToArray();
                line.IsThick = State.GetVariable(CurrentPreset.FrameVariables, Var.wave_thick) != 0f;
                line.LineColors = new Color[] { Color.white };
                line.LineMaterial = LineMaterial;
                LinesToDraw.Add(line);

                if (newWaveMode == 7 || oldWaveMode == 7)
                {
                    line = new LineQueue();
                    line.LinePositions = BasicWaveFormPositionsSmooth2.Take(smoothedNumVert).ToArray();
                    line.IsThick = State.GetVariable(CurrentPreset.FrameVariables, Var.wave_thick) != 0f;
                    line.LineColors = new Color[] { Color.white };
                    line.LineMaterial = LineMaterial;
                    LinesToDraw.Add(line);
                }

                TargetMeshFilter.sharedMesh = TargetMeshWarp;
                TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

                DoNothingMaterial.mainTexture = TempTexture;

                TargetCamera.targetTexture = TempTexture;
                TargetCamera.Render();
            }

            UnityEngine.Profiling.Profiler.EndSample();
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

                positionsSmoothed[j] = ValidatePosition(new Vector3(positions[i].x, -positions[i].y, 0f));

                if (zCoord)
                {
                    positionsSmoothed[j + 1] = ValidatePosition(new Vector3
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
                    ));
                }
                else
                {
                    positionsSmoothed[j + 1] = ValidatePosition(new Vector3
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
                    ));
                }

                colorsSmoothed[j] = colors[i];
                colorsSmoothed[j + 1] = colors[i];

                iBelow = i;
                j += 2;
            }

            positionsSmoothed[j] = ValidatePosition(new Vector3(positions[nVertsIn - 1].x, -positions[nVertsIn - 1].y, 0f));
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

                positionsSmoothed[j] = ValidatePosition(new Vector3(positions[i].x, -positions[i].y, 0f));

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
                    positionsSmoothed[j + 1] = ValidatePosition(new Vector3
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
                    ));
                }

                iBelow = i;
                j += 2;
            }

            positionsSmoothed[j] = ValidatePosition(new Vector3(positions[nVertsIn - 1].x, -positions[nVertsIn - 1].y, 0f));
        }

        Vector3 ValidatePosition(Vector3 pos)
        {
            if (float.IsNaN(pos.x))
            {
                pos.x = 0f;
            }
            else
            {
                pos.x = Mathf.Clamp(pos.x, -10f, 10f);
            }

            if (float.IsNaN(pos.y))
            {
                pos.y = 0f;
            }
            else
            {
                pos.y = Mathf.Clamp(pos.y, -10f, 10f);
            }

            return pos;
        }

        void DrawComp(Preset preset, bool blending)
        {
            if (preset.CompMaterial == null)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("DrawComp");

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
                        State.GetVariable(preset.FrameVariables, Var.rand_start_w)
                    );
                hueBase[i * 3 + 1] =
                    0.6f +
                    0.3f *
                    Mathf.Sin(
                        CurrentTime * 30.0f * 0.0107f +
                        1f +
                        i * 13f +
                        State.GetVariable(preset.FrameVariables, Var.rand_start_y)
                    );
                hueBase[i * 3 + 2] =
                    0.6f +
                    0.3f *
                    Mathf.Sin(
                        CurrentTime * 30.0f * 0.0129f +
                        6f +
                        i * 9f +
                        State.GetVariable(preset.FrameVariables, Var.rand_start_z)
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

                    CompColor[offsetColor] = new Color
                    (
                        hueBase[0] * x * y + hueBase[3] * (1f - x) * y + hueBase[6] * x * (1f - y) + hueBase[9] * (1f - x) * (1f - y),
                        hueBase[1] * x * y + hueBase[4] * (1f - x) * y + hueBase[7] * x * (1f - y) + hueBase[10] * (1f - x) * (1f - y),
                        hueBase[2] * x * y + hueBase[5] * (1f - x) * y + hueBase[8] * x * (1f - y) + hueBase[11] * (1f - x) * (1f - y),
                        alpha
                    );

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
                        
                        CompColor[offsetColor].a = alpha;
                    }

                    offsetColor++;
                }
            }

            TargetMeshFilter.sharedMesh = TargetMeshComp;
            TargetMeshComp.SetColors(CompColor);

            preset.CompMaterial.mainTexture = FinalTexture;

            preset.CompMaterial.SetTexture("_MainTexPrev", TempTexture);

            preset.CompMaterial.SetFloat("blending", blending ? 1f : 0f);

            preset.CompMaterial.SetFloat("gammaAdj", State.GetVariable(preset.FrameVariables, Var.gammaadj));
            preset.CompMaterial.SetFloat("echo_zoom", State.GetVariable(preset.FrameVariables, Var.echo_zoom));
            preset.CompMaterial.SetFloat("echo_alpha", State.GetVariable(preset.FrameVariables, Var.echo_alpha));
            preset.CompMaterial.SetFloat("echo_orientation", State.GetVariable(preset.FrameVariables, Var.echo_orient));
            preset.CompMaterial.SetFloat("invert", State.GetVariable(preset.FrameVariables, Var.invert));
            preset.CompMaterial.SetFloat("brighten", State.GetVariable(preset.FrameVariables, Var.brighten));
            preset.CompMaterial.SetFloat("_darken", State.GetVariable(preset.FrameVariables, Var.darken));
            preset.CompMaterial.SetFloat("solarize", State.GetVariable(preset.FrameVariables, Var.solarize));
            preset.CompMaterial.SetFloat("fShader", State.GetVariable(preset.FrameVariables, Var.shader));

            if (!SkipCustomShaded)
            {
                Graphics.Blit(TempTexture, TempTextureFW);
                Graphics.Blit(TempTexture, TempTextureFC);
                Graphics.Blit(TempTexture, TempTexturePW);
                Graphics.Blit(TempTexture, TempTexturePC);

                preset.WarpMaterial.SetTexture("_MainTex2", TempTextureFW);
                preset.WarpMaterial.SetTexture("_MainTex3", TempTextureFC);
                preset.WarpMaterial.SetTexture("_MainTex4", TempTexturePW);
                preset.WarpMaterial.SetTexture("_MainTex5", TempTexturePC);

                preset.CompMaterial.SetTexture("_MainTex6", Blur1Texture);
                preset.CompMaterial.SetTexture("_MainTex7", Blur2Texture);
                preset.CompMaterial.SetTexture("_MainTex8", Blur3Texture);

                preset.CompMaterial.SetTexture("_MainTex9", TextureNoiseLQ);
                preset.CompMaterial.SetTexture("_MainTex10", TextureNoiseLQLite);
                preset.CompMaterial.SetTexture("_MainTex11", TextureNoiseMQ);
                preset.CompMaterial.SetTexture("_MainTex12", TextureNoiseHQ);
                preset.CompMaterial.SetTexture("_MainTex13", TexturePWNoiseLQ);
                preset.CompMaterial.SetTexture("_MainTex14", TextureNoiseVolLQ);
                preset.CompMaterial.SetTexture("_MainTex15", TextureNoiseVolHQ);

                preset.CompMaterial.SetFloat("time", CurrentTime);
                preset.CompMaterial.SetVector("resolution", new Vector2(Resolution.x, Resolution.y));
                preset.CompMaterial.SetVector("aspect", new Vector4(AspectRatio.x, AspectRatio.y, 1f / AspectRatio.x, 1f / AspectRatio.y));
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
                preset.CompMaterial.SetFloat("frame", CurrentFrame);
                preset.CompMaterial.SetFloat("fps", FPS);
                preset.CompMaterial.SetVector("rand_preset", 
                    new Vector4(
                        State.GetVariable(preset.FrameVariables, Var.rand_preset_x),
                        State.GetVariable(preset.FrameVariables, Var.rand_preset_y),
                        State.GetVariable(preset.FrameVariables, Var.rand_preset_z),
                        State.GetVariable(preset.FrameVariables, Var.rand_preset_w)
                    )
                );
                preset.CompMaterial.SetVector("rand_frame", 
                    new Vector4(
                        UnityEngine.Random.Range(0f, 1f),
                        UnityEngine.Random.Range(0f, 1f),
                        UnityEngine.Random.Range(0f, 1f),
                        UnityEngine.Random.Range(0f, 1f)
                    )
                );
                preset.CompMaterial.SetVector("_qa", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q1),
                        State.GetVariable(preset.AfterFrameVariables, Var.q2),
                        State.GetVariable(preset.AfterFrameVariables, Var.q3),
                        State.GetVariable(preset.AfterFrameVariables, Var.q4)
                    )
                );
                preset.CompMaterial.SetVector("_qb", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q5),
                        State.GetVariable(preset.AfterFrameVariables, Var.q6),
                        State.GetVariable(preset.AfterFrameVariables, Var.q7),
                        State.GetVariable(preset.AfterFrameVariables, Var.q8)
                    )
                );
                preset.CompMaterial.SetVector("_qc", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q9),
                        State.GetVariable(preset.AfterFrameVariables, Var.q10),
                        State.GetVariable(preset.AfterFrameVariables, Var.q11),
                        State.GetVariable(preset.AfterFrameVariables, Var.q12)
                    )
                );
                preset.CompMaterial.SetVector("_qd", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q13),
                        State.GetVariable(preset.AfterFrameVariables, Var.q14),
                        State.GetVariable(preset.AfterFrameVariables, Var.q15),
                        State.GetVariable(preset.AfterFrameVariables, Var.q16)
                    )
                );
                preset.CompMaterial.SetVector("_qe", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q17),
                        State.GetVariable(preset.AfterFrameVariables, Var.q18),
                        State.GetVariable(preset.AfterFrameVariables, Var.q19),
                        State.GetVariable(preset.AfterFrameVariables, Var.q20)
                    )
                );
                preset.CompMaterial.SetVector("_qf", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q21),
                        State.GetVariable(preset.AfterFrameVariables, Var.q22),
                        State.GetVariable(preset.AfterFrameVariables, Var.q23),
                        State.GetVariable(preset.AfterFrameVariables, Var.q24)
                    )
                );
                preset.CompMaterial.SetVector("_qg", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q25),
                        State.GetVariable(preset.AfterFrameVariables, Var.q26),
                        State.GetVariable(preset.AfterFrameVariables, Var.q27),
                        State.GetVariable(preset.AfterFrameVariables, Var.q28)
                    )
                );
                preset.CompMaterial.SetVector("_qh", 
                    new Vector4(
                        State.GetVariable(preset.AfterFrameVariables, Var.q29),
                        State.GetVariable(preset.AfterFrameVariables, Var.q30),
                        State.GetVariable(preset.AfterFrameVariables, Var.q31),
                        State.GetVariable(preset.AfterFrameVariables, Var.q32)
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
                preset.CompMaterial.SetFloat("bias3", bias3);

                // todo rotations
            }

            TargetCamera.targetTexture = FinalTexture;
            TargetCamera.Render();

            UnityEngine.Profiling.Profiler.EndSample();
        }

        public void UnloadPresets()
        {
            if (CurrentPreset != null)
            {
                Destroy(CurrentPreset.WarpMaterial);
                Destroy(CurrentPreset.CompMaterial);

                foreach (var wave in CurrentPreset.Waves)
                {
                    Destroy(wave.LineMaterial);
                }

                foreach (var shape in CurrentPreset.Shapes)
                {
                    foreach (var mesh in shape.ShapeMeshes)
                    {
                        Destroy(mesh);
                    }
                    foreach (var mat in shape.ShapeMaterials)
                    {
                        Destroy(mat);
                    }
                    foreach (var mat in shape.BorderMaterials)
                    {
                        Destroy(mat);
                    }
                }
            }

            if (PrevPreset != null)
            {
                Destroy(PrevPreset.WarpMaterial);
                Destroy(PrevPreset.CompMaterial);

                foreach (var wave in PrevPreset.Waves)
                {
                    Destroy(wave.LineMaterial);
                }

                foreach (var shape in PrevPreset.Shapes)
                {
                    foreach (var mesh in shape.ShapeMeshes)
                    {
                        Destroy(mesh);
                    }
                    foreach (var mat in shape.ShapeMaterials)
                    {
                        Destroy(mat);
                    }
                    foreach (var mat in shape.BorderMaterials)
                    {
                        Destroy(mat);
                    }
                }
            }
        }

        public static float GetRating(string file)
        {
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

                if (line.StartsWith("fRating="))
                {
                    var val = line.Substring(8);

                    float result = 0f;

                    if (val == "." || val == "-")
                    {
                        val = "0";
                    }

                    if (!float.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                    {
                        //Debug.LogError("Invalid number " + val + ": " + line);
                        continue;
                    }

                    return result;
                }
            }

            return 0f;
        }

        public static Preset LoadPreset(string file)
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

                if (arg.StartsWith("wave_") && char.IsDigit(arg[5]))
                {
                    int num = int.Parse(arg.Split('_')[1]);

                    while (num >= preset.Waves.Count)
                    {
                        preset.Waves.Add(new Wave(preset));
                    }

                    string codeName = arg.Split('_').Skip(2).Aggregate((a, b) => a + "_" + b);

                    if (string.IsNullOrEmpty(val))
                    {
                        val = ";";
                    }

                    if (codeName.StartsWith("init"))
                    {
                        preset.Waves[num].InitEquationSource += val;
                    }
                    else if (codeName.StartsWith("per_frame"))
                    {
                        preset.Waves[num].FrameEquationSource += val;
                    }
                    else if (codeName.StartsWith("per_point"))
                    {
                        preset.Waves[num].PointEquationSource += val;
                    }
                    else
                    {
                        Debug.LogError("Unknown wave code name " + codeName + ": " + line);
                    }
                }
                else if (arg.StartsWith("wavecode_"))
                {
                    int num = int.Parse(arg.Split('_')[1]);

                    while (num >= preset.Waves.Count)
                    {
                        preset.Waves.Add(new Wave(preset));
                    }

                    string varName = arg.Split('_').Skip(2).Aggregate((a, b) => a + "_" + b);
                    
                    if (val == "." || val == "-")
                    {
                        val = "0";
                    }

                    if (!float.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
                    {
                        Debug.LogError("Invalid number " + val + ": " + line);
                        continue;
                    }

                    if (State.VariableNameLookup.TryGetValue(varName, out Var properVarName))
                    {
                        State.SetVariable(preset.Waves[num].BaseVariables, properVarName, result);
                    }
                    else
                    {
                        if (Enum.TryParse<Var>(varName, out Var varNameEnum))
                        {
                            State.SetVariable(preset.Waves[num].BaseVariables, varNameEnum, result);
                        }
                        else
                        {
                            Debug.LogError("Unknown variable name " + varName + ": " + line);
                        }
                    }
                }
                else if (arg.StartsWith("shape_") && char.IsDigit(arg[6]))
                {
                    int num = int.Parse(arg.Split('_')[1]);

                    while (num >= preset.Shapes.Count)
                    {
                        preset.Shapes.Add(new Shape(preset));
                    }

                    string codeName = arg.Split('_').Skip(2).Aggregate((a, b) => a + "_" + b);

                    if (string.IsNullOrEmpty(val))
                    {
                        val = ";";
                    }

                    if (codeName.StartsWith("init"))
                    {
                        preset.Shapes[num].InitEquationSource += val;
                    }
                    else if (codeName.StartsWith("per_frame"))
                    {
                        preset.Shapes[num].FrameEquationSource += val;
                    }
                    else
                    {
                        Debug.LogError("Unknown shape code name " + codeName + ": " + line);
                    }
                }
                else if (arg.StartsWith("shapecode_"))
                {
                    int num = int.Parse(arg.Split('_')[1]);

                    while (num >= preset.Shapes.Count)
                    {
                        preset.Shapes.Add(new Shape(preset));
                    }

                    string varName = arg.Split('_').Skip(2).Aggregate((a, b) => a + "_" + b);

                    if (varName == "imageurl")
                    {
                        preset.Shapes[num].TextureName = System.IO.Path.GetFileNameWithoutExtension(val);
                    }
                    else
                    {
                        if (val == "." || val == "-")
                        {
                            val = "0";
                        }

                        if (!float.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
                        {
                            Debug.LogError("Invalid number " + val + ": " + line);
                            continue;
                        }

                        if (State.VariableNameLookup.TryGetValue(varName, out Var properVarName))
                        {
                            State.SetVariable(preset.Shapes[num].BaseVariables, properVarName, result);
                        }
                        else
                        {
                            if (Enum.TryParse<Var>(varName, out Var varNameEnum))
                            {
                                State.SetVariable(preset.Shapes[num].BaseVariables, varNameEnum, result);
                            }
                            else
                            {
                                Debug.LogError("Unknown variable name " + varName + ": " + line);
                            }
                        }
                    }
                }
                else if (arg.StartsWith("per_frame_init_"))
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        val = ";";
                    }

                    preset.InitEquationSource += val;
                }
                else if (arg.StartsWith("per_frame_"))
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        val = ";";
                    }

                    preset.FrameEquationSource += val;
                }
                else if (arg.StartsWith("per_pixel_"))
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        val = ";";
                    }

                    preset.PixelEquationSource += val;
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
                    if (val == "." || val == "-")
                    {
                        val = "0";
                    }

                    if (!float.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
                    {
                        Debug.LogError("Invalid number " + val + ": " + line);
                        continue;
                    }

                    if (State.VariableNameLookup.TryGetValue(arg, out Var properVarName))
                    {
                        State.SetVariable(preset.BaseVariables, properVarName, result);
                    }
                    else
                    {
                        if (Enum.TryParse<Var>(arg, out Var varNameEnum))
                        {
                            State.SetVariable(preset.BaseVariables, varNameEnum, result);
                        }
                        else
                        {
                            Debug.LogError("Unknown variable name " + arg + ": " + line);
                        }
                    }
                }
            }

            preset.InitEquationCompiled = Equations.Compile(preset.InitEquationSource);
            preset.FrameEquationCompiled = Equations.Compile(preset.FrameEquationSource);
            preset.PixelEquationCompiled = Equations.Compile(preset.PixelEquationSource);

            foreach (var wave in preset.Waves)
            {
                wave.InitEquationCompiled = Equations.Compile(wave.InitEquationSource);
                wave.FrameEquationCompiled = Equations.Compile(wave.FrameEquationSource);
                wave.PointEquationCompiled = Equations.Compile(wave.PointEquationSource);
            }

            foreach (var shape in preset.Shapes)
            {
                shape.InitEquationCompiled = Equations.Compile(shape.InitEquationSource);
                shape.FrameEquationCompiled = Equations.Compile(shape.FrameEquationSource);
            }

            return preset;
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
                    blendingVertInfoC[midy * (MeshSize.x + 1) + x0] = 0.5f * (t00 + t10) + (UnityEngine.Random.Range(0f, 2f) - 1f) * dt * AspectRatio.y;
                }

                blendingVertInfoC[midy * (MeshSize.x + 1) + x1] = 0.5f * (t01 + t11) + (UnityEngine.Random.Range(0f, 2f) - 1f) * dt * AspectRatio.y;
            }

            if (x1 - x0 >= 2)
            {
                if (y0 == 0)
                {
                    blendingVertInfoC[y0 * (MeshSize.x + 1) + midx] = 0.5f * (t00 + t01) + (UnityEngine.Random.Range(0f, 2f) - 1f) * dt * AspectRatio.x;
                }

                blendingVertInfoC[y1 * (MeshSize.x + 1) + midx] = 0.5f * (t10 + t11) + (UnityEngine.Random.Range(0f, 2f) - 1f) * dt * AspectRatio.x;
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
                    float fy = y / (float)MeshSize.y * AspectRatio.y;

                    for (int x = 0; x <= MeshSize.x; x++)
                    {
                        float fx = x / (float)MeshSize.x * AspectRatio.x;

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
                    float dy = (y / (float)MeshSize.y - 0.5f) * AspectRatio.y;
                    for (int x = 0; x <= MeshSize.x; x++)
                    {
                        float dx = (x / (float)MeshSize.x - 0.5f) * AspectRatio.x;

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

        public bool PlayPreset(int presetIndex, float transitionDuration)
        {
            if (CurrentPreset != null && transitionDuration > 0f)
            {
                CreateBlendPattern();

                blending = true;
                blendDuration = transitionDuration;
                blendProgress = 0f;
            }

            presetStartTime = CurrentTime;

            var newPreset = LoadPreset(PresetFiles[presetIndex].text);

            if (SkipCustomShaded)
            {
                if (!string.IsNullOrEmpty(newPreset.Warp) || !string.IsNullOrEmpty(newPreset.Comp))
                //if (string.IsNullOrEmpty(newPreset.Warp) && string.IsNullOrEmpty(newPreset.Comp))
                {
                    blending = false;
                    return false;
                }
            }

            if (PrevPreset != null)
            {
                Destroy(PrevPreset.WarpMaterial);
                Destroy(PrevPreset.CompMaterial);

                foreach (var wave in PrevPreset.Waves)
                {
                    Destroy(wave.LineMaterial);
                }

                foreach (var shape in PrevPreset.Shapes)
                {
                    foreach (var mesh in shape.ShapeMeshes)
                    {
                        Destroy(mesh);
                    }
                    foreach (var mat in shape.ShapeMaterials)
                    {
                        Destroy(mat);
                    }
                    foreach (var mat in shape.BorderMaterials)
                    {
                        Destroy(mat);
                    }
                }
            }

            PrevPreset = CurrentPreset;
            CurrentPreset = newPreset;

            PresetName = PresetFiles[presetIndex].name;

            foreach (var v in CurrentPreset.BaseVariables.Keys)
            {
                State.SetVariable(CurrentPreset.Variables, v, CurrentPreset.BaseVariables.Heap[(int)v]);
            }

            SetGlobalVars(CurrentPreset.Variables);

            State.SetVariable(CurrentPreset.Variables, Var.rand_start_x, UnityEngine.Random.Range(0f, 1f));
            State.SetVariable(CurrentPreset.Variables, Var.rand_start_y, UnityEngine.Random.Range(0f, 1f));
            State.SetVariable(CurrentPreset.Variables, Var.rand_start_z, UnityEngine.Random.Range(0f, 1f));
            State.SetVariable(CurrentPreset.Variables, Var.rand_start_w, UnityEngine.Random.Range(0f, 1f));
            State.SetVariable(CurrentPreset.Variables, Var.rand_preset_x, UnityEngine.Random.Range(0f, 1f));
            State.SetVariable(CurrentPreset.Variables, Var.rand_preset_y, UnityEngine.Random.Range(0f, 1f));
            State.SetVariable(CurrentPreset.Variables, Var.rand_preset_z, UnityEngine.Random.Range(0f, 1f));
            State.SetVariable(CurrentPreset.Variables, Var.rand_preset_w, UnityEngine.Random.Range(0f, 1f));

            List<Var> nonUserKeys = new List<Var>(CurrentPreset.Variables.Keys);
            for (int i = (int)Var.reg00; i <= (int)Var.reg99; i++)
            {
                nonUserKeys.Add((Var)i);
            }

            var afterInit = new State(CurrentPreset.Variables);

            CurrentPreset.InitEquationCompiled(afterInit);

            CurrentPreset.InitVariables = State.PickQs(afterInit);

            CurrentPreset.RegVariables = State.PickRegs(afterInit);
            var initUserVars = State.Pick(afterInit, nonUserKeys.ToArray());

            CurrentPreset.FrameVariables = new State(CurrentPreset.Variables);

            foreach (var v in CurrentPreset.InitVariables.Keys)
            {
                State.SetVariable(CurrentPreset.FrameVariables, v, CurrentPreset.InitVariables.Heap[(int)v]);
            }

            foreach (var v in CurrentPreset.RegVariables.Keys)
            {
                State.SetVariable(CurrentPreset.FrameVariables, v, CurrentPreset.RegVariables.Heap[(int)v]);
            }

            CurrentPreset.FrameEquationCompiled(CurrentPreset.FrameVariables);

            CurrentPreset.UserKeys = State.Omit(CurrentPreset.FrameVariables, nonUserKeys.ToArray()).Keys.ToArray();
            CurrentPreset.FrameMap = State.Pick(CurrentPreset.FrameVariables, CurrentPreset.UserKeys);
            CurrentPreset.AfterFrameVariables = State.PickQs(CurrentPreset.FrameVariables);
            CurrentPreset.RegVariables = State.PickRegs(CurrentPreset.FrameVariables);

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

                    if (State.GetVariable(CurrentWave.BaseVariables, Var.enabled) != 0f)
                    {
                        foreach (var v in CurrentWave.BaseVariables.Keys)
                        {
                            State.SetVariable(CurrentWave.Variables, v, CurrentWave.BaseVariables.Heap[(int)v]);
                        }

                        SetGlobalVars(CurrentWave.Variables);

                        State.SetVariable(CurrentWave.Variables, Var.rand_start_x, State.GetVariable(CurrentWave.BaseVariables, Var.rand_start_x));
                        State.SetVariable(CurrentWave.Variables, Var.rand_start_y, State.GetVariable(CurrentWave.BaseVariables, Var.rand_start_y));
                        State.SetVariable(CurrentWave.Variables, Var.rand_start_z, State.GetVariable(CurrentWave.BaseVariables, Var.rand_start_z));
                        State.SetVariable(CurrentWave.Variables, Var.rand_start_w, State.GetVariable(CurrentWave.BaseVariables, Var.rand_start_w));
                        State.SetVariable(CurrentWave.Variables, Var.rand_preset_x, State.GetVariable(CurrentWave.BaseVariables, Var.rand_preset_x));
                        State.SetVariable(CurrentWave.Variables, Var.rand_preset_y, State.GetVariable(CurrentWave.BaseVariables, Var.rand_preset_y));
                        State.SetVariable(CurrentWave.Variables, Var.rand_preset_z, State.GetVariable(CurrentWave.BaseVariables, Var.rand_preset_z));
                        State.SetVariable(CurrentWave.Variables, Var.rand_preset_w, State.GetVariable(CurrentWave.BaseVariables, Var.rand_preset_w));

                        List<Var> nonUserWaveKeys = new List<Var>(CurrentWave.Variables.Keys);
                        for (int i = (int)Var.reg00; i <= (int)Var.reg99; i++)
                        {
                            nonUserWaveKeys.Add((Var)i);
                        }
                        for (int i = (int)Var.t1; i <= (int)Var.t8; i++)
                        {
                            nonUserWaveKeys.Add((Var)i);
                        }

                        foreach (var v in CurrentPreset.AfterFrameVariables.Keys)
                        {
                            State.SetVariable(CurrentWave.Variables, v, CurrentPreset.AfterFrameVariables.Heap[(int)v]);
                        }

                        foreach (var v in CurrentPreset.RegVariables.Keys)
                        {
                            State.SetVariable(CurrentWave.Variables, v, CurrentPreset.RegVariables.Heap[(int)v]);
                        }

                        CurrentWave.InitEquationCompiled(CurrentWave.Variables);
                        
                        CurrentPreset.RegVariables = State.PickRegs(CurrentWave.Variables);

                        foreach (var v in CurrentWave.BaseVariables.Keys)
                        {
                            State.SetVariable(CurrentWave.Variables, v, CurrentWave.BaseVariables.Heap[(int)v]);
                        }

                        CurrentWave.Inits = State.PickTs(CurrentWave.Variables);
                        CurrentWave.UserKeys = State.Omit(CurrentWave.Variables, nonUserWaveKeys.ToArray()).Keys.ToArray();
                        CurrentWave.FrameMap = State.Pick(CurrentWave.Variables, CurrentWave.UserKeys);
                    }

                    CurrentWave.LineMaterial = new Material(LineMaterial);
                }
            }

            if (CurrentPreset.Shapes.Count > 0)
            {
                foreach (var CurrentShape in CurrentPreset.Shapes)
                {
                    CurrentShape.Positions = new Vector3[MaxShapeSides + 2];
                    CurrentShape.Colors = new Color[MaxShapeSides + 2];
                    CurrentShape.UVs = new Vector2[MaxShapeSides + 2];
                    CurrentShape.BorderPositions = new Vector3[MaxShapeSides + 1];

                    if (State.GetVariable(CurrentShape.BaseVariables, Var.enabled) != 0f)
                    {
                        foreach (var v in CurrentShape.BaseVariables.Keys)
                        {
                            State.SetVariable(CurrentShape.Variables, v, CurrentShape.BaseVariables.Heap[(int)v]);
                        }

                        SetGlobalVars(CurrentShape.Variables);

                        State.SetVariable(CurrentShape.Variables, Var.rand_start_x, State.GetVariable(CurrentShape.BaseVariables, Var.rand_start_x));
                        State.SetVariable(CurrentShape.Variables, Var.rand_start_y, State.GetVariable(CurrentShape.BaseVariables, Var.rand_start_y));
                        State.SetVariable(CurrentShape.Variables, Var.rand_start_z, State.GetVariable(CurrentShape.BaseVariables, Var.rand_start_z));
                        State.SetVariable(CurrentShape.Variables, Var.rand_start_w, State.GetVariable(CurrentShape.BaseVariables, Var.rand_start_w));
                        State.SetVariable(CurrentShape.Variables, Var.rand_preset_x, State.GetVariable(CurrentShape.BaseVariables, Var.rand_preset_x));
                        State.SetVariable(CurrentShape.Variables, Var.rand_preset_y, State.GetVariable(CurrentShape.BaseVariables, Var.rand_preset_y));
                        State.SetVariable(CurrentShape.Variables, Var.rand_preset_z, State.GetVariable(CurrentShape.BaseVariables, Var.rand_preset_z));
                        State.SetVariable(CurrentShape.Variables, Var.rand_preset_w, State.GetVariable(CurrentShape.BaseVariables, Var.rand_preset_w));

                        List<Var> nonUserShapeKeys = new List<Var>(CurrentShape.Variables.Keys);
                        for (int i = (int)Var.reg00; i <= (int)Var.reg99; i++)
                        {
                            nonUserShapeKeys.Add((Var)i);
                        }
                        for (int i = (int)Var.t1; i <= (int)Var.t8; i++)
                        {
                            nonUserShapeKeys.Add((Var)i);
                        }

                        foreach (var v in CurrentPreset.AfterFrameVariables.Keys)
                        {
                            State.SetVariable(CurrentShape.Variables, v, CurrentPreset.AfterFrameVariables.Heap[(int)v]);
                        }

                        foreach (var v in CurrentPreset.RegVariables.Keys)
                        {
                            State.SetVariable(CurrentShape.Variables, v, CurrentPreset.RegVariables.Heap[(int)v]);
                        }

                        CurrentShape.InitEquationCompiled(CurrentShape.Variables);

                        CurrentPreset.RegVariables = State.PickRegs(CurrentShape.Variables);

                        foreach (var v in CurrentShape.BaseVariables.Keys)
                        {
                            State.SetVariable(CurrentShape.Variables, v, CurrentShape.BaseVariables.Heap[(int)v]);
                        }

                        CurrentShape.Inits = State.PickTs(CurrentShape.Variables);
                        CurrentShape.UserKeys = State.Omit(CurrentShape.Variables, nonUserShapeKeys.ToArray()).Keys.ToArray();
                        CurrentShape.FrameMap = State.Pick(CurrentShape.Variables, CurrentShape.UserKeys);
                    }

                    int numInst = Mathf.FloorToInt(Mathf.Clamp(State.GetVariable(CurrentShape.BaseVariables, Var.num_inst), 1f, 1024f));

                    CurrentShape.ShapeMeshes = new Mesh[numInst];
                    CurrentShape.ShapeMaterials = new Material[numInst];
                    CurrentShape.BorderMaterials = new Material[numInst];

                    for (int i = 0; i < numInst; i++)
                    {
                        CurrentShape.ShapeMeshes[i] = new Mesh();
                        CurrentShape.ShapeMaterials[i] = new Material(ShapeMaterial);
                        CurrentShape.BorderMaterials[i] = new Material(LineMaterial);
                    }

                    if (!string.IsNullOrEmpty(CurrentShape.TextureName))
                    {
                        foreach (var tex in PresetTextures)
                        {
                            if (tex.name == CurrentShape.TextureName)
                            {
                                CurrentShape.Texture = tex;
                                break;
                            }
                        }

                        if (CurrentShape.Texture == null)
                        {
                            Debug.LogError("Texture not found: " + CurrentShape.TextureName);
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(CurrentPreset.Warp))
            {
                string shaderName = "Milkdrop/" + PresetName + " - Warp";

                foreach (var shader in PresetWarpShaders)
                {
                    if (shader.name == shaderName)
                    {
                        if (!shader.isSupported)
                        {
                            Debug.LogError("Shader not supported: " + shaderName);
                            CurrentPreset.WarpMaterial = new Material(DefaultWarpShader);
                            break;
                        }

                        CurrentPreset.WarpMaterial = new Material(shader);
                        break;
                    }
                }

                if (CurrentPreset.WarpMaterial == null)
                {
                    Debug.LogError("Shader not found: " + shaderName);
                    CurrentPreset.WarpMaterial = new Material(DefaultWarpShader);
                }
            }
            else
            {
                CurrentPreset.WarpMaterial = new Material(DefaultWarpShader);
            }

            if (!string.IsNullOrEmpty(CurrentPreset.Comp))
            {
                string shaderName = "Milkdrop/" + PresetName + " - Comp";

                foreach (var shader in PresetCompShaders)
                {
                    if (shader.name == shaderName)
                    {
                        if (!shader.isSupported)
                        {
                            Debug.LogError("Shader not supported: " + shaderName);
                            CurrentPreset.CompMaterial = new Material(DefaultCompShader);
                            break;
                        }

                        CurrentPreset.CompMaterial = new Material(shader);
                        break;
                    }
                }

                if (CurrentPreset.CompMaterial == null)
                {
                    Debug.LogError("Shader not found: " + shaderName);
                    CurrentPreset.CompMaterial = new Material(DefaultCompShader);
                }
            }
            else
            {
                CurrentPreset.CompMaterial = new Material(DefaultCompShader);
            }

            foreach (var tex in PresetTextures)
            {
                CurrentPreset.WarpMaterial.SetTexture("sampler_" + tex.name, tex);
                CurrentPreset.CompMaterial.SetTexture("sampler_" + tex.name, tex);
            }

            if (CurrentPreset.Warp.Contains("sampler_blur3") || CurrentPreset.Comp.Contains("sampler_blur3") || CurrentPreset.Warp.Contains("GetBlur3") || CurrentPreset.Comp.Contains("GetBlur3"))
            {
                CurrentPreset.MaxBlurLevel = 3;
            }
            else if (CurrentPreset.Warp.Contains("sampler_blur2") || CurrentPreset.Comp.Contains("sampler_blur2") || CurrentPreset.Warp.Contains("GetBlur2") || CurrentPreset.Comp.Contains("GetBlur2"))
            {
                CurrentPreset.MaxBlurLevel = 2;
            }
            else if (CurrentPreset.Warp.Contains("sampler_blur1") || CurrentPreset.Comp.Contains("sampler_blur1") || CurrentPreset.Warp.Contains("GetBlur1") || CurrentPreset.Comp.Contains("GetBlur1"))
            {
                CurrentPreset.MaxBlurLevel = 1;
            }
            else
            {
                CurrentPreset.MaxBlurLevel = 0;
            }

            return true;
        }
    }
}