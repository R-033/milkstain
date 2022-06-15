using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using System;
using System.Runtime.CompilerServices;

namespace Milkstain
{
    public class Milkdrop : MonoBehaviour
    {
        public Preset CurrentPreset;

        public TextAsset[] PresetFiles;

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

        public MeshFilter TargetMeshFilter2;
        public MeshRenderer TargetMeshRenderer2;

        public LineRenderer WaveformRenderer;
        public LineRenderer WaveformRenderer2;
        public LineRenderer WaveformRenderer3;
        public LineRenderer WaveformRenderer4;
        public LineRenderer WaveformRenderer5;
        public LineRenderer WaveformRenderer6;
        public LineRenderer WaveformRenderer7;
        public LineRenderer WaveformRenderer8;

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

        public TextMesh SuperText;

        public bool RandomOrder = true;

        private ulong CurrentFrame = 0;
        private float CurrentTime = 0f;

        private Vector3 baseDotScale;
        private Vector3 baseSquareScale;

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

        int varInd_x;
        int varInd_y;
        int varInd_rad;
        int varInd_ang;
        int varInd_zoom;
        int varInd_zoomexp;
        int varInd_rot;
        int varInd_warp;
        int varInd_cx;
        int varInd_cy;
        int varInd_dx;
        int varInd_dy;
        int varInd_sx;
        int varInd_sy;

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
                State.RegisterVariable(v);
            }

            foreach (var v in _ts)
            {
                State.RegisterVariable(v);
            }

            foreach (var v in _regs)
            {
                State.RegisterVariable(v);
            }

            qs = new int[_qs.Length];

            for (int i = 0; i < qs.Length; i++)
            {
                qs[i] = State.VariableNameTable[_qs[i]];
            }

            ts = new int[_ts.Length];

            for (int i = 0; i < ts.Length; i++)
            {
                ts[i] = State.VariableNameTable[_ts[i]];
            }

            regs = new int[_regs.Length];

            for (int i = 0; i < regs.Length; i++)
            {
                regs[i] = State.VariableNameTable[_regs[i]];
            }

            varInd_x = State.RegisterVariable("x");
            varInd_y = State.RegisterVariable("y");
            varInd_rad = State.RegisterVariable("rad");
            varInd_ang = State.RegisterVariable("ang");
            varInd_zoom = State.RegisterVariable("zoom");
            varInd_zoomexp = State.RegisterVariable("zoomexp");
            varInd_rot = State.RegisterVariable("rot");
            varInd_warp = State.RegisterVariable("warp");
            varInd_cx = State.RegisterVariable("cx");
            varInd_cy = State.RegisterVariable("cy");
            varInd_dx = State.RegisterVariable("dx");
            varInd_dy = State.RegisterVariable("dy");
            varInd_sx = State.RegisterVariable("sx");
            varInd_sy = State.RegisterVariable("sy");
            
            UnloadPresets();

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
                State.RegisterVariable(v);
            }

            foreach (var v in _snappedVariables)
            {
                State.RegisterVariable(v);
            }

            mixedVariables = new int[_mixedVariables.Length];

            for (int i = 0; i < mixedVariables.Length; i++)
            {
                mixedVariables[i] = State.VariableNameTable[_mixedVariables[i]];
            }

            snappedVariables = new int[_snappedVariables.Length];

            for (int i = 0; i < snappedVariables.Length; i++)
            {
                snappedVariables[i] = State.VariableNameTable[_snappedVariables[i]];
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

            var dotSprite = DotPrefab.GetComponent<SpriteRenderer>().sprite;
            var squareSprite = MotionVectorPrefab.GetComponent<SpriteRenderer>().sprite;

            baseDotScale = Vector3.one * (4f / Resolution.y * Scale);
            baseSquareScale = Vector3.one * (1f / Resolution.y * Scale);

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

            PrevTempTexture = new RenderTexture(Resolution.x, Resolution.y, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm);
            TempTexture = new RenderTexture(Resolution.x, Resolution.y, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm);
            FinalTexture = new RenderTexture(Resolution.x, Resolution.y, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm);

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
            WaveformRenderer3.transform.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);
            WaveformRenderer4.transform.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);
            WaveformRenderer5.transform.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);
            WaveformRenderer6.transform.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);
            WaveformRenderer7.transform.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);
            WaveformRenderer8.transform.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);
            BorderParent.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);
            TargetMeshFilter2.transform.localScale = new Vector3(Resolution.x / (float)Resolution.y, 1f, 1f);

            WaveformRenderer.enabled = false;
            WaveformRenderer2.enabled = false;
            WaveformRenderer3.enabled = false;
            WaveformRenderer4.enabled = false;
            WaveformRenderer5.enabled = false;
            WaveformRenderer6.enabled = false;
            WaveformRenderer7.enabled = false;
            WaveformRenderer8.enabled = false;

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

        public void PlayRandomPreset(float transitionDuration)
        {
            if (blending)
            {
                return;
            }

            int ind;
            if (RandomOrder)
            {
                ind = UnityEngine.Random.Range(0, PresetFiles.Length);
            }
            else
            {
                ind = index++;
                if (index >= PresetFiles.Length)
                {
                    index = 0;
                }
            }
            PlayPreset(ind, transitionDuration);
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

        void Render()
        {
            CurrentTime += Mathf.Max(Time.deltaTime, 1f / MaxFPS);
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

            RenderImage();
        }

        void MixFrameEquations()
        {
            float mix = 0.5f - 0.5f * Mathf.Cos(blendProgress * Mathf.PI);
            float mix2 = 1f - mix;
            float snapPoint = 0.5f;
            
            for (int i = 0; i < mixedVariables.Length; i++)
            {
                var v = mixedVariables[i];
                State.SetVariable(CurrentPreset.FrameVariables, v, mix * CurrentPreset.FrameVariables.Heap[v] + mix2 * PrevPreset.FrameVariables.Heap[v]);
            }

            for (int i = 0; i < mixedVariables.Length; i++)
            {
                var v = mixedVariables[i];
                State.SetVariable(CurrentPreset.FrameVariables, v, mix < snapPoint ? PrevPreset.FrameVariables.Heap[v] : CurrentPreset.FrameVariables.Heap[v]);
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

        void RunFrameEquations(Preset preset)
        {
            UnityEngine.Profiling.Profiler.BeginSample("RunFrameEquations");

            foreach (var v in preset.Variables.Keys)
            {
                State.SetVariable(preset.FrameVariables, v, preset.Variables.Heap[v]);
            }

            foreach (var v in preset.InitVariables.Keys)
            {
                State.SetVariable(preset.FrameVariables, v, preset.InitVariables.Heap[v]);
            }

            foreach (var v in preset.FrameMap.Keys)
            {
                State.SetVariable(preset.FrameVariables, v, preset.FrameMap.Heap[v]);
            }

            State.SetVariable(preset.FrameVariables, "frame", CurrentFrame);
            State.SetVariable(preset.FrameVariables, "time", CurrentTime);
            State.SetVariable(preset.FrameVariables, "fps", FPS);
            State.SetVariable(preset.FrameVariables, "bass", Bass);
            State.SetVariable(preset.FrameVariables, "bass_att", BassAtt);
            State.SetVariable(preset.FrameVariables, "mid", Mid);
            State.SetVariable(preset.FrameVariables, "mid_att", MidAtt);
            State.SetVariable(preset.FrameVariables, "treb", Treb);
            State.SetVariable(preset.FrameVariables, "treb_att", TrebAtt);
            State.SetVariable(preset.FrameVariables, "meshx", MeshSize.x);
            State.SetVariable(preset.FrameVariables, "meshy", MeshSize.y);
            State.SetVariable(preset.FrameVariables, "aspectx", 1f);
            State.SetVariable(preset.FrameVariables, "aspecty", 1f);
            State.SetVariable(preset.FrameVariables, "pixelsx", Resolution.x);
            State.SetVariable(preset.FrameVariables, "pixelsy", Resolution.y);

            preset.FrameEquationCompiled(preset.FrameVariables);

            CurrentPreset.AfterFrameVariables = State.Pick(CurrentPreset.FrameVariables, qs);

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void RunPixelEquations(Preset preset, bool blending)
        {
            UnityEngine.Profiling.Profiler.BeginSample("RunPixelEquations");

            int gridX = MeshSize.x;
            int gridZ = MeshSize.y;

            int gridX1 = gridX + 1;
            int gridZ1 = gridZ + 1;

            float warpTimeV = CurrentTime * State.GetVariable(preset.FrameVariables, "warpanimspeed");
            float warpScaleInv = 1f / State.GetVariable(preset.FrameVariables, "warpscale");

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

            var pixelVariables = preset.PixelVariables;

            foreach (var vv in preset.FrameVariables.Keys)
            {
                State.SetVariable(pixelVariables, vv, preset.FrameVariables.Heap[vv]);
            }

            float warp = State.GetVariable(pixelVariables, varInd_warp);
            float zoom = State.GetVariable(pixelVariables, varInd_zoom);
            float zoomExp = State.GetVariable(pixelVariables, varInd_zoomexp);
            float cx = State.GetVariable(pixelVariables, varInd_cx);
            float cy = State.GetVariable(pixelVariables, varInd_cy);
            float sx = State.GetVariable(pixelVariables, varInd_sx);
            float sy = State.GetVariable(pixelVariables, varInd_sy);
            float dx = State.GetVariable(pixelVariables, varInd_dx);
            float dy = State.GetVariable(pixelVariables, varInd_dy);
            float rot = State.GetVariable(pixelVariables, varInd_rot);

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

                    State.SetVariable(pixelVariables, varInd_x, x * 0.5f * aspectx + 0.5f);
                    State.SetVariable(pixelVariables, varInd_y, y * -0.5f * aspecty + 0.5f);
                    State.SetVariable(pixelVariables, varInd_rad, rad);
                    State.SetVariable(pixelVariables, varInd_ang, ang);

                    State.SetVariable(pixelVariables, varInd_zoom, frameZoom);
                    State.SetVariable(pixelVariables, varInd_zoomexp, frameZoomExp);
                    State.SetVariable(pixelVariables, varInd_rot, frameRot);
                    State.SetVariable(pixelVariables, varInd_warp, frameWarp);
                    State.SetVariable(pixelVariables, varInd_cx, framecx);
                    State.SetVariable(pixelVariables, varInd_cy, framecy);
                    State.SetVariable(pixelVariables, varInd_dx, framedx);
                    State.SetVariable(pixelVariables, varInd_dy, framedy);
                    State.SetVariable(pixelVariables, varInd_sx, framesx);
                    State.SetVariable(pixelVariables, varInd_sy, framesy);

                    preset.PixelEquationCompiled(pixelVariables);

                    warp = State.GetVariable(pixelVariables, varInd_warp);
                    zoom = State.GetVariable(pixelVariables, varInd_zoom);
                    zoomExp = State.GetVariable(pixelVariables, varInd_zoomexp);
                    cx = State.GetVariable(pixelVariables, varInd_cx);
                    cy = State.GetVariable(pixelVariables, varInd_cy);
                    sx = State.GetVariable(pixelVariables, varInd_sx);
                    sy = State.GetVariable(pixelVariables, varInd_sy);
                    dx = State.GetVariable(pixelVariables, varInd_dx);
                    dy = State.GetVariable(pixelVariables, varInd_dy);
                    rot = State.GetVariable(pixelVariables, varInd_rot);

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
            float blurMin1 = State.GetVariable(variables, "b1n");
            float blurMin2 = State.GetVariable(variables, "b2n");
            float blurMin3 = State.GetVariable(variables, "b3n");
            float blurMax1 = State.GetVariable(variables, "b1x");
            float blurMax2 = State.GetVariable(variables, "b2x");
            float blurMax3 = State.GetVariable(variables, "b3x");

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

            var pick = State.Pick(CurrentPreset.PixelVariables, regs);

            foreach (var v in pick.Keys)
            {
                State.SetVariable(CurrentPreset.RegVariables, v, pick.Heap[v]);
            }

            // assing regs to global

            if (blending)
            {
                RunFrameEquations(PrevPreset);
                RunPixelEquations(PrevPreset, true);

                MixFrameEquations();
            }

            var swapTexture = TempTexture;
            TempTexture = PrevTempTexture;
            PrevTempTexture = swapTexture;

            TempTexture.wrapMode = State.GetVariable(CurrentPreset.FrameVariables, "wrap") == 0f ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;

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

            //DrawSuperText();

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

        void DrawSuperText()
        {
            UnityEngine.Profiling.Profiler.BeginSample("DrawSuperText");

            SuperText.gameObject.SetActive(true);

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

            foreach (var CurrentShape in preset.Shapes)
            {
                if (State.GetVariable(CurrentShape.BaseVariables, "enabled") == 0f)
                {
                    continue;
                }

                foreach (var v in CurrentShape.Variables.Keys)
                {
                    State.SetVariable(CurrentShape.FrameVariables, v, CurrentShape.Variables.Heap[v]);
                }

                foreach (var v in CurrentShape.FrameMap.Keys)
                {
                    State.SetVariable(CurrentShape.FrameVariables, v, CurrentShape.FrameMap.Heap[v]);
                }

                if (string.IsNullOrEmpty(CurrentShape.FrameEquationSource))
                {
                    foreach (var v in preset.AfterFrameVariables.Keys)
                    {
                        State.SetVariable(CurrentShape.FrameVariables, v, preset.AfterFrameVariables.Heap[v]);
                    }

                    foreach (var v in CurrentShape.Inits.Keys)
                    {
                        State.SetVariable(CurrentShape.FrameVariables, v, CurrentShape.Inits.Heap[v]);
                    }
                }

                State.SetVariable(CurrentShape.FrameVariables, "frame", CurrentFrame);
                State.SetVariable(CurrentShape.FrameVariables, "time", CurrentTime);
                State.SetVariable(CurrentShape.FrameVariables, "fps", FPS);
                State.SetVariable(CurrentShape.FrameVariables, "bass", Bass);
                State.SetVariable(CurrentShape.FrameVariables, "bass_att", BassAtt);
                State.SetVariable(CurrentShape.FrameVariables, "mid", Mid);
                State.SetVariable(CurrentShape.FrameVariables, "mid_att", MidAtt);
                State.SetVariable(CurrentShape.FrameVariables, "treb", Treb);
                State.SetVariable(CurrentShape.FrameVariables, "treb_att", TrebAtt);
                State.SetVariable(CurrentShape.FrameVariables, "meshx", MeshSize.x);
                State.SetVariable(CurrentShape.FrameVariables, "meshy", MeshSize.y);
                State.SetVariable(CurrentShape.FrameVariables, "aspectx", 1f);
                State.SetVariable(CurrentShape.FrameVariables, "aspecty", 1f);
                State.SetVariable(CurrentShape.FrameVariables, "pixelsx", Resolution.x);
                State.SetVariable(CurrentShape.FrameVariables, "pixelsy", Resolution.y);

                int numInst = Mathf.FloorToInt(Mathf.Clamp(State.GetVariable(CurrentShape.BaseVariables, "num_inst"), 1f, 1024f));

                float baseX = State.GetVariable(CurrentShape.BaseVariables, "x");
                float baseY = State.GetVariable(CurrentShape.BaseVariables, "y");
                float baseRad = State.GetVariable(CurrentShape.BaseVariables, "rad");
                float baseAng = State.GetVariable(CurrentShape.BaseVariables, "ang");
                float baseR = State.GetVariable(CurrentShape.BaseVariables, "r");
                float baseG = State.GetVariable(CurrentShape.BaseVariables, "g");
                float baseB = State.GetVariable(CurrentShape.BaseVariables, "b");
                float baseA = State.GetVariable(CurrentShape.BaseVariables, "a");
                float baseR2 = State.GetVariable(CurrentShape.BaseVariables, "r2");
                float baseG2 = State.GetVariable(CurrentShape.BaseVariables, "g2");
                float baseB2 = State.GetVariable(CurrentShape.BaseVariables, "b2");
                float baseA2 = State.GetVariable(CurrentShape.BaseVariables, "a2");
                float baseBorderR = State.GetVariable(CurrentShape.BaseVariables, "border_r");
                float baseBorderG = State.GetVariable(CurrentShape.BaseVariables, "border_g");
                float baseBorderB = State.GetVariable(CurrentShape.BaseVariables, "border_b");
                float baseBorderA = State.GetVariable(CurrentShape.BaseVariables, "border_a");
                float baseThickOutline = State.GetVariable(CurrentShape.BaseVariables, "thickouline");
                float baseTextured = State.GetVariable(CurrentShape.BaseVariables, "textured");
                float baseTexZoom = State.GetVariable(CurrentShape.BaseVariables, "tex_zoom");
                float baseTexAng = State.GetVariable(CurrentShape.BaseVariables, "tex_ang");
                float baseAdditive = State.GetVariable(CurrentShape.BaseVariables, "additive");

                for (int j = 0; j < numInst; j++)
                {
                    State.SetVariable(CurrentShape.FrameVariables, "instance", j);
                    State.SetVariable(CurrentShape.FrameVariables, "x", baseX);
                    State.SetVariable(CurrentShape.FrameVariables, "y", baseY);
                    State.SetVariable(CurrentShape.FrameVariables, "rad", baseRad);
                    State.SetVariable(CurrentShape.FrameVariables, "ang", baseAng);
                    State.SetVariable(CurrentShape.FrameVariables, "r", baseR);
                    State.SetVariable(CurrentShape.FrameVariables, "g", baseG);
                    State.SetVariable(CurrentShape.FrameVariables, "b", baseB);
                    State.SetVariable(CurrentShape.FrameVariables, "a", baseA);
                    State.SetVariable(CurrentShape.FrameVariables, "r2", baseR2);
                    State.SetVariable(CurrentShape.FrameVariables, "g2", baseG2);
                    State.SetVariable(CurrentShape.FrameVariables, "b2", baseB2);
                    State.SetVariable(CurrentShape.FrameVariables, "a2", baseA2);
                    State.SetVariable(CurrentShape.FrameVariables, "border_r", baseBorderR);
                    State.SetVariable(CurrentShape.FrameVariables, "border_g", baseBorderG);
                    State.SetVariable(CurrentShape.FrameVariables, "border_b", baseBorderB);
                    State.SetVariable(CurrentShape.FrameVariables, "border_a", baseBorderA);
                    State.SetVariable(CurrentShape.FrameVariables, "thickouline", baseThickOutline);
                    State.SetVariable(CurrentShape.FrameVariables, "textured", baseTextured);
                    State.SetVariable(CurrentShape.FrameVariables, "tex_zoom", baseTexZoom);
                    State.SetVariable(CurrentShape.FrameVariables, "tex_ang", baseTexAng);
                    State.SetVariable(CurrentShape.FrameVariables, "additive", baseAdditive);

                    if (!string.IsNullOrEmpty(CurrentShape.FrameEquationSource))
                    {
                        foreach (var v in preset.AfterFrameVariables.Keys)
                        {
                            State.SetVariable(CurrentShape.FrameVariables, v, preset.AfterFrameVariables.Heap[v]);
                        }

                        foreach (var v in CurrentShape.Inits.Keys)
                        {
                            State.SetVariable(CurrentShape.FrameVariables, v, CurrentShape.Inits.Heap[v]);
                        }

                        CurrentShape.FrameEquationCompiled(CurrentShape.FrameVariables);
                    }

                    int sides = Mathf.Clamp(Mathf.FloorToInt(State.GetVariable(CurrentShape.FrameVariables, "sides")), 3, 100);

                    float rad = State.GetVariable(CurrentShape.FrameVariables, "rad");
                    float ang = State.GetVariable(CurrentShape.FrameVariables, "ang");

                    float x = State.GetVariable(CurrentShape.FrameVariables, "x") * 2f - 1f;
                    float y = State.GetVariable(CurrentShape.FrameVariables, "y") * 2f - 1f;

                    float r = State.GetVariable(CurrentShape.FrameVariables, "r");
                    float g = State.GetVariable(CurrentShape.FrameVariables, "g");
                    float b = State.GetVariable(CurrentShape.FrameVariables, "b");
                    float a = State.GetVariable(CurrentShape.FrameVariables, "a");
                    float r2 = State.GetVariable(CurrentShape.FrameVariables, "r2");
                    float g2 = State.GetVariable(CurrentShape.FrameVariables, "g2");
                    float b2 = State.GetVariable(CurrentShape.FrameVariables, "b2");
                    float a2 = State.GetVariable(CurrentShape.FrameVariables, "a2");

                    float borderR = State.GetVariable(CurrentShape.FrameVariables, "border_r");
                    float borderG = State.GetVariable(CurrentShape.FrameVariables, "border_g");
                    float borderB = State.GetVariable(CurrentShape.FrameVariables, "border_b");
                    float borderA = State.GetVariable(CurrentShape.FrameVariables, "border_a");

                    Color borderColor = new Color
                    (
                        borderR,
                        borderG,
                        borderB,
                        borderA * blendProgress
                    );

                    float thickoutline = State.GetVariable(CurrentShape.FrameVariables, "thickouline");
                    
                    float textured = State.GetVariable(CurrentShape.FrameVariables, "textured");
                    float texZoom = State.GetVariable(CurrentShape.FrameVariables, "tex_zoom");
                    float texAng = State.GetVariable(CurrentShape.FrameVariables, "tex_ang");

                    float additive = State.GetVariable(CurrentShape.FrameVariables, "additive");

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
                            x + rad * Mathf.Cos(angSum),
                            -(y + rad * Mathf.Sin(angSum)),
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

                    CurrentShape.ShapeMesh.Clear();

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
                    ShapeMaterial.SetTexture("_MainTexPrev", PrevTempTexture);
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
                            WaveformRenderer.widthMultiplier = 2f / Resolution.y * Scale;
                        }
                        else
                        {
                            WaveformRenderer.widthMultiplier = 1f / Resolution.y * Scale;
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

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void DrawWaves(Preset preset, float blendProgress)
        {
            if (preset.Waves.Count == 0)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("DrawWaves");

            foreach (var CurrentWave in preset.Waves)
            {
                if (State.GetVariable(CurrentWave.BaseVariables, "enabled") == 0f)
                {
                    continue;
                }

                foreach (var v in CurrentWave.Variables.Keys)
                {
                    State.SetVariable(CurrentWave.FrameVariables, v, CurrentWave.Variables.Heap[v]);
                }

                foreach (var v in CurrentWave.FrameMap.Keys)
                {
                    State.SetVariable(CurrentWave.FrameVariables, v, CurrentWave.FrameMap.Heap[v]);
                }

                foreach (var v in preset.AfterFrameVariables.Keys)
                {
                    State.SetVariable(CurrentWave.FrameVariables, v, preset.AfterFrameVariables.Heap[v]);
                }

                foreach (var v in CurrentWave.Inits.Keys)
                {
                    State.SetVariable(CurrentWave.FrameVariables, v, CurrentWave.Inits.Heap[v]);
                }

                State.SetVariable(CurrentWave.FrameVariables, "frame", CurrentFrame);
                State.SetVariable(CurrentWave.FrameVariables, "time", CurrentTime);
                State.SetVariable(CurrentWave.FrameVariables, "fps", FPS);
                State.SetVariable(CurrentWave.FrameVariables, "bass", Bass);
                State.SetVariable(CurrentWave.FrameVariables, "bass_att", BassAtt);
                State.SetVariable(CurrentWave.FrameVariables, "mid", Mid);
                State.SetVariable(CurrentWave.FrameVariables, "mid_att", MidAtt);
                State.SetVariable(CurrentWave.FrameVariables, "treb", Treb);
                State.SetVariable(CurrentWave.FrameVariables, "treb_att", TrebAtt);
                State.SetVariable(CurrentWave.FrameVariables, "meshx", MeshSize.x);
                State.SetVariable(CurrentWave.FrameVariables, "meshy", MeshSize.y);
                State.SetVariable(CurrentWave.FrameVariables, "aspectx", 1f);
                State.SetVariable(CurrentWave.FrameVariables, "aspecty", 1f);
                State.SetVariable(CurrentWave.FrameVariables, "pixelsx", Resolution.x);
                State.SetVariable(CurrentWave.FrameVariables, "pixelsy", Resolution.y);

                CurrentWave.FrameEquationCompiled(CurrentWave.FrameVariables);

                int maxSamples = 512;
                int samples = Mathf.FloorToInt(Mathf.Min(State.GetVariable(CurrentWave.FrameVariables, "samples", maxSamples), maxSamples));

                int sep = Mathf.FloorToInt(State.GetVariable(CurrentWave.FrameVariables, "sep"));
                float scaling = State.GetVariable(CurrentWave.FrameVariables, "scaling");
                float spectrum = State.GetVariable(CurrentWave.FrameVariables, "spectrum");
                float smoothing = State.GetVariable(CurrentWave.FrameVariables, "smoothing");
                float usedots = State.GetVariable(CurrentWave.BaseVariables, "usedots");

                float frameR = State.GetVariable(CurrentWave.FrameVariables, "r");
                float frameG = State.GetVariable(CurrentWave.FrameVariables, "g");
                float frameB = State.GetVariable(CurrentWave.FrameVariables, "b");
                float frameA = State.GetVariable(CurrentWave.FrameVariables, "a");

                float waveScale = State.GetVariable(CurrentWave.FrameVariables, "wave_scale");

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

                    State.SetVariable(CurrentWave.FrameVariables, "sample", j / (samples - 1f));
                    State.SetVariable(CurrentWave.FrameVariables, "value1", value1);
                    State.SetVariable(CurrentWave.FrameVariables, "value2", value2);
                    State.SetVariable(CurrentWave.FrameVariables, "x", 0.5f + value1);
                    State.SetVariable(CurrentWave.FrameVariables, "y", 0.5f + value2);
                    State.SetVariable(CurrentWave.FrameVariables, "r", frameR);
                    State.SetVariable(CurrentWave.FrameVariables, "g", frameG);
                    State.SetVariable(CurrentWave.FrameVariables, "b", frameB);
                    State.SetVariable(CurrentWave.FrameVariables, "a", frameA);

                    if (!string.IsNullOrEmpty(CurrentWave.PointEquationSource))
                    {
                        CurrentWave.PointEquationCompiled(CurrentWave.FrameVariables);
                    }

                    float x = State.GetVariable(CurrentWave.FrameVariables, "x") * 2f - 1f;
                    float y = State.GetVariable(CurrentWave.FrameVariables, "y") * 2f - 1f;

                    float r = State.GetVariable(CurrentWave.FrameVariables, "r");
                    float g = State.GetVariable(CurrentWave.FrameVariables, "g");
                    float b = State.GetVariable(CurrentWave.FrameVariables, "b");
                    float a = State.GetVariable(CurrentWave.FrameVariables, "a");

                    CurrentWave.Positions[j] = new Vector3(x, y, 0f);
                    CurrentWave.Colors[j] = new Color(r, g, b, a * blendProgress);
                }

                bool thick = State.GetVariable(CurrentWave.FrameVariables, "thick") != 0f;

                if (usedots != 0f)
                {
                    DotParent.gameObject.SetActive(true);

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

                    DotParent.gameObject.SetActive(false);
                }
                else
                {
                    SmoothWaveAndColor(CurrentWave.Positions, CurrentWave.Colors, CurrentWave.SmoothedPositions, CurrentWave.SmoothedColors, samples);

                    if (thick)
                    {
                        WaveformRenderer.widthMultiplier = 2f / Resolution.y * Scale;
                        WaveformRenderer2.widthMultiplier = 2f / Resolution.y * Scale;
                        WaveformRenderer3.widthMultiplier = 2f / Resolution.y * Scale;
                        WaveformRenderer4.widthMultiplier = 2f / Resolution.y * Scale;
                        WaveformRenderer5.widthMultiplier = 2f / Resolution.y * Scale;
                        WaveformRenderer6.widthMultiplier = 2f / Resolution.y * Scale;
                        WaveformRenderer7.widthMultiplier = 2f / Resolution.y * Scale;
                        WaveformRenderer8.widthMultiplier = 2f / Resolution.y * Scale;
                    }
                    else
                    {
                        WaveformRenderer.widthMultiplier = 1f / Resolution.y * Scale;
                        WaveformRenderer2.widthMultiplier = 1f / Resolution.y * Scale;
                        WaveformRenderer3.widthMultiplier = 1f / Resolution.y * Scale;
                        WaveformRenderer4.widthMultiplier = 1f / Resolution.y * Scale;
                        WaveformRenderer5.widthMultiplier = 1f / Resolution.y * Scale;
                        WaveformRenderer6.widthMultiplier = 1f / Resolution.y * Scale;
                        WaveformRenderer7.widthMultiplier = 1f / Resolution.y * Scale;
                        WaveformRenderer8.widthMultiplier = 1f / Resolution.y * Scale;
                    }

                    WaveformRenderer.sharedMaterial.mainTexture = TempTexture;
                    WaveformRenderer.sharedMaterial.SetColor("waveColor", Color.white);
                    WaveformRenderer.sharedMaterial.SetFloat("additivewave", State.GetVariable(CurrentWave.FrameVariables, "additive"));
                    WaveformRenderer.sharedMaterial.SetFloat("aspect_ratio", Resolution.x / (float)Resolution.y);

                    TargetMeshFilter.sharedMesh = TargetMeshWarp;
                    TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

                    DoNothingMaterial.mainTexture = TempTexture;

                    TargetCamera.targetTexture = TempTexture;

                    samples = samples * 2 - 1;

                    int iterations = Mathf.CeilToInt(samples / (7f * 8f));

                    for (int i = 0; i < iterations; i++)
                    {
                        int start = i * 7 * 8;
                        int end = Mathf.Min(samples, start + 8);
                        int start2 = Mathf.Min(samples, start + 7);
                        int end2 = Mathf.Min(samples, start2 + 8);
                        int start3 = Mathf.Min(samples, start2 + 7);
                        int end3 = Mathf.Min(samples, start3 + 8);
                        int start4 = Mathf.Min(samples, start3 + 7);
                        int end4 = Mathf.Min(samples, start4 + 8);
                        int start5 = Mathf.Min(samples, start4 + 7);
                        int end5 = Mathf.Min(samples, start5 + 8);
                        int start6 = Mathf.Min(samples, start5 + 7);
                        int end6 = Mathf.Min(samples, start6 + 8);
                        int start7 = Mathf.Min(samples, start6 + 7);
                        int end7 = Mathf.Min(samples, start7 + 8);
                        int start8 = Mathf.Min(samples, start7 + 7);
                        int end8 = Mathf.Min(samples, start8 + 8);

                        WaveformRenderer.enabled = start != end;
                        WaveformRenderer2.enabled = start2 != end2;
                        WaveformRenderer3.enabled = start3 != end3;
                        WaveformRenderer4.enabled = start4 != end4;
                        WaveformRenderer5.enabled = start5 != end5;
                        WaveformRenderer6.enabled = start6 != end6;
                        WaveformRenderer7.enabled = start7 != end7;
                        WaveformRenderer8.enabled = start8 != end8;

                        void SetupWave(LineRenderer renderer, int _start, int _end)
                        {
                            if (_start != _end)
                            {
                                renderer.positionCount = _end - _start;

                                for (int j = _start; j < _end; j++)
                                {
                                    renderer.SetPosition(j - _start, CurrentWave.SmoothedPositions[j]);
                                }

                                var grad = new Gradient();

                                var colorKeys = new GradientColorKey[_end - _start];
                                var alphaKeys = new GradientAlphaKey[_end - _start];

                                for (int j = _start; j < _end; j++)
                                {
                                    colorKeys[j - _start] = new GradientColorKey(CurrentWave.SmoothedColors[j], (j - _start) / (float)(_end - _start - 1));
                                    alphaKeys[j - _start] = new GradientAlphaKey(CurrentWave.SmoothedColors[j].a, (j - _start) / (float)(_end - _start - 1));
                                }

                                grad.SetKeys(colorKeys, alphaKeys);

                                renderer.colorGradient = grad;
                            }
                        }

                        SetupWave(WaveformRenderer, start, end);
                        SetupWave(WaveformRenderer2, start2, end2);
                        SetupWave(WaveformRenderer3, start3, end3);
                        SetupWave(WaveformRenderer4, start4, end4);
                        SetupWave(WaveformRenderer5, start5, end5);
                        SetupWave(WaveformRenderer6, start6, end6);
                        SetupWave(WaveformRenderer7, start7, end7);
                        SetupWave(WaveformRenderer8, start8, end8);

                        TargetCamera.Render();
                    }

                    WaveformRenderer.enabled = false;
                    WaveformRenderer2.enabled = false;
                    WaveformRenderer3.enabled = false;
                    WaveformRenderer4.enabled = false;
                    WaveformRenderer5.enabled = false;
                    WaveformRenderer6.enabled = false;
                    WaveformRenderer7.enabled = false;
                    WaveformRenderer8.enabled = false;
                }
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

            preset.WarpMaterial.SetFloat("decay", State.GetVariable(preset.FrameVariables, "decay"));
            preset.WarpMaterial.SetFloat("blending", blending ? 1f : 0f);

            TargetCamera.targetTexture = TempTexture;
            TargetCamera.Render();

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void DrawDarkenCenter()
        {
            if (CurrentPreset.DarkenCenterMaterial == null)
            {
                return;
            }

            if (State.GetVariable(CurrentPreset.FrameVariables, "darken_center") == 0f)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("DrawDarkenCenter");

            TargetMeshFilter.sharedMesh = TargetMeshDarkenCenter;

            TargetMeshRenderer.sharedMaterial = CurrentPreset.DarkenCenterMaterial;

            CurrentPreset.DarkenCenterMaterial.mainTexture = TempTexture;

            TargetCamera.targetTexture = TempTexture;
            TargetCamera.Render();

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void DrawOuterBorder()
        {
            UnityEngine.Profiling.Profiler.BeginSample("DrawOuterBorder");

            Color outerColor = new Color
            (
                State.GetVariable(CurrentPreset.FrameVariables, "ob_r"),
                State.GetVariable(CurrentPreset.FrameVariables, "ob_g"),
                State.GetVariable(CurrentPreset.FrameVariables, "ob_b"),
                State.GetVariable(CurrentPreset.FrameVariables, "ob_a")
            );

            float borderSize = State.GetVariable(CurrentPreset.FrameVariables, "ob_size");

            DrawBorder(outerColor, borderSize, 0f);

            UnityEngine.Profiling.Profiler.EndSample();
        }

        void DrawInnerBorder()
        {
            UnityEngine.Profiling.Profiler.BeginSample("DrawInnerBorder");

            Color innerColor = new Color
            (
                State.GetVariable(CurrentPreset.FrameVariables, "ib_r"),
                State.GetVariable(CurrentPreset.FrameVariables, "ib_g"),
                State.GetVariable(CurrentPreset.FrameVariables, "ib_b"),
                State.GetVariable(CurrentPreset.FrameVariables, "ib_a")
            );

            float borderSize = State.GetVariable(CurrentPreset.FrameVariables, "ib_size");
            float prevBorderSize = State.GetVariable(CurrentPreset.FrameVariables, "ob_size");

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
            float mvOn = State.GetVariable(CurrentPreset.FrameVariables, "bmotionvectorson");
            float mvA = mvOn == 0f ? 0f : State.GetVariable(CurrentPreset.FrameVariables, "mv_a");

            float mv_x = State.GetVariable(CurrentPreset.FrameVariables, "mv_x");
            float mv_y = State.GetVariable(CurrentPreset.FrameVariables, "mv_y");

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

            float dx2 = State.GetVariable(CurrentPreset.FrameVariables, "mv_dx");
            float dy2 = State.GetVariable(CurrentPreset.FrameVariables, "mv_dy");

            float lenMult = State.GetVariable(CurrentPreset.FrameVariables, "mv_l");

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
                State.GetVariable(CurrentPreset.FrameVariables, "mv_r"),
                State.GetVariable(CurrentPreset.FrameVariables, "mv_g"), 
                State.GetVariable(CurrentPreset.FrameVariables, "mv_b"),
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
            float alpha = State.GetVariable(CurrentPreset.FrameVariables, "wave_a");

            float vol = (Bass + Mid + Treb) / 3f;

            if (vol <= -0.01f || alpha <= 0.001f || timeArrayL.Length == 0f)
            {
                return;
            }

            UnityEngine.Profiling.Profiler.BeginSample("DrawBasicWaveform");

            float scale = State.GetVariable(CurrentPreset.FrameVariables, "wave_scale") / 128f;
            float smooth = State.GetVariable(CurrentPreset.FrameVariables, "wave_smoothing");
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

            int newWaveMode = Mathf.FloorToInt(State.GetVariable(CurrentPreset.FrameVariables, "wave_mode")) % 8;
            int oldWaveMode = PrevPreset == null ? 0 : Mathf.FloorToInt(State.GetVariable(PrevPreset.FrameVariables, "wave_mode")) % 8;

            float wavePosX = State.GetVariable(CurrentPreset.FrameVariables, "wave_x") * 2f - 1f;
            float wavePosY = State.GetVariable(CurrentPreset.FrameVariables, "wave_y") * 2f - 1f;

            int numVert = 0;
            int oldNumVert = 0;

            float globalAlpha = 0f;
            float globalAlphaOld = 0f;

            int its = blending && newWaveMode != oldWaveMode ? 2 : 1;

            for (int it = 0; it < its; it++)
            {
                int waveMode = (it == 0) ? newWaveMode : oldWaveMode;

                float fWaveParam2 = State.GetVariable(CurrentPreset.FrameVariables, "wave_mystery");

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
                
                alpha = State.GetVariable(CurrentPreset.FrameVariables, "wave_a");

                if (waveMode == 0)
                {
                    if (State.GetVariable(CurrentPreset.FrameVariables, "modwavealphabyvolume") > 0f)
                    {
                        float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, "modwavealphaend") - State.GetVariable(CurrentPreset.FrameVariables, "modwavealphastart");
                        alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, "modwavealphastart")) / alphaDiff;
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
                    if (State.GetVariable(CurrentPreset.FrameVariables, "modwavealphabyvolume") > 0f)
                    {
                        float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, "modwavealphaend") - State.GetVariable(CurrentPreset.FrameVariables, "modwavealphastart");
                        alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, "modwavealphastart")) / alphaDiff;
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

                    if (State.GetVariable(CurrentPreset.FrameVariables, "modwavealphabyvolume") > 0f)
                    {
                        float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, "modwavealphaend") - State.GetVariable(CurrentPreset.FrameVariables, "modwavealphastart");
                        alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, "modwavealphastart")) / alphaDiff;
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

                    if (State.GetVariable(CurrentPreset.FrameVariables, "modwavealphabyvolume") > 0f)
                    {
                        float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, "modwavealphaend") - State.GetVariable(CurrentPreset.FrameVariables, "modwavealphastart");
                        alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, "modwavealphastart")) / alphaDiff;
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
                    if (State.GetVariable(CurrentPreset.FrameVariables, "modwavealphabyvolume") > 0f)
                    {
                        float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, "modwavealphaend") - State.GetVariable(CurrentPreset.FrameVariables, "modwavealphastart");
                        alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, "modwavealphastart")) / alphaDiff;
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

                    if (State.GetVariable(CurrentPreset.FrameVariables, "modwavealphabyvolume") > 0f)
                    {
                        float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, "modwavealphaend") - State.GetVariable(CurrentPreset.FrameVariables, "modwavealphastart");
                        alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, "modwavealphastart")) / alphaDiff;
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
                    if (State.GetVariable(CurrentPreset.FrameVariables, "modwavealphabyvolume") > 0f)
                    {
                        float alphaDiff = State.GetVariable(CurrentPreset.FrameVariables, "modwavealphaend") - State.GetVariable(CurrentPreset.FrameVariables, "modwavealphastart");
                        alpha *= (vol - State.GetVariable(CurrentPreset.FrameVariables, "modwavealphastart")) / alphaDiff;
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

            float r = Mathf.Clamp01(State.GetVariable(CurrentPreset.FrameVariables, "wave_r"));
            float g = Mathf.Clamp01(State.GetVariable(CurrentPreset.FrameVariables, "wave_g"));
            float b = Mathf.Clamp01(State.GetVariable(CurrentPreset.FrameVariables, "wave_b"));

            if (State.GetVariable(CurrentPreset.FrameVariables, "wave_brighten") != 0f)
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

            if (State.GetVariable(CurrentPreset.FrameVariables, "wave_dots") != 0f)
            {
                DotParent.gameObject.SetActive(true);

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

                DotParent.gameObject.SetActive(false);
            }
            else
            {
                WaveformRenderer.enabled = true;
                
                WaveformRenderer.positionCount = smoothedNumVert;
                WaveformRenderer.SetPositions(BasicWaveFormPositionsSmooth);

                if (State.GetVariable(CurrentPreset.FrameVariables, "wave_thick") != 0f)
                {
                    WaveformRenderer.widthMultiplier = 2f / Resolution.y * Scale;
                }
                else
                {
                    WaveformRenderer.widthMultiplier = 1f / Resolution.y * Scale;
                }

                if (newWaveMode == 7 || oldWaveMode == 7)
                {
                    WaveformRenderer2.enabled = true;

                    WaveformRenderer2.positionCount = smoothedNumVert;
                    WaveformRenderer2.SetPositions(BasicWaveFormPositionsSmooth2);

                    if (State.GetVariable(CurrentPreset.FrameVariables, "wave_thick") != 0f)
                    {
                        WaveformRenderer2.widthMultiplier = 2f / Resolution.y * Scale;
                    }
                    else
                    {
                        WaveformRenderer2.widthMultiplier = 1f / Resolution.y * Scale;
                    }
                }

                WaveformRenderer.colorGradient = new Gradient()
                {
                    colorKeys = new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                };

                WaveformRenderer.sharedMaterial.mainTexture = TempTexture;
                WaveformRenderer.sharedMaterial.SetColor("waveColor", color);
                WaveformRenderer.sharedMaterial.SetFloat("additivewave", State.GetVariable(CurrentPreset.FrameVariables, "additivewave"));
                WaveformRenderer.sharedMaterial.SetFloat("aspect_ratio", Resolution.x / (float)Resolution.y);

                TargetMeshFilter.sharedMesh = TargetMeshWarp;
                TargetMeshRenderer.sharedMaterial = DoNothingMaterial;

                DoNothingMaterial.mainTexture = TempTexture;

                TargetCamera.targetTexture = TempTexture;
                TargetCamera.Render();

                WaveformRenderer.enabled = false;
                WaveformRenderer2.enabled = false;
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

            if (float.IsNaN(pos.y))
            {
                pos.y = 0f;
            }

            return new Vector3(Mathf.Clamp(pos.x, -10f, 10f), Mathf.Clamp(pos.y, -10f, 10f), 0f);
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
                        State.GetVariable(preset.FrameVariables, "rand_start.w")
                    );
                hueBase[i * 3 + 1] =
                    0.6f +
                    0.3f *
                    Mathf.Sin(
                        CurrentTime * 30.0f * 0.0107f +
                        1f +
                        i * 13f +
                        State.GetVariable(preset.FrameVariables, "rand_start.y")
                    );
                hueBase[i * 3 + 2] =
                    0.6f +
                    0.3f *
                    Mathf.Sin(
                        CurrentTime * 30.0f * 0.0129f +
                        6f +
                        i * 9f +
                        State.GetVariable(preset.FrameVariables, "rand_start.z")
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

            preset.CompMaterial.SetFloat("gammaAdj", State.GetVariable(preset.FrameVariables, "gammaadj"));
            preset.CompMaterial.SetFloat("echo_zoom", State.GetVariable(preset.FrameVariables, "echo_zoom"));
            preset.CompMaterial.SetFloat("echo_alpha", State.GetVariable(preset.FrameVariables, "echo_alpha"));
            preset.CompMaterial.SetFloat("echo_orientation", State.GetVariable(preset.FrameVariables, "echo_orient"));
            preset.CompMaterial.SetFloat("invert", State.GetVariable(preset.FrameVariables, "invert"));
            preset.CompMaterial.SetFloat("brighten", State.GetVariable(preset.FrameVariables, "brighten"));
            preset.CompMaterial.SetFloat("_darken", State.GetVariable(preset.FrameVariables, "darken"));
            preset.CompMaterial.SetFloat("solarize", State.GetVariable(preset.FrameVariables, "solarize"));
            preset.CompMaterial.SetFloat("fShader", State.GetVariable(preset.FrameVariables, "fshader"));

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
                Destroy(CurrentPreset.DarkenCenterMaterial);

                foreach (var shape in CurrentPreset.Shapes)
                {
                    Destroy(shape.ShapeMesh);
                }
            }

            if (PrevPreset != null)
            {
                Destroy(PrevPreset.WarpMaterial);
                Destroy(PrevPreset.CompMaterial);
                Destroy(PrevPreset.DarkenCenterMaterial);

                foreach (var shape in PrevPreset.Shapes)
                {
                    Destroy(shape.ShapeMesh);
                }
            }
        }

        private Preset LoadPreset(string file)
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
                        preset.Waves.Add(new Wave());
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
                        preset.Waves.Add(new Wave());
                    }

                    string varName = arg.Split('_').Skip(2).Aggregate((a, b) => a + "_" + b);

                    float result = 0f;

                    if (val == "." || val == "-")
                    {
                        val = "0";
                    }

                    if (!float.TryParse(val, out result))
                    {
                        Debug.LogError("Invalid number " + val + ": " + line);
                        continue;
                    }

                    if (State.VariableNameLookup.ContainsKey(varName))
                    {
                        State.SetVariable(preset.Waves[num].BaseVariables, State.VariableNameLookup[varName], result);
                    }
                    else
                    {
                        State.SetVariable(preset.Waves[num].BaseVariables, varName, result);
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
                        preset.Shapes.Add(new Shape());
                    }

                    string varName = arg.Split('_').Skip(2).Aggregate((a, b) => a + "_" + b);

                    float result = 0f;

                    if (val == "." || val == "-")
                    {
                        val = "0";
                    }

                    if (!float.TryParse(val, out result))
                    {
                        Debug.LogError("Invalid number " + val + ": " + line);
                        continue;
                    }

                    if (State.VariableNameLookup.ContainsKey(varName))
                    {
                        State.SetVariable(preset.Shapes[num].BaseVariables, State.VariableNameLookup[varName], result);
                    }
                    else
                    {
                        State.SetVariable(preset.Shapes[num].BaseVariables, varName, result);
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
                    float result = 0f;

                    if (val == "." || val == "-")
                    {
                        val = "0";
                    }

                    if (!float.TryParse(val, out result))
                    {
                        Debug.LogError("Invalid number " + val + ": " + line);
                        continue;
                    }

                    if (State.VariableNameLookup.ContainsKey(arg))
                    {
                        State.SetVariable(preset.BaseVariables, State.VariableNameLookup[arg], result);
                    }
                    else
                    {
                        State.SetVariable(preset.BaseVariables, arg, result);
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

        public void PlayPreset(int presetIndex, float transitionDuration)
        {
            if (CurrentPreset != null && transitionDuration > 0f)
            {
                CreateBlendPattern();

                blending = true;
                blendStartTime = CurrentTime;
                blendDuration = transitionDuration;
                blendProgress = 0f;
            }

            PresetName = PresetFiles[presetIndex].name;

            PrevPreset = CurrentPreset;
            CurrentPreset = LoadPreset(PresetFiles[presetIndex].text);

            foreach (var v in CurrentPreset.BaseVariables.Keys)
            {
                State.SetVariable(CurrentPreset.Variables, v, CurrentPreset.BaseVariables.Heap[v]);
            }

            State.SetVariable(CurrentPreset.Variables, "frame", CurrentFrame);
            State.SetVariable(CurrentPreset.Variables, "time", CurrentTime);
            State.SetVariable(CurrentPreset.Variables, "fps", FPS == 0f ? 30f : FPS);
            State.SetVariable(CurrentPreset.Variables, "bass", Bass);
            State.SetVariable(CurrentPreset.Variables, "bass_att", BassAtt);
            State.SetVariable(CurrentPreset.Variables, "mid", Mid);
            State.SetVariable(CurrentPreset.Variables, "mid_att", MidAtt);
            State.SetVariable(CurrentPreset.Variables, "treb", Treb);
            State.SetVariable(CurrentPreset.Variables, "treb_att", TrebAtt);
            State.SetVariable(CurrentPreset.Variables, "meshx", MeshSize.x);
            State.SetVariable(CurrentPreset.Variables, "meshy", MeshSize.y);
            State.SetVariable(CurrentPreset.Variables, "aspectx", 1f);
            State.SetVariable(CurrentPreset.Variables, "aspecty", 1f);
            State.SetVariable(CurrentPreset.Variables, "pixelsx", Resolution.x);
            State.SetVariable(CurrentPreset.Variables, "pixelsy", Resolution.y);
            State.SetVariable(CurrentPreset.Variables, "rand_start.x", UnityEngine.Random.Range(0f, 1f));
            State.SetVariable(CurrentPreset.Variables, "rand_start.y", UnityEngine.Random.Range(0f, 1f));
            State.SetVariable(CurrentPreset.Variables, "rand_start.z", UnityEngine.Random.Range(0f, 1f));
            State.SetVariable(CurrentPreset.Variables, "rand_start.w", UnityEngine.Random.Range(0f, 1f));
            State.SetVariable(CurrentPreset.Variables, "rand_preset.x", UnityEngine.Random.Range(0f, 1f));
            State.SetVariable(CurrentPreset.Variables, "rand_preset.y", UnityEngine.Random.Range(0f, 1f));
            State.SetVariable(CurrentPreset.Variables, "rand_preset.z", UnityEngine.Random.Range(0f, 1f));
            State.SetVariable(CurrentPreset.Variables, "rand_preset.w", UnityEngine.Random.Range(0f, 1f));

            List<int> nonUserKeys = new List<int>(CurrentPreset.Variables.Keys);
            nonUserKeys.AddRange(regs);

            var afterInit = new State(CurrentPreset.Variables);

            CurrentPreset.InitEquationCompiled(afterInit);

            CurrentPreset.InitVariables = State.Pick(afterInit, qs);

            CurrentPreset.RegVariables = State.Pick(afterInit, regs);
            var initUserVars = State.Pick(afterInit, nonUserKeys.ToArray());

            CurrentPreset.FrameVariables = new State(CurrentPreset.Variables);

            foreach (var v in CurrentPreset.InitVariables.Keys)
            {
                State.SetVariable(CurrentPreset.FrameVariables, v, CurrentPreset.InitVariables.Heap[v]);
            }

            foreach (var v in CurrentPreset.RegVariables.Keys)
            {
                State.SetVariable(CurrentPreset.FrameVariables, v, CurrentPreset.RegVariables.Heap[v]);
            }

            CurrentPreset.FrameEquationCompiled(CurrentPreset.FrameVariables);

            CurrentPreset.UserKeys = State.Omit(CurrentPreset.FrameVariables, nonUserKeys.ToArray()).Keys.ToArray();
            CurrentPreset.FrameMap = State.Pick(CurrentPreset.FrameVariables, CurrentPreset.UserKeys);
            CurrentPreset.AfterFrameVariables = State.Pick(CurrentPreset.FrameVariables, qs);
            CurrentPreset.RegVariables = State.Pick(CurrentPreset.FrameVariables, regs);

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

                    if (State.GetVariable(CurrentWave.BaseVariables, "enabled") != 0f)
                    {
                        foreach (var v in CurrentWave.BaseVariables.Keys)
                        {
                            State.SetVariable(CurrentWave.Variables, v, CurrentWave.BaseVariables.Heap[v]);
                        }

                        State.SetVariable(CurrentWave.Variables, "frame", CurrentFrame);
                        State.SetVariable(CurrentWave.Variables, "time", CurrentTime);
                        State.SetVariable(CurrentWave.Variables, "fps", FPS);
                        State.SetVariable(CurrentWave.Variables, "bass", Bass);
                        State.SetVariable(CurrentWave.Variables, "bass_att", BassAtt);
                        State.SetVariable(CurrentWave.Variables, "mid", Mid);
                        State.SetVariable(CurrentWave.Variables, "mid_att", MidAtt);
                        State.SetVariable(CurrentWave.Variables, "treb", Treb);
                        State.SetVariable(CurrentWave.Variables, "treb_att", TrebAtt);
                        State.SetVariable(CurrentWave.Variables, "meshx", MeshSize.x);
                        State.SetVariable(CurrentWave.Variables, "meshy", MeshSize.y);
                        State.SetVariable(CurrentWave.Variables, "aspectx", 1f);
                        State.SetVariable(CurrentWave.Variables, "aspecty", 1f);
                        State.SetVariable(CurrentWave.Variables, "pixelsx", Resolution.x);
                        State.SetVariable(CurrentWave.Variables, "pixelsy", Resolution.y);
                        State.SetVariable(CurrentWave.Variables, "rand_start.x", State.GetVariable(CurrentWave.BaseVariables, "rand_start.x"));
                        State.SetVariable(CurrentWave.Variables, "rand_start.y", State.GetVariable(CurrentWave.BaseVariables, "rand_start.y"));
                        State.SetVariable(CurrentWave.Variables, "rand_start.z", State.GetVariable(CurrentWave.BaseVariables, "rand_start.z"));
                        State.SetVariable(CurrentWave.Variables, "rand_start.w", State.GetVariable(CurrentWave.BaseVariables, "rand_start.w"));
                        State.SetVariable(CurrentWave.Variables, "rand_preset.x", State.GetVariable(CurrentWave.BaseVariables, "rand_preset.x"));
                        State.SetVariable(CurrentWave.Variables, "rand_preset.y", State.GetVariable(CurrentWave.BaseVariables, "rand_preset.y"));
                        State.SetVariable(CurrentWave.Variables, "rand_preset.z", State.GetVariable(CurrentWave.BaseVariables, "rand_preset.z"));
                        State.SetVariable(CurrentWave.Variables, "rand_preset.w", State.GetVariable(CurrentWave.BaseVariables, "rand_preset.w"));

                        List<int> nonUserWaveKeys = new List<int>(CurrentWave.Variables.Keys);
                        nonUserWaveKeys.AddRange(regs);
                        nonUserWaveKeys.AddRange(ts);

                        foreach (var v in CurrentPreset.AfterFrameVariables.Keys)
                        {
                            State.SetVariable(CurrentWave.Variables, v, CurrentPreset.AfterFrameVariables.Heap[v]);
                        }

                        foreach (var v in CurrentPreset.RegVariables.Keys)
                        {
                            State.SetVariable(CurrentWave.Variables, v, CurrentPreset.RegVariables.Heap[v]);
                        }

                        CurrentWave.InitEquationCompiled(CurrentWave.Variables);
                        
                        CurrentPreset.RegVariables = State.Pick(CurrentWave.Variables, regs);

                        foreach (var v in CurrentWave.BaseVariables.Keys)
                        {
                            State.SetVariable(CurrentWave.Variables, v, CurrentWave.BaseVariables.Heap[v]);
                        }

                        CurrentWave.Inits = State.Pick(CurrentWave.Variables, ts);
                        CurrentWave.UserKeys = State.Omit(CurrentWave.Variables, nonUserWaveKeys.ToArray()).Keys.ToArray();
                        CurrentWave.FrameMap = State.Pick(CurrentWave.Variables, CurrentWave.UserKeys);
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

                    if (State.GetVariable(CurrentShape.BaseVariables, "enabled") != 0f)
                    {
                        foreach (var v in CurrentShape.BaseVariables.Keys)
                        {
                            State.SetVariable(CurrentShape.Variables, v, CurrentShape.BaseVariables.Heap[v]);
                        }

                        State.SetVariable(CurrentShape.Variables, "frame", CurrentFrame);
                        State.SetVariable(CurrentShape.Variables, "time", CurrentTime);
                        State.SetVariable(CurrentShape.Variables, "fps", FPS);
                        State.SetVariable(CurrentShape.Variables, "bass", Bass);
                        State.SetVariable(CurrentShape.Variables, "bass_att", BassAtt);
                        State.SetVariable(CurrentShape.Variables, "mid", Mid);
                        State.SetVariable(CurrentShape.Variables, "mid_att", MidAtt);
                        State.SetVariable(CurrentShape.Variables, "treb", Treb);
                        State.SetVariable(CurrentShape.Variables, "treb_att", TrebAtt);
                        State.SetVariable(CurrentShape.Variables, "meshx", MeshSize.x);
                        State.SetVariable(CurrentShape.Variables, "meshy", MeshSize.y);
                        State.SetVariable(CurrentShape.Variables, "aspectx", 1f);
                        State.SetVariable(CurrentShape.Variables, "aspecty", 1f);
                        State.SetVariable(CurrentShape.Variables, "pixelsx", Resolution.x);
                        State.SetVariable(CurrentShape.Variables, "pixelsy", Resolution.y);
                        State.SetVariable(CurrentShape.Variables, "rand_start.x", State.GetVariable(CurrentShape.BaseVariables, "rand_start.x"));
                        State.SetVariable(CurrentShape.Variables, "rand_start.y", State.GetVariable(CurrentShape.BaseVariables, "rand_start.y"));
                        State.SetVariable(CurrentShape.Variables, "rand_start.z", State.GetVariable(CurrentShape.BaseVariables, "rand_start.z"));
                        State.SetVariable(CurrentShape.Variables, "rand_start.w", State.GetVariable(CurrentShape.BaseVariables, "rand_start.w"));
                        State.SetVariable(CurrentShape.Variables, "rand_preset.x", State.GetVariable(CurrentShape.BaseVariables, "rand_preset.x"));
                        State.SetVariable(CurrentShape.Variables, "rand_preset.y", State.GetVariable(CurrentShape.BaseVariables, "rand_preset.y"));
                        State.SetVariable(CurrentShape.Variables, "rand_preset.z", State.GetVariable(CurrentShape.BaseVariables, "rand_preset.z"));
                        State.SetVariable(CurrentShape.Variables, "rand_preset.w", State.GetVariable(CurrentShape.BaseVariables, "rand_preset.w"));

                        List<int> nonUserShapeKeys = new List<int>(CurrentShape.Variables.Keys);
                        nonUserShapeKeys.AddRange(regs);
                        nonUserShapeKeys.AddRange(ts);

                        foreach (var v in CurrentPreset.AfterFrameVariables.Keys)
                        {
                            State.SetVariable(CurrentShape.Variables, v, CurrentPreset.AfterFrameVariables.Heap[v]);
                        }

                        foreach (var v in CurrentPreset.RegVariables.Keys)
                        {
                            State.SetVariable(CurrentShape.Variables, v, CurrentPreset.RegVariables.Heap[v]);
                        }

                        CurrentShape.InitEquationCompiled(CurrentShape.Variables);

                        CurrentPreset.RegVariables = State.Pick(CurrentShape.Variables, regs);

                        foreach (var v in CurrentShape.BaseVariables.Keys)
                        {
                            State.SetVariable(CurrentShape.Variables, v, CurrentShape.BaseVariables.Heap[v]);
                        }

                        CurrentShape.Inits = State.Pick(CurrentShape.Variables, ts);
                        CurrentShape.UserKeys = State.Omit(CurrentShape.Variables, nonUserShapeKeys.ToArray()).Keys.ToArray();
                        CurrentShape.FrameMap = State.Pick(CurrentShape.Variables, CurrentShape.UserKeys);
                    }
                }
            }

            if (!string.IsNullOrEmpty(CurrentPreset.Warp))
            {
                Debug.LogError("Compiling shaders is not supported yet");
            }
            CurrentPreset.WarpMaterial = new Material(DefaultWarpShader);

            if (!string.IsNullOrEmpty(CurrentPreset.Comp))
            {
                Debug.LogError("Compiling shaders is not supported yet");
            }
            CurrentPreset.CompMaterial = new Material(DefaultCompShader);

            CurrentPreset.DarkenCenterMaterial = new Material(DarkenCenterShader);
        }
    }
}