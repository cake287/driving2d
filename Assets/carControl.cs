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
    public float braking = 10000;


    public float maxSteerAngle = 25;
    public float steerAngle = 0;
    public float maxLatFriction = 6;
    public float maxLatFrictionHandbrake = 2;


    public Transform wheelFrontLeft;
    public Transform wheelFrontRight;
    public Transform wheelBackLeft;
    public Transform wheelBackRight;


    public Collider track;
        
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
        Debug.Log(Mathf.Atan2(1, 0));
        Debug.Log(Mathf.Atan2(-1, 0));


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



    }

    void Update()
    {

        //float3 pos = new float3(transform.position.x, transform.position.y, 0);
        //float3 nearestPoint = new();
        //float t;
        //SplineUtility.GetNearestPoint(track.Spline, pos, out nearestPoint, out t);
        //Debug.Log(math.distance(pos, nearestPoint));
        //bool isOnTrack = math.distance(pos, nearestPoint) > 3;

        Vector3 pos = new float3(transform.position.x, transform.position.y, track.transform.position.z);
        //bool isOnTrack = track.bounds.Contains(pos);
        RaycastHit temp;
        bool isOnTrack = track.Raycast(new Ray(transform.position, new(0, 0, -1)), out temp, 100);

        if (!isOnTrack)
            GetComponent<SpriteRenderer>().color = new Color(1, 0, 0);
        else
            GetComponent<SpriteRenderer>().color = new Color(1, 1, 1);




        accelBrakeInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");


        ////// longitudinal (forward/back) forces

        engineForce = maxEngineTorque * enginePower.Evaluate(velocity.magnitude / maxSpeed) * Mathf.Max(0, accelBrakeInput);
        Vector2 tractiveForce =  engineForce * bodyDir;

        Vector2 brakingForce = -braking * Mathf.Max(0, -accelBrakeInput) * velocity.normalized; // TODO - braking should only apply to the component of velocity in the direction of the car's body


        Vector2 friction = -maxLongFriction * velocity; 
        friction *= 1 - Mathf.Abs(accelBrakeInput); // don't include friction if we're accelerating or braking. makes it easier to change driving behaviour since you only need to change one variable
        //friction *= 1 + 0.01f / Mathf.Max(velocity.magnitude, 0.001f); // when speed is low, apply lots of friction so the car stops succinctly // THIS DOESNT SEEM TO DO ANYTHING >:(

        Vector2 longForce = tractiveForce + brakingForce + friction;
        acceleration = longForce / mass;
        velocity += acceleration * Time.deltaTime;


        ////// lateral forces

        steerAngle = -steerInput * maxSteerAngle;

        float handbrake = Input.GetKey(KeyCode.Space) ? 1.5f : 1;
        bodyDir = rotateVec2(bodyDir, steerAngle * 2*(float)Math.Log(velocity.magnitude + 1) * handbrake * Time.deltaTime);

        float latFriction = Input.GetKey(KeyCode.Space) ? maxLatFrictionHandbrake : maxLatFriction;
        velocity = Vector2.Lerp(velocity.normalized, bodyDir, latFriction * Time.deltaTime) * velocity.magnitude;




        Func<Vector2, Vector3> Vec2to3 = v => new Vector3(v.x, v.y, 0);
        transform.position += Vec2to3(velocity * Time.deltaTime);









        ////// update visual rotation of the car and front wheels

        transform.SetLocalPositionAndRotation(transform.position, Quaternion.FromToRotation(Vector3.up, Vec2to3(bodyDir)));
        Debug.DrawRay(transform.position, bodyDir * 2, Color.yellow);

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


    }

}
