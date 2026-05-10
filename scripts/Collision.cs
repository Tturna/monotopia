using Godot;

public static class Collision
{
    public record struct Rect
    {
        public float X;
        public float Y;
        public float W;
        public float H;
    }

    public static bool AABB(float xPos1, float yPos1, float xSize1, float ySize1,
                             float xPos2, float yPos2, float xSize2, float ySize2)
    {
        if (xPos1 < xPos2 + xSize2 &&
            xPos1 + xSize1 > xPos2 &&
            yPos1 < yPos2 + ySize2 &&
            yPos1 + ySize1 > yPos2)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool AreRectsColliding(Rect rectA, Rect rectB)
    {
        var x1 = rectA.X;
        var y1 = rectA.Y;
        var w1 = rectA.W;
        var h1 = rectA.H;
        var x2 = rectB.X;
        var y2 = rectB.Y;
        var w2 = rectB.W;
        var h2 = rectB.H;

        return AABB(x1, y1, w1, h1, x2, y2, w2, h2);
    }

    public static bool AreRectAndCircleColliding(Rect rect,
        Vector2 circlePos, float circleRadius)
    {
        var closestX = Mathf.Clamp(circlePos.X, rect.X, rect.X + rect.W);
        var closestY = Mathf.Clamp(circlePos.Y, rect.Y, rect.Y + rect.H);

        var dx = circlePos.X - closestX;
        var dy = circlePos.Y - closestY;

        return dx * dx + dy * dy <= circleRadius * circleRadius;
    }

    public static bool IsPointInRect(Vector2 point, Rect rect)
    {
        var x1 = rect.X;
        var y1 = rect.Y;
        var w1 = rect.W;
        var h1 = rect.H;
        var x2 = point.X;
        var y2 = point.Y;

        return AABB(x1, y1, w1, h1, x2, y2, 0f, 0f);
    }

    public static bool IsLineInRect(Vector2 linePointA, Vector2 linePointB,
        Rect rect, out Vector2 entryPoint, out Vector2 exitPoint)
    {
        entryPoint = Vector2.Zero;
        exitPoint = Vector2.Zero;

        // Find line intersection in an AABB using the slab method.

        // Use a parametric line, meaning a representation of the line that can be used
        // to get any point along the line.
        // Line: P1 + t(P2 - P1), where t is within [0 - 1]. Effectively it's lerp.
        // t is the factor that can be used to get any point along the line.

        // Split line into X and Y and check against AABB.
        // When box.minX = P1.X + t(P2.X - P1.X), the line intersects the left vertical
        // edge of the AABB. Solving t gets the intersection point.
        // t = (box.minX - P1.X) / (P2.X - P1.X)

        var lineDirection = linePointB - linePointA;
        float horizontalEnterFactor, horizontalExitFactor, verticalEnterFactor, verticalExitFactor;

        if (Mathf.Abs(lineDirection.X) < float.Epsilon)
        {
            // Line is vertical
            if (linePointA.X < rect.X || linePointA.X > rect.X + rect.W)
            {
                return false;
            }

            horizontalEnterFactor = float.NegativeInfinity;
            horizontalExitFactor = float.PositiveInfinity;
        }
        else
        {
            // Get enter and exit factors regardless of line direction
            var leftIntersectFactor = (rect.X - linePointA.X) / lineDirection.X;
            var rightIntersectFactor = (rect.X + rect.W - linePointA.X) / lineDirection.X;
            horizontalEnterFactor = Mathf.Min(leftIntersectFactor, rightIntersectFactor);
            horizontalExitFactor = Mathf.Max(leftIntersectFactor, rightIntersectFactor);
        }

        if (Mathf.Abs(lineDirection.Y) < float.Epsilon)
        {
            // Line is horizontal
            if (linePointA.Y < rect.Y || linePointA.Y > rect.Y + rect.H)
            {
                return false;
            }

            verticalEnterFactor = float.NegativeInfinity;
            verticalExitFactor = float.PositiveInfinity;
        }
        else
        {
            var topIntersectFactor = (rect.Y - linePointA.Y) / lineDirection.Y;
            var bottomIntersectFactor = (rect.Y + rect.H - linePointA.Y) / lineDirection.Y;
            verticalEnterFactor = Mathf.Min(topIntersectFactor, bottomIntersectFactor);
            verticalExitFactor = Mathf.Max(topIntersectFactor, bottomIntersectFactor);
        }

        var furthestEnterFactor = Mathf.Max(horizontalEnterFactor, verticalEnterFactor);
        var nearestExitFactor = Mathf.Min(horizontalExitFactor, verticalExitFactor);

        // If the line exits an axis before entering the other, it missed the AABB.
        // In other words, if it intersected both lines in an axis before intersecting one
        // on the other axis, it missed the AABB.
        // Or, the line exits one slab before entering the other.
        if (furthestEnterFactor > nearestExitFactor)
        {
            return false;
        }

        // Line intersects at some point

        var clampedEnterFactor = Mathf.Max(0, furthestEnterFactor);
        var clampedExitFactor = Mathf.Min(1, nearestExitFactor);

        if (clampedEnterFactor > clampedExitFactor)
        {
            // Intersection happens outside of given line segment
            return false;
        }

        entryPoint = linePointA + lineDirection * clampedEnterFactor;
        exitPoint = linePointA + lineDirection * clampedExitFactor;

        return true;

        /* Example illustration of a line that misses an AABB
         *
         *       |     |
         *       |     |         s
         *       |     |        /
         * --------------------t---
         *       |xxxxx|      /
         *       |xxxxx|     /
         *       |xxxxx|    /
         * ----------------b---
         *       |     |  /
         *       |     | /
         *       |     |/
         *       |     r
         *       |    /|
         *       |   / |
         *       |  /  |
         *       | /   |
         *       |/    |
         *       l     |
         *      /
         * x = AABB
         * s = line start point
         * t = top intersect factor
         * b = bottom intersect factor
         * r = right intersect factor
         * l = left intersect factor
         *
         * To get any of these as a point on the line, you do:
         * A + T(B - A), where A is the line start point, B is the line end point, and T
         * is any of these factors.
         *
         * There are two parallel lines on each axis that the line ccan intersect. Each axis
         * has an enter and exit line. If the line intersects both enter and exit lines of an axis,
         * it has to miss the AABB. If the line enters both enter lines before exiting one,
         * it has to hit the AABB.
         *
         * This can be checked by checking if the furtherst enter factor is greater than the
         * nearest exit factor. Effectively, if the second exit point is closer than the first
         * enter point, both lines of an axis have been crossed and the AABB is has been missed.
        */
    }
}
