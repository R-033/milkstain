using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Runtime.CompilerServices;

namespace Milkstain
{
    public enum Var : int
    {
        x, y,
        rad, ang,
        zoom, zoomexp,
        rot,
        warp,
        cx, cy,
        dx, dy,
        sx, sy,
        enabled,
        sep,
        scaling,
        spectrum,
        smoothing,
        usedots,
        r, g, b, a,
        r2, g2, b2, a2,
        wave_scale,
        sample,
        value1, value2,
        thick,
        num_inst,
        border_r, border_g, border_b, border_a,
        thickoutline,
        textured, tex_zoom, tex_ang,
        additive,
        instance,
        sides,
        samples,
        warpanimspeed,
        warpscale,
        rating,
        gammaadj,
        decay,
        echo_zoom, echo_alpha, echo_orient,
        wave_mode, additivewave,
        wave_dots, wave_thick,
        modwavealphabyvolume,
        wave_brighten,
        ob_size, ob_r, ob_g, ob_b, ob_a,
        ib_size, ib_r, ib_g, ib_b, ib_a,
        wrap,
        darken_center,
        red_blue,
        brighten,
        darken,
        solarize,
        invert,
        wave_r, wave_g, wave_b, wave_a,
        wave_smoothing, wave_mystery,
        wave_x, wave_y,
        modwavealphastart,
        modwavealphaend,
        mv_x, mv_y,
        mv_dx, mv_dy,
        mv_l,
        mv_r, mv_g, mv_b, mv_a,
        b1n, b2n, b3n,
        b1x, b2x, b3x,
        b1ed, b2ed, b3ed,
        frame, time, fps, progress,
        bass, bass_att, mid, mid_att, treb, treb_att,
        meshx, meshy,
        aspectx, aspecty,
        pixelsx, pixelsy,
        motionvectorson,
        shader,
        rand_preset_x, rand_preset_y, rand_preset_z, rand_preset_w,
        rand_start_x, rand_start_y, rand_start_z, rand_start_w,
        q1, q2, q3, q4, q5, q6, q7, q8, q9, q10, q11, q12, q13, q14, q15, q16,
        q17, q18, q19, q20, q21, q22, q23, q24, q25, q26, q27, q28, q29, q30, q31, q32,
        t1, t2, t3, t4, t5, t6, t7, t8,
        reg00, reg01, reg02, reg03, reg04, reg05, reg06, reg07, reg08, reg09,
        reg10, reg11, reg12, reg13, reg14, reg15, reg16, reg17, reg18, reg19,
        reg20, reg21, reg22, reg23, reg24, reg25, reg26, reg27, reg28, reg29,
        reg30, reg31, reg32, reg33, reg34, reg35, reg36, reg37, reg38, reg39,
        reg40, reg41, reg42, reg43, reg44, reg45, reg46, reg47, reg48, reg49,
        reg50, reg51, reg52, reg53, reg54, reg55, reg56, reg57, reg58, reg59,
        reg60, reg61, reg62, reg63, reg64, reg65, reg66, reg67, reg68, reg69,
        reg70, reg71, reg72, reg73, reg74, reg75, reg76, reg77, reg78, reg79,
        reg80, reg81, reg82, reg83, reg84, reg85, reg86, reg87, reg88, reg89,
        reg90, reg91, reg92, reg93, reg94, reg95, reg96, reg97, reg98, reg99,
        VariableCount
    }
    
    public class State
    {
        const int HeapSize = 2048;

        public static Var[] MixedVariables = new Var[]
        {
            Var.wave_a, Var.wave_r, Var.wave_g, Var.wave_b, Var.wave_x, Var.wave_y, Var.wave_mystery,
            Var.ob_size, Var.ob_r, Var.ob_g, Var.ob_b, Var.ob_a,
            Var.ib_size, Var.ib_r, Var.ib_g, Var.ib_b, Var.ib_a,
            Var.mv_x, Var.mv_y, Var.mv_dx, Var.mv_dy, Var.mv_l, Var.mv_r, Var.mv_g, Var.mv_b, Var.mv_a,
            Var.echo_zoom, Var.echo_alpha, Var.echo_orient,
            Var.b1n, Var.b2n, Var.b3n,
            Var.b1x, Var.b2x, Var.b3x,
            Var.b1ed, Var.b2ed, Var.b3ed
        };

        public static Var[] SnappedVariables = new Var[]
        {
            Var.wave_dots, Var.wave_thick, Var.additivewave, Var.wave_brighten, Var.darken_center, Var.gammaadj,
            Var.wrap, Var.invert, Var.brighten, Var.darken, Var.solarize
        };

        public State(Preset preset)
        {
            Heap = new float[HeapSize];
            SourcePreset = preset;
        }

        public State(State other)
        {
            Heap = new float[HeapSize];
            for (int i = 0; i < Heap.Length; i++)
            {
                Heap[i] = other.Heap[i];
            }
            Keys = new List<Var>(other.Keys);
            SourcePreset = other.SourcePreset;
        }

        public static Dictionary<string, Var> CustomVariables = new Dictionary<string, Var>();

        public List<Var> Keys = new List<Var>();
        public float[] Heap;
        public Preset SourcePreset;

        public static State Pick(State source, Var[] keys)
        {
            State result = new State(source.SourcePreset);
            for (int i = 0; i < keys.Length; i++)
            {
                result.Heap[(int)keys[i]] = source.Heap[(int)keys[i]];
            }
            result.Keys.AddRange(keys);
            return result;
        }

        public static State PickQs(State source)
        {
            State result = new State(source.SourcePreset);
            for (int i = (int)Var.q1; i <= (int)Var.q32; i++)
            {
                result.Heap[i] = source.Heap[i];
                result.Keys.Add((Var)i);
            }
            return result;
        }

        public static State PickTs(State source)
        {
            State result = new State(source.SourcePreset);
            for (int i = (int)Var.t1; i <= (int)Var.t8; i++)
            {
                result.Heap[i] = source.Heap[i];
                result.Keys.Add((Var)i);
            }
            return result;
        }

        public static State PickRegs(State source)
        {
            State result = new State(source.SourcePreset);
            for (int i = (int)Var.reg00; i <= (int)Var.reg99; i++)
            {
                result.Heap[i] = source.Heap[i];
                result.Keys.Add((Var)i);
            }
            return result;
        }

        public static State Omit(State source, Var[] keys)
        {
            State result = new State(source.SourcePreset);
            var allVars = (Var[])Enum.GetValues(typeof(Var));
            for (int i = 0; i < allVars.Length - 1; i++)
            {
                if (keys.Contains(allVars[i]))
                {
                    continue;
                }
                result.Heap[(int)allVars[i]] = source.Heap[(int)allVars[i]];
                result.Keys.Add(allVars[i]);
            }
            return result;
        }

        public static readonly Dictionary<string, Var> VariableNameLookup = new Dictionary<string, Var>
        {
            {"frating", Var.rating},
            {"fgammaadj", Var.gammaadj},
            {"fdecay", Var.decay},
            {"fvideoechozoom", Var.echo_zoom},
            {"fvideoechoalpha", Var.echo_alpha},
            {"nvideoechoorientation", Var.echo_orient},
            {"nwavemode", Var.wave_mode},
            {"badditivewaves", Var.additivewave},
            {"bwavedots", Var.wave_dots},
            {"bwavethick", Var.wave_thick},
            {"bmodwavealphabyvolume", Var.modwavealphabyvolume},
            {"bmaximizewavecolor", Var.wave_brighten},
            {"btexwrap", Var.wrap},
            {"bdarkencenter", Var.darken_center},
            {"bredbluestereo", Var.red_blue},
            {"bbrighten", Var.brighten},
            {"bdarken", Var.darken},
            {"bsolarize", Var.solarize},
            {"binvert", Var.invert},
            {"fwavealpha", Var.wave_a},
            {"fwavescale", Var.wave_scale},
            {"fwavesmoothing", Var.wave_smoothing},
            {"fwaveparam", Var.wave_mystery},
            {"fmodwavealphastart", Var.modwavealphastart},
            {"fmodwavealphaend", Var.modwavealphaend},
            {"fwarpanimspeed", Var.warpanimspeed},
            {"fwarpscale", Var.warpscale},
            {"fzoomexponent", Var.zoomexp},
            {"nmotionvectorsx", Var.mv_x},
            {"nmotionvectorsy", Var.mv_y},
            {"thick", Var.thickoutline},
            {"instances", Var.num_inst},
            {"num_instances", Var.num_inst},
            {"badditive", Var.additive},
            {"busedots", Var.usedots},
            {"bspectrum", Var.spectrum},
            {"bdrawthick", Var.thick},
            {"bmotionvectorson", Var.motionvectorson},
            {"rand_preset.x", Var.rand_preset_x},
            {"rand_preset.y", Var.rand_preset_y},
            {"rand_preset.z", Var.rand_preset_z},
            {"rand_preset.w", Var.rand_preset_w},
            {"rand_start.x", Var.rand_start_x},
            {"rand_start.y", Var.rand_start_y},
            {"rand_start.z", Var.rand_start_z},
            {"rand_start.w", Var.rand_start_w},
            {"fshader", Var.shader}
        };

        public static float GetVariable(State Variables, Var key)
        {
            return Variables.Heap[(int)key];
        }

        public static void SetVariable(State Variables, Var key, float value)
        {
            if (Variables.Heap[(int)key] == 0f && !Variables.Keys.Contains(key))
            {
                Variables.Keys.Add(key);
            }
            Variables.Heap[(int)key] = value;
        }
    }
}