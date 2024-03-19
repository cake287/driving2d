using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using static UnityEditor.PlayerSettings;

public class CarControl : MonoBehaviour
{
    //public Vector2 velocity { get; private set; } = Vector2.zero;
    public Vector2 velocity = Vector2.zero; // for debugging only
    private Vector2 acceleration = Vector2.zero;
    public Vector2 bodyDir = Vector2.up; // direction of the body of the car
    public float angularVelocity = 0; // in degrees/s


    public float mass = 1000;
    public float engineForce = 0;

    private float accelBrakeInput = 0;
    private float steerInput = 0;

    public float maxSpeed = 15;
    public float maxEngineTorque = 6000;
    public AnimationCurve enginePower;
    
    public float maxLongFriction = 500;

    public float maxBrakingForce = 10000;


    public float maxSteerAngle = 25;
    public float steerAngle = 0;
    public float maxLatFriction = 6;
    public float handbrakeLatFrictionMultiplier = 0.33f;


    public Transform wheelFrontLeft;
    public Transform wheelFrontRight;
    public Transform wheelBackLeft;
    public Transform wheelBackRight;

    public TrailRenderer leftTrail;
    public TrailRenderer rightTrail;


    public SplineContainer trackSpline;
        
    Vector2 rotateVec2(Vector2 v, float a) // angle in degrees
    {
        return Quaternion.AngleAxis(a, Vector3.forward) * v;
    }
    Vector2 rot90(Vector2 v) // rotates 90 degrees clockwise
    {
        return new Vector2(v.y, -v.x);
    }


    private void Start()
    {

        //Keyframe[] engineKeyFrames =
        //{
        //    new Keyframe(0, 0.5f, 4, 4, 0, 0),
        //    new Keyframe(0.25f, 0.5f, 4, 4, 0, 0),
        //    new Keyframe(0, 0.5f, 4, 4, 0, 0),
        //};


        wheelFrontLeft = transform.Find("wheelFrontLeft");
        wheelFrontRight = transform.Find("wheelFrontRight");
        wheelBackLeft = transform.Find("wheelBackLeft");
        wheelBackRight = transform.Find("wheelBackRight");

        leftTrail = wheelBackLeft.GetComponentInChildren<TrailRenderer>();
        rightTrail = wheelBackRight.GetComponentInChildren<TrailRenderer>();

    }


    private void DebugDrawPoint(Vector3 p, float diameter = 0.5f)
    {
        Debug.DrawRay(new(p.x - diameter, p.y, p.z), 2 * diameter * Vector3.right, Color.magenta);
        Debug.DrawRay(new(p.x, p.y - diameter, p.z), 2 * diameter * Vector3.up, Color.magenta);
    }
    private void DebugDrawPoint(float3 p)
    {
        DebugDrawPoint(new Vector3(p.x, p.y, p.z));
    }

    bool isOnTrack()
    {
        float3 pos = new(transform.position.x, transform.position.y, trackSpline.transform.position.z);
        float3 nearestPoint = new(); // the point on the spline closest to the car
        float t;
        SplineUtility.GetNearestPoint(trackSpline.Spline, pos, out nearestPoint, out t);
        //DebugDrawPoint(new Vector3(nearestPoint.x, nearestPoint.y, 100));


        float3 curveCentre = SplineUtility.EvaluateCurvatureCenter(trackSpline.Spline, t);
        //DebugDrawPoint(curveCentre);
        //Debug.DrawLine(pos, curveCentre);



        float trackRadius = trackSpline.gameObject.GetComponent<SplineExtrude>().Radius;
        return math.distance(new float2(pos.x, pos.y), new float2(nearestPoint.x, nearestPoint.y)) > trackRadius;
    }

    void Update()
    {




        accelBrakeInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal"); 


        ////// longitudinal (forward/back) forces

        engineForce = maxEngineTorque * enginePower.Evaluate(velocity.magnitude / maxSpeed) * Mathf.Max(0, accelBrakeInput);
        Vector2 tractiveForce =  engineForce * bodyDir;

        Vector2 brakingForce = -maxBrakingForce * Mathf.Max(0, -accelBrakeInput) * velocity.normalized; // TODO - braking should only apply to the component of velocity in the direction of the car's body


        Vector2 friction = -maxLongFriction * velocity; 
        friction *= 1 - Mathf.Abs(accelBrakeInput); // don't include friction if we're accelerating or braking. makes it easier to change driving behaviour since you only need to change one variable
        //friction *= 1 + 0.01f / Mathf.Max(velocity.magnitude, 0.001f); // when speed is low, apply lots of friction so the car stops succinctly // THIS DOESNT SEEM TO DO ANYTHING >:(

        Vector2 longForce = tractiveForce + brakingForce + friction;
        acceleration = longForce / mass;
        velocity += acceleration * Time.deltaTime;


        ////// lateral forces

        steerAngle = -steerInput * maxSteerAngle;

        bool handbrake = Input.GetKey(KeyCode.Space);


        float angularVelocity = (handbrake ? 3 : 2) * steerAngle * (float)Math.Log(velocity.magnitude + 1);
        bodyDir = rotateVec2(bodyDir, angularVelocity * Time.deltaTime);


        float handbrakeMultiplier = handbrake ? handbrakeLatFrictionMultiplier : 1;

        float latFriction =  handbrakeMultiplier * maxLatFriction;
        velocity = Vector2.Lerp(velocity.normalized, bodyDir, latFriction * Time.deltaTime) * velocity.magnitude;




        Func<Vector2, Vector3> Vec2to3 = v => new Vector3(v.x, v.y, 0);
        transform.position += Vec2to3(velocity * Time.deltaTime);









        ////// update visual rotation of the car and front wheels

        transform.SetLocalPositionAndRotation(transform.position, Quaternion.FromToRotation(Vector3.up, Vec2to3(bodyDir)));
        //Debug.DrawRay(transform.position, bodyDir * 2, Color.yellow);

        Transform outsideWheel = steerAngle >= 0 ? wheelFrontLeft : wheelFrontRight;
        Transform insideWheel = steerAngle < 0 ? wheelFrontLeft : wheelFrontRight;

        // increase the angle of the wheel on the inside of the curve, since it has a smaller circle path to follow so needs to be tighter
        float visualWheelBase = Mathf.Abs(wheelFrontLeft.localPosition.y - wheelBackLeft.localPosition.y);
        float visualAxleWidth = Mathf.Abs(wheelFrontLeft.localPosition.x - wheelFrontRight.localPosition.x);

        float turnRadius = Math.Abs(visualWheelBase / Mathf.Tan(steerAngle * Mathf.Deg2Rad));

        float insideSteerAngle = Mathf.Sign(steerAngle) * Mathf.Atan2(visualWheelBase, turnRadius + visualAxleWidth / 2) * Mathf.Rad2Deg;
        insideWheel.SetLocalPositionAndRotation(insideWheel.localPosition, Quaternion.Euler(0, 0, insideSteerAngle));

        float outsideSteerAngle = Mathf.Sign(steerAngle) * Mathf.Atan2(visualWheelBase, turnRadius - visualAxleWidth / 2) * Mathf.Rad2Deg;
        outsideWheel.SetLocalPositionAndRotation(outsideWheel.localPosition, Quaternion.Euler(0, 0, outsideSteerAngle));


        //// rays for showing ackerman steering
        //Debug.DrawRay(outsideWheel.transform.position, -Mathf.Sign(steerAngle) * outsideWheel.transform.right.normalized * 10, Color.blue);
        //Debug.DrawRay(insideWheel.transform.position, -Mathf.Sign(steerAngle) * insideWheel.transform.right.normalized * 10, Color.blue);
        //Debug.DrawRay(0.5f * (wheelBackRight.transform.position + wheelBackLeft.transform.position), -Mathf.Sign(steerAngle) * wheelBackRight.transform.right.normalized * turnRadius, Color.blue);




        //// tyre marks

        float driftAngle = Mathf.Acos(Vector2.Dot(bodyDir.normalized, velocity.normalized)) * Mathf.Rad2Deg;
        bool showTyreMarks = driftAngle > 20;
        leftTrail.emitting = showTyreMarks;
        rightTrail.emitting = showTyreMarks;





        if (!isOnTrack())
            GetComponent<SpriteRenderer>().color = new Color(1, 1, 1);
        else
            GetComponent<SpriteRenderer>().color = new Color(1, 0, 0);

    }

}
