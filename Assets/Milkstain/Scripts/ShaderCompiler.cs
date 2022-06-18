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

                        for (int i = 0; i < lines.Length; i++)
                        {
                            string line = lines[i];

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
                                        header += line + "\n";
                                    }
                                    break;
                                case 1:
                                    if (line.StartsWith("{"))
                                    {
                                        state = 2;
                                    }
                                    break;
                                case 2:
                                    if (line.StartsWith("}"))
                                    {
                                        state = 3;
                                    }
                                    else
                                    {
                                        body += line + "\n";
                                    }
                                    break;
                            }
                        }

                        var header_lines = header.Replace('\n', ' ').Split(';');

                        for (int i = 0; i < header_lines.Length; i++)
                        {
                            string line = header_lines[i].Trim();

                            if (
                                line.StartsWith("float ") || line.StartsWith("half ") || line.StartsWith("fixed ") || line.StartsWith("int ")
                            )
                            {
                                string[] varNames = string.Join(' ', line.Split(' ').Skip(1)).Split(',');

                                foreach (var varName in varNames)
                                {
                                    properties += varName + " (\"" + varName + "\", Float) = 1\n";
                                }
                            }
                            else if (
                                line.StartsWith("float2 ") || line.StartsWith("half2 ") || line.StartsWith("fixed2 ") || line.StartsWith("int2 ") ||
                                line.StartsWith("float3 ") || line.StartsWith("half3 ") || line.StartsWith("fixed3 ") || line.StartsWith("int3 ") ||
                                line.StartsWith("float4 ") || line.StartsWith("half4 ") || line.StartsWith("fixed4 ") || line.StartsWith("int4 ")
                            )
                            {
                                string[] varNames = string.Join(' ', line.Split(' ').Skip(1)).Split(',');

                                foreach (var varName in varNames)
                                {
                                    properties += varName + " (\"" + varName + "\", Vector) = (1,1,1,1)\n";
                                }
                            }
                            else if (
                                line.StartsWith("sampler ")
                            )
                            {
                                string[] varNames = string.Join(' ', line.Split(' ').Skip(1)).Split(',');

                                foreach (var varName in varNames)
                                {
                                    properties += varName + " (\"" + varName + "\", 2D) = \"white\" {}\n";
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

                        for (int i = 0; i < lines.Length; i++)
                        {
                            string line = lines[i];

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
                                        header += line + "\n";
                                    }
                                    break;
                                case 1:
                                    if (line.StartsWith("{"))
                                    {
                                        state = 2;
                                    }
                                    break;
                                case 2:
                                    if (line.StartsWith("}"))
                                    {
                                        state = 3;
                                    }
                                    else
                                    {
                                        body += line + "\n";
                                    }
                                    break;
                            }
                        }

                        var header_lines = header.Split('\n');

                        for (int i = 0; i < header_lines.Length; i++)
                        {
                            string line = header_lines[i].Trim();

                            if (
                                line.StartsWith("float ") || line.StartsWith("half ") || line.StartsWith("fixed ") || line.StartsWith("int ")
                            )
                            {
                                string[] varNames = string.Join(' ', line.Split(' ').Skip(1)).Split(',');

                                foreach (var varName in varNames)
                                {
                                    properties += varName + " (\"" + varName + "\", Float) = 1\n";
                                }
                            }
                            else if (
                                line.StartsWith("float2 ") || line.StartsWith("half2 ") || line.StartsWith("fixed2 ") || line.StartsWith("int2 ") ||
                                line.StartsWith("float3 ") || line.StartsWith("half3 ") || line.StartsWith("fixed3 ") || line.StartsWith("int3 ") ||
                                line.StartsWith("float4 ") || line.StartsWith("half4 ") || line.StartsWith("fixed4 ") || line.StartsWith("int4 ")
                            )
                            {
                                string[] varNames = string.Join(' ', line.Split(' ').Skip(1)).Split(',');

                                foreach (var varName in varNames)
                                {
                                    properties += varName + " (\"" + varName + "\", Vector) = (1,1,1,1)\n";
                                }
                            }
                            else if (
                                line.StartsWith("sampler ")
                            )
                            {
                                string[] varNames = string.Join(' ', line.Split(' ').Skip(1)).Split(',');

                                foreach (var varName in varNames)
                                {
                                    properties += varName + " (\"" + varName + "\", 2D) = \"white\" {}\n";
                                }
                            }
                        }

                        string result = warpTemplate.Replace("{0}", shaderName).Replace("{1}", properties).Replace("{2}", header).Replace("{3}", body);

                        File.WriteAllText(Application.dataPath + "/Milkstain/Shaders/Comp/Custom/" + shaderName + ".shader", result);
                    }
                }

                AssetDatabase.Refresh();
            }
        }
    }
}

#endif