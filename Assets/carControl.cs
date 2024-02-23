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

    public float gearRatio = 0;
    public float engineForce = 0;


    private float accelBrakeInput = 0;
    private float steerInput = 0;


    public float constDrag = 1f;
    public float constRollingResistance = 5;
    public float constBraking = 7000;
    public float constMaxEngineTorque = 2000;

    float wheelBase = 2;
    float maxSteerAngle = 50;

    public const float mass = 1000;

    
    Vector2 rotateVec2(Vector2 v, float a)
    {
        return Quaternion.AngleAxis(a, Vector3.forward) * v;
    }


    void Update()
    {

        accelBrakeInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");

        float steerAngle = -steerInput * maxSteerAngle;

        // longitudinal (forward/back) forces

        gearRatio = Mathf.Clamp(4 / velocity.magnitude, 0.2f, 2); // simulating changing up gears as speed increases (effectively a continuously variable trans) // TODO change to linear model dumbass
        engineForce = constMaxEngineTorque * gearRatio * Mathf.Max(0, accelBrakeInput);
        Vector2 tractiveForce =  engineForce * bodyDir;

        Vector2 brakingForce = -constBraking * Mathf.Max(0, -accelBrakeInput) * velocity.normalized; // TODO - braking should only apply to the component of velocity in the direction of the car's body

        Vector2 dragForce = -constDrag * velocity * velocity.magnitude;
        Vector2 rollingResistanceForce = -constRollingResistance * velocity;

        //float weightOnFrontWheels = 0.5f*mass*10 - 0.2*mass*() 

        Vector2 longForce = tractiveForce + brakingForce + dragForce + rollingResistanceForce;


        float angularVelocity = velocity.magnitude * Mathf.Sin(steerAngle * Mathf.Deg2Rad) * Mathf.Rad2Deg / wheelBase;

        bodyDir = rotateVec2(bodyDir, angularVelocity * Time.deltaTime);
        velocity = rotateVec2(velocity, angularVelocity * Time.deltaTime);
        transform.Rotate(Vector3.forward, angularVelocity * Time.deltaTime);


        //float slipAngle = Mathf.Acos(Vector2.Dot(velocity, bodyDir) / (velocity.magnitude * bodyDir.magnitude));


        acceleration = longForce / mass;

        velocity += acceleration * Time.deltaTime;

        Func<Vector2, Vector3> Vec2to3 = v => new Vector3(v.x, v.y, 0);
        transform.position += Vec2to3(velocity * Time.deltaTime);


    }

    void FixedUpdate() {


    }
}
