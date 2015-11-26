using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;

public class NPFrame2
{

    public class Synthesis
    {
        private List<RenderTexture> _synths;
        private int _analyzedFrom;

        /// <summary>
        /// Generates a synthesis object. This holds a list of RenderTextures that corresponds to one synthesis as well as where it is analyzed from.
        /// </summary>
        /// <param name="analyzedFrom"> The analysation level this synthesis is generated from.</param>
        internal Synthesis(int analyzedFrom)
        {
            _analyzedFrom = analyzedFrom;
            _synths = new List<RenderTexture>();
        }

        public int SourceLevel
        {
            get { return _analyzedFrom; }
        }

        // Creates an indexer for the list on the class so you can access the list directly.
        public RenderTexture this[int index]
        {
            get { return _synths[index]; }
        }

        public int Count
        {
            get { return _synths.Count; }
        }

        public List<RenderTexture> Pyramid
        {
            get { return _synths; }
        }
    };

    #region Variables

    // Static to allow access in any script, lets user reuse the same pyramids again.
    private static Dictionary<string, NPFrame2> _masterDic = new Dictionary<string, NPFrame2>();

    private Dictionary<string, Synthesis> _synthDic = new Dictionary<string, Synthesis>();
    private List<RenderTexture> _analyzeList = new List<RenderTexture>();
    private List<int> _pow2S = new List<int>();

    private int _levels;
    private bool _isInit;
    private Vector2 _lastScreenSize;
    private ComputeShader _cSMain;
    private AnalysisMode _analysisMode = AnalysisMode.Box2x2;
    private SynthesisMode _synthesisMode = SynthesisMode.BiQuadBSpline;
    private FilterMode _filterMode = FilterMode.Bilinear;
    private RenderTextureFormat _textureFormat = RenderTextureFormat.DefaultHDR;

    private RenderTexture _done;
    private RenderTexture _donePow2;

    #endregion

    #region Constructors

    //Different constructors depending on how the user wants to specify source and target levels.

    /// <summary>
    /// Creates an instance of the framework which analyses all the way down to 1x1 pixels.
    /// </summary>
    /// <param name="name">The name used to access this framework in the MasterList.</param>
    [Obsolete("Use other constructors for now")]
    public NPFrame2(string name)
    {
        BasicInit(name);
    }

    /// <summary>
    /// Creates an instance of the framework which analyses down the specified amount of times.
    /// </summary>
    /// <param name="name">The name used to access this framework in the MasterList.</param>
    /// <param name="analyzeLevels">How many analysation levels to generate.</param>
    public NPFrame2(string name, int analyzeLevels)
    {
        _levels = analyzeLevels;
        BasicInit(name);
    }

    /// <summary>
    /// Creates an instance of the framework which analyses down to the specified resolution.
    /// </summary>
    /// <param name="name">The name used to access this framework in the MasterList.</param>
    /// <param name="analyzeRes">The target resolution of the analysis.</param>
    public NPFrame2(string name, Vector2 analyzeRes)
    {
        _levels = LevelFromRes(analyzeRes);
        BasicInit(name);
    }

    #endregion

    #region Properties

    public List<RenderTexture> AnalyzeList
    {
        get { return _analyzeList; }
    }

    public Synthesis GetSynthesis(string key)
    {
        return _synthDic[key];
    }

    public ComputeShader GetShader
    {
        get { return _cSMain; }
    }

    public RenderTexture GetDoneNPOT
    {
        get { return _done; }
    }

    public int GetNativePOTRes
    {
        get { return NextPow2(new Vector2(Screen.width, Screen.height)); }
    }

    public AnalysisMode GetAnalysisMode
    {
        get { return _analysisMode; }
    }
    public AnalysisMode SetAnalysisMode
    {
        set { _analysisMode = value; }
    }

    public SynthesisMode GetSynthesisMode
    {
        get { return _synthesisMode; }
    }
    public SynthesisMode SetSynthesisMode
    {
        set { _synthesisMode = value; }
    }

    public FilterMode GetFilterMode
    {
        get { return _filterMode; }
    }

    public FilterMode SetFilterMode
    {
        set { _filterMode = value; }
    }

    public static Dictionary<string, NPFrame2> MasterDic
    {
        get { return _masterDic; }
    }

    public RenderTextureFormat GetTextureFormat
    {
        get { return _textureFormat; }
    }
    public RenderTextureFormat SetTextureFormat
    {
        set { _textureFormat = value; }
    }
    #endregion

    // Enums to allow us to change certain parameters. Descriptions let them return strings via a custom function later in the script, this allows us to directly call the compute shader
    // with the enums.

    public enum AnalysisMode
    {
        [Description("Analyze2x2Box")]
        Box2x2,
        [Description("Analyze4x4Box")]
        Box4x4,
        [Description("AnalyzeBQBS")]
        BiQuadBSpline
    };

    public enum SynthesisMode
    {
        //[Description("Synthesize2x2Box")]
        //Box2x2,
        [Description("SynthesizeBQBS")]
        BiQuadBSpline
    };

    /// <summary>
    /// Creates a NPOT texture from a POT texture, must be a 0
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public RenderTexture MakeNPOT(RenderTexture source)
    {
        if (_done != null)
        {
            MakeNonPow2Call(ref source, ref _done);
            return _done;
        }
        Debug.Log("You dun goofed: _done texture not initialized");
        return null;
    }

    /// <summary>
    /// Handles creating a power of 2 image and starting analyzastion.
    /// </summary>
    /// <param name="source">The texture to be analysed.</param>
    public void Analyze(ref RenderTexture source)
    {
        // Checks if the analyze list is created or if the screen size has changed. If it has then reinit the list
        if (!_isInit || _lastScreenSize != new Vector2(Screen.width, Screen.height)) InitAnalyze(ref source);

        // Create the power of 2 texture.
        MakePow2Call(ref source, ref _analyzeList);

        // Set the textures and dispatch the shader to create analysis pyramid.
        AnalyzeCall();
    }

    /// <summary>
    /// Generates a synthesis from a specified non-zero based source level of the analyzation pyramid.
    /// </summary>
    /// <param name="sourceLevel"> The non-zero based analyzation level to synthesise from. ( This is also the amount of textures generated.</param>
    /// <param name="name"> Name of the synthesis, used to access it later.</param>
    public void GenerateSynthesis(int sourceLevel, string name, SynthesisMode synthMode = SynthesisMode.BiQuadBSpline)
    {
        // Check if a synthesis with the supplied name exists, if it does not make it and fill it out.
        if (!_synthDic.ContainsKey(name))
        {
            // Add the new synthesis in the dictionary.
            _synthDic.Add(name, new Synthesis(sourceLevel));

            // Fill the list in synthesis.
            for (int i = 0; i < sourceLevel; i++)
            {
                _synthDic[name].Pyramid.Add(new RenderTexture(_analyzeList[sourceLevel - 1 - i].width, _analyzeList[sourceLevel - 1 - i].height, 0, _textureFormat, RenderTextureReadWrite.Linear));
                _synthDic[name][i].enableRandomWrite = true;
                _synthDic[name][i].filterMode = _filterMode;
                _synthDic[name][i].Create();
            }

            SynthesizeCall(_synthDic[name], sourceLevel, synthMode);
        }
        // If it does exist but the size is different, delete the old one and make a new one with the right size.
        else if (_synthDic.ContainsKey(name) && sourceLevel != _synthDic[name].SourceLevel)
        {
            foreach (RenderTexture rT in _synthDic[name].Pyramid)
            {
                if (rT.IsCreated())
                {
                    rT.Release();
                }
                _synthDic[name].Pyramid.Clear();
            }

            for (int i = 0; i < sourceLevel; i++)
            {
                _synthDic[name].Pyramid.Add(new RenderTexture(_analyzeList[sourceLevel - 1 - i].width, _analyzeList[sourceLevel - 1 - i].height, 0, _textureFormat, RenderTextureReadWrite.Linear));
                _synthDic[name][i].enableRandomWrite = true;
                _synthDic[name][i].filterMode = _filterMode;
                _synthDic[name][i].Create();
            }

            SynthesizeCall(_synthDic[name], sourceLevel, synthMode);
        }
        // if everything is good, just do the call to generate the synthesis in the list.
        else
        {
            SynthesizeCall(_synthDic[name], sourceLevel, synthMode);
        }

    }

    /// <summary>
    /// Inits basic stuff like the adding each generated framework to the master list, setting the shader and making the list of POTs.
    /// </summary>
    /// <param name="name">The name of this instance of the framework.</param>
    private void BasicInit(string name)
    {
        if (CheckCompatibility())
        {
            _masterDic.Add(name, this);
            _cSMain = (ComputeShader)Resources.Load("NPFrame/Shaders/NPFrame");
            GenPow2S();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    private void InitAnalyze(ref RenderTexture source)
    {
        foreach (RenderTexture rT in _analyzeList)
        {
            if (rT.IsCreated())
            {
                rT.Release();
            }
        }

        _analyzeList.Clear();
        int size = NextPow2(new Vector2(source.width, source.height));

        for (int i = 0; i < _levels; i++)
        {
            _analyzeList.Add(new RenderTexture(_pow2S[_pow2S.IndexOf(size) - i], _pow2S[_pow2S.IndexOf(size) - i], 0, _textureFormat, RenderTextureReadWrite.Linear));
            _analyzeList[i].enableRandomWrite = true;
            _analyzeList[i].filterMode = _filterMode;
            _analyzeList[i].Create();
        }

        _donePow2 = new RenderTexture(size, size, 0, _textureFormat, RenderTextureReadWrite.Linear);
        _donePow2.enableRandomWrite = true;
        _donePow2.filterMode = _filterMode;
        _donePow2.Create();

        _done = new RenderTexture(Screen.width, Screen.height, 0, _textureFormat, RenderTextureReadWrite.Linear);
        _done.enableRandomWrite = true;
        _done.filterMode = _filterMode;
        _done.Create();

        _lastScreenSize = new Vector2(Screen.width, Screen.height);
        _isInit = true;

    }

    private void AnalyzeCall()
    {
        for (int i = 0; i < _levels - 1; i++)
        {
            _cSMain.SetTexture(_cSMain.FindKernel(GetEnumDescription(_analysisMode)), "source", _analyzeList[i]);
            _cSMain.SetTexture(_cSMain.FindKernel(GetEnumDescription(_analysisMode)), "dest", _analyzeList[i + 1]);

            if (_analyzeList[i].width > 32 || _analyzeList[i].width > 32)
                _cSMain.Dispatch(_cSMain.FindKernel(GetEnumDescription(_analysisMode)), (int)Mathf.Ceil(_analyzeList[i + 1].width / 32), (int)Mathf.Ceil(_analyzeList[i + 1].height / 32), 1);
            else
            {
                _cSMain.Dispatch(_cSMain.FindKernel(GetEnumDescription(_analysisMode)), 1, 1, 1);
            }
        }
    }

    private void SynthesizeCall(Synthesis synth, int levels = 0, SynthesisMode synthMode = SynthesisMode.BiQuadBSpline)
    {
        if (levels == 0) levels = _levels;

        if (levels == 1)
        {
            _cSMain.SetTexture(_cSMain.FindKernel(GetEnumDescription(synthMode)), "source", _analyzeList[synth.SourceLevel]);
            _cSMain.SetTexture(_cSMain.FindKernel(GetEnumDescription(synthMode)), "dest", synth.Pyramid[0]);

            if (synth.Pyramid[0].width > 32 || synth.Pyramid[0].width > 32)
                _cSMain.Dispatch(_cSMain.FindKernel(GetEnumDescription(synthMode)), (int)Mathf.Ceil(synth.Pyramid[0].width / 32), (int)Mathf.Ceil(synth.Pyramid[0].height / 32), 1);
            else
                _cSMain.Dispatch(_cSMain.FindKernel(GetEnumDescription(synthMode)), 1, 1, 1);
        }

        for (int i = 0; i < levels - 1; i++)
        {
            if (i == 0)
            {
                _cSMain.SetTexture(_cSMain.FindKernel(GetEnumDescription(synthMode)), "source", _analyzeList[synth.SourceLevel]);
                _cSMain.SetTexture(_cSMain.FindKernel(GetEnumDescription(synthMode)), "dest", synth.Pyramid[i]);

                _cSMain.Dispatch(_cSMain.FindKernel(GetEnumDescription(synthMode)), (int)Mathf.Ceil(_analyzeList[synth.SourceLevel].width / 32), (int)Mathf.Ceil(_analyzeList[synth.SourceLevel].height / 32), 1);
            }
            _cSMain.SetTexture(_cSMain.FindKernel(GetEnumDescription(synthMode)), "source", synth.Pyramid[i]);
            _cSMain.SetTexture(_cSMain.FindKernel(GetEnumDescription(synthMode)), "dest", synth.Pyramid[i + 1]);

            if (synth.Pyramid[i].width > 32 || synth.Pyramid[i].width > 32)
                _cSMain.Dispatch(_cSMain.FindKernel(GetEnumDescription(synthMode)), (int)Mathf.Ceil(synth.Pyramid[i].width / 32), (int)Mathf.Ceil(synth.Pyramid[i].height / 32), 1);
            else
                _cSMain.Dispatch(_cSMain.FindKernel(GetEnumDescription(synthMode)), 1, 1, 1);
        }
    }

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

    /// <summary>
    /// Calculates the non-zero based index / level of a specified resolution in the analysis pyramid.
    /// </summary>
    /// <param name="res">The resolution to find the corresponding level of.</param>
    /// <returns></returns>
    private int LevelFromRes(Vector2 res)
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
                Debug.Log("Analyze list does not contain specified resolution.");
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

    public static string GetEnumDescription(Enum value)
    {
        FieldInfo fi = value.GetType().GetField(value.ToString());

        DescriptionAttribute[] attributes =
            (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

        if (attributes != null && attributes.Length > 0)
            return attributes[0].Description;
        else
            return value.ToString();
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

    private void GenPow2S()
    {
        for (int i = 1; i < 20 + 1; i++)
        {
            _pow2S.Add((int)Mathf.Pow(2, i));
        }
    }

    private void MakePow2Call(ref RenderTexture source, ref List<RenderTexture> destination)
    {
        //Set the shader uniforms
        _cSMain.SetTexture(_cSMain.FindKernel("MakePow2"), "source", source);
        _cSMain.SetTexture(_cSMain.FindKernel("MakePow2"), "dest", destination[0]);

        //Dispatch the compute shader 
        _cSMain.Dispatch(_cSMain.FindKernel("MakePow2"), (int)Mathf.Ceil(destination[0].width / 32), (int)Mathf.Ceil(destination[0].height / 32), 1);
    }

    private void MakeNonPow2Call(ref RenderTexture source, ref RenderTexture destination)
    {
        //Set the shader uniforms
        _cSMain.SetTexture(_cSMain.FindKernel("MakeNPow2"), "source", source);
        _cSMain.SetTexture(_cSMain.FindKernel("MakeNPow2"), "dest", destination);

        //Dispatch the compute shader
        _cSMain.Dispatch(_cSMain.FindKernel("MakeNPow2"), (int)Mathf.Ceil(source.width / 32), (int)Mathf.Ceil(source.height / 32), 1);
    }
}
