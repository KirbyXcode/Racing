using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour 
{
    public GameObject target;
    public float distance = 7;
    public float heightDiff = 3;

    public float angleDamping = 1.5f;
    public float heightDamping = 1.0f;

    public float defaultFOV = 60f;
    public float zoomRatio = 1.2f;

    private float dstAngleWithDirection = 0f;

    private Rigidbody carRB;
    private Camera camera;

	void Start () 
	{
        if (target != null) 
            carRB = target.GetComponent<Rigidbody>();

        camera = transform.GetComponent<Camera>();
	}

	void Update () 
	{
        float camHeight = transform.position.y;
        float dstHeight = target.transform.position.y + heightDiff;
        float retHeight = Mathf.Lerp(camHeight, dstHeight, heightDamping * Time.deltaTime);

        float camAngle = transform.eulerAngles.y;
        float dstAngle = dstAngleWithDirection;
        float retAngle = Mathf.LerpAngle(camAngle, dstAngle, angleDamping * Time.deltaTime);
        Quaternion retRotation = Quaternion.Euler(0, retAngle, 0);

        transform.position = target.transform.position;
        transform.position -= retRotation * Vector3.forward * distance;

        Vector3 temp = transform.position;
        temp.y = retHeight;
        transform.position = temp;
          
        transform.LookAt(target.transform);
	}

    void FixedUpdate()
    {
        Vector3 vel = carRB.velocity;
        Vector3 faceDirection = target.transform.forward;
        float dot = Vector3.Dot(vel, faceDirection);

        //Vector3 carLocalVel = target.transform.InverseTransformDirection(vel);

        float dstAngleY = target.transform.eulerAngles.y;
        //if (carLocalVel.z < -0.01f) ;
        if (dot < -0.01f)
            dstAngleWithDirection = dstAngleY + 180;
        else
            dstAngleWithDirection = dstAngleY;

        float speed = vel.magnitude;
        camera.fieldOfView = defaultFOV + speed * zoomRatio;
    }
}
