using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Runtime.CompilerServices;

namespace Milkstain
{
    public class State
    {
        const int HeapSize = 2048;

        public State()
        {
            Keys = new List<int>();
            Heap = new float[HeapSize];
        }

        public State(State other)
        {
            Keys = new List<int>(other.Keys);

            Heap = new float[HeapSize];

            for (int i = 0; i < Keys.Count; i++)
            {
                Heap[Keys[i]] = other.Heap[Keys[i]];
            }
        }
        
        public List<int> Keys;
        public float[] Heap;

        public void Set(int index, float value)
        {
            if (!Keys.Contains(index))
            {
                Keys.Add(index);
            }

            Heap[index] = value;
        }

        public static State Pick(State source, int[] keys)
        {
            State result = new State(source);
            result.Keys = source.Keys.Where(x => keys.Contains(x)).ToList();

            return result;
        }

        public static State Omit(State source, int[] keys)
        {
            State result = new State(source);
            result.Keys = source.Keys.Where(x => !keys.Contains(x)).ToList();

            return result;
        }

        public static Dictionary<string, int> VariableNameTable = new Dictionary<string, int>();
        static int LatestVariableIndex = 0;

        public static readonly Dictionary<string, string> VariableNameLookup = new Dictionary<string, string>
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

        public static int RegisterVariable(string name)
        {
            if (VariableNameTable.TryGetValue(name, out int result))
            {
                return result;
            }

            int ind = LatestVariableIndex++;

            VariableNameTable.Add(name, ind);

            return ind;
        }

        public static float GetVariable(State Variables, string name, float defaultValue)
        {
            int key;

            if (!VariableNameTable.TryGetValue(name, out key))
            {
                return defaultValue;
            }

            if (Variables.Keys.Contains(key))
            {
                return Variables.Heap[key];
            }

            return defaultValue;
        }

        public static float GetVariable(State Variables, string name)
        {
            int key;

            if (!VariableNameTable.TryGetValue(name, out key))
            {
                return 0f;
            }

            return Variables.Heap[key];
        }

        public static void SetVariable(State Variables, string name, float value)
        {
            RegisterVariable(name);

            int key = VariableNameTable[name];

            Variables.Set(key, value);
        }

        public static float GetVariable(State Variables, int key)
        {
            return Variables.Heap[key];
        }

        public static void SetVariable(State Variables, int key, float value)
        {
            Variables.Set(key, value);
        }
    }
}