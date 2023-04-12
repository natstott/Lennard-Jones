/*
 https://github.com/Shinao/Unity-GPU-Boids was inspiration and source of original code.
 
Sample data from
https://spiral.imperial.ac.uk/bitstream/10044/1/87687/2/1102_camera_ready.pdf

A GPU accelerated Lennard-Jones system for immersive molecular dynamics simulations in Virtual Reality - 
Nitesh Bhatia, Erich A. Müller, and Omar Matar

Physical Quantity Units CO2 Parameters Scaling Factor Scaled CO2 parameters for VR
Length      σ       2.8 x 10^−10 m      10^8        0.028 m
Energy      ϵ       2.69 x 10^−21 J     10^18       0.00269 J
Mass        m       7.30 x 10^−26 Kg    10^25       0.73 Kg
Boltzmann Constant kB 1.38 x 10−23 J/k  10^18       0.0000138 J/k

 */


//using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


 struct Molecule
{
    public Vector3 position;
    public Vector3 direction;
    public Vector3 velocity;
    public Vector3 acceleration;
    public float boidsize;
    public float boidmass;
 
}


public class PeriodicFunction : MonoBehaviour {
 
    public ComputeShader _ComputeFlock;
    public ComputeShader _UpdatePositions;
    public int BoidsCount;
    public Mesh BoidMesh;
    public Material BoidMaterial;
    public float molecularMass;
    public float wellSize;
    //public float BoidSpeed = 1f;
    public float NeighbourDistance = 1f; //interatomic spacing
    public float CrystalOverlap = 1.3f; // increased crystal density for formation
    public float Temperature;
    public float Cooling = 0.999999f; //1= no cooling, <1 reduces velocities
    float LJA;
    float LJB;
    float BoundsLJA;
    float BoundsLJB;
    float offsetwidth = 1.0f; //used to avoid edges of volume for creation,plus used to create potential well width of boundaries.
    public float BoundsWellSize; // used if reflective boundary
    public float FixedTimeStep = 0.01f; //Fixed at 1/200

    const float Boltzmann = 0.0000138f; // J/k scaled as above

    private Molecule[] boidsData;
    private int kernelHandle;
    private int UpdateKernelHandle;
    private ComputeBuffer BoidBuffer;
    public float BoxSize;
    public float createSize;
    Bounds particleBounds; //used for Drawmeshinstanced- avoid drawing if not looking?



    ComputeBuffer _drawArgsBuffer;
    MaterialPropertyBlock _props;

    const int GROUP_SIZE = 256;


    void Start()
    {
        //calculate Lennard Jones variables
        LJA = 48f * wellSize *  Mathf.Pow(NeighbourDistance , 12.0f);
        LJB = 24f * wellSize * Mathf.Pow(NeighbourDistance, 6.0f);

        Debug.Log("LJA, " + LJA.ToString()+ " LJB ,"+LJB.ToString());
        // Initialize the indirect draw args buffer.
        _drawArgsBuffer = new ComputeBuffer(
            1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments
        );
        _drawArgsBuffer.SetData(new uint[5] {
            BoidMesh.GetIndexCount(0), (uint) BoidsCount, 0, 0, 0
        });

        particleBounds = new Bounds(Vector3.zero, Vector3.one * BoxSize);

        // This property block is used only for avoiding an instancing bug.
        _props = new MaterialPropertyBlock();
        _props.SetFloat("_UniqueID", Random.value);


        //create particles as "boids" in buffer
        this.boidsData = new Molecule[this.BoidsCount];
        for (int i = 0; i < this.BoidsCount; i++)
        {
            //   this.boidsData[i] = this.CreateBoidData(molecularMass, createSize);
            this.boidsData[i] = this.CreateBoidCrystal(molecularMass, i);
        }
        BoidBuffer = new ComputeBuffer(BoidsCount, 56);
        BoidBuffer.SetData(this.boidsData);


        this.kernelHandle = _ComputeFlock.FindKernel("CSMain");
        UpdateFixedValues();

        this.UpdateKernelHandle = _UpdatePositions.FindKernel("CSUpdateMain");
        _UpdatePositions.SetFloat("DeltaTime", FixedTimeStep);
        _UpdatePositions.SetBuffer(this.UpdateKernelHandle, "boidBuffer", BoidBuffer);
        BoidMaterial.SetBuffer("boidBuffer", BoidBuffer);

    }

    void Update()
    {
        UpdateLiveValues(); // *** Currently running withfixed parameters

        _UpdatePositions.Dispatch(this.UpdateKernelHandle, this.BoidsCount / GROUP_SIZE + 1, 1, 1);
        _ComputeFlock.Dispatch(this.kernelHandle, this.BoidsCount / GROUP_SIZE + 1, 1, 1);

        //Draw Particles
        Graphics.DrawMeshInstancedIndirect(
            BoidMesh, 0, BoidMaterial,
            particleBounds,
            _drawArgsBuffer, 0, _props
        );  //BoidMaterial linked to shader with buffer connection.


    }
    Molecule CreateBoidData(float mass, float Size)
    {
        Molecule boidData = new Molecule();
        
        float offsetX = Random.Range(-Size + offsetwidth, Size - offsetwidth);
        float offsetY = Random.Range(-Size + offsetwidth, Size - offsetwidth);
        float offsetZ = Random.Range(-Size + offsetwidth, Size - offsetwidth);

        Vector3 pos = new Vector3(offsetX, offsetY, offsetZ);
        boidData.position = pos;
        boidData.direction = Random.insideUnitSphere;

        boidData.velocity = boidData.direction *Random.Range(0.0001f, Mathf.Sqrt(3f*Boltzmann*Temperature/mass));
        boidData.boidsize = 1.5f*NeighbourDistance;
        boidData.boidmass = mass;
        return boidData;
    }
    Molecule CreateBoidCrystal(float mass, int index)
    {
        Molecule boidData = new Molecule();
        int rows = (int)(Mathf.Pow(BoidsCount,1f/3f));
        int x = index / (rows * rows);
        int y = (index - (x * rows * rows)) / rows;
        int z = (index - (x* rows * rows) - (y * rows));
        float r = NeighbourDistance  * Mathf.Pow(2.0f, 1.0f/6.0f)/CrystalOverlap;


        //Close packed
        float offsetX = 2*x+((y+z)%2);
        float offsetY = Mathf.Sqrt(3f)*(y+(z%3)/3.0f);
        float offsetZ = 2f*Mathf.Sqrt(6f)/3f*z;

        Vector3 pos = transform.position+new Vector3(offsetX, offsetY, offsetZ)*r;
        boidData.position = pos;
        boidData.direction = Random.insideUnitSphere;

        boidData.velocity = boidData.direction * 0.0f;// Random.Range(0.000001f, Mathf.Sqrt(3f * Boltzmann * Temperature / mass));
        boidData.boidsize = 2.0f * r / CrystalOverlap;
        boidData.boidmass = mass;
        return boidData;
    }

    void OnDestroy()
    {
        if (BoidBuffer != null) BoidBuffer.Release();
        if (_drawArgsBuffer != null) _drawArgsBuffer.Release();

    }
    void UpdateFixedValues()
    {
        _ComputeFlock.SetInt("BoidsCount", BoidsCount);
        _ComputeFlock.SetFloat("DeltaTime", FixedTimeStep);
        _ComputeFlock.SetFloat("LJA", LJA);
        _ComputeFlock.SetFloat("LJB", LJB);
        _ComputeFlock.SetFloat("Size", BoxSize);
        _ComputeFlock.SetFloat("Cooling", Cooling);
        _ComputeFlock.SetBuffer(this.kernelHandle, "boidBuffer", BoidBuffer);


    }


    void UpdateLiveValues() {
        _ComputeFlock.SetFloat("Cooling", Cooling);



    }
    
}
