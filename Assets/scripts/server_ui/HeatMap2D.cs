/******************************************************************************************************************************************************
* MIT License																																		  *
*																																					  *
* Copyright (c) 2020																																  *
* Emmanuel Badier <emmanuel.badier@gmail.com>																										  *
* 																																					  *
* Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),  *
* to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,  *
* and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:		  *
* 																																					  *
* The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.					  *
* 																																					  *
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, *
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 																							  *
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 		  *
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.							  *
******************************************************************************************************************************************************/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;


public class HeatMap2D : MonoBehaviour
{
    // Unity maximum allowed array size for shaders.
    // We can use ComputeBuffer to overpass this limit,
    // but anyway shader performance drops too much beyond this limit.
    public const int MAX_POINTS_COUNT = 1023;

    private Material heatmapMaterial;

    private float decayDeltaSeconds = 0.05f;

    private float decayDeltaStep = 0.0005f;

    private float gainDeltaStep = 0.01f;

    private float _intensity = 0.1f;

    private float _radius = 0.1f;

    private float _canvasWidth = 1.0f;

    private float _canvasHeight = 1.0f;

    private float timer = 0;

    private Dictionary<Atom, System.Tuple<bool, float>> focusedAtoms = new Dictionary<Atom, System.Tuple<bool, float>>();

    public float CanvasWidth
    {
        get { return _canvasWidth; }
        set
        {
            _canvasWidth = value;
            heatmapMaterial.SetFloat("_ImgWidth", _canvasWidth);
        }
    }

    public float CanvasHeight
    {
        get { return _canvasHeight; }
        set
        {
            _canvasHeight = value;
            heatmapMaterial.SetFloat("_ImgHeight", _canvasHeight);
        }
    }

    public float Intensity
    {
        get { return _intensity; }
        set
        {
            _intensity = value;
            heatmapMaterial.SetFloat("_Intensity", _intensity);
        }
    }

    public float Radius
    {
        get { return _intensity; }
        set
        {
            _radius = value;
            heatmapMaterial.SetFloat("_Radius", _radius);
        }
    }

    private void Awake()
    {
        // Initialize shader variables.
        //heatmapMaterial = gameObject.GetComponent<RawImage>().material;
        //heatmapMaterial = new Material(Shader.Find("shaders/Texture"));

        heatmapMaterial = Instantiate(gameObject.GetComponent<RawImage>().material);
        gameObject.GetComponent<RawImage>().material = heatmapMaterial;

        heatmapMaterial.SetFloat("_Intensity", _intensity);
        heatmapMaterial.SetFloat("_Radius", _radius);
        heatmapMaterial.SetFloat("_ImgHeight", _canvasHeight);
        heatmapMaterial.SetFloat("_ImgWidth", _canvasWidth);
        heatmapMaterial.SetInt("_Count", 0);
        heatmapMaterial.SetVectorArray("_Points", new Vector4[MAX_POINTS_COUNT]);

    }

    public void ResetCurrentHeatMap()
    {
        SetPoints(null);
        focusedAtoms.Clear();
    }

    public void SetPoints(List<Vector4> points)
    {
        if ((points == null) || (points.Count == 0))
        {
            heatmapMaterial.SetInt("_Count", 0);
            return;
        }

        if (points.Count > MAX_POINTS_COUNT)
        {
            Debug.LogError("[HeatMap2D] #points (" + points.Count + ") exceeds maximum (" + MAX_POINTS_COUNT + ") !");
            return;
        }

        heatmapMaterial.SetInt("_Count", points.Count);
        heatmapMaterial.SetVectorArray("_Points", points);
    }


    public void UpdateTexture(Texture2D inTex)
    {
        heatmapMaterial.SetTexture("_HeatTex", inTex);
    }

    public void SetAtomFocus(Atom atom, bool focused)
    {
        float weight = focusedAtoms.ContainsKey(atom) ? focusedAtoms[atom].Item2 : 0;
        focusedAtoms[atom] = System.Tuple.Create<bool, float>(focused, weight);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= decayDeltaSeconds)
        {
            List<Vector4> _points = new List<Vector4>();
            foreach (Atom atom in focusedAtoms.Keys.ToList())
            {
                bool isFocused = focusedAtoms[atom].Item1;
                float weight = focusedAtoms[atom].Item2;

                weight += isFocused ? gainDeltaStep : -decayDeltaStep;
                weight = Mathf.Clamp(weight, 0, 1);

                focusedAtoms[atom] = System.Tuple.Create<bool, float>(isFocused, weight);
                _points.Add(new Vector4(atom.structure_coords.x, _canvasHeight - atom.structure_coords.y, weight));
            }

            SetPoints(_points);
            timer = 0;
        }
    }
}
