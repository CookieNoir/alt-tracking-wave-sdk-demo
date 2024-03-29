// "Wave SDK 
// © 2020 HTC Corporation. All Rights Reserved.
//
// Unless otherwise required by copyright law and practice,
// upon the execution of HTC SDK license agreement,
// HTC grants you access to and use of the Wave SDK(s).
// You shall fully comply with all of HTC’s SDK license agreement terms and
// conditions signed by you and all SDK and API requirements,
// specifications, and documentation provided by HTC to You."

using UnityEngine;
using UnityEngine.EventSystems;
using Wave.Native;

namespace Wave.Essence.Controller.Model.Demo
{
	[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter), typeof(PhysicsRaycaster))]
	public class DemoCMGazePointer : MonoBehaviour
	{
		private const string LOG_TAG = "DemoCMGazePointer";
		private void DEBUG(string msg) { Log.d(LOG_TAG, msg, true); }

		// ----------- Width of ring -----------
		private const float DEF_RING_WIDTH = 0.005f;
		private const float MIN_RING_WIDTH = 0.001f;
		[Tooltip("Set the width of the pointer's ring.")]
		public float PointerRingWidth = DEF_RING_WIDTH;

		// ----------- Radius of inner circle -----------
		private const float DEF_INNER_CIRCLE_RADIUS = 0.005f;
		private const float MIN_INNER_CIRCLE_RADIUS = 0.001f;
		[Tooltip("Set the radius of the pointer's inner circle.")]
		public float PointerCircleRadius = DEF_INNER_CIRCLE_RADIUS;

		/// The offset from the pointer to the pointer-mounted object.
		private Vector3 pointerOffset = Vector3.zero;
		/// The offset from the pointer to the pointer-mounted object in every frame.
		private Vector3 pointerFrameOffset = Vector3.zero;
		/// The pointer world position.
		private Vector3 pointerWorldPosition = Vector3.zero;

		// ----------- Z distance of ring -----------
		private const float DEF_POINTER_DISTANCE = 1;
		private const float MIN_POINTER_DISTANCE = 0.1f;
		[Tooltip("Set the z-coordinate of the pointer.")]
		public float PointerDistance = DEF_POINTER_DISTANCE;
		private float pointerDistance = DEF_POINTER_DISTANCE;

		// ----------- Color of ring -----------
		/// Color of ring background.
		[Tooltip("Set the ring background color.")]
		public Color PointerColor = Color.white;
		/// Color of ring foreground
		[Tooltip("Set the ring foreground progess color.")]
		public Color ProgressColor = new Color(0, 245, 255);

		// ----------- Material and Mesh -----------
		private Mesh mMesh = null;
		private const string POINTER_MATERIAL_NAME = "Materials/GazePointer01";
		[Tooltip("Empty for using the default material or set a customized material.")]
		public Material PointerMaterial = null;
		private Material pointerMaterialInstance = null;
		private MeshRenderer pointerMeshRend = null;
		private MeshFilter pointerMeshFilter = null;
		private const int POINTER_MATERIAL_RENDER_QUEUE_MIN = 1000;
		private const int POINTER_MATERIAL_RENDER_QUEUE_MAX = 5000;
		/// The material's renderQueue.
		[Tooltip("Set the Material's renderQueue.")]
		public int PointerRenderQueue = POINTER_MATERIAL_RENDER_QUEUE_MAX;
		/// The MeshRenderer's sortingOrder.
		[Tooltip("Set the MeshRenderer's sortingOrder.")]
		public int PointerSortingOrder = 32767;

		private bool isHovering = false;

		[HideInInspector]
		public bool ShowPointer = true;
		[HideInInspector]
		public int RingPercent = 0;

		private bool ValidateParameters()
		{
			if (pointerMeshRend == null || pointerMeshFilter == null)
				return false;

			if (this.PointerRingWidth < MIN_RING_WIDTH)
				this.PointerRingWidth = DEF_RING_WIDTH;

			if (this.PointerCircleRadius < MIN_INNER_CIRCLE_RADIUS)
				this.PointerCircleRadius = DEF_INNER_CIRCLE_RADIUS;

			if (this.PointerDistance < MIN_POINTER_DISTANCE)
				this.PointerDistance = DEF_POINTER_DISTANCE;

			if (this.PointerRenderQueue < POINTER_MATERIAL_RENDER_QUEUE_MIN ||
				this.PointerRenderQueue > POINTER_MATERIAL_RENDER_QUEUE_MAX)
				this.PointerRenderQueue = POINTER_MATERIAL_RENDER_QUEUE_MAX;

			return true;
		}

		#region MonoBehaviour overrides
		private bool mEnabled = false;
		void OnEnable()
		{
			if (!mEnabled)
			{
				DEBUG("OnEnable()");

				// 1. Texture or Mesh < Material < < MeshFilter < MeshRenderer, we don't use the texture.
				if (mMesh == null)
					mMesh = new Mesh();
				if (mMesh != null)
					mMesh.name = gameObject.name + " Mesh";

				// 2. Load the Material RingUnlitTransparentMat.
				if (PointerMaterial == null)
					PointerMaterial = Resources.Load(POINTER_MATERIAL_NAME) as Material;
				if (PointerMaterial != null)
				{
					pointerMaterialInstance = Instantiate<Material>(PointerMaterial);
					DEBUG("OnEnable() Loaded resource " + pointerMaterialInstance.name);
				}

				// 3. Get the MeshFilter.
				pointerMeshFilter = GetComponent<MeshFilter>();

				// 4. Get the MeshRenderer.
				pointerMeshRend = GetComponent<MeshRenderer>();
				pointerMeshRend.material = pointerMaterialInstance;

				// 5. Create the pointer.
				SetPointerActive(this.ShowPointer);

				mEnabled = true;
			}
		}

		void OnDisable()
		{
			if (mEnabled)
			{
				Mesh mesh = pointerMeshFilter.mesh;
				mesh.Clear();
				PointerMaterial = null;
				Destroy(pointerMaterialInstance);

				mEnabled = false;
				DEBUG("OnDisable()");
			}
		}

		void Start()
		{
			GetComponent<Camera>().stereoTargetEye = StereoTargetEyeMask.None;
			GetComponent<Camera>().enabled = false;
			DEBUG("Start() " + gameObject.name);
		}

		void Update()
		{
			if (!ValidateParameters())
				return;

			if (pointerMeshRend.enabled != this.ShowPointer)
				SetPointerActive(this.ShowPointer);

			if (Log.gpl.Print)
				DEBUG("Update() Pointer is " + (pointerMeshRend.enabled ? "shown." : "hidden."));

			// Do nothing if pointer disabled.
			if (!pointerMeshRend.enabled)
				return;

			pointerDistance = pointerFrameOffset.z;

			pointerFrameOffset = pointerOffset;
			if (pointerFrameOffset == Vector3.zero)
				pointerDistance = this.PointerDistance;
			pointerDistance = pointerDistance < MIN_POINTER_DISTANCE ? this.PointerDistance : pointerDistance;

			pointerFrameOffset.z = pointerDistance;

			float calcRingWidth = this.PointerRingWidth * (1 + ((pointerDistance / DEF_POINTER_DISTANCE) * 0.1f));
			float calcInnerCircleRadius = this.PointerCircleRadius * (1 + ((pointerDistance / DEF_POINTER_DISTANCE) * 0.1f));

			if (pointerOffset != Vector3.zero)
				pointerWorldPosition = transform.position + pointerFrameOffset;
			else
			{
				pointerWorldPosition = transform.position + transform.forward.normalized * pointerDistance;
			}

			DrawRingRoll(calcRingWidth + calcInnerCircleRadius, calcInnerCircleRadius, pointerFrameOffset, this.RingPercent, isHovering);
		}
		#endregion

		private void SetPointerActive(bool active)
		{
			if (pointerMeshRend.enabled != active)
			{
				pointerMeshRend.enabled = active;
				DEBUG("SetPointerActive() " + pointerMeshRend.enabled);
				if (pointerMeshRend.enabled)
				{
					pointerMeshRend.enabled = true;
					pointerMeshRend.sortingOrder = this.PointerSortingOrder;
					if (pointerMaterialInstance != null)
					{
						pointerMeshRend.material = pointerMaterialInstance;
						pointerMeshRend.material.renderQueue = PointerRenderQueue;
					}
					// The MeshFilter's mesh is updated in DrawRingRoll(), not here.
				}
			}
		}

		private const int VERTEX_COUNT = 400;       // 100 percents * 2 + 2, ex: 80% ring -> 80 * 2 + 2
		private Vector3[] ringVert = new Vector3[VERTEX_COUNT];
		private Color[] ringColor = new Color[VERTEX_COUNT];
		private const int TRIANGLE_COUNT = 100 * 6; // 100 percents * 6, ex: 80% ring -> 80 * 6
		private int[] ringTriangle = new int[TRIANGLE_COUNT];
		private Vector2[] ringUv = new Vector2[VERTEX_COUNT];

		private const float percentAngle = 3.6f;    // 100% = 100 * 3.6f = 360 degrees.

		private void DrawRingRoll(float radius, float innerRadius, Vector3 offset, int percent, bool active)
		{
			// vertices and colors
			float start_angle = 90;             // Start angle of drawing ring.
			for (int i = 0; i < VERTEX_COUNT; i += 2)
			{
				float radian_cur = start_angle * Mathf.Deg2Rad;
				float cosA = Mathf.Cos(radian_cur);
				float sinA = Mathf.Sin(radian_cur);

				ringVert[i].x = offset.x + radius * cosA;
				ringVert[i].y = offset.y + radius * sinA;
				ringVert[i].z = offset.z;

				ringColor[i] = (i <= (percent * 2) && i > 0) ? this.ProgressColor : this.PointerColor;

				ringVert[i + 1].x = offset.x + innerRadius * cosA;
				ringVert[i + 1].y = offset.y + innerRadius * sinA;
				ringVert[i + 1].z = offset.z;

				ringColor[i + 1] = (i <= (percent * 2) && i > 0) ? this.ProgressColor : this.PointerColor;

				start_angle -= percentAngle;
			}

			// triangles
			for (int i = 0, vi = 0; i < TRIANGLE_COUNT; i += 6, vi += 2)
			{
				ringTriangle[i] = vi;
				ringTriangle[i + 1] = vi + 3;
				ringTriangle[i + 2] = vi + 1;

				ringTriangle[i + 3] = vi + 2;
				ringTriangle[i + 4] = vi + 3;
				ringTriangle[i + 5] = vi;
			}

			// uv
			for (int i = 0; i < VERTEX_COUNT; i++)
			{
				ringUv[i].x = ringVert[i].x / radius / 2 + 0.5f;
				ringUv[i].y = ringVert[i].z / radius / 2 + 0.5f;
			}

			mMesh.Clear();

			mMesh.vertices = ringVert;
			mMesh.colors = ringColor;
			mMesh.triangles = ringTriangle;
			mMesh.uv = ringUv;
			pointerMeshFilter.mesh = mMesh;
		}

		#region Export Functions
		public Vector3 GetPointerPosition()
		{
			return pointerWorldPosition;
		}

		public void OnHover(bool hovering, Vector3 intersecPosition)
		{
			pointerOffset = intersecPosition;
			OnHover(hovering);
		}

		public void OnHover(bool hovering)
		{
			isHovering = hovering;
		}
		#endregion
	}
}
