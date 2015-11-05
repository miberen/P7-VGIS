using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TestImplement : MonoBehaviour
{
    public List<RenderTexture> anal; 
    public List<RenderTexture> synth; 


    private NpFrame frame;
	// Use this for initialization
    void Awake()
    {
        frame = new NpFrame(5);
    }

    void Start()
    {
        
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        frame.SetSrcTex(ref source);
        frame.Update();      
        NpFrame.Synthesis derp = frame.GetSynthesis(new Vector2(512, 512));
        synth = derp.GetTexList;
        anal = frame.AnalyzeList;
        //Graphics.Blit(derp.GetTex(derp.Count-1), destination);
        Graphics.Blit(source, destination);
    }
	// Update is called once per frame
	void Update () {
	
	}
}
