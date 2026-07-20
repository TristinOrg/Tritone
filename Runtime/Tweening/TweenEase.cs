namespace Tritone.Tweening
{
    /// <summary>
    /// Evaluates built-in easing curves without delegate allocation.
    /// </summary>
    internal static class TweenEase
    {
        /// <summary>
        /// Evaluates one normalized easing curve.
        /// </summary>
        internal static float Evaluate(ETweenEase ease, float value)
        {
            switch (ease)
            {
                case ETweenEase.Linear:
                    return value;
                case ETweenEase.InQuad:
                    return value * value;
                case ETweenEase.OutQuad:
                    return value * (2.0f - value);
                case ETweenEase.InOutQuad:
                    return value < 0.5f
                        ? 2.0f * value * value
                        : -1.0f + (4.0f - 2.0f * value) * value;
                case ETweenEase.InCubic:
                    return value * value * value;
                case ETweenEase.OutCubic:
                {
                    var shifted = value - 1.0f;
                    return shifted * shifted * shifted + 1.0f;
                }
                case ETweenEase.InOutCubic:
                    if (value < 0.5f)
                        return 4.0f * value * value * value;
                    else
                    {
                        var shifted = 2.0f * value - 2.0f;
                        return 0.5f * shifted * shifted * shifted + 1.0f;
                    }
                case ETweenEase.OutBack:
                {
                    const float overshoot = 1.70158f;
                    var shifted = value - 1.0f;
                    return 1.0f + shifted * shifted *
                           ((overshoot + 1.0f) * shifted + overshoot);
                }
                default:
                    return value;
            }
        }
    }
}
