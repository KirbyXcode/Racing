using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour 
{
    public WheelCollider wheelFR;
    public WheelCollider wheelFL;
    public WheelCollider wheelRR;
    public WheelCollider wheelRL;

    public Transform wheelFRTrans;
    public Transform wheelFLTrans;
    public Transform wheelRRTrans;
    public Transform wheelRLTrans;

    public float torque = 50f; //力矩
    //public float angle = 10f;  //旋转角度
    public float maxSpeed = 30f;
    public float curSpeed;

    public float maxAngle = 20f;
    public float minAngle = 1f;
    private float angleFactor = 30f;

    public float brakeTorque = 100f;
    private bool braked = false;

    public float slipForwardStiff = 0.04f;
    public float slipSteerStiff = 0.025f;
    private float initForwardStiff;
    private float initSteerStiff;

    private float originalRotationFL = 0f;
    private float originalRotationFR = 0f;

    private float[] gearLevelSpeed;
    private int gearCount = 4;
    public float minGearSoundPitch = 1.0f;
    public float maxGearSoundPitch = 3.0f;
    
    private Rigidbody rb;
    private AudioSource engineAudio;
    private AudioSource brakeAudio;

	void Start () 
	{
        //降低车体重心
        rb = GetComponent<Rigidbody>();
        Vector3 temp = rb.centerOfMass;
        temp.y -= 0.8f;
        rb.centerOfMass = temp;

        engineAudio  = GetComponent<AudioSource>();
        brakeAudio = transform.Find("Body").GetComponent<AudioSource>();

        initForwardStiff = wheelRL.forwardFriction.stiffness;
        initSteerStiff = wheelRL.sidewaysFriction.stiffness;

        SeperateGeerLevelSpeed();
	}

    private void SeperateGeerLevelSpeed()
    {
        float gearRange = maxSpeed / gearCount;
        gearLevelSpeed = new float[gearCount + 1];

        for (int i = 0; i < gearLevelSpeed.Length; i++)
        {
            gearLevelSpeed[i] = gearRange * i;
        }
    }

    private void Movement(float hor, float ver)
    {
        //限速
        curSpeed = rb.velocity.magnitude; //最高23左右
        if (curSpeed < maxSpeed)
        {
            wheelRR.motorTorque = torque * ver;
            wheelRL.motorTorque = torque * ver;
        }

        //根据速度快慢限制前轮旋转角度
        float factor = rb.velocity.magnitude / angleFactor;
        float angle = Mathf.Lerp(maxAngle, minAngle, factor);

        wheelFL.steerAngle = angle * hor;
        wheelFR.steerAngle = angle * hor;
    }

    private void WheelRearRotate(Transform wheelTrans, WheelCollider wheelCollider)
    {
        wheelTrans.Rotate(wheelCollider.rpm * 360 * Time.deltaTime / 60, 0, 0); // rpm = round per minute
    }

    private void WheelFrontRotate(Transform wheelTrans, float originalRotation, WheelCollider wheelCollider)
    {
        originalRotation += wheelCollider.rpm * 360 * Time.deltaTime / 60;
        originalRotation = Mathf.Repeat(originalRotation, 360);
        Vector3 eularAngle = new Vector3(originalRotation, wheelCollider.steerAngle, 0);
        wheelTrans.localRotation = Quaternion.Euler(eularAngle);
    }

    private void SetWheelPos(Transform wheelTrans, WheelCollider wheelCollider)
    {
        RaycastHit hit;
        bool isGounded = Physics.Raycast(wheelCollider.transform.position, -wheelCollider.transform.up, out hit, 
            wheelCollider.radius + wheelCollider.suspensionDistance);
        if(isGounded)
        {
            if ((hit.point - wheelCollider.transform.position).sqrMagnitude < Mathf.Pow(wheelCollider.radius, 2))
            {
                wheelTrans.position = wheelCollider.transform.position;
            }
            else
            {
                wheelTrans.position = hit.point + wheelCollider.transform.up * wheelCollider.radius;
            }
        }
        else
        {
            wheelTrans.position = wheelTrans.position - wheelCollider.transform.up * wheelCollider.suspensionDistance;
        }
    }

    private void IsBrake()
    {
        if (Input.GetButton("Jump"))
            braked = true;
        if (Input.GetKeyUp(KeyCode.Space))
            braked = false;
    }

    private void ExecuteBrake(bool braked)
    {
        if(braked)
        {
            wheelFL.brakeTorque = brakeTorque;
            wheelFR.brakeTorque = brakeTorque;

            wheelRL.motorTorque = 0;
            wheelRR.motorTorque = 0;

            SetWheelFrictionStiff(wheelRL, slipForwardStiff, slipSteerStiff);
            SetWheelFrictionStiff(wheelRR, slipForwardStiff, slipSteerStiff);

            if (!brakeAudio.isPlaying && curSpeed > 0.2f) 
                brakeAudio.Play();
        }
        else
        {
            wheelFL.brakeTorque = 0;
            wheelFR.brakeTorque = 0;

            SetWheelFrictionStiff(wheelRL, initForwardStiff, initSteerStiff);
            SetWheelFrictionStiff(wheelRR, initForwardStiff, initSteerStiff);

            brakeAudio.Stop();
        }
    }

    private void SetWheelFrictionStiff(WheelCollider wheelColider, float slipForwardStiff, float slipSteerStiff)
    {
        WheelFrictionCurve curveTemp = wheelColider.forwardFriction;
        curveTemp.stiffness = slipForwardStiff;
        wheelColider.forwardFriction = curveTemp;

        curveTemp = wheelColider.sidewaysFriction;
        curveTemp.stiffness = slipSteerStiff;
        wheelColider.sidewaysFriction = curveTemp;
    }

    private void EngineSound(float curSpeed)
    {
        float gearMinSpeed = 0;
        float gearMaxSpeed = maxSpeed;

        for (int i = 0; i < gearLevelSpeed.Length - 1; i++)
        {
            if (curSpeed >= gearLevelSpeed[i] && curSpeed < gearLevelSpeed[i + 1]) 
            {
                gearMinSpeed = gearLevelSpeed[i];
                gearMaxSpeed = gearLevelSpeed[i+1];
                break;
            }
        }
        float ratio = (curSpeed - gearMinSpeed) / (gearMaxSpeed-gearMinSpeed);
        ratio = ratio > 1 ? 1 : ratio;
        engineAudio.pitch = minGearSoundPitch + ratio * (maxGearSoundPitch - minGearSoundPitch);
    }

	void FixedUpdate () 
	{
        Movement(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
	}

    void Update()
    {
        WheelFrontRotate(wheelFRTrans, originalRotationFR, wheelFR);
        WheelFrontRotate(wheelFLTrans, originalRotationFL, wheelFL);
        WheelRearRotate(wheelRRTrans, wheelRR);
        WheelRearRotate(wheelRLTrans, wheelRL);

        SetWheelPos(wheelFRTrans, wheelFR);
        SetWheelPos(wheelFLTrans, wheelFL);
        SetWheelPos(wheelRRTrans, wheelRR);
        SetWheelPos(wheelRLTrans, wheelRL);

        IsBrake();
        ExecuteBrake(braked);

        EngineSound(curSpeed);
    }
}
