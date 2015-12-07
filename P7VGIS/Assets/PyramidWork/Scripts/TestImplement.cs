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
    private NPFrame2 frame2;
	// Use this for initialization
    void Awake()
    {
        frame = new NPFrame2("main", 3);
        frame2 = new NPFrame2("main2", new Vector2(512,512));
    }

    void Start()
    {
        
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {   
        frame.GenerateSynthesis("derp", new RenderTexture(127, 145, 0, RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear));
        frame.Analyze(source);
        frame2.Analyze(source);
        Graphics.Blit(source, destination);
    }
	// Update is called once per frame
	void Update () {
	    
	}
}
