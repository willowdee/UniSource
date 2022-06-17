using UnityEngine;
public class PlayerController : MonoBehaviour
{
    CapsuleCollider collider;
	Transform cameraTransform;
	Rigidbody playerRigidbody;
	public float xMouseSensitivity = 400f;
	public float yMouseSensitivity = 400f;
    public bool lockCursor = true;
    bool disabled = false;
	public float jumpSpeed = 8f;
	public float cameraYOffset = 0.8f;
	public float friction = 8f;
	public float groundAcceleration = 100f;
	public float groundMaxVelocity = 6f;
	public float airAcceleration = 100f;
	public float airMaxVelocity = 6f;
	public float airControl = 1f;

	float m_yaw = 0.022f;
	float m_pitch = 0.022f;
	float rotX;
	float rotY;

	bool jumping;
	bool isGrounded;

	Vector3 cameraOffsetVector;
	Vector3 moveDir = Vector3.zero;
	Vector3 moveDirTemp = Vector3.zero;

	void Start ()
	{
		cameraTransform = gameObject.GetComponentInChildren<Camera> ().transform;
		cameraOffsetVector = new Vector3 (0, cameraYOffset, 0);
		cameraTransform.position = transform.position + cameraOffsetVector;
		playerRigidbody = gameObject.GetComponent<Rigidbody>();
        collider = gameObject.GetComponent<CapsuleCollider>();
	}

	void Update ()
	{
        // Lock / Unlock the cursor
        if(lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if(Cursor.lockState == CursorLockMode.Locked || Cursor.visible == false)
        {
            disabled = false;
        }
        else
        {
            disabled = true;
        }

        if (!disabled)
        {
            // Camera rotation
            rotY += Input.GetAxisRaw("Mouse X") * xMouseSensitivity * m_yaw;
            rotX -= Input.GetAxisRaw("Mouse Y") * yMouseSensitivity * m_pitch;
            // Clamp vertical rotation
            if (rotX < -90)
            {
                rotX = -90;
            }
            else if (rotX > 90)
            {
                rotX = 90;
            }
            // Clamp horizontal rotation
            if (rotY < -360 || rotY > 360)
            {
                rotY = rotY % 360;
            }
            this.transform.rotation = Quaternion.Euler(0, rotY, 0);
            cameraTransform.rotation = Quaternion.Euler(rotX, rotY, 0);

            // Movement input
            float inputX = Input.GetAxisRaw("Horizontal");
            float inputY = Input.GetAxisRaw("Vertical");
            moveDir = new Vector3(inputX, 0, inputY).normalized;
            moveDir = transform.TransformDirection(moveDir);

            CheckGround();
            if (!isGrounded || jumping)
            {
                moveDir = MoveAir(moveDir, playerRigidbody.velocity);
            }
            else if (isGrounded && !jumping)
            {
                moveDir = MoveGround(moveDir, playerRigidbody.velocity);
            }
            // Jump
            if (isGrounded)
            {
                jumping = false;
            }
            if (Input.GetButton("Jump") && isGrounded)
            {
                jumping = true;
                moveDir.y = jumpSpeed;
            }
            else
            {
                moveDir.y = playerRigidbody.velocity.y;
            }
            playerRigidbody.velocity = moveDir;
        }
	}

	private Vector3 Accelerate (Vector3 accelDir, Vector3 prevVelocity, float accelerate, float max_velocity)
	{
		float projVel = Vector3.Dot (prevVelocity, accelDir);
		float accelVel = accelerate * Time.fixedDeltaTime;

		if (projVel + accelVel > max_velocity)
		{
			accelVel = max_velocity - projVel;
		}

		return prevVelocity + accelDir * accelVel;
	}

	private Vector3 AirAccelerate (Vector3 wishDir, Vector3 prevVelocity, float accelerate, float max_velocity)
	{
		if (wishDir.magnitude == 0)
		{
			return prevVelocity;
		}

		float projVel = Vector3.Dot (prevVelocity, wishDir);
		float accelVel = accelerate * Time.fixedDeltaTime;

		if (projVel < 0)
		{
			prevVelocity += wishDir * accelVel;

			float ySpeed = prevVelocity.y;
			prevVelocity.y = 0;
			float speed = prevVelocity.magnitude;
			prevVelocity.Normalize ();
			projVel = Vector3.Dot (prevVelocity, wishDir);
			float k = 32;
			k *= airControl * projVel * projVel * Time.deltaTime;
			// Clamp speed
			if (speed > max_velocity)
			{
				speed *= max_velocity / speed;
			}

			prevVelocity.x = prevVelocity.x * speed + wishDir.x * k;
			prevVelocity.y = prevVelocity.y * speed + wishDir.y * k;
			prevVelocity.z = prevVelocity.z * speed + wishDir.z * k;

			prevVelocity.Normalize ();
			prevVelocity.x *= speed;
			prevVelocity.y = ySpeed;
			prevVelocity.z *= speed;

		}
		else if (projVel >= 0)
		{
			if (Mathf.Sqrt (Mathf.Pow (prevVelocity.x, 2) + Mathf.Pow (prevVelocity.z, 2)) < 2f)
			{
				prevVelocity += wishDir * 2 * Time.fixedDeltaTime;
			}
		}

		return prevVelocity;

	}

	private Vector3 MoveGround (Vector3 accelDir, Vector3 prevVelocity)
	{
		// Apply Friction
		float speed = prevVelocity.magnitude;
		if (speed != 0 && isGrounded) // To avoid divide by zero errors
		{
			float drop = speed * friction * Time.fixedDeltaTime;
			prevVelocity *= Mathf.Max (speed - drop, 0) / speed; // Scale the velocity based on friction.
		}

		return Accelerate (accelDir, prevVelocity, groundAcceleration, groundMaxVelocity);
	}

	private Vector3 MoveAir (Vector3 accelDir, Vector3 prevVelocity)
	{
		return AirAccelerate (accelDir, prevVelocity, airAcceleration, airMaxVelocity);
	}
	void CheckGround()
    {
        RaycastHit hit;
        if(Physics.SphereCast(transform.position, collider.radius, Vector3.down, out hit, collider.height / 2 + 0.1f))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }
}