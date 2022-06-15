using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Milkstain
{
    public class Preset
    {
        public State BaseVariables = new State();
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

        public State Variables = new State();
        public State InitVariables = new State();
        public State RegVariables = new State();
        public State FrameVariables = new State();
        public State PixelVariables = new State();

        public int[] UserKeys = new int[0];
        public State FrameMap = new State();
        public State AfterFrameVariables = new State();

        public Material WarpMaterial;
        public Material DarkenCenterMaterial;
        public Material CompMaterial;
    }
}