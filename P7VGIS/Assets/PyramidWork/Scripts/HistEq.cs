using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class HistEq : MonoBehaviour
{
    private NPFrame2 frame;
    private RenderTexture donePow2;
    private bool firstPass = true;

    void Start()
    {
        frame = new NPFrame2("Stuff", 3);

        donePow2 = new RenderTexture(Screen.width, Screen.height, 0, frame.GetTextureFormat, RenderTextureReadWrite.Linear);
        donePow2.enableRandomWrite = true;
        donePow2.Create();
    }

    void OnRenderImage(RenderTexture source, RenderTexture dest)
    {
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("HistogramEq"), "source", source);
        frame.GetShader.SetTexture(frame.GetShader.FindKernel("HistogramEq"), "dest", donePow2);
        if (firstPass)
        {
            frame.GetShader.SetInt("firstPass", 1);
            frame.GetShader.Dispatch(frame.GetShader.FindKernel("HistogramEq"), (int)Mathf.Ceil(source.width / 32 + 1), (int)Mathf.Ceil(source.height / 32 + 1), 1);
            firstPass = false;
        }

        if (!firstPass)
        {
            frame.GetShader.SetInt("firstPass", 0);
            frame.GetShader.Dispatch(frame.GetShader.FindKernel("HistogramEq"), (int)Mathf.Ceil(source.width / 32 + 1), (int)Mathf.Ceil(source.height / 32 + 1), 1);
            firstPass = true;
        }
        Graphics.Blit(donePow2, dest);

        }
 }