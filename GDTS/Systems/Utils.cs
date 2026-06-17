using UnityEngine;

public class Utils
{
	public static bool Equals(Vector3 a, Vector3 b)
	{
		return AlmostCloseTo(a.x, b.x) && AlmostCloseTo(a.y, b.y) && AlmostCloseTo(a.z, b.z);
	}
	public static float SqrDistance(Vector2 a, Vector2 b)
	{
		Vector2 ab = b - a;
		return ab.x * ab.x + ab.y * ab.y;
	}
	
	public static bool IsOverlapping(Bounds bounds, Bounds other)
	{
		return bounds.min.x <= other.max.x && bounds.max.x >= other.min.x &&
		       bounds.min.y <= other.max.y && bounds.max.y >= other.min.y;
	}
	
    public static float GetDistanceBetween(Bounds a, Bounds b)
    {
        if (IsOverlapping(a, b))
            return 0;
        
        Vector3 p1 = b.ClosestPoint(a.center);
        Vector3 p2 = a.ClosestPoint(p1);

        return (p1 - p2).magnitude;
    }
    
    public static float GetDistanceOnRay(Bounds a, Bounds b, Vector3 dir)
	{
		bool isXMove = (dir.x != 0 && dir.y == 0 && dir.z == 0);
		bool isYMove = (dir.z != 0 && dir.x == 0 && dir.z == 0);
		bool isZMove = (dir.z != 0 && dir.x == 0 && dir.y == 0);

		float xThisSmall = a.min.x;
		float xThisBig = a.max.x;
		float yThisSmall = a.min.y;
		float yThisBig = a.max.y;
		float zThisSmall = a.min.z;
		float zThisBig = a.max.z;

		float xOtherSmall = b.min.x;
		float xOtherBig = b.max.x;
		float yOtherSmall = b.min.y;
		float yOtherBig = b.max.y;
		float zOtherSmall = b.min.z;
		float zOtherBig = b.max.z;

		bool xContact = (xThisSmall >= xOtherSmall && xThisSmall <= xOtherBig) ||
			(xThisBig >= xOtherSmall && xThisBig <= xOtherBig) ||
			(xOtherSmall >= xThisSmall && xOtherSmall <= xThisBig) ||
			(xOtherBig >= xThisSmall && xOtherBig <= xThisBig);

		bool yContact = (yThisSmall >= yOtherSmall && yThisSmall <= yOtherBig) ||
			(yThisBig >= yOtherSmall && yThisBig <= yOtherBig) ||
			(yOtherSmall >= yThisSmall && yOtherSmall <= yThisBig) ||
			(yOtherBig >= yThisSmall && yOtherBig <= yThisBig);

		bool zContact = (zThisSmall >= zOtherSmall && zThisSmall <= zOtherBig) ||
			(zThisBig >= zOtherSmall && zThisBig <= zOtherBig) ||
			(zOtherSmall >= zThisSmall && zOtherSmall <= zThisBig) ||
			(zOtherBig >= zThisSmall && zOtherBig <= zThisBig);

		if (xContact && yContact && zContact)
		{
			return 0;
		}

		if (dir.magnitude == 0)
			return float.MaxValue;

		// Move the hitbox until at least one dimension intersect
		float firstDist = 0;
		float secondDist = 0;

		if (!xContact && !yContact && !zContact)
		{
			float xDiff = 0;
			float yDiff = 0;
			float zDiff = 0;

			if (xOtherSmall > xThisBig)
				xDiff = xOtherSmall - xThisBig;
			else
				xDiff = xOtherBig - xThisSmall;

			if (yOtherSmall > yThisBig)
				yDiff = yOtherSmall - yThisBig;
			else
				yDiff = yOtherBig - yThisSmall;

			if (zOtherSmall > zThisBig)
				zDiff = zOtherSmall - zThisBig;
			else
				zDiff = zOtherBig - zThisSmall;

			float xFirstDist = -1;
			float yFirstDist = -1;
			float zFirstDist = -1;

			if (dir.x * xDiff > 0)
			{
				xFirstDist = xDiff / dir.x;
			}

			if (dir.y * yDiff > 0)
			{
				yFirstDist = yDiff / dir.y;
			}

			if (dir.z * zDiff > 0)
			{
				zFirstDist = zDiff / dir.z;
			}

			if (xFirstDist < 0 && yFirstDist < 0 && zFirstDist < 0)
				return float.MaxValue;

			if ((xFirstDist > 0 && yFirstDist < 0 && zFirstDist < 0)
				|| (xFirstDist > 0 && yFirstDist > 0 && zFirstDist < 0 && xFirstDist < yFirstDist)
				|| (xFirstDist > 0 && zFirstDist > 0 && yFirstDist < 0 && xFirstDist < zFirstDist)
				|| (xFirstDist > 0 && yFirstDist > 0 && zFirstDist > 0 && xFirstDist < yFirstDist &&
				xFirstDist < zFirstDist))
			{
				xContact = true;

				xThisSmall += dir.x * xFirstDist;
				xThisBig += dir.x * xFirstDist;
				yThisSmall += dir.y * xFirstDist;
				yThisBig += dir.y * xFirstDist;
				zThisSmall += dir.z * xFirstDist;
				zThisBig += dir.z * xFirstDist;

				yContact = (yThisSmall >= yOtherSmall && yThisSmall <= yOtherBig) ||
					(yThisBig >= yOtherSmall && yThisBig <= yOtherBig) ||
					(yOtherSmall >= yThisSmall && yOtherSmall <= yThisBig) ||
					(yOtherBig >= yThisSmall && yOtherBig <= yThisBig);

				zContact = (zThisSmall >= zOtherSmall && zThisSmall <= zOtherBig) ||
					(zThisBig >= zOtherSmall && zThisBig <= zOtherBig) ||
					(zOtherSmall >= zThisSmall && zOtherSmall <= zThisBig) ||
					(zOtherBig >= zThisSmall && zOtherBig <= zThisBig);

				if (yContact && zContact)
					return xFirstDist;

				firstDist = xFirstDist;
			}
			else if ((yFirstDist > 0 && xFirstDist < 0 && zFirstDist < 0)
				|| (xFirstDist > 0 && yFirstDist > 0 && zFirstDist < 0 && yFirstDist < xFirstDist)
				|| (yFirstDist > 0 && zFirstDist > 0 && xFirstDist < 0 && yFirstDist < zFirstDist)
				|| (xFirstDist > 0 && yFirstDist > 0 && zFirstDist > 0 && yFirstDist < xFirstDist &&
				yFirstDist < zFirstDist))
			{
				yContact = true;

				xThisSmall += dir.x * yFirstDist;
				xThisBig += dir.x * yFirstDist;
				yThisSmall += dir.y * yFirstDist;
				yThisBig += dir.y * yFirstDist;
				zThisSmall += dir.z * yFirstDist;
				zThisBig += dir.z * yFirstDist;

				xContact = (xThisSmall >= xOtherSmall && xThisSmall <= xOtherBig) ||
					(xThisBig >= xOtherSmall && xThisBig <= xOtherBig) ||
					(xOtherSmall >= xThisSmall && xOtherSmall <= xThisBig) ||
					(xOtherBig >= xThisSmall && xOtherBig <= xThisBig);

				zContact = (zThisSmall >= zOtherSmall && zThisSmall <= zOtherBig) ||
					(zThisBig >= zOtherSmall && zThisBig <= zOtherBig) ||
					(zOtherSmall >= zThisSmall && zOtherSmall <= zThisBig) ||
					(zOtherBig >= zThisSmall && zOtherBig <= zThisBig);

				if (xContact && zContact)
					return yFirstDist;

				firstDist = yFirstDist;
			}
			else
			{
				zContact = true;

				xThisSmall += dir.x * zFirstDist;
				xThisBig += dir.x * zFirstDist;
				yThisSmall += dir.y * zFirstDist;
				yThisBig += dir.y * zFirstDist;
				zThisSmall += dir.z * zFirstDist;
				zThisBig += dir.z * zFirstDist;

				xContact = (xThisSmall >= xOtherSmall && xThisSmall <= xOtherBig) ||
					(xThisBig >= xOtherSmall && xThisBig <= xOtherBig) ||
					(xOtherSmall >= xThisSmall && xOtherSmall <= xThisBig) ||
					(xOtherBig >= xThisSmall && xOtherBig <= xThisBig);

				yContact = (yThisSmall >= yOtherSmall && yThisSmall <= yOtherBig) ||
					(yThisBig >= yOtherSmall && yThisBig <= yOtherBig) ||
					(yOtherSmall >= yThisSmall && yOtherSmall <= yThisBig) ||
					(yOtherBig >= yThisSmall && yOtherBig <= yThisBig);

				if (xContact && yContact)
					return zFirstDist;

				firstDist = zFirstDist;
			}
		}

		if (xContact && !yContact && !zContact)
		{
			float yDiff = 0;
			float zDiff = 0;

			if (yOtherSmall > yThisBig)
				yDiff = yOtherSmall - yThisBig;
			else
				yDiff = yOtherBig - yThisSmall;

			if (zOtherSmall > zThisBig)
				zDiff = zOtherSmall - zThisBig;
			else
				zDiff = zOtherBig - zThisSmall;

			float ySecondDist = -1;
			float zSecondDist = -1;

			if (dir.y * yDiff > 0)
			{
				ySecondDist = yDiff / dir.y;
			}

			if (dir.z * zDiff > 0)
			{
				zSecondDist = zDiff / dir.z;
			}

			if (ySecondDist < 0 && zSecondDist < 0)
				return float.MaxValue;

			if ((ySecondDist > 0 && zSecondDist < 0)
				|| (ySecondDist > 0 && zSecondDist > 0 && ySecondDist < zSecondDist))
			{
				yContact = true;

				xThisSmall += dir.x * ySecondDist;
				xThisBig += dir.x * ySecondDist;
				yThisSmall += dir.y * ySecondDist;
				yThisBig += dir.y * ySecondDist;
				zThisSmall += dir.z * ySecondDist;
				zThisBig += dir.z * ySecondDist;

				xContact = (xThisSmall >= xOtherSmall && xThisSmall <= xOtherBig) ||
					(xThisBig >= xOtherSmall && xThisBig <= xOtherBig) ||
					(xOtherSmall >= xThisSmall && xOtherSmall <= xThisBig) ||
					(xOtherBig >= xThisSmall && xOtherBig <= xThisBig);

				zContact = (zThisSmall >= zOtherSmall && zThisSmall <= zOtherBig) ||
					(zThisBig >= zOtherSmall && zThisBig <= zOtherBig) ||
					(zOtherSmall >= zThisSmall && zOtherSmall <= zThisBig) ||
					(zOtherBig >= zThisSmall && zOtherBig <= zThisBig);

				if (xContact && zContact)
					return firstDist + ySecondDist;

				secondDist = ySecondDist;
			}
			else
			{
				zContact = true;

				xThisSmall += dir.x * zSecondDist;
				xThisBig += dir.x * zSecondDist;
				yThisSmall += dir.y * zSecondDist;
				yThisBig += dir.y * zSecondDist;
				zThisSmall += dir.z * zSecondDist;
				zThisBig += dir.z * zSecondDist;

				xContact = (xThisSmall >= xOtherSmall && xThisSmall <= xOtherBig) ||
					(xThisBig >= xOtherSmall && xThisBig <= xOtherBig) ||
					(xOtherSmall >= xThisSmall && xOtherSmall <= xThisBig) ||
					(xOtherBig >= xThisSmall && xOtherBig <= xThisBig);

				yContact = (yThisSmall >= yOtherSmall && yThisSmall <= yOtherBig) ||
					(yThisBig >= yOtherSmall && yThisBig <= yOtherBig) ||
					(yOtherSmall >= yThisSmall && yOtherSmall <= yThisBig) ||
					(yOtherBig >= yThisSmall && yOtherBig <= yThisBig);

				if (xContact && yContact)
					return firstDist + zSecondDist;

				secondDist = zSecondDist;
			}
		}
		else if (yContact && !xContact && !zContact)
		{
			float xDiff = 0;
			float zDiff = 0;

			if (xOtherSmall > xThisBig)
				xDiff = xOtherSmall - xThisBig;
			else
				xDiff = xOtherBig - xThisSmall;

			if (zOtherSmall > zThisBig)
				zDiff = zOtherSmall - zThisBig;
			else
				zDiff = zOtherBig - zThisSmall;

			float xSecondDist = -1;
			float zSecondDist = -1;

			if (dir.x * xDiff > 0)
			{
				xSecondDist = xDiff / dir.x;
			}

			if (dir.z * zDiff > 0)
			{
				zSecondDist = zDiff / dir.z;
			}

			if (xSecondDist < 0 && zSecondDist < 0)
				return float.MaxValue;

			if ((xSecondDist > 0 && zSecondDist < 0)
				|| (xSecondDist > 0 && zSecondDist > 0 && xSecondDist < zSecondDist))
			{
				xContact = true;

				xThisSmall += dir.x * xSecondDist;
				xThisBig += dir.x * xSecondDist;
				yThisSmall += dir.y * xSecondDist;
				yThisBig += dir.y * xSecondDist;
				zThisSmall += dir.z * xSecondDist;
				zThisBig += dir.z * xSecondDist;

				yContact = (yThisSmall >= yOtherSmall && yThisSmall <= yOtherBig) ||
					(yThisBig >= yOtherSmall && yThisBig <= yOtherBig) ||
					(yOtherSmall >= yThisSmall && yOtherSmall <= yThisBig) ||
					(yOtherBig >= yThisSmall && yOtherBig <= yThisBig);

				zContact = (zThisSmall >= zOtherSmall && zThisSmall <= zOtherBig) ||
					(zThisBig >= zOtherSmall && zThisBig <= zOtherBig) ||
					(zOtherSmall >= zThisSmall && zOtherSmall <= zThisBig) ||
					(zOtherBig >= zThisSmall && zOtherBig <= zThisBig);

				if (yContact && zContact)
					return firstDist + xSecondDist;

				secondDist = xSecondDist;
			}
			else
			{
				zContact = true;

				xThisSmall += dir.x * zSecondDist;
				xThisBig += dir.x * zSecondDist;
				yThisSmall += dir.y * zSecondDist;
				yThisBig += dir.y * zSecondDist;
				zThisSmall += dir.z * zSecondDist;
				zThisBig += dir.z * zSecondDist;

				xContact = (xThisSmall >= xOtherSmall && xThisSmall <= xOtherBig) ||
					(xThisBig >= xOtherSmall && xThisBig <= xOtherBig) ||
					(xOtherSmall >= xThisSmall && xOtherSmall <= xThisBig) ||
					(xOtherBig >= xThisSmall && xOtherBig <= xThisBig);

				yContact = (yThisSmall >= yOtherSmall && yThisSmall <= yOtherBig) ||
					(yThisBig >= yOtherSmall && yThisBig <= yOtherBig) ||
					(yOtherSmall >= yThisSmall && yOtherSmall <= yThisBig) ||
					(yOtherBig >= yThisSmall && yOtherBig <= yThisBig);

				if (xContact && yContact)
					return firstDist + zSecondDist;

				secondDist = zSecondDist;
			}
		}
		else if (zContact && !xContact && !yContact)
		{
			float xDiff = 0;
			float yDiff = 0;

			if (xOtherSmall > xThisBig)
				xDiff = xOtherSmall - xThisBig;
			else
				xDiff = xOtherBig - xThisSmall;

			if (yOtherSmall > yThisBig)
				yDiff = yOtherSmall - yThisBig;
			else
				yDiff = yOtherBig - yThisSmall;

			float xSecondDist = -1;
			float ySecondDist = -1;

			if (dir.x * xDiff > 0)
			{
				xSecondDist = xDiff / dir.x;
			}

			if (dir.y * yDiff > 0)
			{
				ySecondDist = yDiff / dir.y;
			}

			if (xSecondDist < 0 && ySecondDist < 0)
				return float.MaxValue;

			if ((xSecondDist > 0 && ySecondDist < 0)
				|| (xSecondDist > 0 && ySecondDist > 0 && xSecondDist < ySecondDist))
			{
				xContact = true;

				xThisSmall += dir.x * xSecondDist;
				xThisBig += dir.x * xSecondDist;
				yThisSmall += dir.y * xSecondDist;
				yThisBig += dir.y * xSecondDist;
				zThisSmall += dir.z * xSecondDist;
				zThisBig += dir.z * xSecondDist;

				yContact = (yThisSmall >= yOtherSmall && yThisSmall <= yOtherBig) ||
					(yThisBig >= yOtherSmall && yThisBig <= yOtherBig) ||
					(yOtherSmall >= yThisSmall && yOtherSmall <= yThisBig) ||
					(yOtherBig >= yThisSmall && yOtherBig <= yThisBig);

				zContact = (zThisSmall >= zOtherSmall && zThisSmall <= zOtherBig) ||
					(zThisBig >= zOtherSmall && zThisBig <= zOtherBig) ||
					(zOtherSmall >= zThisSmall && zOtherSmall <= zThisBig) ||
					(zOtherBig >= zThisSmall && zOtherBig <= zThisBig);

				if (yContact && zContact)
					return firstDist + xSecondDist;

				secondDist = xSecondDist;
			}
			else
			{
				yContact = true;

				xThisSmall += dir.x * ySecondDist;
				xThisBig += dir.x * ySecondDist;
				yThisSmall += dir.y * ySecondDist;
				yThisBig += dir.y * ySecondDist;
				zThisSmall += dir.z * ySecondDist;
				zThisBig += dir.z * ySecondDist;

				xContact = (xThisSmall >= xOtherSmall && xThisSmall <= xOtherBig) ||
					(xThisBig >= xOtherSmall && xThisBig <= xOtherBig) ||
					(xOtherSmall >= xThisSmall && xOtherSmall <= xThisBig) ||
					(xOtherBig >= xThisSmall && xOtherBig <= xThisBig);

				zContact = (zThisSmall >= zOtherSmall && zThisSmall <= zOtherBig) ||
					(zThisBig >= zOtherSmall && zThisBig <= zOtherBig) ||
					(zOtherSmall >= zThisSmall && zOtherSmall <= zThisBig) ||
					(zOtherBig >= zThisSmall && zOtherBig <= zThisBig);

				if (xContact && zContact)
					return firstDist + ySecondDist;

				secondDist = ySecondDist;
			}
		}

		if (!xContact && yContact && zContact)
		{
			float diff;
			float dist = -1;

			if (xOtherSmall > xThisBig)
				diff = xOtherSmall - xThisBig;
			else
				diff = xOtherBig - xThisSmall;

			if (dir.x * diff > 0)
				dist = diff / dir.x;

			if (dist < 0)
				return float.MaxValue;

			float yOffset = dist * dir.y;
			float zOffset = dist * dir.z;

			yThisSmall += yOffset;
			yThisBig += yOffset;
			zThisSmall += zOffset;
			zThisBig += zOffset;

			yContact = (yThisSmall >= yOtherSmall && yThisSmall <= yOtherBig) ||
				(yThisBig >= yOtherSmall && yThisBig <= yOtherBig) ||
				(yOtherSmall >= yThisSmall && yOtherSmall <= yThisBig) ||
				(yOtherBig >= yThisSmall && yOtherBig <= yThisBig);

			zContact = (zThisSmall >= zOtherSmall && zThisSmall <= zOtherBig) ||
				(zThisBig >= zOtherSmall && zThisBig <= zOtherBig) ||
				(zOtherSmall >= zThisSmall && zOtherSmall <= zThisBig) ||
				(zOtherBig >= zThisSmall && zOtherBig <= zThisBig);

			if (yContact && zContact)
				return firstDist + secondDist + dist;
			return float.MaxValue;
		}
		else if (!yContact && xContact && zContact)
		{
			float diff;
			float dist = -1;

			if (yOtherSmall > yThisBig)
				diff = yOtherSmall - yThisBig;
			else
				diff = yOtherBig - yThisSmall;

			if (dir.y * diff > 0)
				dist = diff / dir.y;

			if (dist < 0)
				return float.MaxValue;

			float xOffset = dist * dir.x;
			float zOffset = dist * dir.z;

			xThisSmall += xOffset;
			xThisBig += xOffset;
			zThisSmall += zOffset;
			zThisBig += zOffset;

			xContact = (xThisSmall >= xOtherSmall && xThisSmall <= xOtherBig) ||
				(xThisBig >= xOtherSmall && xThisBig <= xOtherBig) ||
				(xOtherSmall >= xThisSmall && xOtherSmall <= xThisBig) ||
				(xOtherBig >= xThisSmall && xOtherBig <= xThisBig);

			zContact = (zThisSmall >= zOtherSmall && zThisSmall <= zOtherBig) ||
				(zThisBig >= zOtherSmall && zThisBig <= zOtherBig) ||
				(zOtherSmall >= zThisSmall && zOtherSmall <= zThisBig) ||
				(zOtherBig >= zThisSmall && zOtherBig <= zThisBig);

			if (xContact && zContact)
				return firstDist + secondDist + dist;
			return float.MaxValue;
		}
		else if (!zContact && xContact && yContact)
		{
			float diff;
			float dist = -1;

			if (zOtherSmall > zThisBig)
				diff = zOtherSmall - zThisBig;
			else
				diff = zOtherBig - zThisSmall;

			if (dir.z * diff > 0)
				dist = diff / dir.z;

			if (dist < 0)
				return float.MaxValue;

			float xOffset = dist * dir.x;
			float yOffset = dist * dir.y;

			xThisSmall += xOffset;
			xThisBig += xOffset;
			yThisSmall += yOffset;
			yThisBig += yOffset;

			xContact = (xThisSmall >= xOtherSmall && xThisSmall <= xOtherBig) ||
				(xThisBig >= xOtherSmall && xThisBig <= xOtherBig) ||
				(xOtherSmall >= xThisSmall && xOtherSmall <= xThisBig) ||
				(xOtherBig >= xThisSmall && xOtherBig <= xThisBig);

			yContact = (yThisSmall >= yOtherSmall && yThisSmall <= yOtherBig) ||
				(yThisBig >= yOtherSmall && yThisBig <= yOtherBig) ||
				(yOtherSmall >= yThisSmall && yOtherSmall <= yThisBig) ||
				(yOtherBig >= yThisSmall && yOtherBig <= yThisBig);

			if (xContact && yContact)
				return firstDist + secondDist + dist;
			return float.MaxValue;
		}

		return float.MaxValue;
	}
    public static bool AlmostCloseTo(float a, float b)
    {
        return Mathf.Abs(a - b) < 0.0001f;
    }

    public static bool IsAlmostZero(float a)
    {
        return Mathf.Abs(a) < 0.0001f;
    }
    
    public static Vector2 RotateToward(Vector2 from, Vector2 to, float maxRadiansDelta, float maxMagnitudeDelta)
    {
        float currentMag = from.magnitude;
        float targetMag = to.magnitude;

        // 방향이 0 벡터인 경우 처리
        if (currentMag > 0.0001f && targetMag > 0.0001f)
        {
            float angle = Vector2.SignedAngle(from, to) * Mathf.Deg2Rad;
            float rotate = Mathf.Clamp(angle, -maxRadiansDelta, maxRadiansDelta);

            float sin = Mathf.Sin(rotate);
            float cos = Mathf.Cos(rotate);

            from = new Vector2(
                from.x * cos - from.y * sin,
                from.x * sin + from.y * cos
            );
        }

        // 크기 보정
        float newMag = Mathf.MoveTowards(currentMag, targetMag, maxMagnitudeDelta);

        return from.normalized * newMag;
    }
}
