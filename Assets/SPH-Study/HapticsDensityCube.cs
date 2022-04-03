using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[ExecuteInEditMode]
public class HapticsDensityCube : MonoBehaviour
{
    [SerializeField]
    public IATK.CSVDataSource dataSource;

    public const int hapticsCubeResolution = 64;

    [SerializeField]
    public float[,,] hapticsCube = new float[hapticsCubeResolution, hapticsCubeResolution, hapticsCubeResolution];

    public string hapticsDimensionName = "u";

    public bool getDensityCube;
    public bool writeDensityCubeToText;

    private Vector3[] positions;
    private const int texDepth = hapticsCubeResolution; // Resolution/Sidelength of 3D cube

    // Set up Compute Shader
    // Gaussian GPU computer shader
    [SerializeField]
    ComputeShader computeShader;
    ComputeBuffer densityBuffer;
    ComputeBuffer hapticsBuffer;
    ComputeBuffer pointsBuffer;
    ComputeBuffer kernelSizeBuffer;

    // Gaussian GPU compute shader fields IDs
    int
        texDepthId,
        kernelSizeId,
        coefIntensityId,
        muId,
        sigmaId,
        densityBufferId,
        hapticsBufferId,
        pointsBufferId,
        kernelSizeBufferId;

    void GetComputeShaderIds()
    {
        texDepthId = Shader.PropertyToID("_texDepth");
        kernelSizeId = Shader.PropertyToID("_kernelSize");
        coefIntensityId = Shader.PropertyToID("_coefIntensity");
        muId = Shader.PropertyToID("_mu");
        sigmaId = Shader.PropertyToID("_sigma");
        densityBufferId = Shader.PropertyToID("_DensityBuffer");
        hapticsBufferId = Shader.PropertyToID("_HapticsBuffer");
        pointsBufferId = Shader.PropertyToID("_PointsBuffer");
        kernelSizeBufferId = Shader.PropertyToID("_KernelSizeBuffer");
    }

    // Update is called once per frame
    void Update()
    {
        if (getDensityCube)
        {
            GenerateDensityCube();
        }
        getDensityCube = false;

        if (writeDensityCubeToText)
        {
            WriteCubeToTextFile();
        }
        writeDensityCubeToText = false;
    }

    void UpdateGPUGaussianValues(int texDepth, float coefIntensity, float mu, float sigma)
    {
        computeShader.SetInt(texDepthId, texDepth);
        //computeShader.SetInt(kernelSizeId, kernelSize);

        computeShader.SetFloat(coefIntensityId, coefIntensity);
        computeShader.SetFloat(muId, mu);
        computeShader.SetFloat(sigmaId, sigma);

        computeShader.SetBuffer(0, densityBufferId, densityBuffer);
        computeShader.SetBuffer(0, pointsBufferId, pointsBuffer);
        computeShader.SetBuffer(0, kernelSizeBufferId, kernelSizeBuffer);
        computeShader.SetBuffer(0, hapticsBufferId, hapticsBuffer);
    }

    int[] MapHValuesToGPU()
    {
        // The spatial length of each axis is equivalent to 1 game unit and _VolumeResolution cells.
        int[] mappedKernelSizes = new int[dataSource["h"].Data.Length];

        // Assuming the data cube is 1x1x1 in Unity unit in size,
        float cellSideLength = 1 / hapticsCubeResolution;

        // Length of cube in spatial units of the data.
        // This assumes the ranges of x,y,z in the data are the same.
        float cubeLength = (float)dataSource.getMaxOriginalValue("x") - (float)dataSource.getMinOriginalValue("x");

        // Considering h value to be the standard deviation
        // Include 3 standard deviations ~ 99.7%
        int sigmaIncluded = 3;

        for (int i = 0; i < dataSource["h"].Data.Length; i++)
        {
            float h = (float)dataSource.getOriginalValue(dataSource["h"].Data[i], "h");

            // h in Unity unit = h/cubeLength -> h in number of cells = (h/cubeLength) * _VolumeResolution
            // The kernel size will be 2 * (h in number of cells), because particle radius is half kernel size
            // = 2 * sigmaIncluded * h_cells
            int k = Mathf.CeilToInt(2 * sigmaIncluded * hapticsCubeResolution * h / cubeLength);
            //mappedKernelsizes[i] = (k >= 3) ? k : 3;
            if (k >= 150)
            {
                k = 0;
            }
            else
            {
                mappedKernelSizes[i] = (k >= 3) ? k : 3;
            }

        }

        //print(mappedKernelSizes[0]);
        return mappedKernelSizes;
    }

    private void SetDensityCubeData(float[] densityArray)
    {
        // _DensityBuffer[x + (y * _texDepth) + (z * _texDepth * _texDepth)] += gaussianValue * _coefIntensity;

        for (int i = 0; i < densityArray.Length; i++)
        {
            int x = i % texDepth;
            int y = (i / texDepth) % texDepth;
            int z = i / (texDepth * texDepth);
            hapticsCube[x, y, z] = densityArray[i];
        }
    }

    private void GenerateDensityCube()
    {
        print("Initialising getting density cube...");
        GetComputeShaderIds();
        ProcessDataSource();

        // Initialise 1D density array
        float[] densityArray = new float[hapticsCubeResolution * hapticsCubeResolution * hapticsCubeResolution];
        for (int i = 0; i < densityArray.Length; i++)
        {
            densityArray[i] = 0f;
        }

        // Initialise density buffer with initial data (all 0)
        densityBuffer = new ComputeBuffer(densityArray.Length, 4);
        densityBuffer.SetData(densityArray);

        // Initialise density buffer with initial data (all 0)
        hapticsBuffer = new ComputeBuffer(dataSource[hapticsDimensionName].Data.Length, 4);
        hapticsBuffer.SetData(dataSource[hapticsDimensionName].Data);

        // Create new compute buffer for reading particle positions
        pointsBuffer = new ComputeBuffer(positions.Length, 3 * 4);
        // Set the point coords to compute buffer
        pointsBuffer.SetData(positions);

        // Map H values to compute buffer
        int[] kernelSizeArray = MapHValuesToGPU();
        kernelSizeBuffer = new ComputeBuffer(kernelSizeArray.Length, 4);
        kernelSizeBuffer.SetData(kernelSizeArray);

        // Pass the necessary values to compute buffer to calculate density
        UpdateGPUGaussianValues(texDepth, 1, 0f, 0.15f);

        // Number of groups (1-D array of points split into 1024 sections
        // There will be 1024x1x1 = 1024x1x1 threads for each warp.
        int groupsX = Mathf.CeilToInt(positions.Length / 1024f);

        print("Dispatching to GPU...");
        // Dispatch the compute shader
        computeShader.Dispatch(0, groupsX, 1, 1);

        // Populate output array with the color data from compute buffer
        float[] outputDensityArray = new float[densityArray.Length];
        densityBuffer.GetData(outputDensityArray);

        // Release compute buffer for next point in point cloud
        densityBuffer.Release();
        densityBuffer = null;

        pointsBuffer.Release();
        pointsBuffer = null;

        kernelSizeBuffer.Release();
        kernelSizeBuffer = null;

        //////// IMPORTANT !!!! Map 1D density to 3D here !!!!
        SetDensityCubeData(outputDensityArray);

        print("Finished generating density cube.");
    }

    private void ProcessDataSource()
    {
        float[] xs = dataSource["x"].Data;
        float[] ys = dataSource["y"].Data;
        float[] zs = dataSource["z"].Data;

        positions = new Vector3[dataSource[0].Data.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = new Vector3(xs[i], ys[i], zs[i]);
        }

        print("Finished processing data source.");
    }

    private void WriteCubeToTextFile()
    {
        string path = "Assets/SPH-Study/Cube.txt";

        File.CreateText(path).Dispose();
        using (TextWriter writer = new StreamWriter(path, false))
        {
            
            for (int x = 0; x < hapticsCube.GetLength(0); x++)
            {
                string xLayer = "";
                for (int y = 0; y < hapticsCube.GetLength(1); y++)
                {
                    string yLayer = "{";
                    for (int z = 0; z < hapticsCube.GetLength(2); z++)
                    {
                        yLayer += hapticsCube[x, y, z].ToString() + " ";
                    }
                    yLayer += "},\n";
                    xLayer += yLayer;
                }
                xLayer += "\n";
                writer.WriteLine("############ X: " + x.ToString() + " ##########\n");
                writer.WriteLine(xLayer);
            }
            
            writer.Close();
        }
    }

}
