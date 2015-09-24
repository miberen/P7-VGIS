using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class PyramidEffects : MonoBehaviour {

    private Material material;

    // Use this for initialization
    void Awake () {
        material = new Material(Shader.Find("Hidden/PyramidBlur"));
	}
	
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {       
        Graphics.Blit(source, destination, material);
    }
}