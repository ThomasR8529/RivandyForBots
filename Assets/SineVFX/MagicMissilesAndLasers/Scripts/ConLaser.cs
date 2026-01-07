using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ConLaser : MonoBehaviour
{

    public float maxLength = 16.0f;
    public GameObject hitEffect;
    public Renderer meshRenderer1;
    public Renderer meshRenderer2;
    public ParticleSystem[] hitPsArray;
    public int segmentCount = 32;
    public float globalProgressSpeed = 1f;
    public AnimationCurve shaderProgressCurve;
    public AnimationCurve lineWidthCurve;
    public Light pl;
    public float moveHitToSource;

    private LineRenderer lr;
    private Vector3[] resultVectors;
    private float dist;
    private float globalProgress;
    private Vector3 hitPosition;
    private Vector3 currentPosition;

    void Start()
    {
        globalProgress = 1f;
        lr = this.GetComponent<LineRenderer>();
        if (lr == null)
        {
            Debug.LogError("LineRenderer component is missing on the GameObject.");
            return;
        }
        lr.positionCount = segmentCount;
        resultVectors = new Vector3[segmentCount + 1];
        for (int i = 0; i < segmentCount + 1; i++)
        {
            resultVectors[i] = transform.forward;
        }
    }

    void Update()
    {
        //Curvy Start

        for (int i = segmentCount - 1; i > 0; i--)
        {
            resultVectors[i] = resultVectors[i - 1];
        }
        resultVectors[0] = transform.forward;
        resultVectors[segmentCount] = resultVectors[segmentCount - 1];
        float blockLength = maxLength / segmentCount;


        currentPosition = new Vector3(0, 0, 0);

        for (int i = 0; i < segmentCount; i++)
        {
            currentPosition = transform.position;
            for (int j = 0; j < i; j++)
            {
                currentPosition += resultVectors[j] * blockLength;
            }
            lr.SetPosition(i, currentPosition);
        }

        //Curvy End



        //Collision Start

        for (int i = 0; i < segmentCount; i++)
        {

            currentPosition = transform.position;
            for (int j = 0; j < i; j++)
            {
                currentPosition += resultVectors[j] * blockLength;
            }

            RaycastHit hit;
            if (Physics.Raycast(currentPosition, resultVectors[i], out hit, blockLength))
            {
                hitPosition = currentPosition + resultVectors[i] * hit.distance;
                hitPosition = Vector3.MoveTowards(hitPosition, transform.position, moveHitToSource);
                if (hitEffect != null)
                {
                    hitEffect.transform.position = hitPosition;
                }

                dist = Vector3.Distance(hitPosition, transform.position);

                break;
            }
        }

        //Collision End


        //Emit Particles on Collision Start

        if (hitEffect != null && hitPsArray != null)
        {
            if (globalProgress < 0.75f)
            {
                foreach (ParticleSystem ps in hitPsArray)
                {
                    if (ps != null)
                    {
                        pl.enabled = true;

                        var em = ps.emission;
                        em.enabled = true;
                    }
                }
            }
            else
            {
                foreach (ParticleSystem ps in hitPsArray)
                {
                    if (ps != null)
                    {
                        pl.enabled = false;

                        var em = ps.emission;
                        em.enabled = false;
                    }
                }
            }
        }

        //Emit Particles on Collision End

        GetComponent<Renderer>().material.SetFloat("_Distance", dist);
        GetComponent<Renderer>().material.SetVector("_Position", transform.position);

#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
        {
            if (mouse.leftButton.isPressed)
            {
                globalProgress = 0f;
            }

            if (mouse.leftButton.wasPressedThisFrame && hitEffect != null && hitPsArray.Length > 1 && hitPsArray[1] != null)
            {
                hitPsArray[1].Emit(100);
            }
        }
#else
        if (Input.GetMouseButton(0))
        {
            globalProgress = 0f;
        }

        if (Input.GetMouseButtonDown(0) && hitEffect != null && hitPsArray.Length > 1 && hitPsArray[1] != null)
        {
            hitPsArray[1].Emit(100);
        }
        #endif

        if (globalProgress <= 1f)
        {
            globalProgress += Time.deltaTime * globalProgressSpeed;
        }

        if (pl != null)
        {
            pl.intensity = shaderProgressCurve.Evaluate(globalProgress) * 1.5f;
        }

        float progress = shaderProgressCurve.Evaluate(globalProgress);
        GetComponent<Renderer>().material.SetFloat("_Progress", progress);

        if (meshRenderer1 != null && meshRenderer2 != null)
        {
            meshRenderer1.material.SetFloat("_Progress", progress);
            meshRenderer2.material.SetFloat("_Progress", progress);
        }

        float width = lineWidthCurve.Evaluate(globalProgress);
        lr.widthMultiplier = width;
    }
}
