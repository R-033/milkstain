using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Milkstain
{
    public class Shape
    {
        public State BaseVariables = new State();
        public string InitEquationSource = "";
        public Action<State> InitEquationCompiled;
        public string FrameEquationSource = "";
        public Action<State> FrameEquationCompiled;
        public State Variables = new State();
        public State InitVariables = new State();
        public State FrameVariables = new State();
        public int[] UserKeys = new int[0];
        public State FrameMap = new State();
        public State Inits = new State();

        public Vector3[] Positions;
        public Color[] Colors;
        public Vector2[] UVs;
        public Vector3[] BorderPositions;

        public Mesh ShapeMesh;
    }
}