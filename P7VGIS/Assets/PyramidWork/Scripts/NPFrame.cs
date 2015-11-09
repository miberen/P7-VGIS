using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class NpFrame {

    /// <summary>
    /// Synthesis container of a list of RenderTextures and its source level.</summary>
    public class Synthesis
    {
        private readonly int _analyzedFrom;
        private readonly List<RenderTexture> _synths;

        /// <summary>
        /// Instantiates a Synthesis container of a list of RenderTextures and its source level.</summary>
        /// <param name="list"> List of synthesized RenderTextures.</param>
        /// <param name="analyzedFrom"> The zero-based index of the source analysis level.</param>
        internal Synthesis(List<RenderTexture> list, int analyzedFrom)
        {
            _analyzedFrom = analyzedFrom;
            _synths = list;
        }
        /// <summary>
        /// Returns the zero-based index of the Analysis level this Synthesis is generated from.</summary>
        public int GetSourceLevel
        {
            get { return _analyzedFrom; }
        }

        /// <summary>
        /// Returns the entire list of RenderTextures contained in the Synthesi pyramid.</summary>
        public List<RenderTexture> GetTexList
        {
            get { return _synths; }
        }

        /// <summary>
        /// Instantiates a Synthesis container of a list of RenderTextures and its source level.</summary>
        /// <param name="index"> The zero-based index of the texture to return</param>
        public RenderTexture GetTex(int index)
        {
            return _synths[index];
        }

        /// <summary>
        /// Returns the non-zero based count of textures in the Synthesis pyramid.</summary>
        public int Count
        {
            get { return _synths.Count; }
        }
    }

    #region Public Properties

    public static List<NpFrame> MasterList
    {
        get { return _masterList; }
    }

    public List<RenderTexture> AnalyzeList
    {
        get { return _analyzeList; }
    }

    #endregion


    #region Private Properties


    //Shader containing pyramid functions
    private ComputeShader _cSMain;

    //The master of lists
    private static List<NpFrame> _masterList = new List<NpFrame>();

    //Creates the nescessary RenderTextures
    private List<RenderTexture> _analyzeList = new List<RenderTexture>();
    private List<Synthesis> _synthesizeList = new List<Synthesis>();
    private RenderTexture _source;
    private RenderTexture _done;
    private RenderTexture _donePow2;

    //Bool to see if textures are initialized
    private bool _isInit = false;

    //How many analyze levels there are
    private int _levels;
    // To check if levels has changed
    private int _lastLevels;

    //The size of the screen last frame;
    private Vector2 _lastScreenSize;

    //List of power of 2s
    private List<int> _pow2S = new List<int>();
    #endregion


    #region Constructors

    [Obsolete("Please use contructor with int for now")]
    public NpFrame()
    {
        if (CheckCompatibility())
        {
            BasicInits();
        }
    }

    public NpFrame (int analyzeLevels)
    {
        if(CheckCompatibility())
        {
            _levels = analyzeLevels;
            BasicInits();
        }      
    }

    [Obsolete("Please use contructor with int for now")]
    public NpFrame(Vector2 analyzeRes)
    {
        if (CheckCompatibility())
        {
            BasicInits();
        }
    }

    #endregion


    #region Public Methods

    /// <summary>
    /// Sets the source texture for the pyramid.</summary>
    /// <param name="src"> The source RenderTexture use for analyzation pyramid.</param>
    public void SetSrcTex(ref RenderTexture src)
    {
        _source = src;
        _isInit = false;
        InitAnalyze(_source, _levels);
    }
    /// <summary>
    /// Generates a Synthesis from the specified power of 2 resolution.</summary>
    /// <param name="fromResolution"> The analysis resolution to synthesize from.</param>
    public Synthesis GetSynthesis(Vector2 fromResolution)
    {      
        int level = LvlFromRes(fromResolution);
        List<RenderTexture> tempSynth = new List<RenderTexture>();
        for (int i = 0; i < level; i++)
        {
            tempSynth.Add(new RenderTexture(_analyzeList[level].width, _analyzeList[level].height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear));
            tempSynth[i].enableRandomWrite = true;
            tempSynth[i].Create();
        }
        
        return Synthesize(level, tempSynth, level);
    }


    #endregion

    private void BasicInits()
    {
        _cSMain = (ComputeShader)Resources.Load("NPFrame/Shaders/NPFrame");
        _masterList.Add(this);

        GenPow2();      
    }

    private void InitAnalyze(RenderTexture src, int levels)
    {
        // These loops to handle changing levels size dynamically works a bit iffy ( I.E Doesnt fucking work ATM, memleak)
        foreach (RenderTexture rT in _analyzeList)
        {
            if (rT.IsCreated())
            {
                rT.Release();
            }
        }      

        _analyzeList.Clear();

        int size = NextPow2(new Vector2(src.width, src.height));
        for (int i = 0; i < levels; i++)
        {
            _analyzeList.Add(new RenderTexture(_pow2S[_pow2S.IndexOf(size) - i], _pow2S[_pow2S.IndexOf(size) - i], 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear));
            _analyzeList[i].enableRandomWrite = true;
            _analyzeList[i].Create();
        }



        // TODO: Maybe move?
        // hack 4 lyfe
        _donePow2 = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        _donePow2.enableRandomWrite = true;
        _donePow2.Create();

        _done = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        _done.enableRandomWrite = true;
        _done.Create();

        _lastScreenSize = new Vector2(Screen.width, Screen.height);
        _lastLevels = levels;
        _isInit = true;
    }

    // Use this for initialization
    void Start ()
    {


	}
	
	// Update is called once per frame
	public void Update ()
    {
        //Check if the temp texture isn't initialized or the screen size has changed requiring a new size texture, if it has then reinit them // TODO: fix last levels change( dem mem leaks)
        if (!_isInit || _lastScreenSize != new Vector2(Screen.width, Screen.height))
            InitAnalyze(_source, _levels);

        Analyze(_levels);
    }

    #region Helper Functions
    /// <summary>
    /// Generates a list of POW2s.</summary>
    private void GenPow2()
    {
        for (int i = 1; i < 20 + 1; i++)
        {
            _pow2S.Add((int)Mathf.Pow(2, i));
        }
    }

    /// <summary>
    /// Finds the power of 2 image which will fit the source image e.g ( 951x100 -> 1024x1024 ).</summary>
    /// <param name="resolution"> The source resolution to find a fit for.</param>
    private int NextPow2(Vector2 resolution)
    {
        int biggest = (int)Mathf.Max(resolution.x, resolution.y);
        int final = 0;

        foreach (int i in _pow2S)
        {
            if (biggest > i) final = _pow2S[_pow2S.IndexOf(i) + 1];
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
        
        return SystemInfo.supportsComputeShaders;
    }
    /// <summary>
    /// Calculates the level that corresponds to a certain resolution.
    /// </summary>
    /// <param name="res"> Resolution to calculate level from</param>
    /// <returns> Zero-based index of Analyzation Pyramid of that resolution. </returns>
    private int LvlFromRes(Vector2 res)
    {
        int ret = -1;
        if (_analyzeList.Count != 0)
        {
            foreach (RenderTexture rT in _analyzeList)
            {
                if (res.x == rT.width && res.y == rT.height) ret = _analyzeList.IndexOf(rT);
            }
            if (ret == -1)
            {
                Debug.Log("Analyze list does not contain specified resolution." );
                return ret;
            }
        }
        else
        {
            Debug.Log("Analyze list is empty, cannot calculate level from given resolution.");
            return ret;
        }
        return ret;
    }

    #endregion

    #region Compute Shader Calls

    void Analyze(int levels)
    {
        for (int i = 0; i < levels - 1; i++)
        {
            _cSMain.SetTexture(_cSMain.FindKernel("Analyze"), "source", _analyzeList[i]);
            _cSMain.SetTexture(_cSMain.FindKernel("Analyze"), "dest", _analyzeList[i + 1]);

            _cSMain.Dispatch(_cSMain.FindKernel("Analyze"), (int)Mathf.Ceil(_analyzeList[i + 1].width / 32), (int)Mathf.Ceil(_analyzeList[i + 1].height / 32), 1);
        }
    }

    /// <summary>
    /// Generates a Synthesis pyramid based on a source texture,
    /// </summary>
    /// <param name="src"></param>
    /// <param name="dest"></param>
    /// <param name="levels"></param>
    /// <returns></returns>
    Synthesis Synthesize(int fromLevel, List<RenderTexture> dest, int levels)
    {
        dest.Insert(0, _analyzeList[levels]); 
        for (int i = 0; i < levels; i++)
        {
            _cSMain.SetTexture(_cSMain.FindKernel("Synthesize"), "source", dest[i]);
            _cSMain.SetTexture(_cSMain.FindKernel("Synthesize"), "dest", dest[i + 1]);
            _cSMain.Dispatch(_cSMain.FindKernel("Synthesize"), (int)Mathf.Ceil(dest[i].width / 32), (int)Mathf.Ceil(dest[i].height / 32), 1);
        }
        dest.RemoveAt(0);
        return new Synthesis(dest, levels);
    }

    #endregion
    


}
