//using UnityEngine;
//using System;
//using System.Collections;
//using System.Collections.Generic;

//public class NpFrame : MonoBehaviour {

//    #region Public Properties

//    public static List<NpFrame> MasterList
//    {
//        get { return _masterList; }
//    }

//    #endregion


//    #region Private Properties

//    //Shader containing pyramid functions
//    private ComputeShader cSMain;

//    //The master of lists
//    private static List<NpFrame> _masterList = new List<NpFrame>();

//    //Creates the nescessary RenderTextures
//    private List<RenderTexture> _analyzeList = new List<RenderTexture>();
//    private List<List<RenderTexture>> _synthesizeLists = new List<List<RenderTexture>>();
//    private RenderTexture _source;
//    private RenderTexture _done;
//    private RenderTexture _donePow2;
//    private RenderTexture _depth;

//    //Bool to see if textures are initialized
//    private bool _isInit = false;

//    //How many analyze levels there are
//    private int _levels;
//    // To check if levels has changed
//    private int _lastLevels;

//    //The size of the screen last frame;
//    private Vector2 _lastScreenSize;

//    //List of power of 2s
//    private List<int> _pow2S = new List<int>();
//    #endregion


//    #region Constructors

//    [Obsolete("Please use contructor with int for now")]
//    public NpFrame()
//    {
//        if (CheckCompatibility())
//        {
//            BasicInits();
//        }
//    }

//    public NpFrame (int analyzeLevels)
//    {
//        if(CheckCompatibility())
//        {
//            _levels = analyzeLevels;
//            BasicInits();
//        }      
//    }

//    [Obsolete("Please use contructor with int for now")]
//    public NpFrame(Vector2 analyzeRes)
//    {
//        if (CheckCompatibility())
//        {
//            BasicInits();
//        }
//    }

//    #endregion


//    #region Public Functions

//    /// <summary>
//    /// Sets the source texture for the pyramid.</summary>
//    /// <param name="src"> The source RenderTexture use for analyzation pyramid.</param>
//    public void SetSrcTex(ref RenderTexture src)
//    {
//        _source = src;
//        _isInit = false;
//    }
//    /// <summary>
//    /// Generates a Synthesis from the specified power of 2 resolution.</summary>
//    /// <param name="fromResolution"> The analysis resolution to synthesize from.</param>
//    public List<RenderTexture> GetSynthesis(Vector2 fromResolution)
//    {
//        return null;
//    }


//    #endregion

//    private void BasicInits()
//    {
//        _masterList.Add(this);
//        GenPow2();
        
//        cSMain = (ComputeShader)Resources.Load("NPFrame/Shaders/NPFrame");
//    }

//    private void InitAnalyze(RenderTexture src, int levels)
//    {
//        // These loops to handle changing levels size dynamically works a bit iffy ( I.E Doesnt fucking work ATM, memleak)
//        foreach (RenderTexture rT in _analyzeList)
//        {
//            if (rT.IsCreated())
//            {
//                rT.Release();
//                Destroy(rT);
//            }
//        }      

//        _analyzeList.Clear();

//        int size = NextPow2(new Vector2(src.width, src.height));
//        for (int i = 0; i < levels; i++)
//        {
//            _analyzeList.Add(new RenderTexture(_pow2S[_pow2S.IndexOf(size) - i], _pow2S[_pow2S.IndexOf(size) - i], 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear));
//            _analyzeList[i].enableRandomWrite = true;
//            _analyzeList[i].Create();
//        }



//        // TODO: Maybe move?
//        // hack 4 lyfe
//        _donePow2 = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
//        _donePow2.enableRandomWrite = true;
//        _donePow2.Create();

//        _done = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
//        _done.enableRandomWrite = true;
//        _done.Create();

//        _lastScreenSize = new Vector2(Screen.width, Screen.height);
//        _lastLevels = levels;
//        _isInit = true;
//    }

//    // Use this for initialization
//    void Start ()
//    {
//        cSMain = (ComputeShader)Resources.Load("NPFrame/Shaders/NPFrame");
//        Debug.Log(cSMain.name);

//        InitAnalyze(_source, _levels);
//	}
	
//	// Update is called once per frame
//	void Update ()
//    {
//        //Check if the temp texture isn't initialized or the screen size has changed requiring a new size texture, if it has then reinit them // TODO: fix last levels change( dem mem leaks)
//        if (!_isInit || _lastScreenSize != new Vector2(Screen.width, Screen.height))
//            InitAnalyze(_source, _levels);

//        Analyze(_levels);
//    }

//    #region Helper Functions
//    /// <summary>
//    /// Generates a list of POW2s.</summary>
//    private void GenPow2()
//    {
//        for (int i = 1; i < 20 + 1; i++)
//        {
//            _pow2S.Add((int)Mathf.Pow(2, i));
//        }
//    }

//    /// <summary>
//    /// Finds the power of 2 image which will fit the source image e.g ( 951x100 -> 1024x1024 ).</summary>
//    /// <param name="resolution"> The source resolution to find a fit for.</param>
//    private int NextPow2(Vector2 resolution)
//    {
//        int biggest = (int)Mathf.Max(resolution.x, resolution.y);
//        int final = 0;

//        foreach (int i in _pow2S)
//        {
//            if (biggest > i) final = _pow2S[_pow2S.IndexOf(i) + 1];
//        }
//        return final;
//    }

//    private bool CheckCompatibility()
//    {
//        if (!SystemInfo.supportsComputeShaders)
//        {
//            Debug.Log("Error: Compute Shaders not supported on this system. Requires Shader Model 50(DX11), Shader Model: " + SystemInfo.graphicsShaderLevel + " detected");
//            return !SystemInfo.supportsComputeShaders;
//        }
//        else
//            return SystemInfo.supportsComputeShaders;
//    }
//    #endregion
    
//    #region Compute Shader Calls

//    void Analyze(int levels)
//    {
//        for (int i = 0; i < levels - 1; i++)
//        {
//            cSMain.SetTexture(cSMain.FindKernel("Analyze"), "source", _analyzeList[i]);
//            cSMain.SetTexture(cSMain.FindKernel("Analyze"), "dest", _analyzeList[i + 1]);

//            cSMain.Dispatch(cSMain.FindKernel("Analyze"), (int)Mathf.Ceil(_analyzeList[i + 1].width / 32), (int)Mathf.Ceil(_analyzeList[i + 1].height / 32), 1);
//        }
//    }

//    void Synthesize(RenderTexture src, int levels)
//    {
//        for (int i = 0; i < levels - 1; i++)
//        {
//            cSMain.SetTexture(cSMain.FindKernel("Synthesize"), "source", synthesizeList[i]);
//            cSMain.SetTexture(cSMain.FindKernel("Synthesize"), "dest", synthesizeList[i + 1]);

//            cSMain.Dispatch(cSMain.FindKernel("Synthesize"), (int)Mathf.Ceil(synthesizeList[i].width / 32), (int)Mathf.Ceil(synthesizeList[i].height / 32), 1);
//        }
//    }

//    #endregion
    


//}
