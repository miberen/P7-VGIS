using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class NPFrame : MonoBehaviour {

    #region Publics
    /// <summary>
    /// Sets the source texture for the pyramid.</summary>
    /// <param name="src"> The source RenderTexture use for analyzation pyramid.</param>
    public void SetSrcTex(ref RenderTexture src)
    {
        source = src;
        isInit = false;
    }

    List<RenderTexture> GetSynthesis(Vector2 fromResolution)
    {

        return null;
    }

    #endregion


    #region Privates

    //Shader containing pyramid functions
    private ComputeShader cSMain;
    
    //The master of lists
    private static List<NPFrame> masterList = new List<NPFrame>();

    //Creates the nescessary RenderTextures
    private List<RenderTexture> analyzeList = new List<RenderTexture>();
    private List<List<RenderTexture>> synthesizeLists = new List<List<RenderTexture>>();
    private RenderTexture source;
    private RenderTexture done;
    private RenderTexture donePow2;
    private RenderTexture depth;

    //Bool to see if textures are initialized
    private bool isInit = false;

    //How many analyze levels there are
    private int levels;
    // To check if levels has changed
    private int lastLevels;

    //The size of the screen last frame;
    private Vector2 lastScreenSize;

    //List of power of 2s
    private List<int> pow2s = new List<int>();
    #endregion

    #region Constructors

    public NPFrame()
    {
        if (CheckCompatibility())
        {
            basicInits();
        }
    }

    public NPFrame (int analyzeLevels)
    {
        if(CheckCompatibility())
        {
            basicInits();
        }      
    }

    public NPFrame(Vector2 analyzeRes)
    {
        if (CheckCompatibility())
        {
            basicInits();
        }
    }
    #endregion

    private void basicInits()
    {
        masterList.Add(this);
        GenPow2();
    }

    private void InitAnalyze(RenderTexture source, int levels)
    {
        // These loops to handle changing levels size dynamically works a bit iffy ( I.E Doesnt fucking work ATM, memleak)
        foreach (RenderTexture rT in analyzeList)
        {
            if (rT.IsCreated())
            {
                rT.Release();
                Destroy(rT);
            }
        }      

        analyzeList.Clear();

        int size = NextPow2(new Vector2(source.width, source.height));
        for (int i = 0; i < levels; i++)
        {
            analyzeList.Add(new RenderTexture(pow2s[pow2s.IndexOf(size) - i], pow2s[pow2s.IndexOf(size) - i], 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear));
            analyzeList[i].enableRandomWrite = true;
            analyzeList[i].Create();
        }

        // TODO: Maybe move?
        // hack 4 lyfe
        donePow2 = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        donePow2.enableRandomWrite = true;
        donePow2.Create();

        done = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        done.enableRandomWrite = true;
        done.Create();

        lastScreenSize = new Vector2(Screen.width, Screen.height);
        lastLevels = levels;
        isInit = true;
    }


    private void Awake()
    {
        cSMain = (ComputeShader)Resources.Load("NPFrame");
    }

    // Use this for initialization
    private void Start ()
    {
        InitAnalyze(source, levels);
	}
	
	// Update is called once per frame
	private void Update ()
    {
        //Check if the temp texture isn't initialized or the screen size has changed requiring a new size texture, if it has then reinit them // TODO: fix last levels change( dem mem leaks)
        if (!isInit || lastScreenSize != new Vector2(Screen.width, Screen.height))
            InitAnalyze(source, levels);
    }

    #region Helper Functions
    /// <summary>
    /// Generates a list of POW2s.</summary>
    private void GenPow2()
    {
        for (int i = 1; i < 20 + 1; i++)
        {
            pow2s.Add((int)Mathf.Pow(2, i));
        }
    }

    /// <summary>
    /// Finds the power of 2 image which will fit the source image e.g ( 951x100 -> 1024x1024 ).</summary>
    /// <param name="source"> The source RenderTexture to find a fit for.</param>
    private int NextPow2(Vector2 resolution)
    {
        int biggest = (int)Mathf.Max(resolution.x, resolution.y);
        int final = 0;

        foreach (int i in pow2s)
        {
            if (biggest > i) final = pow2s[pow2s.IndexOf(i) + 1];
        }
        return final;
    }

    private bool CheckCompatibility()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.Log("Error: Compute Shaders not supported on this system. Requires Shader Model 50(DX11), Shader Model: " + SystemInfo.graphicsShaderLevel + " detected");
            return !SystemInfo.supportsComputeShaders;
        }
        else
            return SystemInfo.supportsComputeShaders;
    }
    #endregion

    #region Compute Shader Calls

    void Analyze(int levels)
    {
        for (int i = 0; i < levels - 1; i++)
        {
            cSMain.SetTexture(cSMain.FindKernel("Analyze"), "source", analyzeList[i]);
            cSMain.SetTexture(cSMain.FindKernel("Analyze"), "dest", analyzeList[i + 1]);

            cSMain.Dispatch(cSMain.FindKernel("Analyze"), (int)Mathf.Ceil(analyzeList[i + 1].width / 32), (int)Mathf.Ceil(analyzeList[i + 1].height / 32), 1);
        }
        synthesizeList[0] = analyzeList[levels - 1];
    }

    void Synthesize(int levels)
    {
        for (int i = 0; i < levels - 1; i++)
        {
            cSMain.SetTexture(cSMain.FindKernel("Synthesize"), "source", synthesizeList[i]);
            cSMain.SetTexture(cSMain.FindKernel("Synthesize"), "dest", synthesizeList[i + 1]);

            cSMain.Dispatch(cSMain.FindKernel("Synthesize"), (int)Mathf.Ceil(synthesizeList[i].width / 32), (int)Mathf.Ceil(synthesizeList[i].height / 32), 1);
        }
    }

    #endregion

}
