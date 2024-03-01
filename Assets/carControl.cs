using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarControl : MonoBehaviour
{
    //public Vector2 velocity { get; private set; } = Vector2.zero;
    public Vector2 velocity = Vector2.zero; // for debugging only
    private Vector2 acceleration = Vector2.zero;
    public Vector2 bodyDir = Vector2.up; // direction of the body of the car
    public float angularVelocity = 0; // in degrees/s


    public float mass = 1000;
    public float inertia = 10;

    public float engineForce = 0;


    private float accelBrakeInput = 0;
    private float steerInput = 0;



    public float maxSpeed = 15;
    public float maxEngineTorque = 6000;
    public AnimationCurve enginePower;


    public float maxLongFriction = 500;

    public float braking = 10000;


    float wheelBase = 2;
    public float maxSteerAngle = 25;



    public float steerAngle = 0;

    public AnimationCurve latForceBySlipAngle;
    public AnimationCurve rearLatForceBySlipAngle;
    public float maxLatFriction = 2000;

    public float k = 1;
    public float b = 1;

    public float corneringStiffness = 10f;
    public float latFriction = 0.95f;

    public float rearSlipAngle = 0;
    public float rearLatForce = 0;
    
    public float frontSlipAngle = 0;
    public float frontLatForce = 0;


    public Transform wheelFrontLeft;
    public Transform wheelFrontRight;
    public Transform wheelBackLeft;
    public Transform wheelBackRight;
        
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


        Keyframe[] engineKeyFrames =
        {
            new Keyframe(0, 0.5f, 4, 4, 0, 0),
            new Keyframe(0.25f, 0.5f, 4, 4, 0, 0),
            new Keyframe(0, 0.5f, 4, 4, 0, 0),
        };


        wheelFrontLeft = transform.Find("wheelFrontLeft");
        wheelFrontRight = transform.Find("wheelFrontRight");
        wheelBackLeft = transform.Find("wheelBackLeft");
        wheelBackRight = transform.Find("wheelBackRight");

        wheelBase = Mathf.Abs(wheelFrontLeft.localPosition.y - wheelBackLeft.localPosition.y);
    }

    void Update()
    {

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




        ////// lateral forces

        steerAngle = -steerInput * maxSteerAngle;



        //Vector2 frontWheelDir = rotateVec2(bodyDir, steerAngle).normalized;
        //velocity = velocity.magnitude * frontWheelDir.normalized;


        //float vLong = Vector2.Dot(velocity, bodyDir); // component of velocity in the direction of the car body
        //float vLat = Vector2.Dot(velocity, rotateVec2(bodyDir, 90));
        //float torque = corneringStiffness2 * steerAngle - latFriction * angularVelocity;
        //torque *= Mathf.Atan2(vLat, vLong);

        //angularVelocity += torque * Time.deltaTime;
        //bodyDir = rotateVec2(bodyDir, angularVelocity * Time.deltaTime);




        //float angularVelocity = velocity.magnitude * Mathf.Sin(steerAngle * Mathf.Deg2Rad) * Mathf.Rad2Deg / wheelBase;
        //bodyDir = rotateVec2(bodyDir, angularVelocity * Time.deltaTime);

        ////Vector2 frontWheelDir = rotateVec2(bodyDir, steerAngle).normalized;
        //velocity = velocity.magnitude * bodyDir.normalized;





        //float slipAngle = Mathf.Acos(Vector2.Dot(velocity, bodyDir) / (velocity.magnitude * bodyDir.magnitude));

        float vLong = Vector2.Dot(velocity, bodyDir); // component of velocity in the direction of the car body
        float vLat = Vector2.Dot(velocity, rotateVec2(bodyDir, 90)); // component of velocity in the direction perpendicular to the car body

        //Vector2 angularVelocityComp = 
        float handbrakeMultiplier = Input.GetKey(KeyCode.Space) ? 0 : 1;
        rearSlipAngle = Mathf.Rad2Deg * Mathf.Atan2(vLat - k * angularVelocity * Mathf.Deg2Rad * wheelBase / 2, handbrakeMultiplier * Mathf.Abs(vLong)); 
        frontSlipAngle = Mathf.Rad2Deg * Mathf.Atan2(vLat + k * angularVelocity * Mathf.Deg2Rad * wheelBase / 2, Mathf.Abs(vLong)) - steerAngle * Mathf.Sign(vLong);

        rearLatForce = -b * rearLatForceBySlipAngle.Evaluate(Mathf.Abs(rearSlipAngle)) * maxLatFriction * Mathf.Sign(rearSlipAngle);
        frontLatForce = -latForceBySlipAngle.Evaluate(Mathf.Abs(frontSlipAngle)) * maxLatFriction * Mathf.Sign(frontSlipAngle);
        //rearLatForce = -corneringStiffness * rearSlipAngle;
        //frontLatForce = -corneringStiffness * frontSlipAngle;

        Debug.DrawRay(wheelBackRight.transform.position, rearLatForce / maxLatFriction * rotateVec2(bodyDir, 90), Color.blue);
        Debug.DrawRay(wheelFrontRight.transform.position, frontLatForce / maxLatFriction * rotateVec2(bodyDir, 90), Color.blue);



        Vector2 latForce = (rearLatForce + Mathf.Cos(steerAngle * Mathf.Deg2Rad) * frontLatForce) * rotateVec2(bodyDir, 90);

        float torque = -rearLatForce * wheelBase / 2 + Mathf.Cos(steerAngle * Mathf.Deg2Rad) * frontLatForce * wheelBase / 2;
        angularVelocity += Time.deltaTime * torque / inertia;
        bodyDir = rotateVec2(bodyDir, angularVelocity * Time.deltaTime);


        acceleration = (longForce + latForce) / mass;



        //acceleration = longForce / mass;
        velocity += acceleration * Time.deltaTime;

        Func<Vector2, Vector3> Vec2to3 = v => new Vector3(v.x, v.y, 0);
        transform.position += Vec2to3(velocity * Time.deltaTime);



        ////// update visual rotation of the car and front wheels

        transform.Rotate(Vector3.forward, angularVelocity * Time.deltaTime);
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
