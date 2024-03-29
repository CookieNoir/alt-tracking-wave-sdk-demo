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
using System.Collections.Generic;
using Wave.Native;
using Wave.Essence.Hand;

namespace Wave.Essence.InputModule
{
	/// <summary>
	/// Draw a beam of hand to indicate to which direction is pointed.
	/// </summary>
	[DisallowMultipleComponent]
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public sealed class HandBeam : MonoBehaviour
	{
		const string LOG_TAG = "Wave.Essence.InputModule.HandBeam";
		private void DEBUG(string msg) { Log.d(LOG_TAG, m_BeamType + " " + msg, true); }

		#region Customized Settings
		[Tooltip("Show or hide the beam.")]
		[SerializeField]
		private bool m_ShowBeam = true;
		public bool ShowBeam { get { return m_ShowBeam; } set { m_ShowBeam = value; } }

		[Tooltip("Right or left beam.")]
		[SerializeField]
		private HandManager.HandType m_BeamType = HandManager.HandType.Right;
		public HandManager.HandType BeamType { get { return m_BeamType; } set { m_BeamType = value; } }

		[Tooltip("The minimal end offset of beam.")]
		private float m_EndOffsetMin = 0.01f;
		public float EndOffsetMin { get { return m_EndOffsetMin; } set { m_EndOffsetMin = value; } }

		[Tooltip("The maximal end offset of beam.")]
		private float m_EndOffsetMax = 99.5f;
		public float EndOffsetMax { get { return m_EndOffsetMax; } set { m_EndOffsetMax = value; } }

		const float kMinimalLength = 0.1f;

		private float m_EndOffsetEx = 0.5f;
		[Tooltip("The end offset of the beam. End offset - start offset = beam length.")]
		[SerializeField]
		private float m_EndOffset = 0.5f;
		public float EndOffset
		{
			get { return m_EndOffset; }
			set
			{
				DEBUG("EndOffset() set to " + value);
				m_EndOffset = value;
			}
		}

		private float m_StartOffsetEx = 0.015f;
		[Tooltip("The offset from the beam start side to the hand model.")]
		[SerializeField]
		private float m_StartOffset = 0.01f;
		public float StartOffset { get { return m_StartOffset; } set { m_StartOffset = value; } }

		const float kMinimalWidth = 0.001f;

		private float m_EndWidthEx = 0.004f;
		[Tooltip("The beam width at the end side.")]
		[SerializeField]
		private float m_EndWidth = 0.004f;
		public float EndWidth { get { return m_EndWidth; } set { m_EndWidth = value; } }

		private float m_StartWidthEx = 0.002f;
		[Tooltip("The beam width at the start side.")]
		[SerializeField]
		private float m_StartWidth = 0.002f;
		public float StartWidth { get { return m_StartWidth; } set { m_StartWidth = value; } }

		private Color32 m_EndColorEx = new Color32(255, 255, 255, 255);
		[Tooltip("The color of beam at the end side.")]
		[SerializeField]
		private Color32 m_EndColor = new Color32(255, 255, 255, 100);
		public Color32 EndColor { get { return m_EndColor; } set { m_EndColor = value; } }

		private Color32 m_StartColorEx = new Color32(255, 255, 255, 255);
		[Tooltip("The color of beam at the start side.")]
		[SerializeField]
		private Color32 m_StartColor = new Color32(52, 255, 255, 255);
		public Color32 StartColor { get { return m_StartColor; } set { m_StartColor = value; } }

		const string kBeamMaterial = "Materials/HandBeam01";

		[Tooltip("Set to use default beam material.")]
		[SerializeField]
		private bool m_UseDefaultMaterial = true;
		public bool UseDefaultMaterial { get { return m_UseDefaultMaterial; } set { m_UseDefaultMaterial = value; } }

		[Tooltip("If not set a custom material, the default beam material will be used.")]
		[SerializeField]
		private Material m_CustomMaterial = null;
		public Material CustomMaterial { get { return m_CustomMaterial; } set { m_CustomMaterial = value; } }

		private bool Color32Equal(Color32 color1, Color32 color2)
		{
			if (color1.r == color2.r &&
				color1.g == color2.g &&
				color1.b == color2.b &&
				color1.a == color2.a)
				return true;

			return false;
		}
		private void ValidateParameters()
		{
			// 1.Check the end offset.
			if (m_EndOffset < m_EndOffsetMin || m_EndOffset > m_EndOffsetMax)
				m_EndOffset = m_EndOffsetEx;

			if (m_EndOffsetEx != m_EndOffset)
			{
				m_EndOffsetEx = m_EndOffset;
				toUpdateBeam = true;
			}

			// 2. Check the start offset.
			if (m_EndOffset - m_StartOffset < kMinimalLength)
				m_StartOffset = m_EndOffset - kMinimalLength;

			if (m_StartOffsetEx != m_StartOffset)
			{
				m_StartOffsetEx = m_StartOffset;
				toUpdateBeam = true;
				DEBUG("ValidateParameters() StartOffset: " + m_StartOffset);
			}

			// 3. Check the end width.
			if (m_EndWidth < kMinimalWidth)
				m_EndWidth = kMinimalWidth;

			if (m_EndWidthEx != m_EndWidth)
			{
				m_EndWidthEx = m_EndWidth;
				toUpdateBeam = true;
				DEBUG("ValidateParameters() EndWidth: " + m_EndWidth);
			}

			// 4. Check the start width.
			if (m_StartWidth < kMinimalWidth)
				m_StartWidth = kMinimalWidth;

			if (m_StartWidthEx != m_StartWidth)
			{
				m_StartWidthEx = m_StartWidth;
				toUpdateBeam = true;
				DEBUG("ValidateParameters() StartWidth: " + m_StartWidth);
			}

			if (!Color32Equal(m_EndColorEx, m_EndColor))
			{
				m_EndColorEx = m_EndColor;
				toUpdateBeam = true;
				DEBUG("ValidateParameters() EndColor: " + m_EndColor.ToString());
			}

			if (!Color32Equal(m_StartColorEx, m_StartColor))
			{
				m_StartColorEx = m_StartColor;
				toUpdateBeam = true;
				DEBUG("ValidateParameters() StartColor: " + m_StartColor.ToString());
			}
		}
		#endregion

		/**
		 * OEM Config
		 * \"beam\": {
		   \"start_width\": 0.000625,
		   \"end_width\": 0.00125,
		   \"start_offset\": 0.015,
		   \"length\":  0.8,
		   \"start_color\": \"#FFFFFFFF\",
		   \"end_color\": \"#FFFFFF4D\"
		   },
		 **/

		private Mesh m_Mesh = null;
		private Material m_Material = null;
		private MeshFilter m_MeshFilter = null;
		private MeshRenderer m_MeshRenderer = null;

		#region Monobehaviour overrides
		private bool mEnabled = false;
		void OnEnable()
		{
			if (!mEnabled)
			{
				GameObject parentGo = transform.parent.gameObject;

				m_Mesh = new Mesh();
				CreateBeamMesh();
				m_MeshFilter = GetComponent<MeshFilter>();
				m_MeshFilter.mesh = m_Mesh;
				m_MeshRenderer = GetComponent<MeshRenderer>();

				if (!m_UseDefaultMaterial && m_CustomMaterial != null)
				{
					DEBUG("OnEnable() Use custom config and material");
					m_MeshRenderer.material = m_CustomMaterial;
				}
				else
				{
					DEBUG("OnEnable() Use default material");
					m_Material = Resources.Load(kBeamMaterial) as Material;
					if (m_Material == null)
						DEBUG("OnEnable() Can NOT load default material " + kBeamMaterial);
					m_MeshRenderer.material = m_Material;
				}

				// Not draw mesh in OnEnable(), thus set the m_MeshRenderer to disable.
				m_MeshRenderer.enabled = false;

				m_EndOffsetEx = m_EndOffset;
				m_StartOffsetEx = m_StartOffset;
				m_StartColorEx = m_StartColor;

				HandBeamProvider.Instance.SetHandBeam(m_BeamType, gameObject);

				mEnabled = true;

				DEBUG("OnEnable() parent name: " + parentGo.name
					+ ", localPos: " + parentGo.transform.localPosition.x + ", " + parentGo.transform.localPosition.y + ", " + parentGo.transform.localPosition.z
					+ ", parent local EulerAngles: " + parentGo.transform.localEulerAngles.ToString()
					+ ", show beam: " + m_MeshRenderer.enabled + ", StartWidth: " + m_StartWidth
					+ ", EndWidth: " + m_EndWidth + ", StartOffset: " + m_StartOffset + ", EndOffset: " + m_EndOffset
					+ ", StartColor: " + m_StartColor.ToString() + ", EndColor: " + m_EndColor.ToString()
				);
			}
		}
		void OnDisable()
		{
			if (mEnabled)
			{
				DEBUG("OnDisable()");
				m_Mesh.Clear();

				mEnabled = false;
			}
		}

		void OnApplicationPause(bool pauseStatus)
		{
			//if (!pauseStatus) // resume
		}

		private bool toUpdateBeam = false;
		public void Update()
		{
			UpdateInputModule();

			ActivateBeam(m_ShowBeam && (m_HandInputModule && m_HandInputModule.enabled));

			ValidateParameters();

			// Redraw mesh if updated.
			if (toUpdateBeam)
			{
				CreateBeamMesh();
				toUpdateBeam = false;
			}

			if (Log.gpl.Print)
				DEBUG("Update() " + gameObject.name + " is " + (m_MeshRenderer.enabled ? "shown" : "hidden")
					+ ", show beam? " + m_ShowBeam
					+ ", start offset: " + m_StartOffset
					+ ", end offset: " + m_EndOffset
					+ ", start width: " + m_StartWidth
					+ ", end width: " + m_EndWidth
					+ ", start color: " + m_StartColor
					+ ", end color: " + m_EndColor);
		}
		#endregion

		private HandInputModule m_HandInputModule = null;
		private void UpdateInputModule()
		{
			if (m_HandInputModule != null)
				return;

			if (EventSystem.current != null)
				m_HandInputModule = EventSystem.current.gameObject.GetComponent<HandInputModule>();
		}
		private void ActivateBeam(bool show)
		{
			if (m_MeshRenderer == null)
				return;

			if (m_MeshRenderer.enabled != show)
			{
				m_MeshRenderer.enabled = show;
				DEBUG("ActivateBeam() " + m_MeshRenderer.enabled);
			}
		}

		const int meshCount = 3;
		private int maxUVAngle = 30;
		private void ValidateMesh()
		{
			/**
			 * The texture pattern should be a radiated image starting 
			 * from the texture center.
			 * If the mesh's meshCount is too low, the uv map can't keep a 
			 * good radiation shap.  Therefore the maxUVAngle should be
			 * reduced to avoid the uv area cutting the radiation circle.
			**/
			int uvAngle = 360 / meshCount;
			if (uvAngle > 30)
				maxUVAngle = 30;
			else
				maxUVAngle = uvAngle;
		}

		private List<Vector3> vertices = new List<Vector3>();
		private List<Vector2> uvs = new List<Vector2>();
		private List<Vector3> normals = new List<Vector3>();
		private List<int> indices = new List<int>();
		private List<Color32> colors = new List<Color32>();

		private Matrix4x4 mat44_rot = Matrix4x4.zero;
		private Matrix4x4 mat44_uv = Matrix4x4.zero;
		private Vector3 vec3_vertices_start = Vector3.zero;
		private Vector3 vec3_vertices_end = Vector3.zero;

		private readonly Vector2 vec2_05_05 = new Vector2(0.5f, 0.5f);
		private readonly Vector3 vec3_0_05_0 = new Vector3(0, 0.5f, 0);

		const bool makeTail = true;
		private void CreateBeamMesh()
		{
			ValidateMesh();
			m_Mesh.Clear();
			uvs.Clear();
			vertices.Clear();
			normals.Clear();
			indices.Clear();
			colors.Clear();

			mat44_rot = Matrix4x4.zero;
			mat44_uv = Matrix4x4.zero;

			for (int i = 0; i <= meshCount; i++)
			{
				int angle = (int)(i * 360.0f / meshCount);
				int UVangle = (int)(i * maxUVAngle / meshCount);
				// make rotation matrix
				mat44_rot.SetTRS(Vector3.zero, Quaternion.AngleAxis(angle, Vector3.forward), Vector3.one);
				mat44_uv.SetTRS(Vector3.zero, Quaternion.AngleAxis(UVangle, Vector3.forward), Vector3.one);

				// start
				vec3_vertices_start.y = m_StartWidth;
				vec3_vertices_start.z = m_StartOffset;
				vertices.Add(mat44_rot.MultiplyVector(vec3_vertices_start));
				uvs.Add(vec2_05_05);
				colors.Add(m_StartColor);
				normals.Add(mat44_rot.MultiplyVector(Vector3.up).normalized);

				// end
				vec3_vertices_end.y = m_EndWidth;
				vec3_vertices_end.z = m_EndOffset;
				vertices.Add(mat44_rot.MultiplyVector(vec3_vertices_end));
				Vector2 uv = mat44_uv.MultiplyVector(vec3_0_05_0);
				uv.x = uv.x + 0.5f;
				uv.y = uv.y + 0.5f;
				uvs.Add(uv);
				colors.Add(m_EndColor);
				normals.Add(mat44_rot.MultiplyVector(Vector3.up).normalized);
			}

			for (int i = 0; i < meshCount; i++)
			{
				// bd
				// ac
				int a, b, c, d;
				a = i * 2;
				b = i * 2 + 1;
				c = i * 2 + 2;
				d = i * 2 + 3;

				// first
				indices.Add(a);
				indices.Add(d);
				indices.Add(b);

				// second
				indices.Add(a);
				indices.Add(c);
				indices.Add(d);
			}

			// Make Tail
			if (makeTail)
			{
				vertices.Add(Vector3.zero);
				colors.Add(m_StartColor);
				uvs.Add(vec2_05_05);
				normals.Add(Vector3.zero);
				int tailIdx = meshCount * 2;
				for (int i = 0; i < meshCount; i++)
				{
					int idx = i * 2;

					indices.Add(tailIdx);
					indices.Add(idx + 2);
					indices.Add(idx);
				}
			}
			m_Mesh.vertices = vertices.ToArray();
			//m_Mesh.SetUVs(0, uvs);
			//m_Mesh.SetUVs(1, uvs);
			m_Mesh.colors32 = colors.ToArray();
			m_Mesh.normals = normals.ToArray();
			m_Mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
			m_Mesh.name = "HandBeam";
		}

		private Color32 colorBlue = new Color32(0, 255, 255, 177);
		private Color32 colorWhite = new Color32(255, 255, 255, 255);
		public void SetEffectiveBeam(bool effective)
		{
			m_StartColor = effective ? colorBlue : colorWhite;
			//m_EndColor = effective ? colorBlue : colorWhite;
		}
	}
}
