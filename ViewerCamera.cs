using Godot;
using Godot.Collections;
using System.Linq;
using OverEasy;
using System.Diagnostics;

public partial class ViewerCamera : Camera3D
{
	public enum CameraMode : int
	{
		Orbit = 0,
		Freecam = 1,
	}

	/// <summary>
	/// Multiplier for zooming in and out with a mouse
	/// </summary>
	
	public static double SCROLL_SPEED = 10;
	/// <summary>
	/// Multiplier for zooming and out with a touch screen
	/// </summary>
	
	public static double ZOOM_SPEED = 5;
	/// <summary>
	/// Multiplier for spinning the camera on the roll axis
	/// </summary>
	
	public static double SPIN_SPEED = 10;
	/// <summary>
	/// Initial distance in meters the camera should distance itself from the target
	/// </summary>
	
	public static double DEFAULT_DISTANCE = 20;
	/// <summary>
	/// Multiplier for rotating the camera on the X axis
	/// </summary>
	
	public static double ROTATE_SPEED_X = 40;
	/// <summary>
	/// Multiplier for rotating the camera on the Y axis
	/// </summary>
	
	public static double ROTATE_SPEED_Y = 40;
	/// <summary>
	/// Multiplier for rotating the camera via touch screen dragging
	/// </summary>
	
	public static double TOUCH_ZOOM_SPEED = 40;
	/// <summary>
	/// Multiplier for camera position fast movement
	/// </summary>
	
	public static double SHIFT_MULTIPLIER = 2.5;
	/// <summary>
	/// Multiplier for camera position slow movemnet
	/// </summary>
	
	public static double CTRL_MULTIPLIER = 0.4;
	/// <summary>
	/// Acceleration multiplier for the freecam
	/// </summary>
	
	public static double FREECAM_ACCELERATION = 30;
	/// <summary>
	/// Deceleration multiplier for the freecam
	/// </summary>
	
	public static double FREECAM_DECELERATION = -10;
	/// <summary>
	/// Velocity multiplier for the freecam
	/// </summary>
	
	public static double FREECAM_VELOCITY_MULTIPLIER = 4;

	//Common params
	/// <summary>
	/// Sensitivity when rotating horizontally
	/// </summary>
	[Export(PropertyHint.Range, "0.0,1.0,")]
	public double sensitivityX = 0.25;
	/// <summary>
	/// Sensitivity when rotating vertically
	/// </summary>
	[Export(PropertyHint.Range, "0.0,1.0,")]
	public double sensitivityY = 0.25;
	/// <summary>
	/// Boolean to decide if camera should invert mouse vertical rotation 
	/// </summary>
	
	public static bool INVERT_MOUSE_VERTICAL_ROTATION = false;
	/// <summary>
	/// Boolean to decide if camera should invert mouse horizontal rotation 
	/// </summary>
	
	public static bool INVERT_MOUSE_HORIZONTAL_ROTATION = false;
	/// <summary>
	/// Boolean to decide if camera should invert gamepad vertical rotation 
	/// </summary>
	
	public static bool INVERT_GAMEPAD_VERTICAL_ROTATION = false;
	/// <summary>
	/// Boolean to decide if camera should invert gamepad horizontal rotation 
	/// </summary>
	
	public static bool INVERT_GAMEPAD_HORIZONTAL_ROTATION = false;
	/// <summary>
	/// Boolean to decide if camera should invert touch event rotation 
	/// </summary>
	
	public static bool INVERT_TOUCH_ROTATION = false;
	/// <summary>
	/// Target node
	/// </summary>
	[Export]
	public Node3D targetNode;

	//Active variables
	/// <summary>
	/// Current camera mode
	/// </summary>
	public CameraMode cameraMode = CameraMode.Freecam;
	/// <summary>
	/// Current mouse movement speed during a right click
	/// </summary>
	public Vector2 mouseRightClickMoveSpeed = new Vector2();
	/// <summary>
	/// Current mouse movement speed 
	/// </summary>
	public Vector2 mouseMoveSpeed = new Vector2();
	/// <summary>
	/// Current scrolling speed. Can be from mouse scroll wheel, touchscreen zoom, etc.
	/// </summary>
	public double scrollSpeed = 0;
	/// <summary>
	/// Current direction the freecam is facing
	/// </summary>
	public Vector3 freeCamDirection = new Vector3();
	/// <summary>
	/// Current velocity the freecam applies
	/// </summary>
	public Vector3 freeCamVelocity = new Vector3();
	/// <summary>
	/// Current pitch for the freecam
	/// </summary>
	public double freecamTotalPitch = 0;
	/// <summary>
	/// Dictionary containing info on recorded touch input
	/// </summary>
	public Dictionary<int, Vector2> touchDictionary = new Dictionary<int, Vector2>();
	/// <summary>
	/// Previous touch rotation event distance calculation
	/// </summary>
	public double oldTouchDistance = 0;
	/// <summary>
	/// The object which the orbit cam will focus on
	/// </summary>
	public Node3D orbitFocusNode = null;
	/// <summary>
	/// Working value for orbit rotation
	/// </summary>
	public Vector3 _orbitRotation = new Vector3();
	/// <summary>
	/// Working value for current orbit camera distance
	/// </summary>
	public double _distance = 0;

	/// <summary>
	/// A determinant for if we're currently draggging an item with the mouse, such as a transform gizmo
	/// </summary>
	public bool isDragging = false;

	/// <summary>
	/// A tracker for if we've started attempting to drag by holding the mouse.
	/// </summary>
    public bool startedDragging = false;

	/// <summary>
	/// A holding object for tracking where the drag area began
	/// </summary>
	public Vector2 dragStart = new Vector2(-1, -1);

	/// <summary>
	/// Default value for drag start
	/// </summary>
	public Vector2 DragStartDefault = new Vector2(-1, -1);

    /// <summary>
    /// Base value for scaling the transformation gizmo
    /// </summary>
    public float FixedGizmoSize = .0025f;

	/// <summary>
	/// Making this much higher causes odd consistency issues and overall problems
	/// </summary>
	public const int MouseRayCastLength = 100000;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		_distance = DEFAULT_DISTANCE;
		_orbitRotation = targetNode.Transform.Basis.GetRotationQuaternion().GetEuler();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_ProcessGizmoSize();
		_ProcessTransformation(delta);
	}

	private void _ProcessGizmoSize()
	{
		var gizmoDistance = (this.GlobalPosition - OverEasyGlobals.TransformGizmo.GlobalPosition).Length();
		var size = gizmoDistance * FixedGizmoSize * this.Fov;
		OverEasyGlobals.TransformGizmo.Scale = Vector3.One * size;
	}

	private void _ProcessTransformation(double delta)
	{
		if(OverEasyGlobals.CanAccess3d)
		{
			switch (cameraMode)
			{
				case CameraMode.Orbit:
					_ProcessOrbit(delta);
					break;
				case CameraMode.Freecam:
					_ProcessFreecam(delta);
					break;
			}

			//Reset mouseMoveSpeed so we don't repeat the previous movement info
			mouseRightClickMoveSpeed = new Vector2();
		}
	}

	public void _ProcessOrbit(double delta)
	{
		// Update rotation
		_orbitRotation.X += (float)(-mouseRightClickMoveSpeed.Y * delta * ROTATE_SPEED_X * sensitivityX);
		_orbitRotation.Y += (float)(-mouseRightClickMoveSpeed.X * delta * ROTATE_SPEED_Y * sensitivityY);

		//_rotation.z += _spin_speed * delta

		if (_orbitRotation.X < -Mathf.Pi / 2)
		{
			_orbitRotation.X = -Mathf.Pi / 2;
		}

		if (_orbitRotation.X > Mathf.Pi / 2)
		{
			_orbitRotation.X = Mathf.Pi / 2;
		}

		mouseRightClickMoveSpeed = new Vector2();

		// Update distance
		_distance += scrollSpeed * delta;

		if (_distance < 0)
		{
			_distance = 0;
		}

		scrollSpeed = 0;
		//spinSpeed = 0

		this.SetIdentity();
		this.TranslateObjectLocal(new Vector3(0, 0, (float)_distance));

		targetNode.SetIdentity();
		var targetNodeTfm = targetNode.Transform;
		targetNodeTfm.Basis = new Basis(Quaternion.FromEuler(_orbitRotation));
		targetNode.Transform = targetNodeTfm;
	}

	public void _ProcessFreecam(double delta)
	{
		//Update Mouse Info
		var yaw = mouseRightClickMoveSpeed.X * sensitivityX;
		var pitch = mouseRightClickMoveSpeed.Y * sensitivityY;

		pitch = Mathf.Clamp(pitch, -90 - freecamTotalPitch, 90 - freecamTotalPitch);
		freecamTotalPitch += pitch;

		RotateY((float)Mathf.DegToRad(-yaw));
		RotateObjectLocal(new Vector3(1, 0, 0), (float)Mathf.DegToRad(-pitch));

		//Update Freecam movement
		var movementVec2 = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		var verticalMovement = Input.GetAxis("move_down", "move_up");
		var movementDirection = new Vector3(movementVec2.X, 0, movementVec2.Y);
		movementDirection = movementDirection.Normalized();

		//Add the vertical movement separately by intention
		movementDirection.Y = verticalMovement;
		freeCamDirection = movementDirection;

		// Computes the change in velocity due to desired direction and "drag"
		// The "drag" is a constant acceleration on the camera to bring it's velocity to 0
		var offset = freeCamDirection.Normalized() * (float)(FREECAM_ACCELERATION * FREECAM_VELOCITY_MULTIPLIER * delta);
		var drag = freeCamVelocity.Normalized() * (float)(FREECAM_DECELERATION * FREECAM_VELOCITY_MULTIPLIER * delta);
		offset += drag;

		// Mixes in speed multipliers
		var ctrlMulti = Input.GetActionStrength("slow_down");
		var shiftMulti = Input.GetActionStrength("speed_up");

		//Multiply by the result in case we're using an axis input
		var speedMulti = (ctrlMulti > 0 ? ctrlMulti * CTRL_MULTIPLIER : 1) * (shiftMulti > 0 ? shiftMulti * SHIFT_MULTIPLIER : 1);

		// Determine if the camera should translate or not
		if (freeCamDirection == Vector3.Zero && offset.LengthSquared() > freeCamVelocity.LengthSquared())
		{
			freeCamVelocity = Vector3.Zero;
		}
		else
		{
			freeCamVelocity.X = Mathf.Clamp(freeCamVelocity.X + offset.X, (float)-FREECAM_VELOCITY_MULTIPLIER, (float)FREECAM_VELOCITY_MULTIPLIER);
			freeCamVelocity.Y = Mathf.Clamp(freeCamVelocity.Y + offset.Y, (float)-FREECAM_VELOCITY_MULTIPLIER, (float)FREECAM_VELOCITY_MULTIPLIER);
			freeCamVelocity.Z = Mathf.Clamp(freeCamVelocity.Z + offset.Z, (float)-FREECAM_VELOCITY_MULTIPLIER, (float)FREECAM_VELOCITY_MULTIPLIER);

			Translate(freeCamVelocity * (float)(delta * speedMulti));
		}
	}

	public override void _Input(InputEvent @event)
	{
		switch (@event)
		{
			case InputEventMouseMotion iemmEvent:
				_ProcessMouseMovementEvent(iemmEvent);
				break;
			case InputEventMouseButton iembEvent:
				_ProcessMouseButtonEvent(iembEvent);
				break;
			case InputEventScreenTouch iestEvent:
				_ProcessTouchZoomEvent(iestEvent);
				break;
			case InputEventScreenDrag iesdEvent:
				_ProcessTouchMovementEvent(iesdEvent);
				break;
			case InputEventKey iekEvent:
				//Placeholder
				break;
			default:
				_ProcessMiscInputEvents(@event);
				break;
		}
	}

	public void _ProcessMiscInputEvents(InputEvent @event)
	{
		if (Input.IsActionJustReleased("mode_toggle"))
		{
			if (cameraMode == CameraMode.Orbit)
			{
				cameraMode = CameraMode.Freecam;
				this.Reparent(GetTree().Root);
				//Adjust targetNode while it's not parented
				targetNode.GlobalRotation = new Vector3(0, targetNode.GlobalRotation.Y, targetNode.GlobalRotation.Z);
				targetNode.Reparent(GetTree().Root, true);

				//Reparent back to targetNode after its adjustment
				this.Reparent(targetNode);
				freecamTotalPitch = -(this.Rotation.X * 180 / Mathf.Pi);
			}
			else if (cameraMode == CameraMode.Freecam)
			{
				if (orbitFocusNode == null)
				{
					cameraMode = CameraMode.Orbit;
					targetNode.Reparent(orbitFocusNode);
				}
			}
		}
	}

	public void _ProcessMouseMovementEvent(InputEventMouseMotion e)
	{                
		//Check for if we're hovering over a gizmo piece. This is for highlighting them as well as handling them as 'selected'.
		var start = ProjectRayOrigin(e.Position);
		var end = ProjectPosition(e.Position, MouseRayCastLength);
		var spaceState = GetWorld3D().DirectSpaceState;

		//Check for a collision with a transform gizmo piece
		var gizmoQuery = PhysicsRayQueryParameters3D.Create(start, end, 2);
		var gizmoResult = spaceState.IntersectRay(gizmoQuery);

		//If we hit a Gizmo piece, we should highlight it
		if(gizmoResult.Count > 0)
		{
			OverEasyGlobals.TransformGizmo.SetHover((StaticBody3D)gizmoResult["collider"]);
		} else
		{
			OverEasyGlobals.TransformGizmo.SetHover(null);
		}

		//Reset the selection point if the mouse moves too far from it
		if (e.Position.X > OverEasyGlobals.PreviousMouseSelectionPoint.X + OverEasyGlobals.MouseNotMovedThresholdX || e.Position.X < OverEasyGlobals.PreviousMouseSelectionPoint.X - OverEasyGlobals.MouseNotMovedThresholdX ||
			e.Position.Y > OverEasyGlobals.PreviousMouseSelectionPoint.Y + OverEasyGlobals.MouseNotMovedThresholdY || e.Position.Y < OverEasyGlobals.PreviousMouseSelectionPoint.Y - OverEasyGlobals.MouseNotMovedThresholdY)
		{
			OverEasyGlobals.ClearPreviousMouseSelectionCache();
		}

		//Get mouse movement speed for camera rotation
		if (Input.IsMouseButtonPressed(MouseButton.Right))
		{
			mouseRightClickMoveSpeed = e.Relative;
		}
		mouseMoveSpeed = e.Relative;
	}

	public void _ProcessMouseButtonEvent(InputEventMouseButton e)
	{
		switch (e.ButtonIndex)
		{
			case MouseButton.WheelUp:
				scrollSpeed = -1 * scrollSpeed;
				break;
			case MouseButton.WheelDown:
				scrollSpeed = 1 * scrollSpeed;
				break;
			case MouseButton.Middle:
				break;
			case MouseButton.Left:
				if(e.IsPressed() && OverEasyGlobals.CanAccess3d)
				{
					if(OverEasyGlobals.TransformGizmo.currentHover != OverEasy.Editor.Gizmo.SelectionRegion.None)
					{
						startedDragging = true;
						if(dragStart.IsEqualApprox(DragStartDefault))
						{
							dragStart = e.Position;
						} else if(!e.Position.IsEqualApprox(dragStart))
                        {
                            isDragging = true;
                        }
					}
				}
				if(e.IsReleased() && OverEasyGlobals.CanAccess3d)
				{
					//If we're dragging a transform, we don't want to select a new object
					if(isDragging == false)
					{
						var start = ProjectRayOrigin(e.Position);
						var end = ProjectPosition(e.Position, MouseRayCastLength);
						var spaceState = GetWorld3D().DirectSpaceState;
						uint objCollisionMask = 1;

						PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(start, end, objCollisionMask, OverEasyGlobals.PreviousMouseSelectionPointRidCache);
						var result = spaceState.IntersectRay(query);
						if (result.Count == 0)
						{
							OverEasyGlobals.ClearPreviousMouseSelectionCache();
							query = PhysicsRayQueryParameters3D.Create(start, end, objCollisionMask, new Godot.Collections.Array<Rid> { });
							result = spaceState.IntersectRay(query);
						}

						//We want to check this separately again and NOT tie it with an else since we may fill 'result' in the previous block 
						if (result.Count != 0)
                        {
                            OverEasyGlobals.PreviousMouseSelectionPointRidCache.Add((Godot.Rid)result["rid"]);
							OverEasyGlobals.TransformGizmo.Reparent(((Node3D)result["collider"]).GetParent().GetParent(), false);
							OverEasyGlobals.TransformGizmo.SetCurrentTransformType(OverEasy.Editor.Gizmo.TransformType.Translation);
						}
					}
					startedDragging = false;
                    isDragging = false;
                    dragStart = new Vector2(-1, -1);
                }
				OverEasyGlobals.PreviousMouseSelectionPoint = e.Position;
				break;
		}
	}

	public void _ProcessTouchMovementEvent(InputEventScreenDrag e)
	{
		if (touchDictionary.ContainsKey(e.Index))
		{
			touchDictionary[e.Index] = e.Position;
		}
		if (touchDictionary.Count == 2)
		{
			var keys = touchDictionary.Keys.ToArray();
			var posFinger1 = touchDictionary[keys[0]];
			var posFinger2 = touchDictionary[keys[1]];
			var dist = posFinger1.DistanceTo(posFinger2);
			if (oldTouchDistance != -1)
			{
				scrollSpeed = (dist - oldTouchDistance) * TOUCH_ZOOM_SPEED;
			}
			if (INVERT_TOUCH_ROTATION)
			{
				scrollSpeed *= -1;
			}
			oldTouchDistance = dist;
		}
		else if (touchDictionary.Count < 2)
		{
			oldTouchDistance = -1;
			mouseRightClickMoveSpeed = e.Relative;
		}
	}

	public void _ProcessTouchZoomEvent(InputEventScreenTouch e)
	{
		if (e.Pressed)
		{
			if (!touchDictionary.ContainsKey(e.Index))
			{
				touchDictionary[e.Index] = e.Position;
			}
		}
		else
		{
			if (touchDictionary.ContainsKey(e.Index))
			{
				touchDictionary.Remove(e.Index);
			}
		}
	}
}
