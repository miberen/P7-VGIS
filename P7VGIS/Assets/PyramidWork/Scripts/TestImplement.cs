using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TestImplement : MonoBehaviour
{
    public List<RenderTexture> anal; 
    public List<RenderTexture> synth;
    public List<RenderTexture> synthg;

    private NPFrame2 frame;
	// Use this for initialization
    void Awake()
    {
        frame = new NPFrame2("main", 9);
    }

    void Start()
    {
        
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        frame.Analyze(ref source);
        frame.GenerateSynthesis(2, "main");
        frame.GenerateSynthesis(3, "gay");

        anal = frame.AnalyzeList;
        synth = frame.GetSynthesis("main").Pyramid;
        synthg = frame.GetSynthesis("gay").Pyramid;
        //Graphics.Blit(derp.GetTex(derp.Count-1), destination);
        Graphics.Blit(source, destination);
    }
	// Update is called once per frame
	void Update () {
	    
	}
}
