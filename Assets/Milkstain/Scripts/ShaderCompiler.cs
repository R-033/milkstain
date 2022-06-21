#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

namespace Milkstain
{
    public class ShaderCompiler : EditorWindow
    {
        [MenuItem ("Window/Milkdrop Shader Compiler")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ShaderCompiler), false, "Milkdrop Shader Compiler");
        }

        static string ProcessShaderCode(string raw)
        {
            raw = raw.Replace("sampler ", "sampler2D ");
            
            raw = raw.Replace("sampler2D sampler_main", "");
            raw = raw.Replace("sampler2D sampler_fw_main", "");
            raw = raw.Replace("sampler2D sampler_fc_main", "");
            raw = raw.Replace("sampler2D sampler_pw_main", "");
            raw = raw.Replace("sampler2D sampler_pc_main", "");
            raw = raw.Replace("sampler2D sampler_blur1", "");
            raw = raw.Replace("sampler2D sampler_blur2", "");
            raw = raw.Replace("sampler2D sampler_blur3", "");
            raw = raw.Replace("sampler2D sampler_noise_lq", "");
            raw = raw.Replace("sampler2D sampler_noise_lq_lite", "");
            raw = raw.Replace("sampler2D sampler_noise_mq", "");
            raw = raw.Replace("sampler2D sampler_noise_hq", "");
            raw = raw.Replace("sampler2D sampler_pw_noise_lq", "");
            raw = raw.Replace("sampler3D sampler_noisevol_lq", "");
            raw = raw.Replace("sampler3D sampler_noisevol_hq", "");

            raw = raw.Replace("sampler_main", "_MainTexPrev");
            raw = raw.Replace("sampler_fw_main", "_MainTex2");
            raw = raw.Replace("sampler_fc_main", "_MainTex3");
            raw = raw.Replace("sampler_pw_main", "_MainTex4");
            raw = raw.Replace("sampler_pc_main", "_MainTex5");
            raw = raw.Replace("sampler_blur1", "_MainTex6");
            raw = raw.Replace("sampler_blur2", "_MainTex7");
            raw = raw.Replace("sampler_blur3", "_MainTex8");
            raw = raw.Replace("sampler_noise_lq", "_MainTex9");
            raw = raw.Replace("sampler_noise_lq_lite", "_MainTex10");
            raw = raw.Replace("sampler_noise_mq", "_MainTex11");
            raw = raw.Replace("sampler_noise_hq", "_MainTex12");
            raw = raw.Replace("sampler_pw_noise_lq", "_MainTex13");
            raw = raw.Replace("sampler_noisevol_lq", "_MainTex14");
            raw = raw.Replace("sampler_noisevol_hq", "_MainTex15");
            raw = raw.Replace("lum(", "lum__(");
            raw = raw.Replace("lum (", "lum__ (");
            raw = raw.Replace("tex2d", "tex2D");
            raw = raw.Replace("tex3d", "tex3D");
            raw = raw.Replace("front", "_front");
            raw = raw.Replace("back", "_back");

            raw = raw.Replace("double ", "float ");
            raw = raw.Replace("double2 ", "float2 ");
            raw = raw.Replace("double3 ", "float3 ");
            raw = raw.Replace("double4 ", "float4 ");

            return raw;
        }

        void OnGUI()
        {
            GUILayout.Space(20f);

            if (GUILayout.Button("Compile all shaders"))
            {
                var milkdrop = Resources.FindObjectsOfTypeAll<Milkdrop>()[0];

                var presets = milkdrop.PresetFiles;

                string warpTemplate = Resources.Load<TextAsset>("Templates/WarpTemplate").text;
                string compTemplate = Resources.Load<TextAsset>("Templates/CompTemplate").text;

                foreach (var preset in presets)
                {
                    var presetData = Milkdrop.LoadPreset(preset.text);

                    if (!string.IsNullOrEmpty(presetData.Warp))
                    {
                        string shaderName = preset.name + " - Warp";
                        string properties = "";
                        string header = "";
                        string body = "";

                        string[] lines = presetData.Warp.Split('\n');

                        int state = 0;

                        int depth = 0;

                        for (int i = 0; i < lines.Length; i++)
                        {
                            string lineRaw = lines[i];
                            string line = lineRaw.Trim();

                            switch (state)
                            {
                                case 0:
                                    if (line.StartsWith("shader_body"))
                                    {
                                        if (line.Contains("{"))
                                        {
                                            if (line.Contains("}"))
                                            {
                                                state = 3;
                                            }
                                            else
                                            {
                                                depth++;
                                                state = 2;
                                            }
                                        }
                                        else
                                        {
                                            state = 1;
                                        }
                                    }
                                    else
                                    {
                                        header += lineRaw + "\n";
                                    }
                                    break;
                                case 1:
                                    if (line.StartsWith("{"))
                                    {
                                        depth++;
                                        state = 2;
                                        if (depth > 1)
                                        {
                                            body += lineRaw + "\n";
                                        }
                                    }
                                    break;
                                case 2:
                                    if (line.Contains("{"))
                                    {
                                        depth++;
                                    }
                                    if (line.StartsWith("}"))
                                    {
                                        depth--;
                                        if (depth <= 0)
                                        {
                                            state = 3;
                                        }
                                        else
                                        {
                                            body += lineRaw + "\n";
                                        }
                                    }
                                    else if (line.Contains("}"))
                                    {
                                        depth--;
                                        body += lineRaw + "\n";
                                    }
                                    else
                                    {
                                        body += lineRaw + "\n";
                                    }
                                    break;
                            }
                        }

                        header = ProcessShaderCode(header);
                        body = ProcessShaderCode(body);

                        var header_lines = header.Replace('\n', ' ').Split(';');

                        depth = 0;

                        for (int i = 0; i < header_lines.Length; i++)
                        {
                            string line = header_lines[i].Trim();

                            if (line.Contains("//"))
                            {
                                line = line.Split(new string[] {"//"}, System.StringSplitOptions.RemoveEmptyEntries)[0];
                            }

                            if (line.Contains('{'))
                            {
                                depth++;
                            }

                            if (line.Contains('}'))
                            {
                                depth--;
                            }

                            if (line.Contains('('))
                            {
                                continue;
                            }

                            if (depth > 0)
                            {
                                continue;
                            }

                            if (
                                line.StartsWith("sampler2D ")
                            )
                            {
                                string[] varNames = string.Join(" ", line.Replace(';', ' ').Split(new char[] {' '}, System.StringSplitOptions.RemoveEmptyEntries).Skip(1)).Split(',');
                                

                                foreach (var varName in varNames)
                                {
                                    string n = varName.Trim();

                                    if (n == "color")
                                    {
                                        continue;
                                    }

                                    properties += n + " (\"" + n + "\", 2D) = \"white\" {}\n";
                                }
                            }
                        }

                        string result = warpTemplate.Replace("{0}", shaderName).Replace("{1}", properties).Replace("{2}", header).Replace("{3}", body);

                        File.WriteAllText(Application.dataPath + "/Milkstain/Shaders/Warp/Custom/" + shaderName + ".shader", result);
                    }

                    if (!string.IsNullOrEmpty(presetData.Comp))
                    {
                        string shaderName = preset.name + " - Comp";
                        string properties = "";
                        string header = "";
                        string body = "";

                        string[] lines = presetData.Comp.Split('\n');

                        int state = 0;

                        int depth = 0;

                        for (int i = 0; i < lines.Length; i++)
                        {
                            string lineRaw = lines[i];
                            string line = lineRaw.Trim();

                            switch (state)
                            {
                                case 0:
                                    if (line.StartsWith("shader_body"))
                                    {
                                        if (line.Contains("{"))
                                        {
                                            if (line.Contains("}"))
                                            {
                                                state = 3;
                                            }
                                            else
                                            {
                                                depth++;
                                                state = 2;
                                            }
                                        }
                                        else
                                        {
                                            state = 1;
                                        }
                                    }
                                    else
                                    {
                                        header += lineRaw + "\n";
                                    }
                                    break;
                                case 1:
                                    if (line.StartsWith("{"))
                                    {
                                        depth++;
                                        state = 2;
                                        if (depth > 1)
                                        {
                                            body += lineRaw + "\n";
                                        }
                                    }
                                    break;
                                case 2:
                                    if (line.Contains("{"))
                                    {
                                        depth++;
                                    }
                                    if (line.StartsWith("}"))
                                    {
                                        depth--;
                                        if (depth <= 0)
                                        {
                                            state = 3;
                                        }
                                        else
                                        {
                                            body += lineRaw + "\n";
                                        }
                                    }
                                    else if (line.Contains("}"))
                                    {
                                        depth--;
                                        body += lineRaw + "\n";
                                    }
                                    else
                                    {
                                        body += lineRaw + "\n";
                                    }
                                    break;
                            }
                        }

                        header = ProcessShaderCode(header);
                        body = ProcessShaderCode(body);

                        var header_lines = header.Split('\n');

                        depth = 0;

                        for (int i = 0; i < header_lines.Length; i++)
                        {
                            string line = header_lines[i].Trim();

                            if (line.Contains("//"))
                            {
                                line = line.Split(new string[] {"//"}, System.StringSplitOptions.RemoveEmptyEntries)[0];
                            }

                            if (line.Contains('{'))
                            {
                                depth++;
                            }

                            if (line.Contains('}'))
                            {
                                depth--;
                            }

                            if (line.Contains('('))
                            {
                                continue;
                            }

                            if (depth > 0)
                            {
                                continue;
                            }

                            if (
                                line.StartsWith("sampler2D ")
                            )
                            {
                                string[] varNames = string.Join(" ", line.Replace(';', ' ').Split(new char[] {' '}, System.StringSplitOptions.RemoveEmptyEntries).Skip(1)).Split(',');

                                foreach (var varName in varNames)
                                {
                                    string n = varName.Trim();

                                    if (n == "color")
                                    {
                                        continue;
                                    }

                                    properties += n + " (\"" + n + "\", 2D) = \"white\" {}\n";
                                }
                            }
                        }

                        string result = compTemplate.Replace("{0}", shaderName).Replace("{1}", properties.Replace("\n", "\n\t\t")).Replace("{2}", header.Replace("\n", "\n\t\t\t")).Replace("{3}", body.Replace("\n", "\n\t\t\t\t"));

                        File.WriteAllText(Application.dataPath + "/Milkstain/Shaders/Comp/Custom/" + shaderName + ".shader", result);
                    }
                }

                AssetDatabase.Refresh();
            }
        }
    }
}

#endif