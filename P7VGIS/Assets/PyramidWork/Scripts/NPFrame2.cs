using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;

public class NPFrame2
{
    // Use nested class to allow for use of internal constructors not accessible from outside our framework
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

        /// <summary>
        /// The analysation level this synthesis is generated from.
        /// </summary>
        public int SourceLevel
        {
            get { return _analyzedFrom; }
        }
        internal int SetSourceLevel
        {
            set { _analyzedFrom = value; }
        }

        // Creates an indexer for the list on the class so you can access the list directly.
        public RenderTexture this[int index]
        {
            get { return _synths[index]; }
        }

        /// <summary>
        /// Returns the amount of levels contained in this synthesis.
        /// </summary>
        public int Count
        {
            get { return _synths.Count; }
        }

        /// <summary>
        /// The list of RenderTexures that makes up this synthesis pyramid.
        /// </summary>
        public List<RenderTexture> Pyramid
        {
            get { return _synths; }
        }
    };

    #region Variables

    // Static to allow access in any script, lets user reuse the same pyramids again.
    private static Dictionary<string, NPFrame2> _masterDic = new Dictionary<string, NPFrame2>();

    // Dictionary to hold all the synthesis pyramids of the analysis pyramid of this instance
    private Dictionary<string, Synthesis> _synthDic = new Dictionary<string, Synthesis>();
    // Holds the analysis
    private List<RenderTexture> _analyzeList = new List<RenderTexture>();
    private List<int> _pow2S = new List<int>();

    private int _levels;
    private bool _analyseIsInit = false;
    private bool _synthesiseIsInit = false;
    private Vector2 _lastScreenSize;
    private ComputeShader _cSMain;

    // Options for how the analysis / synthesis should be done and also properties of the RenderTextures
    private AnalysisMode _analysisMode = AnalysisMode.Box2x2;
    private SynthesisMode _synthesisMode = SynthesisMode.BiQuadBSpline;
    private FilterMode _filterMode = FilterMode.Bilinear;
    private RenderTextureFormat _textureFormat = RenderTextureFormat.DefaultHDR;

    // TODO: implement these properly
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
        BasicInit(name);
        _levels = LevelFromRes(analyzeRes);
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

    // TODO: Make this work with different sizes
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
        if (!_analyseIsInit || _lastScreenSize != new Vector2(Screen.width, Screen.height))
        {
            InitAnalyze(ref source);
            _synthesiseIsInit = false;
        }

        // Create the power of 2 texture.
        MakePow2Call(ref source, ref _analyzeList);

        // Set the textures and dispatch the shader to create analysis pyramid
        AnalyzeCall();
    }

    /// <summary>
    /// Generates a synthesis from a specified non-zero based source level of the analyzation pyramid.
    /// </summary>
    /// <param name="sourceLevel"> The non-zero based analyzation level to synthesise from. ( This is also the amount of textures generated.</param>
    /// <param name="name"> Name of the synthesis, used to access it later.</param>
    /// <param name="synthMode">The filter to use for synthesising.</param>
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

            _synthesiseIsInit = true;

            SynthesizeCall(_synthDic[name], sourceLevel, synthMode);
        }
        // If it does exist but the size is different or if the screen size has changed, delete the old one and make a new one with the right size.
        else if (_synthDic.ContainsKey(name) && (sourceLevel != _synthDic[name].SourceLevel || !_synthesiseIsInit))
        {
            foreach (RenderTexture rT in _synthDic[name].Pyramid)
            {
                if (rT.IsCreated())
                {
                    rT.Release();
                }             
            }
            _synthDic[name].Pyramid.Clear();

            for (int i = 0; i < sourceLevel; i++)
            {
                _synthDic[name].Pyramid.Add(new RenderTexture(_analyzeList[sourceLevel - 1 - i].width, _analyzeList[sourceLevel - 1 - i].height, 0, _textureFormat, RenderTextureReadWrite.Linear));
                _synthDic[name][i].enableRandomWrite = true;
                _synthDic[name][i].filterMode = _filterMode;
                _synthDic[name][i].Create();
            }

            _synthDic[name].SetSourceLevel = sourceLevel;
            _synthesiseIsInit = true;

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
            // Add this instance of the class to the static dictionary.
            _masterDic.Add(name, this);
            // Load the compute shader.
            _cSMain = (ComputeShader)Resources.Load("NPFrame/Shaders/NPFrame");
            // Compute the list of POTs, used to determine what resolution texture to copy image into.
            GenPow2S();
        }
    }

    /// <summary>
    /// Initializes the entire analysis pyramid. ( As well as done textures ATM ).
    /// </summary>
    /// <param name="source">The source texture to be used for analysation</param>
    private void InitAnalyze(ref RenderTexture source)
    {
        // Release previous textures if any
        foreach (RenderTexture rT in _analyzeList)
        {
            if (rT.IsCreated())
            {
                rT.Release();
            }
        }
        // Clear the list
        _analyzeList.Clear();
        // Find the size for the first texture
        int size = NextPow2(new Vector2(source.width, source.height));

        // Fill in list
        for (int i = 0; i < _levels; i++)
        {
            _analyzeList.Add(new RenderTexture(_pow2S[_pow2S.IndexOf(size) - i], _pow2S[_pow2S.IndexOf(size) - i], 0, _textureFormat, RenderTextureReadWrite.Linear));
            _analyzeList[i].enableRandomWrite = true;
            _analyzeList[i].filterMode = _filterMode;
            _analyzeList[i].Create();
        }
        
        // Init done textures 
        _donePow2 = new RenderTexture(size, size, 0, _textureFormat, RenderTextureReadWrite.Linear);
        _donePow2.enableRandomWrite = true;
        _donePow2.filterMode = _filterMode;
        _donePow2.Create();

        _done = new RenderTexture(Screen.width, Screen.height, 0, _textureFormat, RenderTextureReadWrite.Linear);
        _done.enableRandomWrite = true;
        _done.filterMode = _filterMode;
        _done.Create();

        // Set the last screen size to the new one and set init to true.
        _lastScreenSize = new Vector2(Screen.width, Screen.height);
        _analyseIsInit = true;

    }

    /// <summary>
    /// Finds the closest POT match for a specified resolution.
    /// </summary>
    /// <param name="resolution">The resolution to find a match for.</param>
    /// <returns></returns>
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
    /// Applies a custom kernel to an image.
    /// </summary>
    /// <param name="source">The texture to apply the kernel to.</param>
    /// <param name="destination">The texture to contain the result of the kernel operation.</param>
    /// <param name="kernel">The kernel to apply. ( Must be n x n size, 1D represented as row by row, bottom to top. )</param>
    /// <param name="filterFactor">The amount to divide the result of the kernel operation with. ( if left empty, will assume division by sum. )</param>
    public void ApplyCustomKernel(RenderTexture source, RenderTexture destination, int[] kernel, int filterFactor = -1)
    {
        // Declares a compute buffer to hold the kernel and other needed parameters. Its the size of the array
        // plus 3 other variables, the strie is the size of an int and its a standard structured buffer. 
        ComputeBuffer buf = new ComputeBuffer(kernel.Length + 3, sizeof(int), ComputeBufferType.Default);

        // Create a new array and put in all the needed variables.
        int[] newArray = new int[kernel.Length + 3];
        // Find out if its an equal or non-equal kernel.
        newArray[0] = kernel.Length % 2;
        Debug.Log("Mod operation: " + kernel.Length % 2);
        // Put in the filter factor. If default use the sum of the kernel, otherwise use specified filterFactor.
        if (filterFactor == -1)
            newArray[1] = kernel.Sum();
        else
            newArray[1] = filterFactor;
        // Lenght of the kernel
        newArray[2] = kernel.Length;
        // Copy in the kernel to the new array
        Array.Copy(kernel, 0, newArray, 3, kernel.Length);

        // Put the new array into the buffer.
        buf.SetData(newArray);

        // Call the compute shader function with the needed parameters.
        CustomKernelCall(source, destination, buf);
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

    /// <summary>
    /// Gets the description of an enum.
    /// </summary>
    /// <param name="value">The enum to retrieve description from.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Checks if the system supports the correct shader model.
    /// </summary>
    /// <returns></returns>
    private bool CheckCompatibility()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.Log("Error: Compute Shaders not supported on this system. Requires Shader Model 50(DX11), Shader Model: " + SystemInfo.graphicsShaderLevel + " detected");
            return SystemInfo.supportsComputeShaders;
        }

        return SystemInfo.supportsComputeShaders;
    }

    /// <summary>
    /// Generates a list of POTs for later usage.
    /// </summary>
    private void GenPow2S()
    {
        for (int i = 1; i < 20 + 1; i++)
        {
            _pow2S.Add((int)Mathf.Pow(2, i));
        }
    }

    /// <summary>
    /// Turns a NPOT texture into a POT texture. 
    /// </summary>
    /// <param name="source">The NPOT texture.</param>
    /// <param name="destination">The resulting POT texture.</param>
    private void MakePow2Call(ref RenderTexture source, ref List<RenderTexture> destination)
    {
        //Set the shader uniforms
        _cSMain.SetTexture(_cSMain.FindKernel("MakePow2"), "source", source);
        _cSMain.SetTexture(_cSMain.FindKernel("MakePow2"), "dest", destination[0]);

        //Dispatch the compute shader 
        _cSMain.Dispatch(_cSMain.FindKernel("MakePow2"), (int)Mathf.Ceil(destination[0].width / 32), (int)Mathf.Ceil(destination[0].height / 32), 1);
    }

    /// <summary>
    /// Turns POT texture into NPOT texture. (Must be nearest to screen res POT ATM)
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    private void MakeNonPow2Call(ref RenderTexture source, ref RenderTexture destination)
    {
        //Set the shader uniforms
        _cSMain.SetTexture(_cSMain.FindKernel("MakeNPow2"), "source", source);
        _cSMain.SetTexture(_cSMain.FindKernel("MakeNPow2"), "dest", destination);

        //Dispatch the compute shader
        _cSMain.Dispatch(_cSMain.FindKernel("MakeNPow2"), (int)Mathf.Ceil(source.width / 32), (int)Mathf.Ceil(source.height / 32), 1);
    }

    /// <summary>
    /// Create analysis pyramid in analysis list.
    /// </summary>
    private void AnalyzeCall()
    {
        for (int i = 0; i < _levels - 1; i++)
        {
            // Set the correct textures for the kernel
            _cSMain.SetTexture(_cSMain.FindKernel(GetEnumDescription(_analysisMode)), "source", _analyzeList[i]);
            _cSMain.SetTexture(_cSMain.FindKernel(GetEnumDescription(_analysisMode)), "dest", _analyzeList[i + 1]);

            // Check if image is smaller than 32x32, if it is just run the kernel once, otherwise divide the image by 32 and use that number.
            if (_analyzeList[i].width > 32 || _analyzeList[i].width > 32)
                _cSMain.Dispatch(_cSMain.FindKernel(GetEnumDescription(_analysisMode)), (int)Mathf.Ceil(_analyzeList[i + 1].width / 32), (int)Mathf.Ceil(_analyzeList[i + 1].height / 32), 1);
            else
            {
                _cSMain.Dispatch(_cSMain.FindKernel(GetEnumDescription(_analysisMode)), 1, 1, 1);
            }
        }
    }

    /// <summary>
    /// Create synthesis in specified synthesis object.
    /// </summary>
    /// <param name="synth">The syntesis object to work on.</param>
    /// <param name="levels">How many levels to generate.</param>
    /// <param name="synthMode">What kernel to use for synthesis.</param>
    private void SynthesizeCall(Synthesis synth, int levels = 0, SynthesisMode synthMode = SynthesisMode.BiQuadBSpline)
    {
        // Use default value if no value is set ( To be changed later to just synthesize all the way up if nothing else is specific)
        if (levels == 0) levels = _levels;

        // If it is the first level just copy over the top level analysis. (This could be optimized slightly by just directly using it from the analysis list)
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

    /// <summary>
    /// Actually calls the compute shader to apply the kernel.
    /// </summary>
    /// <param name="buf">The buffer containing kernel and other parameters. </param>
    /// <param name="source">Source texture.</param>
    /// <param name="destination">Destination texture.</param>
    private void CustomKernelCall(RenderTexture source, RenderTexture destination, ComputeBuffer buf)
    {
        _cSMain.SetTexture(_cSMain.FindKernel("ApplyCustom"), "source", source);
        _cSMain.SetTexture(_cSMain.FindKernel("ApplyCustom"), "dest", destination);
        _cSMain.SetBuffer(_cSMain.FindKernel("ApplyCustom"), "kernel", buf);

        _cSMain.Dispatch(_cSMain.FindKernel("ApplyCustom"), (int)Mathf.Ceil(source.width / 32 +1), (int)Mathf.Ceil(source.height / 32 + 1), 1);
    }
        
}
