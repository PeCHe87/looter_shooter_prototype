
using UnityEngine;

namespace FusionExamples.Tanknarok
{
    public class Utils
    {
        public static Vector2 GetPositionAroundPoint(Vector2 point, float radius)
        {
            return point + Random.insideUnitCircle * radius;
        }
    }
}