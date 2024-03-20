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

    List<Vector3> gates;

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


        gates = new List<Vector3>();

        float gateWidth = GetComponent<SplineExtrude>().Radius + 2f;

        float prevT = getSplineParam(spline.Knots.Last()) - 1;
        int i = 0;
        foreach (BezierKnot thisKnot in spline.Knots)
        {
            float thisT = getSplineParam(thisKnot);

            if (i % 2 == 0)
            {

                float midT = (prevT + thisT) * 0.5f;
                if (midT < 0)
                    midT += 1;

                Vector3 gatePos = spline.EvaluatePosition(midT);


                gates.Add(gatePos);

                Vector3 gateDir = Quaternion.AngleAxis(90, Vector3.forward) * spline.EvaluateTangent(midT);
                gateDir.Normalize();

                Debug.DrawLine(gatePos + gateWidth * gateDir, gatePos - gateWidth * gateDir);
            }

            prevT = thisT;
            i++;
        }
    }

}
