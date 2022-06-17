// DynamicBone
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Dynamic Bone/Dynamic Bone")]
public class DynamicBone : MonoBehaviour
{
	public enum UpdateMode
	{
		Normal,
		AnimatePhysics,
		UnscaledTime,
		Default
	}

	public enum FreezeAxis
	{
		None,
		X,
		Y,
		Z
	}

	private class Particle
	{
		public Transform m_Transform;

		public int m_ParentIndex = -1;

		public float m_Damping;

		public float m_Elasticity;

		public float m_Stiffness;

		public float m_Inert;

		public float m_Friction;

		public float m_Radius;

		public float m_BoneLength;

		public bool m_isCollide;

		public Vector3 m_Position = Vector3.zero;

		public Vector3 m_PrevPosition = Vector3.zero;

		public Vector3 m_EndOffset = Vector3.zero;

		public Vector3 m_InitLocalPosition = Vector3.zero;

		public Quaternion m_InitLocalRotation = Quaternion.identity;
	}

	[Tooltip("The root of the transform hierarchy to apply physics.")]
	public Transform m_Root;

	[Tooltip("Internal physics simulation rate.")]
	public float m_UpdateRate = 60f;

	public UpdateMode m_UpdateMode = UpdateMode.Default;

	[Tooltip("How much the bones slowed down.")]
	[Range(0f, 1f)]
	public float m_Damping = 0.1f;

	public AnimationCurve m_DampingDistrib;

	[Tooltip("How much the force applied to return each bone to original orientation.")]
	[Range(0f, 4f)]
	public float m_Elasticity = 0.1f;

	public AnimationCurve m_ElasticityDistrib;

	[Tooltip("How much bone's original orientation are preserved.")]
	[Range(0f, 1f)]
	public float m_Stiffness = 0.1f;

	public AnimationCurve m_StiffnessDistrib;

	[Tooltip("How much character's position change is ignored in physics simulation.")]
	[Range(0f, 1f)]
	public float m_Inert;

	public AnimationCurve m_InertDistrib;

	[Tooltip("How much the bones slowed down when collide.")]
	public float m_Friction;

	public AnimationCurve m_FrictionDistrib;

	[Tooltip("Each bone can be a sphere to collide with colliders. Radius describe sphere's size.")]
	public float m_Radius;

	public AnimationCurve m_RadiusDistrib;

	[Tooltip("If End Length is not zero, an extra bone is generated at the end of transform hierarchy.")]
	public float m_EndLength;

	[Tooltip("If End Offset is not zero, an extra bone is generated at the end of transform hierarchy.")]
	public Vector3 m_EndOffset = Vector3.zero;

	[Tooltip("The force apply to bones. Partial force apply to character's initial pose is cancelled out.")]
	public Vector3 m_Gravity = Vector3.zero;

	[Tooltip("The force apply to bones.")]
	public Vector3 m_Force = Vector3.zero;

	[Tooltip("Collider objects interact with the bones.")]
	public List<DynamicBoneColliderBase> m_Colliders;

	[Tooltip("Bones exclude from physics simulation.")]
	public List<Transform> m_Exclusions;

	[Tooltip("Constrain bones to move on specified plane.")]
	public FreezeAxis m_FreezeAxis;

	[Tooltip("Disable physics simulation automatically if character is far from camera or player.")]
	public bool m_DistantDisable;

	public Transform m_ReferenceObject;

	public float m_DistanceToObject = 20f;

	private Vector3 m_LocalGravity = Vector3.zero;

	private Vector3 m_ObjectMove = Vector3.zero;

	private Vector3 m_ObjectPrevPosition = Vector3.zero;

	private float m_BoneTotalLength;

	private float m_ObjectScale = 1f;

	private float m_Time;

	private float m_Weight = 1f;

	private bool m_DistantDisabled;

	private List<Particle> m_Particles = new List<Particle>();

	private void Start()
	{
		SetupParticles();
	}

	private void FixedUpdate()
	{
		if (m_UpdateMode == UpdateMode.AnimatePhysics)
		{
			PreUpdate();
		}
	}

	private void Update()
	{
		if (m_UpdateMode != UpdateMode.AnimatePhysics)
		{
			PreUpdate();
			UpdateParameters();
		}
	}

	private void LateUpdate()
	{
		if (m_DistantDisable)
		{
			CheckDistance();
		}
		if (m_Weight > 0f && (!m_DistantDisable || !m_DistantDisabled))
		{
			float t = ((m_UpdateMode == UpdateMode.UnscaledTime) ? Time.unscaledDeltaTime : Time.deltaTime);
			UpdateDynamicBones(t);
		}
	}

	private void PreUpdate()
	{
		if (m_Weight > 0f && (!m_DistantDisable || !m_DistantDisabled))
		{
			InitTransforms();
		}
	}

	private void CheckDistance()
	{
		Transform referenceObject = m_ReferenceObject;
		if (referenceObject == null && Camera.main != null)
		{
			referenceObject = Camera.main.transform;
		}
		if (!(referenceObject != null))
		{
			return;
		}
		bool flag = (referenceObject.position - base.transform.position).sqrMagnitude > m_DistanceToObject * m_DistanceToObject;
		if (flag != m_DistantDisabled)
		{
			if (!flag)
			{
				ResetParticlesPosition();
			}
			m_DistantDisabled = flag;
		}
	}

	private void OnEnable()
	{
		ResetParticlesPosition();
	}

	private void OnDisable()
	{
		InitTransforms();
	}

	private void OnValidate()
	{
		m_UpdateRate = Mathf.Max(m_UpdateRate, 0f);
		m_Damping = Mathf.Clamp01(m_Damping);
		m_Stiffness = Mathf.Clamp01(m_Stiffness);
		m_Inert = Mathf.Clamp01(m_Inert);
		m_Friction = Mathf.Clamp01(m_Friction);
		m_Radius = Mathf.Max(m_Radius, 0f);
		if (Application.isEditor && Application.isPlaying)
		{
			InitTransforms();
			SetupParticles();
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (!base.enabled || m_Root == null)
		{
			return;
		}
		if (Application.isEditor && !Application.isPlaying && base.transform.hasChanged)
		{
			InitTransforms();
			SetupParticles();
		}
		Gizmos.color = Color.white;
		for (int i = 0; i < m_Particles.Count; i++)
		{
			Particle particle = m_Particles[i];
			if (particle.m_ParentIndex >= 0)
			{
				Particle particle2 = m_Particles[particle.m_ParentIndex];
				Gizmos.DrawLine(particle.m_Position, particle2.m_Position);
			}
			if (particle.m_Radius > 0f)
			{
				Gizmos.DrawWireSphere(particle.m_Position, particle.m_Radius * m_ObjectScale);
			}
		}
	}

	public void SetWeight(float w)
	{
		if (m_Weight != w)
		{
			if (w == 0f)
			{
				InitTransforms();
			}
			else if (m_Weight == 0f)
			{
				ResetParticlesPosition();
			}
			m_Weight = w;
		}
	}

	public float GetWeight()
	{
		return m_Weight;
	}

	private void UpdateDynamicBones(float t)
	{
		if (m_Root == null)
		{
			return;
		}
		m_ObjectScale = Mathf.Abs(base.transform.lossyScale.x);
		m_ObjectMove = base.transform.position - m_ObjectPrevPosition;
		m_ObjectPrevPosition = base.transform.position;
		int num = 1;
		float timeVar = 1f;
		if (m_UpdateMode == UpdateMode.Default)
		{
			timeVar = ((!(m_UpdateRate > 0f)) ? Time.deltaTime : (Time.deltaTime * m_UpdateRate));
		}
		else if (m_UpdateRate > 0f)
		{
			float num2 = 1f / m_UpdateRate;
			m_Time += t;
			num = 0;
			while (m_Time >= num2)
			{
				m_Time -= num2;
				if (++num >= 3)
				{
					m_Time = 0f;
					break;
				}
			}
		}
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				UpdateParticles1(timeVar);
				UpdateParticles2(timeVar);
				m_ObjectMove = Vector3.zero;
			}
		}
		else
		{
			SkipUpdateParticles();
		}
		ApplyParticlesToTransforms();
	}

	public void SetupParticles()
	{
		m_Particles.Clear();
		if (!(m_Root == null))
		{
			m_LocalGravity = m_Root.InverseTransformDirection(m_Gravity);
			m_ObjectScale = Mathf.Abs(base.transform.lossyScale.x);
			m_ObjectPrevPosition = base.transform.position;
			m_ObjectMove = Vector3.zero;
			m_BoneTotalLength = 0f;
			AppendParticles(m_Root, -1, 0f);
			UpdateParameters();
		}
	}

	private void AppendParticles(Transform b, int parentIndex, float boneLength)
	{
		Particle particle = new Particle();
		particle.m_Transform = b;
		particle.m_ParentIndex = parentIndex;
		if (b != null)
		{
			particle.m_Position = (particle.m_PrevPosition = b.position);
			particle.m_InitLocalPosition = b.localPosition;
			particle.m_InitLocalRotation = b.localRotation;
		}
		else
		{
			Transform transform = m_Particles[parentIndex].m_Transform;
			if (m_EndLength > 0f)
			{
				Transform parent = transform.parent;
				if (parent != null)
				{
					particle.m_EndOffset = transform.InverseTransformPoint(transform.position * 2f - parent.position) * m_EndLength;
				}
				else
				{
					particle.m_EndOffset = new Vector3(m_EndLength, 0f, 0f);
				}
			}
			else
			{
				particle.m_EndOffset = transform.InverseTransformPoint(base.transform.TransformDirection(m_EndOffset) + transform.position);
			}
			particle.m_Position = (particle.m_PrevPosition = transform.TransformPoint(particle.m_EndOffset));
		}
		if (parentIndex >= 0)
		{
			boneLength += (m_Particles[parentIndex].m_Transform.position - particle.m_Position).magnitude;
			particle.m_BoneLength = boneLength;
			m_BoneTotalLength = Mathf.Max(m_BoneTotalLength, boneLength);
		}
		int count = m_Particles.Count;
		m_Particles.Add(particle);
		if (!(b != null))
		{
			return;
		}
		for (int i = 0; i < b.childCount; i++)
		{
			Transform child = b.GetChild(i);
			bool flag = false;
			if (m_Exclusions != null)
			{
				flag = m_Exclusions.Contains(child);
			}
			if (!flag)
			{
				AppendParticles(child, count, boneLength);
			}
			else if (m_EndLength > 0f || m_EndOffset != Vector3.zero)
			{
				AppendParticles(null, count, boneLength);
			}
		}
		if (b.childCount == 0 && (m_EndLength > 0f || m_EndOffset != Vector3.zero))
		{
			AppendParticles(null, count, boneLength);
		}
	}

	public void UpdateParameters()
	{
		if (m_Root == null)
		{
			return;
		}
		m_LocalGravity = m_Root.InverseTransformDirection(m_Gravity);
		for (int i = 0; i < m_Particles.Count; i++)
		{
			Particle particle = m_Particles[i];
			particle.m_Damping = m_Damping;
			particle.m_Elasticity = m_Elasticity;
			particle.m_Stiffness = m_Stiffness;
			particle.m_Inert = m_Inert;
			particle.m_Friction = m_Friction;
			particle.m_Radius = m_Radius;
			if (m_BoneTotalLength > 0f)
			{
				float time = particle.m_BoneLength / m_BoneTotalLength;
				if (m_DampingDistrib != null && m_DampingDistrib.keys.Length != 0)
				{
					particle.m_Damping *= m_DampingDistrib.Evaluate(time);
				}
				if (m_ElasticityDistrib != null && m_ElasticityDistrib.keys.Length != 0)
				{
					particle.m_Elasticity *= m_ElasticityDistrib.Evaluate(time);
				}
				if (m_StiffnessDistrib != null && m_StiffnessDistrib.keys.Length != 0)
				{
					particle.m_Stiffness *= m_StiffnessDistrib.Evaluate(time);
				}
				if (m_InertDistrib != null && m_InertDistrib.keys.Length != 0)
				{
					particle.m_Inert *= m_InertDistrib.Evaluate(time);
				}
				if (m_FrictionDistrib != null && m_FrictionDistrib.keys.Length != 0)
				{
					particle.m_Friction *= m_FrictionDistrib.Evaluate(time);
				}
				if (m_RadiusDistrib != null && m_RadiusDistrib.keys.Length != 0)
				{
					particle.m_Radius *= m_RadiusDistrib.Evaluate(time);
				}
			}
			particle.m_Damping = Mathf.Clamp01(particle.m_Damping);
			particle.m_Stiffness = Mathf.Clamp01(particle.m_Stiffness);
			particle.m_Inert = Mathf.Clamp01(particle.m_Inert);
			particle.m_Friction = Mathf.Clamp01(particle.m_Friction);
			particle.m_Radius = Mathf.Max(particle.m_Radius, 0f);
		}
	}

	private void InitTransforms()
	{
		for (int i = 0; i < m_Particles.Count; i++)
		{
			Particle particle = m_Particles[i];
			if (particle.m_Transform != null)
			{
				particle.m_Transform.localPosition = particle.m_InitLocalPosition;
				particle.m_Transform.localRotation = particle.m_InitLocalRotation;
			}
		}
	}

	private void ResetParticlesPosition()
	{
		for (int i = 0; i < m_Particles.Count; i++)
		{
			Particle particle = m_Particles[i];
			if (particle.m_Transform != null)
			{
				particle.m_Position = (particle.m_PrevPosition = particle.m_Transform.position);
			}
			else
			{
				Transform transform = m_Particles[particle.m_ParentIndex].m_Transform;
				particle.m_Position = (particle.m_PrevPosition = transform.TransformPoint(particle.m_EndOffset));
			}
			particle.m_isCollide = false;
		}
		m_ObjectPrevPosition = base.transform.position;
	}

	private void UpdateParticles1(float timeVar)
	{
		Vector3 gravity = m_Gravity;
		Vector3 normalized = m_Gravity.normalized;
		Vector3 lhs = m_Root.TransformDirection(m_LocalGravity);
		Vector3 vector = normalized * Mathf.Max(Vector3.Dot(lhs, normalized), 0f);
		gravity -= vector;
		gravity = (gravity + m_Force) * (m_ObjectScale * timeVar);
		for (int i = 0; i < m_Particles.Count; i++)
		{
			Particle particle = m_Particles[i];
			if (particle.m_ParentIndex >= 0)
			{
				Vector3 vector2 = particle.m_Position - particle.m_PrevPosition;
				Vector3 vector3 = m_ObjectMove * particle.m_Inert;
				particle.m_PrevPosition = particle.m_Position + vector3;
				float num = particle.m_Damping;
				if (particle.m_isCollide)
				{
					num += particle.m_Friction;
					if (num > 1f)
					{
						num = 1f;
					}
					particle.m_isCollide = false;
				}
				particle.m_Position += vector2 * (1f - num) + gravity + vector3;
			}
			else
			{
				particle.m_PrevPosition = particle.m_Position;
				particle.m_Position = particle.m_Transform.position;
			}
		}
	}

	private void UpdateParticles2(float timeVar)
	{
		Plane plane = default(Plane);
		for (int i = 1; i < m_Particles.Count; i++)
		{
			Particle particle = m_Particles[i];
			Particle particle2 = m_Particles[particle.m_ParentIndex];
			float num = ((!(particle.m_Transform != null)) ? particle2.m_Transform.localToWorldMatrix.MultiplyVector(particle.m_EndOffset).magnitude : (particle2.m_Transform.position - particle.m_Transform.position).magnitude);
			float num2 = Mathf.Lerp(1f, particle.m_Stiffness, m_Weight);
			if (num2 > 0f || particle.m_Elasticity > 0f)
			{
				Matrix4x4 localToWorldMatrix = particle2.m_Transform.localToWorldMatrix;
				localToWorldMatrix.SetColumn(3, particle2.m_Position);
				Vector3 vector = ((!(particle.m_Transform != null)) ? localToWorldMatrix.MultiplyPoint3x4(particle.m_EndOffset) : localToWorldMatrix.MultiplyPoint3x4(particle.m_Transform.localPosition));
				Vector3 vector2 = vector - particle.m_Position;
				particle.m_Position += vector2 * (particle.m_Elasticity * timeVar);
				if (num2 > 0f)
				{
					vector2 = vector - particle.m_Position;
					float magnitude = vector2.magnitude;
					float num3 = num * (1f - num2) * 2f;
					if (magnitude > num3)
					{
						particle.m_Position += vector2 * ((magnitude - num3) / magnitude);
					}
				}
			}
			if (m_Colliders != null)
			{
				float particleRadius = particle.m_Radius * m_ObjectScale;
				for (int j = 0; j < m_Colliders.Count; j++)
				{
					DynamicBoneColliderBase dynamicBoneColliderBase = m_Colliders[j];
					if (dynamicBoneColliderBase != null && dynamicBoneColliderBase.enabled)
					{
						particle.m_isCollide |= dynamicBoneColliderBase.Collide(ref particle.m_Position, particleRadius);
					}
				}
			}
			if (m_FreezeAxis != 0)
			{
				switch (m_FreezeAxis)
				{
				case FreezeAxis.X:
					plane.SetNormalAndPosition(particle2.m_Transform.right, particle2.m_Position);
					break;
				case FreezeAxis.Y:
					plane.SetNormalAndPosition(particle2.m_Transform.up, particle2.m_Position);
					break;
				case FreezeAxis.Z:
					plane.SetNormalAndPosition(particle2.m_Transform.forward, particle2.m_Position);
					break;
				}
				particle.m_Position -= plane.normal * plane.GetDistanceToPoint(particle.m_Position);
			}
			Vector3 vector3 = particle2.m_Position - particle.m_Position;
			float magnitude2 = vector3.magnitude;
			if (magnitude2 > 0f)
			{
				particle.m_Position += vector3 * ((magnitude2 - num) / magnitude2);
			}
		}
	}

	private void SkipUpdateParticles()
	{
		for (int i = 0; i < m_Particles.Count; i++)
		{
			Particle particle = m_Particles[i];
			if (particle.m_ParentIndex >= 0)
			{
				particle.m_PrevPosition += m_ObjectMove;
				particle.m_Position += m_ObjectMove;
				Particle particle2 = m_Particles[particle.m_ParentIndex];
				float num = ((!(particle.m_Transform != null)) ? particle2.m_Transform.localToWorldMatrix.MultiplyVector(particle.m_EndOffset).magnitude : (particle2.m_Transform.position - particle.m_Transform.position).magnitude);
				float num2 = Mathf.Lerp(1f, particle.m_Stiffness, m_Weight);
				if (num2 > 0f)
				{
					Matrix4x4 localToWorldMatrix = particle2.m_Transform.localToWorldMatrix;
					localToWorldMatrix.SetColumn(3, particle2.m_Position);
					Vector3 vector = ((!(particle.m_Transform != null)) ? localToWorldMatrix.MultiplyPoint3x4(particle.m_EndOffset) : localToWorldMatrix.MultiplyPoint3x4(particle.m_Transform.localPosition));
					Vector3 vector2 = vector - particle.m_Position;
					float magnitude = vector2.magnitude;
					float num3 = num * (1f - num2) * 2f;
					if (magnitude > num3)
					{
						particle.m_Position += vector2 * ((magnitude - num3) / magnitude);
					}
				}
				Vector3 vector3 = particle2.m_Position - particle.m_Position;
				float magnitude2 = vector3.magnitude;
				if (magnitude2 > 0f)
				{
					particle.m_Position += vector3 * ((magnitude2 - num) / magnitude2);
				}
			}
			else
			{
				particle.m_PrevPosition = particle.m_Position;
				particle.m_Position = particle.m_Transform.position;
			}
		}
	}

	private static Vector3 MirrorVector(Vector3 v, Vector3 axis)
	{
		return v - axis * (Vector3.Dot(v, axis) * 2f);
	}

	private void ApplyParticlesToTransforms()
	{
		for (int i = 1; i < m_Particles.Count; i++)
		{
			Particle particle = m_Particles[i];
			Particle particle2 = m_Particles[particle.m_ParentIndex];
			if (particle2.m_Transform.childCount <= 1)
			{
				Vector3 direction = ((!(particle.m_Transform != null)) ? particle.m_EndOffset : particle.m_Transform.localPosition);
				Vector3 toDirection = particle.m_Position - particle2.m_Position;
				Quaternion quaternion = Quaternion.FromToRotation(particle2.m_Transform.TransformDirection(direction), toDirection);
				particle2.m_Transform.rotation = quaternion * particle2.m_Transform.rotation;
			}
			if (particle.m_Transform != null)
			{
				particle.m_Transform.position = particle.m_Position;
			}
		}
	}
}
