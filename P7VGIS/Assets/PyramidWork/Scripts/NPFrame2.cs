using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class NPFrame2{

    public class Synthesis
    {
        private List<RenderTexture> _synths;
        private int _analyzedFrom;

        internal Synthesis(int analyzedFrom)
        {
            _analyzedFrom = analyzedFrom;
            _synths = new List<RenderTexture>();
        }



        public int SourceLevel
        {
            get { return _analyzedFrom; }
        }

        public List<RenderTexture> Pyramid 
        {
            get { return _synths; }
        }
    };

    private Dictionary<string, NPFrame2> _masterDic = new Dictionary<string, NPFrame2>();
    private Dictionary<string, Synthesis> _synthDic = new Dictionary<string,Synthesis>();
    private List<RenderTexture> _analyzeList = new List<RenderTexture>();

    private int _levels;
    private List<int> _pow2S = new List<int>();
    private bool _isInit;
    private Vector2 _lastScreenSize;
    private ComputeShader _cSMain;

    private RenderTexture _done;
    private RenderTexture _donePow2;

    [Obsolete("Use other constructors for now")]
    public NPFrame2(string name)
    {
        BasicInit(name);
    }

    public NPFrame2(string name, int analyzeLevels)
    {
        _levels = analyzeLevels;
        BasicInit(name);
    }

    public NPFrame2(string name, Vector2 analyzeRes)
    {
        BasicInit(name);
    }

    public List<RenderTexture> AnalyzeList
    {
        get { return _analyzeList; }
    }

    public Synthesis GetSynthesis(string key)
    {
        return _synthDic[key];
    }

    public void Analyze(ref RenderTexture source)
    {
        if (!_isInit || _lastScreenSize != new Vector2(Screen.width, Screen.height)) InitAnalyze(ref source);

        MakePow2Call(ref source, ref _analyzeList);

        AnalyzeCall();
    }

    public void GenerateSynthesis(int sourceLevel, string name)
    {
        if (!_synthDic.ContainsKey(name))
        {
            _synthDic.Add(name, new Synthesis(sourceLevel));

            for (int i = 0; i <= _analyzeList.Count - sourceLevel; i++)
            {
                _synthDic[name].Pyramid.Add(new RenderTexture(_analyzeList[_levels - 1 - i].width, _analyzeList[_levels - 1 - i].height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear));
                _synthDic[name].Pyramid[i].enableRandomWrite = true;
                _synthDic[name].Pyramid[i].Create();
            }
        }
        else
        {
            SynthesizeCall(_synthDic[name]);
        }
        
    }

    private void BasicInit(string name)
    {
        if (CheckCompatibility())
        {
            _masterDic.Add(name, this);
            _cSMain = (ComputeShader)Resources.Load("NPFrame/Shaders/NPFrame");
            GenPow2S();
        }
    }

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
            _analyzeList.Add(new RenderTexture(_pow2S[_pow2S.IndexOf(size) - i], _pow2S[_pow2S.IndexOf(size) - i], 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear));
            _analyzeList[i].enableRandomWrite = true;
            _analyzeList[i].Create();
        }

        _donePow2 = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        _donePow2.enableRandomWrite = true;
        _donePow2.Create();

        _done = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        _done.enableRandomWrite = true;
        _done.Create();

        _lastScreenSize = new Vector2(Screen.width, Screen.height);
        _isInit = true;

    }

    private void AnalyzeCall()
    {
        for (int i = 0; i < _levels - 1; i++)
        {
            _cSMain.SetTexture(_cSMain.FindKernel("Analyze"), "source", _analyzeList[i]);
            _cSMain.SetTexture(_cSMain.FindKernel("Analyze"), "dest", _analyzeList[i + 1]);

            _cSMain.Dispatch(_cSMain.FindKernel("Analyze"), (int)Mathf.Ceil(_analyzeList[i + 1].width / 32), (int)Mathf.Ceil(_analyzeList[i + 1].height / 32), 1);
        }
    }

    private void SynthesizeCall(Synthesis synth, int levels = 0)
    {
        if (levels == 0) levels = _levels;

        for (int i = 0; i < levels - 1; i++)
        {
            _cSMain.SetTexture(_cSMain.FindKernel("Synthesize"), "source", synth.Pyramid[i]);
            _cSMain.SetTexture(_cSMain.FindKernel("Synthesize"), "dest", synth.Pyramid[i + 1]);

            _cSMain.Dispatch(_cSMain.FindKernel("Synthesize"), (int)Mathf.Ceil(synth.Pyramid[i].width / 32), (int)Mathf.Ceil(synth.Pyramid[i].height / 32), 1);
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

    private void MakePow2(ref RenderTexture source, out RenderTexture destination)
    {
        destination = null;
    }

    private void MakeNPow2(ref RenderTexture source, out RenderTexture destination)
    {
        destination = null;
    }

    private void MakePow2Call(ref RenderTexture source, ref List<RenderTexture> destination)
    {
        //Set the shader uniforms
        _cSMain.SetTexture(_cSMain.FindKernel("MakePow2"), "source", source);
        _cSMain.SetTexture(_cSMain.FindKernel("MakePow2"), "dest", destination[0]);

        //Dispatch the compute shader 
        // hack the index
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
