// DynamicBoneCollider
using UnityEngine;

[AddComponentMenu("Dynamic Bone/Dynamic Bone Collider")]
public class DynamicBoneCollider : DynamicBoneColliderBase
{
	[Tooltip("The radius of the sphere or capsule.")]
	public float m_Radius = 0.5f;

	[Tooltip("The height of the capsule.")]
	public float m_Height;

	private void OnValidate()
	{
		m_Radius = Mathf.Max(m_Radius, 0f);
		m_Height = Mathf.Max(m_Height, 0f);
	}

	public override bool Collide(ref Vector3 particlePosition, float particleRadius)
	{
		float num = m_Radius * Mathf.Abs(base.transform.lossyScale.x);
		float num2 = m_Height * 0.5f - m_Radius;
		if (num2 <= 0f)
		{
			if (m_Bound == Bound.Outside)
			{
				return OutsideSphere(ref particlePosition, particleRadius, base.transform.TransformPoint(m_Center), num);
			}
			return InsideSphere(ref particlePosition, particleRadius, base.transform.TransformPoint(m_Center), num);
		}
		Vector3 center = m_Center;
		Vector3 center2 = m_Center;
		switch (m_Direction)
		{
		case Direction.X:
			center.x -= num2;
			center2.x += num2;
			break;
		case Direction.Y:
			center.y -= num2;
			center2.y += num2;
			break;
		case Direction.Z:
			center.z -= num2;
			center2.z += num2;
			break;
		}
		if (m_Bound == Bound.Outside)
		{
			return OutsideCapsule(ref particlePosition, particleRadius, base.transform.TransformPoint(center), base.transform.TransformPoint(center2), num);
		}
		return InsideCapsule(ref particlePosition, particleRadius, base.transform.TransformPoint(center), base.transform.TransformPoint(center2), num);
	}

	private static bool OutsideSphere(ref Vector3 particlePosition, float particleRadius, Vector3 sphereCenter, float sphereRadius)
	{
		float num = sphereRadius + particleRadius;
		float num2 = num * num;
		Vector3 vector = particlePosition - sphereCenter;
		float sqrMagnitude = vector.sqrMagnitude;
		if (sqrMagnitude > 0f && sqrMagnitude < num2)
		{
			float num3 = Mathf.Sqrt(sqrMagnitude);
			particlePosition = sphereCenter + vector * (num / num3);
			return true;
		}
		return false;
	}

	private static bool InsideSphere(ref Vector3 particlePosition, float particleRadius, Vector3 sphereCenter, float sphereRadius)
	{
		float num = sphereRadius - particleRadius;
		float num2 = num * num;
		Vector3 vector = particlePosition - sphereCenter;
		float sqrMagnitude = vector.sqrMagnitude;
		if (sqrMagnitude > num2)
		{
			float num3 = Mathf.Sqrt(sqrMagnitude);
			particlePosition = sphereCenter + vector * (num / num3);
			return true;
		}
		return false;
	}

	private static bool OutsideCapsule(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius)
	{
		float num = capsuleRadius + particleRadius;
		float num2 = num * num;
		Vector3 vector = capsuleP1 - capsuleP0;
		Vector3 vector2 = particlePosition - capsuleP0;
		float num3 = Vector3.Dot(vector2, vector);
		if (num3 <= 0f)
		{
			float sqrMagnitude = vector2.sqrMagnitude;
			if (sqrMagnitude > 0f && sqrMagnitude < num2)
			{
				float num4 = Mathf.Sqrt(sqrMagnitude);
				particlePosition = capsuleP0 + vector2 * (num / num4);
				return true;
			}
		}
		else
		{
			float sqrMagnitude2 = vector.sqrMagnitude;
			if (num3 >= sqrMagnitude2)
			{
				vector2 = particlePosition - capsuleP1;
				float sqrMagnitude3 = vector2.sqrMagnitude;
				if (sqrMagnitude3 > 0f && sqrMagnitude3 < num2)
				{
					float num5 = Mathf.Sqrt(sqrMagnitude3);
					particlePosition = capsuleP1 + vector2 * (num / num5);
					return true;
				}
			}
			else if (sqrMagnitude2 > 0f)
			{
				num3 /= sqrMagnitude2;
				vector2 -= vector * num3;
				float sqrMagnitude4 = vector2.sqrMagnitude;
				if (sqrMagnitude4 > 0f && sqrMagnitude4 < num2)
				{
					float num6 = Mathf.Sqrt(sqrMagnitude4);
					particlePosition += vector2 * ((num - num6) / num6);
					return true;
				}
			}
		}
		return false;
	}

	private static bool InsideCapsule(ref Vector3 particlePosition, float particleRadius, Vector3 capsuleP0, Vector3 capsuleP1, float capsuleRadius)
	{
		float num = capsuleRadius - particleRadius;
		float num2 = num * num;
		Vector3 vector = capsuleP1 - capsuleP0;
		Vector3 vector2 = particlePosition - capsuleP0;
		float num3 = Vector3.Dot(vector2, vector);
		if (num3 <= 0f)
		{
			float sqrMagnitude = vector2.sqrMagnitude;
			if (sqrMagnitude > num2)
			{
				float num4 = Mathf.Sqrt(sqrMagnitude);
				particlePosition = capsuleP0 + vector2 * (num / num4);
				return true;
			}
		}
		else
		{
			float sqrMagnitude2 = vector.sqrMagnitude;
			if (num3 >= sqrMagnitude2)
			{
				vector2 = particlePosition - capsuleP1;
				float sqrMagnitude3 = vector2.sqrMagnitude;
				if (sqrMagnitude3 > num2)
				{
					float num5 = Mathf.Sqrt(sqrMagnitude3);
					particlePosition = capsuleP1 + vector2 * (num / num5);
					return true;
				}
			}
			else if (sqrMagnitude2 > 0f)
			{
				num3 /= sqrMagnitude2;
				vector2 -= vector * num3;
				float sqrMagnitude4 = vector2.sqrMagnitude;
				if (sqrMagnitude4 > num2)
				{
					float num6 = Mathf.Sqrt(sqrMagnitude4);
					particlePosition += vector2 * ((num - num6) / num6);
					return true;
				}
			}
		}
		return false;
	}

	private void OnDrawGizmosSelected()
	{
		if (!base.enabled)
		{
			return;
		}
		if (m_Bound == Bound.Outside)
		{
			Gizmos.color = Color.yellow;
		}
		else
		{
			Gizmos.color = Color.magenta;
		}
		float radius = m_Radius * Mathf.Abs(base.transform.lossyScale.x);
		float num = m_Height * 0.5f - m_Radius;
		if (num <= 0f)
		{
			Gizmos.DrawWireSphere(base.transform.TransformPoint(m_Center), radius);
			return;
		}
		Vector3 center = m_Center;
		Vector3 center2 = m_Center;
		switch (m_Direction)
		{
		case Direction.X:
			center.x -= num;
			center2.x += num;
			break;
		case Direction.Y:
			center.y -= num;
			center2.y += num;
			break;
		case Direction.Z:
			center.z -= num;
			center2.z += num;
			break;
		}
		Gizmos.DrawWireSphere(base.transform.TransformPoint(center), radius);
		Gizmos.DrawWireSphere(base.transform.TransformPoint(center2), radius);
	}
}
