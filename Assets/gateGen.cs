using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class gateGen : MonoBehaviour
{
    [SerializeField]
    SplineContainer splineContainer;

    private void DebugDrawPoint(Vector3 p, Color c, float diameter = 0.5f)
    {
        Debug.DrawRay(new(p.x - diameter, p.y, p.z), 2 * diameter * Vector3.right, c);
        Debug.DrawRay(new(p.x, p.y - diameter, p.z), 2 * diameter * Vector3.up, c);
    }
    private void DebugDrawPoint(float3 p, Color c)
    {
        DebugDrawPoint(new Vector3(p.x, p.y, p.z), c);
    }

    private void DebugDrawPoint(float2 p, Color c)
    {
        DebugDrawPoint(new Vector3(p.x, p.y, 100), c);
    }


    // gets the spline ratio t for a knot on the curve
    float getSplineParam(BezierKnot k)
    {
        float3 temp;
        float t;
        SplineUtility.GetNearestPoint(splineContainer.Spline, k.Position, out temp, out t);

        return t;
    }

    void Start()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();

        
    }

    private void Update()
    {
        Spline spline = splineContainer.Spline;

        int knotCount = spline.Knots.Count();
        Debug.Log(knotCount);

        Vector3[] gates = new Vector3[knotCount];

        float prevT = getSplineParam(spline.Knots.Last());
        int i = 0;
        foreach (BezierKnot thisKnot in spline.Knots)
        {
            float thisT = getSplineParam(thisKnot);
            float3 thisPos = spline.EvaluatePosition(thisT);
            DebugDrawPoint(new float2(thisPos.x, thisPos.y), Color.red);

            float midT = (prevT + thisT) * 0.5f;

            float3 gatePos = spline.EvaluatePosition(midT);
            Debug.Log(gatePos);
            DebugDrawPoint(new float2(gatePos.x, gatePos.y), Color.magenta);
            prevT = thisT;

            gates[i] = gatePos;
            i++;
        }
    }

}
