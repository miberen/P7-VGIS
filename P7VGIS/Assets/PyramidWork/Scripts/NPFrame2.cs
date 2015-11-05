using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class NPFrame2
{
    public class Synthesis
    {
        private List<RenderTexture> _synths;
        private int _analyzedFrom;

        internal Synthesis(List<RenderTexture> synths, int analyzedFrom)
        {
            _analyzedFrom = analyzedFrom;
            _synths = synths;
        }


        public int GetSourceLevel
        {
            get { return _analyzedFrom; }
        }
    };

    private Dictionary<string, NPFrame2> _masterDic;
    private Dictionary<string, Synthesis> __synthDic;
    private List<RenderTexture> _analyzeList;

    private int _level;
    private List<int> pow2S;
    private bool _isInit;
    private Vector2 _lastScreenSize;
    private ComputeShader _cSMain;

    private RenderTexture _done;
    private RenderTexture _donePow2;

    public NPFrame2()
    {
        
    }
    public NPFrame2(int analyzeLevels)
    {

    }
    public NPFrame2(Vector2 analyzeRes)
    {

    }

    public void Analyze()
    {
        
    }

    public void Synthesize()
    {
        
    }

    private void AnalyzeCall()
    {
        
    }

    private void SynthesizeCall()
    {
        
    }

    private int NextPow2()
    {
        return -1;
    }

    private int LevelFromRes()
    {
        return -1;
    }

    private bool CheckCompatibility()
    {
        return true;
    }

    private void GenPow2S()
    {
        
    }
}
