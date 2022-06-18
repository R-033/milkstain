using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Milkstain
{
    public class Preset
    {
        public Preset()
        {
            BaseVariables = new State(this);
            Variables = new State(this);
            InitVariables = new State(this);
            RegVariables = new State(this);
            FrameVariables = new State(this);
            PixelVariables = new State(this);
            FrameMap = new State(this);
            AfterFrameVariables = new State(this);
        }

        public State BaseVariables;
        public string InitEquationSource = "";
        public Action<State> InitEquationCompiled;
        public string FrameEquationSource = "";
        public Action<State> FrameEquationCompiled;
        public string PixelEquationSource = "";
        public Action<State> PixelEquationCompiled;
        public List<Wave> Waves = new List<Wave>();
        public List<Shape> Shapes = new List<Shape>();
        public string WarpEquation = "";
        public string CompEquation = "";
        public string Warp;
        public string Comp;

        public State Variables;
        public State InitVariables;
        public State RegVariables;
        public State FrameVariables;
        public State PixelVariables;

        public int[] UserKeys = new int[0];
        public State FrameMap;
        public State AfterFrameVariables;

        public Material WarpMaterial;
        public Material DarkenCenterMaterial;
        public Material CompMaterial;

        public float[] MegaBuf = new float[1048576];
        public float[] GMegaBuf = new float[1048576];
    }
}