﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PyramidEffects : MonoBehaviour
{
    #region Publics
    public GameObject plane1;
    public ComputeShader computeTest;
    #endregion
    #region Privates

    //Bool to see if nescessary textures and shader variables are initialized
    private int index = 0;
    private bool isInit = false;

    //Creates the nescessary RenderTextures
    public List<RenderTexture> analyzeList = new List<RenderTexture>();
    private List<RenderTexture> synthesizeList = new List<RenderTexture>();

    //The size of the screen last frame;
    private Vector2 lastScreenSize;

    //List of power of 2s
    private List<int> pow2s = new List<int>();
    #endregion

    // Use this for initialization
    void Awake()
    {
        //Generates the list of pow2s
        GenPow2();
    }

    //Start smart
    void Start()
    {
        //Get the initial screen size
        lastScreenSize = new Vector2(Screen.width, Screen.height);
    }

    void Update()
    {
        if (Input.GetKeyDown("k"))
        {           
            plane1.GetComponent<Renderer>().material.SetTexture("_MainTex", analyzeList[index]);
            foreach (RenderTexture rT in analyzeList)
            {
                Debug.Log(rT.height.ToString() + "x" + rT.width.ToString());
                Debug.Log(rT.IsCreated().ToString());
                Debug.Log(rT.enableRandomWrite.ToString());
            }
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            index += 1;
            if (index >= analyzeList.Count - 1)
                index = 0;

            plane1.GetComponent<Renderer>().material.SetTexture("_MainTex", analyzeList[index]);
        }
    }

    /// <summary>
    /// Overrides the OnRenderImage function.</summary>
    /// <param name="source"> The RenderTexture about to be displayed on the screen.</param>
    /// <param name="destination"> The final RenderTexture actually displayed.</param>
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //Check if the temp texture isn't initialized or the screen size has changed requiring a new size texture, if it has then reinit them
        if (!isInit || lastScreenSize != new Vector2(Screen.width, Screen.height)) Init(source, 6);

        Refresh(source);

        Analyze(6);  

        //Blit that shit
        Graphics.Blit(source, destination);
    }

    /// <summary>
    /// Creates a texture that is the next power of 2, creates it on the GPU then sets the compute shader uniforms.</summary>
    /// <param name="source"> Reference to the source texture.</param>
    void Init(RenderTexture source, int levels)
    {

        analyzeList.Clear();
        synthesizeList.Clear();

        int size = NextPow2(source);
        for (int i = 0; i < levels+1; i++)
        {
            analyzeList.Add(new RenderTexture(pow2s[pow2s.IndexOf(size)-i], pow2s[pow2s.IndexOf(size) - i], 0, RenderTextureFormat.ARGB32));
            analyzeList[i].enableRandomWrite = true;
            analyzeList[i].filterMode = FilterMode.Point;
            analyzeList[i].Create();
        }

        for(int i = 0; i > levels; i++)
        {
            synthesizeList.Add(new RenderTexture(pow2s[pow2s.IndexOf(size) + i], pow2s[pow2s.IndexOf(size) + i], 0, RenderTextureFormat.ARGB32));
            synthesizeList[i].enableRandomWrite = true;
            synthesizeList[i].Create();
        }

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        isInit = true;
    }

    void Refresh(RenderTexture source)
    {
        //Check if there is already a texture, if there is then release the old one before making a new one
        foreach (RenderTexture rT in analyzeList)
        {
            if (rT.IsCreated())
            {
                rT.Release();
                rT.Create();
            }
        }

        foreach (RenderTexture rT in synthesizeList)
        {
            if (rT.IsCreated())
                rT.Release();
        }
        
        MakePow2(source);

    }
    /// <summary>
    /// Generates a list of power of 2s up to 13 (8192).</summary>
    void GenPow2()
    {
        for (int i = 1; i < 14; i++)
        {
            pow2s.Add((int)Mathf.Pow(2, i));
        }
    }

    /// <summary>
    /// Finds the power of 2 image which will fit the source image ( 951x100 -> 1024x1024 ).</summary>
    /// <param name="source"> The source RenderTexture to find a fit for.</param>
    int NextPow2(RenderTexture source)
    {
        int biggest = Mathf.Max(source.width, source.height);
        int final = 0;

        foreach (int i in pow2s)
        {
            if (biggest > i) final = pow2s[pow2s.IndexOf(i) + 1];
        }
        return final;
    }

    void MakePow2(RenderTexture source)
    {
        //Set the shader uniforms
        computeTest.SetTexture(computeTest.FindKernel("MakePow2"), "source", source);
        computeTest.SetTexture(computeTest.FindKernel("MakePow2"), "dest", analyzeList[0]);

        //Dispatch the compute shader
        computeTest.Dispatch(computeTest.FindKernel("MakePow2"), analyzeList[0].width / 8, analyzeList[0].height / 8, 1);
    }

    void MakeNonPow2(RenderTexture source)
    {
        //Set the shader uniforms
        computeTest.SetTexture(computeTest.FindKernel("MakeNPow2"), "source", source);
        computeTest.SetTexture(computeTest.FindKernel("MakeNPow2"), "dest", analyzeList[0]);

        //Dispatch the compute shader
        computeTest.Dispatch(computeTest.FindKernel("MakeNPow2"), analyzeList[0].width / 8, analyzeList[0].height / 8, 1);
    }

    void Analyze(int levels)
    {
        for(int i = 0; i <= levels-1; i++)
        {
            computeTest.SetTexture(computeTest.FindKernel("Analyze"), "source", analyzeList[i]);
            computeTest.SetTexture(computeTest.FindKernel("Analyze"), "dest", analyzeList[i+1]);

            computeTest.Dispatch(computeTest.FindKernel("Analyze"), analyzeList[i+1].width / 8, analyzeList[i+1].height / 8, 1);
        }
    }

    void Synthesize(RenderTexture source, int levels)
    {
        
    }
}