namespace Tritone.Tweening
{
    /// <summary>
    /// Defines built-in allocation-free tween easing curves.
    /// </summary>
    public enum ETweenEase
    {
        /// <summary>Uses constant interpolation speed.</summary>
        Linear,
        /// <summary>Accelerates quadratically.</summary>
        InQuad,
        /// <summary>Decelerates quadratically.</summary>
        OutQuad,
        /// <summary>Accelerates and then decelerates quadratically.</summary>
        InOutQuad,
        /// <summary>Accelerates cubically.</summary>
        InCubic,
        /// <summary>Decelerates cubically.</summary>
        OutCubic,
        /// <summary>Accelerates and then decelerates cubically.</summary>
        InOutCubic,
        /// <summary>Overshoots the target before settling.</summary>
        OutBack
    }
}
