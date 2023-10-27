using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Collections;

namespace RuntimeGizmos
{
	//To be safe, if you are changing any transforms hierarchy, such as parenting an object to something,
	//you should call ClearTargets before doing so just to be sure nothing unexpected happens... as well as call UndoRedoManager.Clear()
	//For example, if you select an object that has children, move the children elsewhere, deselect the original object, then try to add those old children to the selection, I think it wont work.

	public class RotateGizmo : MonoBehaviour
	{
		public TransformSpace space = TransformSpace.Local;
		public TransformType transformType = TransformType.Rotate;
		public CenterType centerType = CenterType.All;

		public bool isSnapping = false;

		public Color xColor = new Color(1, 0, 0, 0.8f);
		public Color yColor = new Color(0, 1, 0, 0.8f);
		public Color zColor = new Color(0, 0, 1, 0.8f);
		public Color allColor = new Color(.7f, .7f, .7f, 0.8f);
		public Color selectedColor = new Color(1, 1, 0, 0.8f);
		public Color hoverColor = new Color(1, .75f, 0, 0.8f);
		public float planesOpacity = .5f;
		//public Color rectPivotColor = new Color(0, 0, 1, 0.8f);
		//public Color rectCornerColor = new Color(0, 0, 1, 0.8f);
		//public Color rectAnchorColor = new Color(.7f, .7f, .7f, 0.8f);
		//public Color rectLineColor = new Color(.7f, .7f, .7f, 0.8f);

		public float rotationSnap = 15f;

		public float handleLength = .25f;
		public float handleWidth = .003f;
		public float planeSize = .035f;
		public float triangleSize = .03f;
		public float boxSize = .03f;
		public int circleDetail = 40;
		public float allRotateHandleLengthMultiplier = 1.4f;
		public float minSelectedDistanceCheck = .01f;
		public float rotateSpeedMultiplier = 1f;
		public float allRotateSpeedMultiplier = 20f;

		//If circularRotationMethod is true, when rotating you will need to move your mouse around the object as if turning a wheel.
		//If circularRotationMethod is false, when rotating you can just click and drag in a line to rotate.
		public bool circularRotationMethod;

		//Mainly for if you want the pivot point to update correctly if selected objects are moving outside the transformgizmo.
		//Might be poor on performance if lots of objects are selected...
		public bool forceUpdatePivotPointOnChange = true;

		public Action onCheckForSelectedAxis;
		public Action onDrawCustomGizmo;

		public Camera myCamera { get; private set; }

		public bool isTransforming { get; private set; }
		public Quaternion totalRotationAmount {get; private set;}
		public TransformType transformingType { get { return translatingType; } }
		public Axis translatingAxis { get { return nearAxis; } }
		public Axis translatingAxisPlane { get { return planeAxis; } }
		public bool hasTranslatingAxisPlane { get { return translatingAxisPlane != Axis.None && translatingAxisPlane != Axis.Any; } }


		public Vector3 pivotPoint { get; private set; }

		AxisInfo axisInfo;
		Axis nearAxis = Axis.None;
		Axis planeAxis = Axis.None;
		TransformType translatingType;

		AxisVectors circlesLines = new AxisVectors();

		//We use a HashSet and a List for targetRoots so that we get fast lookup with the hashset while also keeping track of the order with the list.
		HashSet<Renderer> highlightedRenderers = new HashSet<Renderer>();

		List<Renderer> renderersBuffer = new List<Renderer>();
		List<Material> materialsBuffer = new List<Material>();

		WaitForEndOfFrame waitForEndOFFrame = new WaitForEndOfFrame();
		Coroutine forceUpdatePivotCoroutine;

		static Material lineMaterial;
		static Material outlineMaterial;

		void Awake()
		{
			SetMaterial();
		}
		private void RenderPipelineManager_endFrameRendering(ScriptableRenderContext context, Camera[] camera)
		{
			//Camera.onPostRender.Invoke();
		}

		void OnEnable()
		{
			myCamera = Camera.main;
			RenderPipelineManager.endFrameRendering += RenderPipelineManager_endFrameRendering;
			forceUpdatePivotCoroutine = StartCoroutine(ForceUpdatePivotPointAtEndOfFrame());
		}

        private void Start()
        {
			AddTargetHighlightedRenderers(transform);
			Camera.onPostRender += OnPostRenderCallback;
		}

        void OnDisable()
		{
			RenderPipelineManager.endFrameRendering -= RenderPipelineManager_endFrameRendering;
			StopCoroutine(forceUpdatePivotCoroutine);
		}

		void OnDestroy()
		{
			ClearAllHighlightedRenderers();
			Camera.onPostRender -= OnPostRenderCallback;
		}

		void Update()
		{
			SetNearAxis();
			TransformSelected();
		}

		void LateUpdate()
		{
			//We run this in lateupdate since coroutines run after update and we want our gizmos to have the updated target transform position after TransformSelected()
			SetAxisInfo();
			SetLines();
		}

		void OnPostRenderCallback(Camera cam)
		{
			if (cam == myCamera)
            {
				lineMaterial.SetPass(0);

				Color xColor = (nearAxis == Axis.X) ? (isTransforming) ? selectedColor : hoverColor : this.xColor;
				Color yColor = (nearAxis == Axis.Y) ? (isTransforming) ? selectedColor : hoverColor : this.yColor;
				Color zColor = (nearAxis == Axis.Z) ? (isTransforming) ? selectedColor : hoverColor : this.zColor;
				Color allColor = (nearAxis == Axis.Any) ? (isTransforming) ? selectedColor : hoverColor : this.allColor;

				//Note: The order of drawing the axis decides what gets drawn over what.

				DrawQuads(circlesLines.all, GetColor(TransformType.Rotate, this.allColor, allColor));
				DrawQuads(circlesLines.x, GetColor(TransformType.Rotate, this.xColor, xColor));
				DrawQuads(circlesLines.y, GetColor(TransformType.Rotate, this.yColor, yColor));
				DrawQuads(circlesLines.z, GetColor(TransformType.Rotate, this.zColor, zColor));
			}
		}

		IEnumerator ForceUpdatePivotPointAtEndOfFrame()
		{
			while (this.enabled)
			{
				ForceUpdatePivotPointOnChange();
				yield return waitForEndOFFrame;
			}
		}

		void ForceUpdatePivotPointOnChange()
		{
			if (forceUpdatePivotPointOnChange)
			{
				bool hasSet = false;
				if (!hasSet)
				{
					SetPivotPoint();
					hasSet = true;
				}
			}
		}

		public void SetPivotPoint()
		{
			pivotPoint = transform.position;
		}

		void SetPivotPointOffset(Vector3 offset)
		{
			pivotPoint += offset;
		}

		Color GetColor(TransformType type, Color normalColor, Color nearColor, bool forceUseNormal = false)
		{
			return GetColor(type, normalColor, nearColor, false, 1, forceUseNormal);
		}
		Color GetColor(TransformType type, Color normalColor, Color nearColor, float alpha, bool forceUseNormal = false)
		{
			return GetColor(type, normalColor, nearColor, true, alpha, forceUseNormal);
		}
		Color GetColor(TransformType type, Color normalColor, Color nearColor, bool setAlpha, float alpha, bool forceUseNormal = false)
		{
			Color color;
			if(!forceUseNormal && TranslatingTypeContains(type, false))
			{
				color = nearColor;
			}else{
				color = normalColor;
			}

			if(setAlpha)
			{
				color.a = alpha;
			}

			return color;
		}

		//We only support scaling in local space.
		public TransformSpace GetProperTransformSpace()
		{
			return transformType == TransformType.Scale ? TransformSpace.Local : space;
		}

		public bool TransformTypeContains(TransformType type)
		{
			return TransformTypeContains(transformType, type);
		}
		public bool TranslatingTypeContains(TransformType type, bool checkIsTransforming = true)
		{
			TransformType transType = !checkIsTransforming || isTransforming ? translatingType : transformType;
			return TransformTypeContains(transType, type);
		}
		public bool TransformTypeContains(TransformType mainType, TransformType type)
		{
			return ExtTransformType.TransformTypeContains(mainType, type, GetProperTransformSpace());
		}
		
		public float GetHandleLength(TransformType type, Axis axis = Axis.None, bool multiplyDistanceMultiplier = true)
		{
			float length = handleLength;

			if(multiplyDistanceMultiplier) length *= GetDistanceMultiplier();

			return length;
		}

		void TransformSelected()
		{
			if(nearAxis != Axis.None && Input.GetMouseButtonDown(0))
			{
				StartCoroutine(TransformSelected(translatingType));
			}
		}
		
		IEnumerator TransformSelected(TransformType transType)
		{
			isTransforming = true;
			totalRotationAmount = Quaternion.identity;

			Vector3 originalPivot = pivotPoint;

			Vector3 otherAxis1, otherAxis2;
			Vector3 axis = GetNearAxisDirection(out otherAxis1, out otherAxis2);
			Vector3 planeNormal = hasTranslatingAxisPlane ? axis : (myCamera.transform.position - originalPivot).normalized;
			Vector3 projectedAxis = Vector3.ProjectOnPlane(axis, planeNormal).normalized;
			Vector3 previousMousePosition = Vector3.zero;

			float currentSnapRotationAmount = 0;

			while (!Input.GetMouseButtonUp(0))
			{
				Ray mouseRay = myCamera.ScreenPointToRay(Input.mousePosition);
				Vector3 mousePosition = Geometry.LinePlaneIntersect(mouseRay.origin, mouseRay.direction, originalPivot, planeNormal);

				if(previousMousePosition != Vector3.zero && mousePosition != Vector3.zero)
				{

					float rotateAmount = 0;
					Vector3 rotationAxis = axis;

					if(nearAxis == Axis.Any)
					{
						Vector3 rotation = myCamera.transform.TransformDirection(new Vector3(Input.GetAxis("Mouse Y"), -Input.GetAxis("Mouse X"), 0));
						Quaternion.Euler(rotation).ToAngleAxis(out rotateAmount, out rotationAxis);
						rotateAmount *= allRotateSpeedMultiplier;
					}else{
						if(circularRotationMethod)
						{
							float angle = Vector3.SignedAngle(previousMousePosition - originalPivot, mousePosition - originalPivot, axis);
							rotateAmount = angle * rotateSpeedMultiplier;
						}else{
							Vector3 projected = (nearAxis == Axis.Any || ExtVector3.IsParallel(axis, planeNormal)) ? planeNormal : Vector3.Cross(axis, planeNormal);
							rotateAmount = (ExtVector3.MagnitudeInDirection(mousePosition - previousMousePosition, projected) * (rotateSpeedMultiplier * 100f)) / GetDistanceMultiplier();
						}
					}

					if(isSnapping && rotationSnap > 0)
					{
						currentSnapRotationAmount += rotateAmount;
						rotateAmount = 0;

						float remainder;
						float snapAmount = CalculateSnapAmount(rotationSnap, currentSnapRotationAmount, out remainder);

						if(snapAmount != 0)
						{
							rotateAmount = snapAmount;
							currentSnapRotationAmount = remainder;
						}
					}


					Transform target = transform;

					//if(pivot == TransformPivot.Pivot)
					//{
					//	target.Rotate(rotationAxis, rotateAmount, Space.World);
					//}
					//else if(pivot == TransformPivot.Center)
					//{
					//	target.RotateAround(originalPivot, rotationAxis, rotateAmount);
					//}
					target.Rotate(rotationAxis, rotateAmount, Space.World);
					totalRotationAmount *= Quaternion.Euler(rotationAxis * rotateAmount);
				}

				previousMousePosition = mousePosition;

				yield return null;
			}


			totalRotationAmount = Quaternion.identity;
			isTransforming = false;
		}

		float CalculateSnapAmount(float snapValue, float currentAmount, out float remainder)
		{
			remainder = 0;
			if(snapValue <= 0) return currentAmount;

			float currentAmountAbs = Mathf.Abs(currentAmount);
			if(currentAmountAbs > snapValue)
			{
				remainder = currentAmountAbs % snapValue;
				return snapValue * (Mathf.Sign(currentAmount) * Mathf.Floor(currentAmountAbs / snapValue));
			}

			return 0;
		}

		Vector3 GetNearAxisDirection(out Vector3 otherAxis1, out Vector3 otherAxis2)
		{
			otherAxis1 = otherAxis2 = Vector3.zero;

			if(nearAxis != Axis.None)
			{
				if(nearAxis == Axis.X)
				{
					otherAxis1 = axisInfo.yDirection;
					otherAxis2 = axisInfo.zDirection;
					return axisInfo.xDirection;
				}
				if(nearAxis == Axis.Y)
				{
					otherAxis1 = axisInfo.xDirection;
					otherAxis2 = axisInfo.zDirection;
					return axisInfo.yDirection;
				}
				if(nearAxis == Axis.Z)
				{
					otherAxis1 = axisInfo.xDirection;
					otherAxis2 = axisInfo.yDirection;
					return axisInfo.zDirection;
				}
				if(nearAxis == Axis.Any)
				{
					return Vector3.one;
				}
			}

			return Vector3.zero;
		}
	

		void AddTargetHighlightedRenderers(Transform target)
		{
			if(target != null)
			{
				GetTargetRenderers(target, renderersBuffer);

				for(int i = 0; i < renderersBuffer.Count; i++)
				{
					Renderer render = renderersBuffer[i];

					if(!highlightedRenderers.Contains(render))
					{
						materialsBuffer.Clear();
						materialsBuffer.AddRange(render.sharedMaterials);

						if(!materialsBuffer.Contains(outlineMaterial))
						{
							materialsBuffer.Add(outlineMaterial);
							render.materials = materialsBuffer.ToArray();
						}

						highlightedRenderers.Add(render);
					}
				}

				materialsBuffer.Clear();
			}
		}

		void GetTargetRenderers(Transform target, List<Renderer> renderers)
		{
			renderers.Clear();
			if(target != null)
			{
				target.GetComponentsInChildren<Renderer>(true, renderers);
			}
		}

		void ClearAllHighlightedRenderers()
		{
			RemoveTargetHighlightedRenderers(transform);

			//In case any are still left, such as if they changed parents or what not when they were highlighted.
			renderersBuffer.Clear();
			renderersBuffer.AddRange(highlightedRenderers);
			RemoveHighlightedRenderers(renderersBuffer);
		}

		void RemoveTargetHighlightedRenderers(Transform target)
		{
			GetTargetRenderers(target, renderersBuffer);

			RemoveHighlightedRenderers(renderersBuffer);
		}

		void RemoveHighlightedRenderers(List<Renderer> renderers)
		{
			for(int i = 0; i < renderersBuffer.Count; i++)
			{
				Renderer render = renderersBuffer[i];
				if(render != null)
				{
					materialsBuffer.Clear();
					materialsBuffer.AddRange(render.sharedMaterials);

					if(materialsBuffer.Contains(outlineMaterial))
					{
						materialsBuffer.Remove(outlineMaterial);
						render.materials = materialsBuffer.ToArray();
					}
				}

				highlightedRenderers.Remove(render);
			}

			renderersBuffer.Clear();
		}

		public void SetTranslatingAxis(TransformType type, Axis axis, Axis planeAxis = Axis.None)
		{
			this.translatingType = type;
			this.nearAxis = axis;
			this.planeAxis = planeAxis;
		}

		public AxisInfo GetAxisInfo()
		{
			AxisInfo currentAxisInfo = axisInfo;

			if(isTransforming && GetProperTransformSpace() == TransformSpace.Global && translatingType == TransformType.Rotate)
			{
				currentAxisInfo.xDirection = totalRotationAmount * Vector3.right;
				currentAxisInfo.yDirection = totalRotationAmount * Vector3.up;
				currentAxisInfo.zDirection = totalRotationAmount * Vector3.forward;
			}

			return currentAxisInfo;
		}

		void SetNearAxis()
		{
			if(isTransforming) return;

			SetTranslatingAxis(transformType, Axis.None);

			float distanceMultiplier = GetDistanceMultiplier();
			float handleMinSelectedDistanceCheck = (this.minSelectedDistanceCheck + handleWidth) * distanceMultiplier;
			
			if(nearAxis == Axis.None && TransformTypeContains(TransformType.Rotate))
			{
				HandleNearestLines(TransformType.Rotate, circlesLines, handleMinSelectedDistanceCheck);
			}
		}

		void HandleNearestLines(TransformType type, AxisVectors axisVectors, float minSelectedDistanceCheck)
		{
			float xClosestDistance = ClosestDistanceFromMouseToLines(axisVectors.x);
			float yClosestDistance = ClosestDistanceFromMouseToLines(axisVectors.y);
			float zClosestDistance = ClosestDistanceFromMouseToLines(axisVectors.z);
			float allClosestDistance = ClosestDistanceFromMouseToLines(axisVectors.all);

			HandleNearest(type, xClosestDistance, yClosestDistance, zClosestDistance, allClosestDistance, minSelectedDistanceCheck);
		}

		void HandleNearestPlanes(TransformType type, AxisVectors axisVectors, float minSelectedDistanceCheck)
		{
			float xClosestDistance = ClosestDistanceFromMouseToPlanes(axisVectors.x);
			float yClosestDistance = ClosestDistanceFromMouseToPlanes(axisVectors.y);
			float zClosestDistance = ClosestDistanceFromMouseToPlanes(axisVectors.z);
			float allClosestDistance = ClosestDistanceFromMouseToPlanes(axisVectors.all);

			HandleNearest(type, xClosestDistance, yClosestDistance, zClosestDistance, allClosestDistance, minSelectedDistanceCheck);
		}

		void HandleNearest(TransformType type, float xClosestDistance, float yClosestDistance, float zClosestDistance, float allClosestDistance, float minSelectedDistanceCheck)
		{
			if(type == TransformType.Scale && allClosestDistance <= minSelectedDistanceCheck) SetTranslatingAxis(type, Axis.Any);
			else if(xClosestDistance <= minSelectedDistanceCheck && xClosestDistance <= yClosestDistance && xClosestDistance <= zClosestDistance) SetTranslatingAxis(type, Axis.X);
			else if(yClosestDistance <= minSelectedDistanceCheck && yClosestDistance <= xClosestDistance && yClosestDistance <= zClosestDistance) SetTranslatingAxis(type, Axis.Y);
			else if(zClosestDistance <= minSelectedDistanceCheck && zClosestDistance <= xClosestDistance && zClosestDistance <= yClosestDistance) SetTranslatingAxis(type, Axis.Z);
			else if(type == TransformType.Rotate)
			{
				Ray mouseRay = myCamera.ScreenPointToRay(Input.mousePosition);
				Vector3 mousePlaneHit = Geometry.LinePlaneIntersect(mouseRay.origin, mouseRay.direction, pivotPoint, (myCamera.transform.position - pivotPoint).normalized);
				if((pivotPoint - mousePlaneHit).sqrMagnitude <= (GetHandleLength(TransformType.Rotate)).Squared()) SetTranslatingAxis(type, Axis.Any);
			}
		}

		float ClosestDistanceFromMouseToLines(List<Vector3> lines)
		{
			Ray mouseRay = myCamera.ScreenPointToRay(Input.mousePosition);

			float closestDistance = float.MaxValue;
			for(int i = 0; i + 1 < lines.Count; i++)
			{
				IntersectPoints points = Geometry.ClosestPointsOnSegmentToLine(lines[i], lines[i + 1], mouseRay.origin, mouseRay.direction);
				float distance = Vector3.Distance(points.first, points.second);
				if(distance < closestDistance)
				{
					closestDistance = distance;
				}
			}
			return closestDistance;
		}

		float ClosestDistanceFromMouseToPlanes(List<Vector3> planePoints)
		{
			float closestDistance = float.MaxValue;

			if(planePoints.Count >= 4)
			{
				Ray mouseRay = myCamera.ScreenPointToRay(Input.mousePosition);

				for(int i = 0; i < planePoints.Count; i += 4)
				{
					Plane plane = new Plane(planePoints[i], planePoints[i + 1], planePoints[i + 2]);

					float distanceToPlane;
					if(plane.Raycast(mouseRay, out distanceToPlane))
					{
						Vector3 pointOnPlane = mouseRay.origin + (mouseRay.direction * distanceToPlane);
						Vector3 planeCenter = (planePoints[0] + planePoints[1] + planePoints[2] + planePoints[3]) / 4f;

						float distance = Vector3.Distance(planeCenter, pointOnPlane);
						if(distance < closestDistance)
						{
							closestDistance = distance;
						}
					}
				}
			}

			return closestDistance;
		}

		//float DistanceFromMouseToPlane(List<Vector3> planeLines)
		//{
		//	if(planeLines.Count >= 4)
		//	{
		//		Ray mouseRay = myCamera.ScreenPointToRay(Input.mousePosition);
		//		Plane plane = new Plane(planeLines[0], planeLines[1], planeLines[2]);

		//		float distanceToPlane;
		//		if(plane.Raycast(mouseRay, out distanceToPlane))
		//		{
		//			Vector3 pointOnPlane = mouseRay.origin + (mouseRay.direction * distanceToPlane);
		//			Vector3 planeCenter = (planeLines[0] + planeLines[1] + planeLines[2] + planeLines[3]) / 4f;

		//			return Vector3.Distance(planeCenter, pointOnPlane);
		//		}
		//	}

		//	return float.MaxValue;
		//}

		void SetAxisInfo()
		{
			axisInfo.Set(transform, pivotPoint, GetProperTransformSpace());
		}

		//This helps keep the size consistent no matter how far we are from it.
		public float GetDistanceMultiplier()
		{
			if(myCamera.orthographic) return Mathf.Max(.01f, myCamera.orthographicSize * 2f);
			return Mathf.Max(.01f, Mathf.Abs(ExtVector3.MagnitudeInDirection(pivotPoint - myCamera.transform.position, myCamera.transform.forward)));
		}

		void SetLines()
		{
			SetCircles(GetAxisInfo(), circlesLines);
		}

		int AxisDirectionMultiplier(Vector3 direction, Vector3 otherDirection)
		{
			return ExtVector3.IsInDirection(direction, otherDirection) ? 1 : -1;
		}

		void AddSquares(Vector3 axisStart, Vector3 axisDirection, Vector3 axisOtherDirection1, Vector3 axisOtherDirection2, float size, List<Vector3> resultsBuffer)
		{
			AddQuads(axisStart, axisDirection, axisOtherDirection1, axisOtherDirection2, size, size * .5f, resultsBuffer);
		}
		void AddQuads(Vector3 axisStart, Vector3 axisDirection, Vector3 axisOtherDirection1, Vector3 axisOtherDirection2, float length, float width, List<Vector3> resultsBuffer)
		{
			Vector3 axisEnd = axisStart + (axisDirection * length);
			AddQuads(axisStart, axisEnd, axisOtherDirection1, axisOtherDirection2, width, resultsBuffer);
		}
		void AddQuads(Vector3 axisStart, Vector3 axisEnd, Vector3 axisOtherDirection1, Vector3 axisOtherDirection2, float width, List<Vector3> resultsBuffer)
		{
			Square baseRectangle = GetBaseSquare(axisStart, axisOtherDirection1, axisOtherDirection2, width);
			Square baseRectangleEnd = GetBaseSquare(axisEnd, axisOtherDirection1, axisOtherDirection2, width);

			resultsBuffer.Add(baseRectangle.bottomLeft);
			resultsBuffer.Add(baseRectangle.topLeft);
			resultsBuffer.Add(baseRectangle.topRight);
			resultsBuffer.Add(baseRectangle.bottomRight);

			resultsBuffer.Add(baseRectangleEnd.bottomLeft);
			resultsBuffer.Add(baseRectangleEnd.topLeft);
			resultsBuffer.Add(baseRectangleEnd.topRight);
			resultsBuffer.Add(baseRectangleEnd.bottomRight);

			for(int i = 0; i < 4; i++)
			{
				resultsBuffer.Add(baseRectangle[i]);
				resultsBuffer.Add(baseRectangleEnd[i]);
				resultsBuffer.Add(baseRectangleEnd[i + 1]);
				resultsBuffer.Add(baseRectangle[i + 1]);
			}
		}

		void AddQuad(Vector3 axisStart, Vector3 axisOtherDirection1, Vector3 axisOtherDirection2, float width, List<Vector3> resultsBuffer)
		{
			Square baseRectangle = GetBaseSquare(axisStart, axisOtherDirection1, axisOtherDirection2, width);

			resultsBuffer.Add(baseRectangle.bottomLeft);
			resultsBuffer.Add(baseRectangle.topLeft);
			resultsBuffer.Add(baseRectangle.topRight);
			resultsBuffer.Add(baseRectangle.bottomRight);
		}

		Square GetBaseSquare(Vector3 axisEnd, Vector3 axisOtherDirection1, Vector3 axisOtherDirection2, float size)
		{
			Square square;
			Vector3 offsetUp = ((axisOtherDirection1 * size) + (axisOtherDirection2 * size));
			Vector3 offsetDown = ((axisOtherDirection1 * size) - (axisOtherDirection2 * size));
			//These might not really be the proper directions, as in the bottomLeft might not really be at the bottom left...
			square.bottomLeft = axisEnd + offsetDown;
			square.topLeft = axisEnd + offsetUp;
			square.bottomRight = axisEnd - offsetUp;
			square.topRight = axisEnd - offsetDown;
			return square;
		}

		void SetCircles(AxisInfo axisInfo, AxisVectors axisVectors)
		{
			axisVectors.Clear();

			float circleLength = GetHandleLength(TransformType.Rotate);
			AddCircle(pivotPoint, axisInfo.xDirection, circleLength, axisVectors.x);
			AddCircle(pivotPoint, axisInfo.yDirection, circleLength, axisVectors.y);
			AddCircle(pivotPoint, axisInfo.zDirection, circleLength, axisVectors.z);
			AddCircle(pivotPoint, (pivotPoint - myCamera.transform.position).normalized, circleLength, axisVectors.all, false);
		}

		void AddCircle(Vector3 origin, Vector3 axisDirection, float size, List<Vector3> resultsBuffer, bool depthTest = true)
		{
			Vector3 up = axisDirection.normalized * size;
			Vector3 forward = Vector3.Slerp(up, -up, .5f);
			Vector3 right = Vector3.Cross(up, forward).normalized * size;
		
			Matrix4x4 matrix = new Matrix4x4();
		
			matrix[0] = right.x;
			matrix[1] = right.y;
			matrix[2] = right.z;
		
			matrix[4] = up.x;
			matrix[5] = up.y;
			matrix[6] = up.z;
		
			matrix[8] = forward.x;
			matrix[9] = forward.y;
			matrix[10] = forward.z;
		
			Vector3 lastPoint = origin + matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)));
			Vector3 nextPoint = Vector3.zero;
			float multiplier = 360f / circleDetail;

			Plane plane = new Plane((myCamera.transform.position - pivotPoint).normalized, pivotPoint);

			float circleHandleWidth = handleWidth * GetDistanceMultiplier();

			for(int i = 0; i < circleDetail + 1; i++)
			{
				nextPoint.x = Mathf.Cos((i * multiplier) * Mathf.Deg2Rad);
				nextPoint.z = Mathf.Sin((i * multiplier) * Mathf.Deg2Rad);
				nextPoint.y = 0;
			
				nextPoint = origin + matrix.MultiplyPoint3x4(nextPoint);
			
				if(!depthTest || plane.GetSide(lastPoint))
				{
					Vector3 centerPoint = (lastPoint + nextPoint) * .5f;
					Vector3 upDirection = (centerPoint - origin).normalized;
					AddQuads(lastPoint, nextPoint, upDirection, axisDirection, circleHandleWidth, resultsBuffer);
				}

				lastPoint = nextPoint;
			}
		}

		void DrawLines(List<Vector3> lines, Color color)
		{
			if(lines.Count == 0) return;

			GL.Begin(GL.LINES);
			GL.Color(color);

			for(int i = 0; i < lines.Count; i += 2)
			{
				GL.Vertex(lines[i]);
				GL.Vertex(lines[i + 1]);
			}

			GL.End();
		}

		void DrawTriangles(List<Vector3> lines, Color color)
		{
			if(lines.Count == 0) return;

			GL.Begin(GL.TRIANGLES);
			GL.Color(color);

			for(int i = 0; i < lines.Count; i += 3)
			{
				GL.Vertex(lines[i]);
				GL.Vertex(lines[i + 1]);
				GL.Vertex(lines[i + 2]);
			}

			GL.End();
		}

		void DrawQuads(List<Vector3> lines, Color color)
		{
			if(lines.Count == 0) return;

			GL.Begin(GL.QUADS);
			GL.Color(color);

			for(int i = 0; i < lines.Count; i += 4)
			{
				GL.Vertex(lines[i]);
				GL.Vertex(lines[i + 1]);
				GL.Vertex(lines[i + 2]);
				GL.Vertex(lines[i + 3]);
			}

			GL.End();
		}

		void DrawFilledCircle(List<Vector3> lines, Color color)
		{
			if(lines.Count == 0) return;

			Vector3 center = Vector3.zero;
			for(int i = 0; i < lines.Count; i++)
			{
				center += lines[i];
			}
			center /= lines.Count;

			GL.Begin(GL.TRIANGLES);
			GL.Color(color);

			for(int i = 0; i + 1 < lines.Count; i++)
			{
				GL.Vertex(lines[i]);
				GL.Vertex(lines[i + 1]);
				GL.Vertex(center);
			}

			GL.End();
		}

		void SetMaterial()
		{
			if(lineMaterial == null)
			{
				lineMaterial = new Material(Shader.Find("Custom/Lines"));
				outlineMaterial = new Material(Shader.Find("Custom/Outline"));
			}
		}
	}
}
