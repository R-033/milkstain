using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Milkstain
{
    public class Wave
    {
        public State BaseVariables = new State();
        public string InitEquationSource = "";
        public Action<State> InitEquationCompiled;
        public string FrameEquationSource = "";
        public Action<State> FrameEquationCompiled;
        public string PointEquationSource = "";
        public Action<State> PointEquationCompiled;
        public State Variables = new State();
        public State InitVariables = new State();
        public State FrameVariables = new State();
        public State PointVariables = new State();
        public int[] UserKeys = new int[0];
        public State FrameMap = new State();
        public State Inits = new State();

        public float[] PointsDataL;
        public float[] PointsDataR;
        public Vector3[] Positions;
        public Color[] Colors;
        public Vector3[] SmoothedPositions;
        public Color[] SmoothedColors;
    }
}