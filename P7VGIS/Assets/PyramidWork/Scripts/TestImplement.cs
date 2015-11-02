using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class TestImplement : MonoBehaviour
{


    private NpFrame frame;
	// Use this for initialization

    void Start ()
    {
        frame = new NpFrame(5);
	}

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        frame.SetSrcTex(ref source);
        List<RenderTexture> derp = frame.GetSynthesis(new Vector2(32, 32));
    }
	// Update is called once per frame
	void Update () {
	
	}
}
