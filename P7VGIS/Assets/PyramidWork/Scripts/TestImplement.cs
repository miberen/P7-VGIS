using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TestImplement : MonoBehaviour
{
    public List<RenderTexture> anal; 
    public List<RenderTexture> synth; 


    private NPFrame2 frame;
	// Use this for initialization
    void Awake()
    {
        frame = new NPFrame2("main");
    }

    void Start()
    {
        
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        
        //Graphics.Blit(derp.GetTex(derp.Count-1), destination);
        Graphics.Blit(source, destination);
    }
	// Update is called once per frame
	void Update () {
	
	}
}
