using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BoxCastUtils;

[RequireComponent(typeof(BoxCollider))] // Collider is necessary for custom collision detection
[RequireComponent(typeof(Rigidbody))] // Rigidbody is necessary to ignore certain colliders for portals
public class PlayerMovement : MonoBehaviour
{
    [Header("Set In Editor")]
    public LayerMask collideableLayers;

    [Header("Debugging properties")]
    [Tooltip("Red line is current velocity, blue is the new direction")]
    public bool showDebugGizmos = false;
    //The velocity applied at the end of every physics frame
    public Vector3 newVelocity;

    [SerializeField]
    private bool grounded;
    [SerializeField]
    private bool wasGrounded;
    [SerializeField]
    private bool crouching;

    private BoxCollider myCollider;
    private CameraMove cameraMove;
    private Level currentLevel;

    private void Awake()
    {
        newVelocity = Vector3.zero;
    }

    private void Start()
    {
        myCollider = GetComponent<BoxCollider>();
        cameraMove = GetComponent<CameraMove>();
        currentLevel = GameManager.GetCurrentLevel();
    }

    private void FixedUpdate()
    {
        CheckCrouch();
        ApplyGravity();
        //CheckGrounded();
        //FixCeiling();

        CheckJump();

        var inputVector = GetWorldSpaceInputVector();
        var wishDir = inputVector.normalized;
        var wishSpeed = inputVector.magnitude;

        if (grounded)
        {
            if (IsPlayerWalkingBackwards())
            {
                wishSpeed *= PlayerConstants.BackWardsMoveSpeedScale;
            }
            ApplyFriction();
            ApplyGroundAcceleration(wishDir, wishSpeed, PlayerConstants.NormalSurfaceFriction);
            ClampVelocity(PlayerConstants.MoveSpeed);
        }
        else
        {
            ApplyAirAcceleration(wishDir, wishSpeed);
        }

        ClampVelocity(PlayerConstants.MaxVelocity);
        ContinuousCollisionDetection();
    }

    private void CheckCrouch()
    {
        if (InputManager.GetKey(PlayerConstants.Crouch))
        {
            crouching = true;
        }
        else
        {
            if (crouching)
            {
                crouching = CheckAbove(0.8f);
            }
            else
            {
                crouching = false;
            }
        }

        // Update player collider
        float endHeight = crouching ? PlayerConstants.CrouchingPlayerHeight : PlayerConstants.StandingPlayerHeight;
        float velocity = 0;
        float height = Mathf.SmoothDamp(myCollider.size.y, endHeight, ref velocity, Time.deltaTime);

        myCollider.size = new Vector3(myCollider.size.x, height, myCollider.size.z);

        DampenCamera();

    }

    private void DampenCamera()
    {
        Vector3 endOffset = crouching ? PlayerConstants.CrouchingCameraOffset : PlayerConstants.StandingCameraOffset;
        Vector3 currentOffset = cameraMove.playerCamera.transform.localPosition;
        float v = 0;
        float yOffset = Mathf.SmoothDamp(currentOffset.y, endOffset.y, ref v, Time.deltaTime);
        Vector3 newOffset = new Vector3(0, yOffset, 0);
        cameraMove.playerCamera.transform.localPosition = newOffset;
    }

    private void ApplyGravity()
    {
        if (!grounded && newVelocity.y > -PlayerConstants.MaxFallSpeed)
        {
            float gravityScale = currentLevel.gravityMultiplier;
            newVelocity.y -= gravityScale * PlayerConstants.Gravity * Time.fixedDeltaTime;
        }
    }

    private void FixCeiling()
    {
        if (CheckAbove() && newVelocity.y > 0)
        {
            newVelocity.y = 0;
        }
    }

    private bool CheckAbove(float distanceToCheck = 0.1f)
    {
        Ray[] boxTests = GetRays(Vector3.up);


        foreach (Ray ray in boxTests)
        {
            if (showDebugGizmos)
            {
                //Debug.DrawRay(ray.origin, ray.direction, Color.yellow, 3);
            }
            if (Physics.Raycast(
                ray: ray,
                hitInfo: out RaycastHit hit,
                maxDistance: myCollider.bounds.extents.y + distanceToCheck, // add a small offset to allow the player to find the ground is ResolveCollision() sets us too far away
                layerMask: collideableLayers,
                QueryTriggerInteraction.Ignore))
            {
                if (hit.point.y > transform.position.y)
                {
                    return true;
                }
            }
        }

        return false;


    }

    private Ray[] GetRays(Vector3 direction)
    {
        Vector3 center = myCollider.bounds.center;
        Vector3 frontLeft = myCollider.bounds.center;
        frontLeft.x -= myCollider.bounds.extents.x - PlayerConstants.groundCheckOffset;
        frontLeft.z += myCollider.bounds.extents.z - PlayerConstants.groundCheckOffset;
        Vector3 backLeft = myCollider.bounds.center;
        backLeft.x -= myCollider.bounds.extents.x - PlayerConstants.groundCheckOffset;
        backLeft.z -= myCollider.bounds.extents.z - PlayerConstants.groundCheckOffset;
        Vector3 frontRight = myCollider.bounds.center;
        frontRight.x += myCollider.bounds.extents.x - PlayerConstants.groundCheckOffset;
        frontRight.z -= myCollider.bounds.extents.z - PlayerConstants.groundCheckOffset;
        Vector3 backRight = myCollider.bounds.center;
        backRight.x += myCollider.bounds.extents.x - PlayerConstants.groundCheckOffset;
        backRight.z += myCollider.bounds.extents.z - PlayerConstants.groundCheckOffset;

        Ray ray0 = new Ray(center, direction);
        Ray ray1 = new Ray(frontLeft, direction);
        Ray ray2 = new Ray(backLeft, direction);
        Ray ray3 = new Ray(frontRight, direction);
        Ray ray4 = new Ray(backRight, direction);

        return new Ray[] { ray0, ray1, ray2, ray3, ray4 };
    }

    private void CheckJump()
    {
        if (grounded && InputManager.GetKey(PlayerConstants.Jump))
        {
            newVelocity.y = 0;
            newVelocity.y += crouching ? PlayerConstants.CrouchingJumpPower : PlayerConstants.JumpPower;
            grounded = false;
        }
    }

    private Vector3 GetWorldSpaceInputVector()
    {
        float moveSpeed = crouching ? PlayerConstants.CrouchingMoveSpeed : PlayerConstants.MoveSpeed;

        var inputVelocity = GetInputVelocity(moveSpeed);
        if (inputVelocity.magnitude > moveSpeed)
        {
            inputVelocity *= moveSpeed / inputVelocity.magnitude;
        }

        //Get the velocity vector in world space coordinates, by rotating around the camera's y-axis
        return Quaternion.AngleAxis(cameraMove.playerCamera.transform.rotation.eulerAngles.y, Vector3.up) * inputVelocity;
    }

    private Vector3 GetInputVelocity(float moveSpeed)
    {
        float horizontalSpeed = 0;
        float verticalSpeed = 0;

        if (InputManager.GetKey(PlayerConstants.Left))
        {
            horizontalSpeed = -moveSpeed;
        }

        if (InputManager.GetKey(PlayerConstants.Right))
        {
            horizontalSpeed = moveSpeed;
        }

        if (InputManager.GetKey(PlayerConstants.Back))
        {
            verticalSpeed = -moveSpeed;
        }

        if (InputManager.GetKey(PlayerConstants.Forward))
        {
            verticalSpeed = moveSpeed;
        }

        return new Vector3(horizontalSpeed, 0, verticalSpeed);
    }

    private bool IsPlayerWalkingBackwards()
    {
        Vector3 inputDirection = GetInputVelocity(PlayerConstants.MoveSpeed);

        return inputDirection.z < 0;
    }

    //wishDir: the direction the player wishes to go in the newest frame
    //wishSpeed: the speed the player wishes to go this frame
    private void ApplyGroundAcceleration(Vector3 wishDir, float wishSpeed, float surfaceFriction)
    {
        var currentSpeed = Vector3.Dot(newVelocity, wishDir); //Vector projection of the current velocity onto the new direction
        var speedToAdd = wishSpeed - currentSpeed;

        var acceleration = PlayerConstants.GroundAcceleration * Time.fixedDeltaTime; //acceleration to apply in the newest direction

        if (speedToAdd <= 0)
        {
            return;
        }

        var accelspeed = Mathf.Min(acceleration * wishSpeed * surfaceFriction, speedToAdd);
        newVelocity += accelspeed * wishDir; //add acceleration in the new direction
    }

    //wishDir: the direction the player  wishes to goin the newest frame
    //wishSpeed: the speed the player wishes to go this frame
    private void ApplyAirAcceleration(Vector3 wishDir, float wishSpeed)
    {
        var wishSpd = Mathf.Min(wishSpeed, PlayerConstants.AirAccelerationCap);
        Vector3 xzVelocity = newVelocity;
        xzVelocity.y = 0;
        var currentSpeed = Vector3.Dot(xzVelocity, wishDir);
        var speedToAdd = wishSpd - currentSpeed;

        if (speedToAdd <= 0)
        {
            return;
        }

        var accelspeed = Mathf.Min(speedToAdd, PlayerConstants.AirAcceleration * wishSpeed * Time.fixedDeltaTime);
        var velocityTransformation = accelspeed * wishDir;

        newVelocity += velocityTransformation;
    }

    private void ApplyFriction()
    {
        var speed = newVelocity.magnitude;

        // Don't apply friction if the player isn't moving
        // Clear speed if it's too low to prevent accidental movement
        // Also makes the player's friction feel more snappy
        if (speed < PlayerConstants.MinimumSpeedCutoff)
        {
            newVelocity = Vector3.zero;
            return;
        }

        // Bleed off some speed, but if we have less than the bleed
        //  threshold, bleed the threshold amount.

        var control = (speed < PlayerConstants.StopSpeed) ? PlayerConstants.StopSpeed : speed;

        // Add the amount to the loss amount.
        var lossInSpeed = control * PlayerConstants.Friction * Time.fixedDeltaTime;
        var newSpeed = Mathf.Max(speed - lossInSpeed, 0);

        if (newSpeed != speed)
        {
            newVelocity *= newSpeed / speed; //Scale velocity based on friction
        }
    }

    // This function keeps the player from exceeding a maximum velocity
    private void ClampVelocity(float range)
    {
        if (newVelocity.x >= PlayerConstants.MaxVelocity)
        {
            newVelocity.x = PlayerConstants.MaxVelocity;
        }

        if (newVelocity.x <= -PlayerConstants.MaxVelocity)
        {
            newVelocity.x = -PlayerConstants.MaxVelocity;
        }

        if (newVelocity.y >= PlayerConstants.MaxVelocity)
        {
            newVelocity.y = PlayerConstants.MaxVelocity;
        }

        if (newVelocity.y <= -PlayerConstants.MaxVelocity)
        {
            newVelocity.y = -PlayerConstants.MaxVelocity;
        }

        if (newVelocity.z >= PlayerConstants.MaxVelocity)
        {
            newVelocity.z = PlayerConstants.MaxVelocity;
        }

        if (newVelocity.z <= -PlayerConstants.MaxVelocity)
        {
            newVelocity.z = -PlayerConstants.MaxVelocity;
        }
    }

    // This function is what keeps the player from walking through walls
    // We calculate how far we are inside of an object from moving this frame
    // and move the player just barely outside of the colliding object

    private void ContinuousCollisionDetection()
    {
        StandingGroundCheck();

        // - boxcast the player to the position the player will be in the next frame
        // based on speed, find the point in time where the player hits and object, and it there
        float castDistance = newVelocity.magnitude * Time.fixedDeltaTime;
        RaycastHit[] hits = Physics.BoxCastAll(
            center: myCollider.bounds.center,
            halfExtents: myCollider.bounds.extents,
            direction: newVelocity.normalized,
            orientation: Quaternion.identity,
            maxDistance: castDistance,
            layerMask: collideableLayers);

        List<RaycastHit> validHits = hits
            .ToList()
            .OrderBy(hit => hit.distance)
            .Where(hit => !hit.collider.isTrigger)
            .Where(hit => !Physics.GetIgnoreCollision(hit.collider, myCollider))
            .Where(hit => hit.point != Vector3.zero)
            .ToList();

        if (showDebugGizmos)
        {
            // Show the collider of the player next frame
            DebugUtils.DrawCube(myCollider.bounds.center + (newVelocity * castDistance), myCollider.bounds.extents);
            if (validHits.Count() > 0)
            {
                Debug.Log($"ccd hit : {validHits.First().collider.gameObject.name}");
            }
        }

        // If we are going to hit something, set ourselves just outside of the object and translate momentum along the wall
        if (validHits.Count() > 0)
        {
            RaycastHit closestHit = validHits.First();
            float timeToImpact;
            if (closestHit.distance > 0 && newVelocity.magnitude == 0)
            {
                //Prevent TimeToImpact being infinity
                timeToImpact = Time.fixedDeltaTime;
            }
            else
            {
                // find the time at which we would have hit the wall between this and the next frame
                timeToImpact = closestHit.distance / newVelocity.magnitude;
            }

            // slide along the wall and prevent a complete loss of momentum
            ClipVelocity(closestHit.normal);
            // set our position to just outside of the wall
            transform.position += newVelocity * timeToImpact;

            //StepMove();
        }
        else
        {
            transform.position += newVelocity * Time.fixedDeltaTime;
            StayOnGround();
        }
        wasGrounded = grounded;
    }

    private void StayOnGround()
    {
        // TODO : fix slope check
        SourceBoxCastOutput castOutput;
        Vector3 start = myCollider.bounds.center;
        start.y += PlayerConstants.StepOffset;
        Vector3 end = myCollider.bounds.center;
        end.y -= PlayerConstants.StepOffset;

        // See how far up we can go without getting stuck
        SourceBoxCast(new SourceBoxCastInput(myCollider.bounds.center, start, collideableLayers, myCollider), out castOutput);
        start = castOutput.endPosition;

        // Now trace down from a known safe position
        SourceBoxCast(new SourceBoxCastInput(start, end, collideableLayers, myCollider), out castOutput);

        if (castOutput.fraction > 0 &&   // Must go somewhere
            castOutput.fraction < 1 &&  // Must hit something
            castOutput.normal.y > 0.7f) // can't hit a steep slope
        {
            float zDelta = Mathf.Abs(transform.position.z - castOutput.endPosition.z);

            //if( zDelta > 0.01f)
            //{
            transform.position = castOutput.endPosition;
            //}
        }
    }

    private void StepMove()
    {

    }

    private void StandingGroundCheck()
    {
        if (newVelocity.y > 3)
        {
            grounded = false;
            return;
        }

        float castDistance = Mathf.Abs(newVelocity.y * Time.fixedDeltaTime);

        RaycastHit[] hits = Physics.BoxCastAll(
            center: myCollider.bounds.center,
            halfExtents: myCollider.bounds.extents,
            direction: Vector3.down,
            orientation: Quaternion.identity,
            maxDistance: castDistance,
            layerMask: collideableLayers);

        List<RaycastHit> validHits = hits
            .ToList()
            .OrderBy(hit => hit.distance)
            .Where(hit => !hit.collider.isTrigger)
            .Where(hit => !Physics.GetIgnoreCollision(hit.collider, myCollider))
            //.Where(hit => hit.point != Vector3.zero)
            .ToList();

        if (showDebugGizmos)
        {
            //Debug.Log($"pos: {transform.position} vel: {newVelocity} dist: {castDistance} hits: {hits.Count()}, valid: {validHits.Count}");
        }

        CheckGrounded(validHits);
    }

    private void CheckGrounded(List<RaycastHit> validHits)
    {
        bool shouldBeGrounded = false;

        foreach (RaycastHit hit in validHits)
        {
            // If the slope is less than 45 degrees
            if (hit.normal.y > 0.7f)
            {
                shouldBeGrounded = true;

                if (shouldBeGrounded && newVelocity.y < 0)
                {
                    ClipVelocity(hit.normal);
                    newVelocity.y = 0;
                }
            }
            else
            {
                Debug.Log($"can't step on {hit.collider.gameObject.name}");
            }
        }

        grounded = shouldBeGrounded;
    }

    //Slide off of the impacting surface
    private void ClipVelocity(Vector3 normal)
    {
        // Keep from continuously bouncing, let friction handle stopping
        //if(newVelocity.magnitude < 1)
        //{
        //    return;
        //}

        // Determine how far along plane to slide based on incoming direction.
        var backoff = Vector3.Dot(newVelocity, normal) * PlayerConstants.Overbounce;

        var change = normal * backoff;
        //change.y = 0; // only affect horizontal velocity
        newVelocity -= change;
    }
}