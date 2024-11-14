using Godot;
using Godot.Collections;
using System.Linq;
using OverEasy;
using OverEasy.Editor;
using OverEasy.Util;
using System;
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
	/// Dampens orbit sensitivity to be closer to the feel of freecam sensitivity. 
	/// </summary>
	public double orbitDampener = 0.04;

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
	public bool dragButtonHeld = false;

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

	/// <summary>
	/// Records if the left mouse button is clicked while in 3d space to bypass CanAccess3d if we're holding it already
	/// </summary>
	public bool mouseLeftClickedIn3d = false;

	/// <summary>
	/// Records if the right mouse buttton is clicked while in 3d space to bypass CanAccess3d if we're holding it already
	/// </summary>
	public bool mouseRightClickedIn3d = false;

	/// <summary>
	/// For locking a transformation type on the gizmo while manipulating it
	/// </summary>
	public bool transformLocked = false;

	/// <summary>
	/// For bypassing CanAccess3d while in GUI, such as during a selection via a GUI element
	/// </summary>
	public bool oneTimeProcessTransform = false;

	/// <summary>
	/// For holding the gizmo global position from the start of a drag
	/// </summary>
	public System.Numerics.Vector3 dragStartPosition;

	/// <summary>
	/// For holding the gizmo global rotation from the start of a drag
	/// </summary>
	public System.Numerics.Quaternion dragStartRotation;

	/// <summary>
	/// For holding the gizmo global scale from the start of a drag
	/// </summary>
	public System.Numerics.Vector3 dragStartScale;

	/// <summary>
	/// First rotation projection for the drag
	/// </summary>
	public System.Numerics.Vector3? dragStartRotationProjection = null;

	/// <summary>
	/// For holding the position in 2d where the gizmo was at the start of the drag
	/// </summary>
	public Godot.Vector2 dragGizmoCenter = new Vector2(-1, -1);

	/// <summary>
	/// Current gizmo raycast result. Null if unselected 
	/// </summary>
	public Dictionary currentGizmoRaycastResults = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		OverEasyGlobals.ViewCamera = this;
		_distance = DEFAULT_DISTANCE;
		_orbitRotation = targetNode.Transform.Basis.GetRotationQuaternion().GetEuler();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_ProcessDrag();
		_ProcessGizmoSize();
		_ProcessTransformation(delta);
	}

	private void _ProcessDrag()
	{
		if(Input.IsMouseButtonPressed(MouseButton.Left) && (OverEasyGlobals.CanAccess3d || mouseLeftClickedIn3d))
		{
			var mousePosition = OverEasyGlobals.setDataTree.GetGlobalMousePosition();
			if(dragButtonHeld)
			{
				if (isDragging == false && !mousePosition.IsEqualApprox(dragStart))
				{
					dragStartPosition = OverEasyGlobals.TransformGizmo.GlobalPosition.ToSNVec3();
					if(OverEasyGlobals.TransformGizmoWorld)
					{
						dragStartRotation = Quaternion.FromEuler((OverEasyGlobals.TransformGizmo).GlobalRotation).ToSNQuat();
					} else
					{
						dragStartRotation = Quaternion.FromEuler(((Node3D)OverEasyGlobals.TransformGizmo.GetParent()).GlobalRotation).ToSNQuat();
					}
					dragStartScale = OverEasyGlobals.TransformGizmo.Scale.ToSNVec3();
					dragGizmoCenter = this.UnprojectPosition(OverEasyGlobals.TransformGizmo.GlobalPosition);
					isDragging = true;
				}
				transformLocked = true;

				if (isDragging)
				{
					var start = ProjectRayOrigin(mousePosition);
					var direction = ProjectPosition(mousePosition, 1);
					direction = System.Numerics.Vector3.Normalize((start - direction).ToSNVec3()).ToGVec3();

					Vector3? pos = null;
					Godot.Quaternion? rot = null;
					Vector3? scale = null;
					System.Numerics.Vector3 axis = System.Numerics.Vector3.Zero;
					switch (OverEasyGlobals.TransformGizmo.currentHover)
					{
						case OverEasy.Editor.Gizmo.SelectionRegion.PositionX:
						case OverEasy.Editor.Gizmo.SelectionRegion.PositionY:
						case OverEasy.Editor.Gizmo.SelectionRegion.PositionZ:
							pos = Gizmo.GetSingleAxisProjection(start.ToSNVec3(), direction.ToSNVec3(), dragStartPosition, dragStartRotation, OverEasyGlobals.TransformGizmo.currentHover).ToGVec3();
							break;
						case OverEasy.Editor.Gizmo.SelectionRegion.PositionXY:
						case OverEasy.Editor.Gizmo.SelectionRegion.PositionXZ:
						case OverEasy.Editor.Gizmo.SelectionRegion.PositionYZ:
							pos = Gizmo.GetDoubleAxisProjection(start.ToSNVec3(), direction.ToSNVec3(), dragStartPosition, dragStartRotation, OverEasyGlobals.TransformGizmo.currentHover).ToGVec3();
							break;
						case OverEasy.Editor.Gizmo.SelectionRegion.RotationX:
							axis = new System.Numerics.Vector3(1.0f, 0.0f, 0.0f);
							if(OverEasyGlobals.TransformGizmoWorld == false)
							{
								axis = System.Numerics.Vector3.Transform(axis, dragStartRotation);
							}
							rot = _ProcessRotation(axis, start, direction, dragStartPosition, dragStartRotation, Gizmo.SelectionRegion.RotationX);
							break;
						case OverEasy.Editor.Gizmo.SelectionRegion.RotationY:
							axis = new System.Numerics.Vector3(0.0f, 1.0f, 0.0f);
							if (OverEasyGlobals.TransformGizmoWorld == false)
							{
								axis = System.Numerics.Vector3.Transform(axis, dragStartRotation);
							}
							rot = _ProcessRotation(axis, start, direction, dragStartPosition, dragStartRotation, Gizmo.SelectionRegion.RotationY);
							break;
						case OverEasy.Editor.Gizmo.SelectionRegion.RotationZ:
							axis = new System.Numerics.Vector3(0.0f, 0.0f, 1.0f);
							if (OverEasyGlobals.TransformGizmoWorld == false)
							{
								axis = System.Numerics.Vector3.Transform(axis, dragStartRotation);
							}
							rot = _ProcessRotation(axis, start, direction, dragStartPosition, dragStartRotation, Gizmo.SelectionRegion.RotationZ);
							break;
						case OverEasy.Editor.Gizmo.SelectionRegion.ScaleX:
						case OverEasy.Editor.Gizmo.SelectionRegion.ScaleY:
						case OverEasy.Editor.Gizmo.SelectionRegion.ScaleZ:
							break;
						case OverEasy.Editor.Gizmo.SelectionRegion.ScaleXY:
						case OverEasy.Editor.Gizmo.SelectionRegion.ScaleXZ:
						case OverEasy.Editor.Gizmo.SelectionRegion.ScaleYZ:
							break;
						case OverEasy.Editor.Gizmo.SelectionRegion.None:
						default:
							//Shouldn't happen, but who knows
							break;
					}

					//Make sure we're not multiplying a null here
					Quaternion? finalRot = null;
					if(rot != null)
					{
						finalRot = dragStartRotation.ToGQuat() * (Quaternion)rot;
					}
					GD.Print($"Current rot: {dragStartRotation} Delta: {rot} New Rot: {finalRot}");
					OverEasyGlobals.TransformFromGizmo(pos, finalRot, scale);
				}
			}
		}
	}

	private Quaternion _ProcessRotation(System.Numerics.Vector3 axis, Vector3 start, Vector3 direction, System.Numerics.Vector3 startPosition, System.Numerics.Quaternion startRotation, Gizmo.SelectionRegion selectionAxis)
	{
		axis = System.Numerics.Vector3.Normalize(axis);
		System.Numerics.Vector3 newproj = Gizmo.GetAxisPlaneProjection(start.ToSNVec3(), direction.ToSNVec3(), startPosition, startRotation, selectionAxis);
		if (dragStartRotationProjection == null)
		{
			dragStartRotationProjection = newproj;
		}
		System.Numerics.Vector3 delta = System.Numerics.Vector3.Normalize(newproj - startPosition);
		System.Numerics.Vector3 deltaorig = System.Numerics.Vector3.Normalize((System.Numerics.Vector3)dragStartRotationProjection - startPosition);
		System.Numerics.Vector3 side = System.Numerics.Vector3.Cross(axis, deltaorig);
		var y = Math.Max(-1.0f, Math.Min(1.0f, System.Numerics.Vector3.Dot(delta, deltaorig)));
		var x = Math.Max(-1.0f, Math.Min(1.0f, System.Numerics.Vector3.Dot(delta, side)));
		var angle = (float)Math.Atan2(x, y);
		return System.Numerics.Quaternion.Normalize(System.Numerics.Quaternion.CreateFromAxisAngle(axis, angle) *
														 startRotation).ToGQuat();
	}

	private void _ProcessGizmoSize()
	{
		var gizmoDistance = (this.GlobalPosition - OverEasyGlobals.TransformGizmo.GlobalPosition).Length();
		var size = gizmoDistance * FixedGizmoSize * this.Fov;
		OverEasyGlobals.TransformGizmo.Scale = Vector3.One * size;
	}

	private void _ProcessTransformation(double delta)
	{
		if (OverEasyGlobals.CanAccess3d || mouseRightClickedIn3d || oneTimeProcessTransform)
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
			oneTimeProcessTransform = false;
		}
	}

	public void _ProcessOrbit(double delta)
	{
		// Update rotation
		_orbitRotation.X += (float)(-mouseRightClickMoveSpeed.Y * delta * ROTATE_SPEED_X * sensitivityX * orbitDampener);
		_orbitRotation.Y += (float)(-mouseRightClickMoveSpeed.X * delta * ROTATE_SPEED_Y * sensitivityY * orbitDampener);

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
		targetNodeTfm.Basis = new Basis(Godot.Quaternion.FromEuler(_orbitRotation));
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
		var movementDirection = new Vector3(movementVec2.X, 0, (float)(movementVec2.Y + scrollSpeed));
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

		scrollSpeed = 0;
	}

	public override void _Input(InputEvent @event)
	{
		if(!_ProcessMiscInputEvents(@event))
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
					break;
				default:
					break;
			}
		}
	}

	public bool _ProcessMiscInputEvents(InputEvent @event)
	{
	   // GD.Print("mode_toggle pressed");
		if (Input.IsActionJustReleased("mode_toggle"))
		{
			ToggleMode();

			return true;
		}

		return false;
	}

	public void ToggleMode()
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
			cameraMode = CameraMode.Orbit;
			TrySetOrbitCam();
		}
	}

	public bool TrySetOrbitCam()
	{
		if (orbitFocusNode != null)
		{
			targetNode.Reparent(orbitFocusNode);
			var aabb = OverEasy.Util.GodotUtil.GetHierarchyAABB(orbitFocusNode);
			_distance = aabb.Size.Length() * 2;

			return true;
		}

		return false;
	}

	public void _ProcessMouseMovementEvent(InputEventMouseMotion e)
	{
		if (transformLocked == false)
		{
			//Check for if we're hovering over a gizmo piece. This is for highlighting them as well as handling them as 'selected'.
			var start = ProjectRayOrigin(e.Position);
			var end = ProjectPosition(e.Position, MouseRayCastLength);
			var spaceState = GetWorld3D().DirectSpaceState;

			//Check for a collision with a transform gizmo piece
			var gizmoQuery = PhysicsRayQueryParameters3D.Create(start, end, 2);
			var gizmoResult = spaceState.IntersectRay(gizmoQuery);

			//If we hit a Gizmo piece, we should highlight it
			if (gizmoResult.Count > 0)
			{
				OverEasyGlobals.TransformGizmo.SetHover((StaticBody3D)gizmoResult["collider"]);
				currentGizmoRaycastResults = gizmoResult;
			}
			else
			{
				OverEasyGlobals.TransformGizmo.SetHover(null);
			}
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
				scrollSpeed = -1 * SCROLL_SPEED;
				break;
			case MouseButton.WheelDown:
				scrollSpeed = 1 * SCROLL_SPEED;
				break;
			case MouseButton.Middle:
				break;
			case MouseButton.Right:
				if (e.IsPressed() && OverEasyGlobals.CanAccess3d)
				{
					mouseRightClickedIn3d = true;
				}
				if (e.IsReleased())
				{
					mouseRightClickedIn3d = false;
				}
				break;
			case MouseButton.Left:
				var start = ProjectRayOrigin(e.Position);
				if (e.IsPressed() && OverEasyGlobals.CanAccess3d)
				{
					mouseLeftClickedIn3d = true;
					if (OverEasyGlobals.TransformGizmo.currentHover != OverEasy.Editor.Gizmo.SelectionRegion.None)
					{
						dragButtonHeld = true;
						//When dragging, we want to check on subsequent frames if we're actually moving the gizmo or not.
						//If not, we want the user to still be able to select what's under the gizmo later.
						dragStart = e.Position;
					}
				}
				if (e.IsReleased())
				{
					if (OverEasyGlobals.CanAccess3d)
					{
						//If we're dragging a transform, we don't want to select a new object
						if (isDragging == false)
						{
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
								var parentNode = ((Node3D)result["collider"]).GetParent().GetParent();
								orbitFocusNode = (Node3D)parentNode;
								if(cameraMode == CameraMode.Orbit)
								{
									TrySetOrbitCam();
								}

								var activeTreeItem = (TreeItem)parentNode.GetMeta("treeItem");
								var parentTreeItem = activeTreeItem.GetParent();

								//Uncollapse it before the event fires so we can then ScrollToItem properly
								parentTreeItem.Collapsed = false;
								OverEasyGlobals.setDataTree.SetSelected((TreeItem)parentNode.GetMeta("treeItem"), 0);
								OverEasyGlobals.HandleTreeNodeSelected();
							}
						}
					}
					dragGizmoCenter = new Vector2(-1, -1);
					dragStartPosition = new System.Numerics.Vector3();
					dragStartRotation = new System.Numerics.Quaternion(0, 0, 0, 1);
					dragStartRotationProjection = null;
					dragStartScale = new System.Numerics.Vector3(1, 1, 1);
					currentGizmoRaycastResults = null;
					dragButtonHeld = false;
					isDragging = false;
					dragStart = new Vector2(-1, -1);
					mouseLeftClickedIn3d = false;
					transformLocked = false;
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
