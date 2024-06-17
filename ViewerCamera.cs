using Godot;
using Godot.Collections;
using System.Linq;
using OverEasy;

public partial class ViewerCamera : Camera3D
{
	public enum CameraMode : int
	{
		Orbit = 0,
		Freecam = 1,
	}

	//External params
	/// <summary>
	/// Multiplier for zooming in and out with a mouse
	/// </summary>
	[Export]
	public static double SCROLL_SPEED = 10;
	/// <summary>
	/// Multiplier for zooming and out with a touch screen
	/// </summary>
	[Export]
	public static double ZOOM_SPEED = 5;
	/// <summary>
	/// Multiplier for spinning the camera on the roll axis
	/// </summary>
	[Export]
	public static double SPIN_SPEED = 10;
	/// <summary>
	/// Initial distance in meters the camera should distance itself from the target
	/// </summary>
	[Export]
	public static double DEFAULT_DISTANCE = 20;
	/// <summary>
	/// Multiplier for rotating the camera on the X axis
	/// </summary>
	[Export]
	public static double ROTATE_SPEED_X = 40;
	/// <summary>
	/// Multiplier for rotating the camera on the Y axis
	/// </summary>
	[Export]
	public static double ROTATE_SPEED_Y = 40;
	/// <summary>
	/// Multiplier for rotating the camera via touch screen dragging
	/// </summary>
	[Export]
	public static double TOUCH_ZOOM_SPEED = 40;
	/// <summary>
	/// Multiplier for camera position fast movement
	/// </summary>
	[Export]
	public static double SHIFT_MULTIPLIER = 2.5;
	/// <summary>
	/// Multiplier for camera position slow movemnet
	/// </summary>
	[Export]
	public static double CTRL_MULTIPLIER = 0.4;
	/// <summary>
	/// Acceleration multiplier for the freecam
	/// </summary>
	[Export]
	public static double FREECAM_ACCELERATION = 30;
	/// <summary>
	/// Deceleration multiplier for the freecam
	/// </summary>
	[Export]
	public static double FREECAM_DECELERATION = -10;
	/// <summary>
	/// Velocity multiplier for the freecam
	/// </summary>
	[Export]
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
	[Export]
	public static bool INVERT_MOUSE_VERTICAL_ROTATION = false;
	/// <summary>
	/// Boolean to decide if camera should invert mouse horizontal rotation 
	/// </summary>
	[Export]
	public static bool INVERT_MOUSE_HORIZONTAL_ROTATION = false;
	/// <summary>
	/// Boolean to decide if camera should invert gamepad vertical rotation 
	/// </summary>
	[Export]
	public static bool INVERT_GAMEPAD_VERTICAL_ROTATION = false;
	/// <summary>
	/// Boolean to decide if camera should invert gamepad horizontal rotation 
	/// </summary>
	[Export]
	public static bool INVERT_GAMEPAD_HORIZONTAL_ROTATION = false;
	/// <summary>
	/// Boolean to decide if camera should invert touch event rotation 
	/// </summary>
	[Export]
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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_distance = DEFAULT_DISTANCE;
		_orbitRotation = targetNode.Transform.Basis.GetRotationQuaternion().GetEuler();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_ProcessTransformation(delta);
	}

	public void _ProcessTransformation(double delta)
	{
		if(OverEasyGlobals.CanMove3dCamera)
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
			mouseMoveSpeed = new Vector2();
		}
	}

	public void _ProcessOrbit(double delta)
	{
		// Update rotation
		_orbitRotation.X += (float)(-mouseMoveSpeed.Y * delta * ROTATE_SPEED_X * sensitivityX);
		_orbitRotation.Y += (float)(-mouseMoveSpeed.X * delta * ROTATE_SPEED_Y * sensitivityY);

		//_rotation.z += _spin_speed * delta

		if (_orbitRotation.X < -Mathf.Pi / 2)
		{
			_orbitRotation.X = -Mathf.Pi / 2;
		}

		if (_orbitRotation.X > Mathf.Pi / 2)
		{
			_orbitRotation.X = Mathf.Pi / 2;
		}

		mouseMoveSpeed = new Vector2();

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
		var yaw = mouseMoveSpeed.X * sensitivityX;
		var pitch = mouseMoveSpeed.Y * sensitivityY;

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
				_ProcessMouseRotationEvent(iemmEvent);
				break;
			case InputEventMouseButton iembEvent:
				_ProcessMouseScrollEvent(iembEvent);
				break;
			case InputEventScreenTouch iestEvent:
				_ProcessTouchZoomEvent(iestEvent);
				break;
			case InputEventScreenDrag iesdEvent:
				_ProcessTouchRotationEvent(iesdEvent);
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

	public void _ProcessMouseRotationEvent(InputEventMouseMotion e)
	{
		if (Input.IsMouseButtonPressed(MouseButton.Right))
		{
			mouseMoveSpeed = e.Relative;
		}
	}

	public void _ProcessMouseScrollEvent(InputEventMouseButton e)
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
		}
	}

	public void _ProcessTouchRotationEvent(InputEventScreenDrag e)
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
			mouseMoveSpeed = e.Relative;
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
