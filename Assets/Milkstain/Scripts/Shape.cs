using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Milkstain
{
    public class Shape
    {
        public Shape(Preset preset)
        {
            BaseVariables = new State(preset);
            Variables = new State(preset);
            InitVariables = new State(preset);
            FrameVariables = new State(preset);
            FrameMap = new State(preset);
            Inits = new State(preset);
        }

        public State BaseVariables;
        public string InitEquationSource = "";
        public Action<State> InitEquationCompiled;
        public string FrameEquationSource = "";
        public Action<State> FrameEquationCompiled;
        public State Variables;
        public State InitVariables;
        public State FrameVariables;
        public int[] UserKeys = new int[0];
        public State FrameMap;
        public State Inits;

        public Vector3[] Positions;
        public Color[] Colors;
        public Vector2[] UVs;
        public Vector3[] BorderPositions;

        public Mesh[] ShapeMeshes;
        public Material[] ShapeMaterials;
        public Material[] BorderMaterials;

        public string TextureName;
        public Texture Texture;
    }
}