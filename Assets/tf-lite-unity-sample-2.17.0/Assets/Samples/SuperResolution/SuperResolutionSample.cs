﻿using TensorFlowLite;
using UnityEngine;
using UnityEngine.UI;

public class SuperResolutionSample : MonoBehaviour
{
    [SerializeField, FilePopup("*.tflite")] string fileName = "";
    [SerializeField] Texture2D inputTex;
    [SerializeField] RawImage outputImage;
    [SerializeField] ComputeShader compute;

    SuperResolution superResolution;

    void Start()
    {
        superResolution = new SuperResolution(fileName, compute);
        superResolution.Run(inputTex);
        outputImage.texture = superResolution.ResultTexture;
    }

    void OnDestroy()
    {
        superResolution?.Dispose();
    }
}
