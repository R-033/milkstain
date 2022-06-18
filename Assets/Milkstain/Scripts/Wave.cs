using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Milkstain
{
    public class Wave
    {
        public Wave(Preset preset)
        {
            BaseVariables = new State(preset);
            Variables = new State(preset);
            InitVariables = new State(preset);
            FrameVariables = new State(preset);
            PointVariables = new State(preset);
            FrameMap = new State(preset);
            Inits = new State(preset);
        }

        public State BaseVariables;
        public string InitEquationSource = "";
        public Action<State> InitEquationCompiled;
        public string FrameEquationSource = "";
        public Action<State> FrameEquationCompiled;
        public string PointEquationSource = "";
        public Action<State> PointEquationCompiled;
        public State Variables;
        public State InitVariables;
        public State FrameVariables;
        public State PointVariables;
        public int[] UserKeys = new int[0];
        public State FrameMap;
        public State Inits;

        public float[] PointsDataL;
        public float[] PointsDataR;
        public Vector3[] Positions;
        public Color[] Colors;
        public Vector3[] SmoothedPositions;
        public Color[] SmoothedColors;
    }
}