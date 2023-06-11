using UnityEngine;

namespace FusionExamples.Tanknarok
{
	/// <summary>
	/// This camera strategy is to follow only one target, the local player
	/// </summary>
	public class CameraStrategy_SingleTarget : CameraStrategy
	{
		// Defining the size of the arena, easily accessable by anyone who needs it
		public const float ARENA_X = 26.35f;
		public const float ARENA_Y = 17.7f;
		
		private Vector3 _offset;
		private Vector3 _averageTarget;

		[Header("Camera Settings")] private float cameraTiltAngle = 55f;

		[SerializeField] private bool _applyAimingOffset = true;
		[SerializeField] private float _zOffset = 5f;
		[SerializeField] private float _maxDist = 64f;
		[SerializeField] private float _minDist = 50f;
		[SerializeField] private float _inventoryOffset = 30f;

		private float _distanceMultiplier = 1f;
		private float _maxDistanceMultiplier = 1.4f;

		private float _furtestTargetWeightMultiplier = 2.08f;

		[Header("Boundries")] [SerializeField] private CameraBounds _cameraBounds;

		private ScreenShaker _screenShaker;

		private float _initialLongestDistance;

		private Vector3 _averageTargetGizmoPosition;
		private Vector3 _weightedTargetGizmoPosition;
		private Transform _localPlayer = default;
		private bool _inventoryOpen = false;


		private void Awake()
		{
			Initialize();
		}

		public override void Initialize()
		{
			base.Initialize();

			_screenShaker = GetComponent<ScreenShaker>();
			UpdateCamera();
			ForceUpdatePositionAndRotation();
		}

		public void ResetCameraPosition()
		{
			transform.position = Vector3.zero;
			_myCamera.transform.localPosition = _offset + _screenShaker.finalPositionalShake;
			_myCamera.transform.rotation = Quaternion.Euler(cameraTiltAngle, 0, 0);
		}

		[SerializeField] private float singleTargetYadditionalOffset = 2f;

		void UpdateCamera()
		{
			CalculateAverages();
			
			CalculateOffset();

			_averageTarget = _cameraBounds.StayWithinBounds(_averageTarget, _offset, cameraTiltAngle, _maxDist, _myCamera.transform);

			if (_targets.Count == 1 && _targets[0] != null)
			{
				float arenaYMultiplier = Mathf.Clamp01(_targets[0].transform.position.z / ARENA_Y);
				float additionalZOffsetForOnlineTeleporter = arenaYMultiplier * singleTargetYadditionalOffset;
				_averageTarget += Vector3.forward * additionalZOffsetForOnlineTeleporter;
			}

			UpdatePositionAndRotation();
		}

		public void LateUpdate()
		{
			UpdateCamera();
		}

		void CalculateAverages()
		{
			//Reset the distance
			float yDifference = 0f;

			//Reset the average target variable
			_averageTarget = Vector3.zero;
			Vector3 weightedAverageTarget = Vector3.zero;

			Vector3 furtestAwayPosition = Vector3.zero;

			_activeTargets.Clear();

			//Go through each target and calculate its distance to the targets average position and add it to the distance variable
			for (int i = 0; i < _targets.Count; i++)
			{
				var isLocal = false;

				GameObject targetGameObject = _targets[i];

				// Detect the local player
				if (targetGameObject.TryGetComponent<Player>(out var player))
                {
					if (player.IsLocal)
					{
						_localPlayer = targetGameObject.transform;
					}
                }

				if (targetGameObject != null) 
				{
					isLocal = targetGameObject.transform == _localPlayer;

					if (!isLocal) continue;

					//Get current target
					Transform targetTransform = _targets[i].transform;

					_activeTargets.Add(targetTransform.gameObject);

					//Add target position to average target - gets divided later
					_averageTarget += targetTransform.position;
				}

				if (isLocal) break;
			}

			// Compensate the camera position when driving above the center of the arena
			_distanceMultiplier = Mathf.Clamp(yDifference / (_cameraBounds.Bounds.z * 2), 0, 1f);

			_distanceMultiplier = Remap(_distanceMultiplier, 0, 1, 1, _maxDistanceMultiplier);

			/*
			//If target count is greater 3 or more, then we need to use a weighted target position to try to keep all players on the screen 
			if (_activeTargets.Count > 2)
			{
				//Reset weightedAverageTarget
				weightedAverageTarget = Vector3.zero;

				//Calculate the average of all the positions
				_averageTarget = _averageTarget / _activeTargets.Count;

				//Save position for gizmo drawing
				_averageTargetGizmoPosition = _averageTarget;

				float[] distances = new float[_activeTargets.Count];
				Vector3[] directions = new Vector3[_activeTargets.Count];
				float longestDistance = 0;

				//Find distances to average point
				for (int i = 0; i < _activeTargets.Count; i++)
				{
					Vector3 direction = _activeTargets[i].transform.position - _averageTarget;
					Debug.DrawRay(_activeTargets[i].transform.position, direction, Color.green);
					directions[i] = direction;

					float distanceToAverage = direction.magnitude;
					distances[i] = distanceToAverage;
					if (distanceToAverage > longestDistance)
						longestDistance = distanceToAverage;
				}

				//Calculate a average target offset with weights between 0-1 based on longestDistance
				//The longer a tank is from the average target, the more impact it will have on the weighted offset
				for (int i = 0; i < _activeTargets.Count; i++)
				{
					float multiplier = Remap(distances[i], 0, longestDistance, 0, 1);
					//weightedAverageTarget += (directions[i] * Mathf.Pow(multiplier, furtestTargetWeightMultiplier));
					weightedAverageTarget += (directions[i] * multiplier * _furtestTargetWeightMultiplier);
				}

				weightedAverageTarget /= _activeTargets.Count;
				_averageTarget += weightedAverageTarget; //Offset the average target with the weightes 

				//Save weighted target for gizmo drawing
				_weightedTargetGizmoPosition = _averageTarget;
			}
			//If there is only 1-2 players, we just use straight up average positioning
			else 
			*/
			if (_activeTargets.Count > 0)
			{
				_averageTarget = _averageTarget / _activeTargets.Count;
			}
			else
			{
				_averageTarget = transform.position;
			}
		}

		public float Remap(float value, float oldFrom, float oldTo, float newFrom, float newTo)
		{
			return (value - oldFrom) / (oldTo - oldFrom) * (newTo - newFrom) + newFrom;
		}

		//Use trigonomerty to calculate the distance and height of the camera given a distance and a camera tilt angle
		void CalculateOffset()
		{
			float modifiedAngle = cameraTiltAngle - 90;
			float zOffset = (Mathf.Sin(modifiedAngle * Mathf.Deg2Rad) * _maxDist);
			float yOffset = (Mathf.Cos(modifiedAngle * Mathf.Deg2Rad) * _maxDist);
			_offset = new Vector3(0, yOffset, zOffset);
		}

		//Update the position and rotation of both the camera and the camera parent
		void UpdatePositionAndRotation()
		{
			//Camera local position and rotation
			_myCamera.transform.localPosition = Vector3.Lerp(_myCamera.transform.localPosition, _offset, Time.fixedDeltaTime * 10f) + _screenShaker.finalPositionalShake;

			_myCamera.transform.rotation = Quaternion.Lerp(_myCamera.transform.rotation, Quaternion.Euler(cameraTiltAngle, 0, 0), Time.fixedDeltaTime * 10f) * _screenShaker.finalRotationShake;

			var goalPosition = _averageTarget;

			if (_applyAimingOffset && _targets.Count > 0)
			{
				goalPosition += _targets[0].transform.forward * _zOffset;
			}

			if (_inventoryOpen)
			{
				goalPosition += Vector3.right * _inventoryOffset;
			}

			//Camera parent position
			transform.position = Vector3.Lerp(transform.position, goalPosition, Time.fixedDeltaTime * _moveSpeed);
		}

		void ForceUpdatePositionAndRotation()
		{
			//Camera local position and rotation
			_myCamera.transform.localPosition = _offset + _screenShaker.finalPositionalShake;
			_myCamera.transform.rotation = Quaternion.Euler(cameraTiltAngle, 0, 0) * _screenShaker.finalRotationShake;
		}

		public void CameraShake(float impact = 1f)
		{
			ScreenShaker.AddTrauma(impact);
		}

		private void OnDrawGizmos()
		{
			Gizmos.DrawSphere(_averageTarget, 0.4f);

			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(_weightedTargetGizmoPosition, 0.4f);

			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(_averageTargetGizmoPosition, 0.4f);
		}

		#region Inventory events

		public void InventoryOpen()
		{
			_inventoryOpen = true;
		}

		public void InventoryClose()
		{
			_inventoryOpen = false;
		}

		#endregion
	}
}