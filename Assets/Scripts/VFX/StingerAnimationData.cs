using UnityEngine;

namespace Janken.VFX
{
    [CreateAssetMenu(fileName = "NewStingerAnimation", menuName = "Janken/Stinger Animation Data")]
    public class StingerAnimationData : ScriptableObject
    {
        [Header("Stinger Metadata")]
        public string stingerName = "Stinger_01";
        
        [Header("Frame Rate & Timing")]
        [Tooltip("Playback speed in frames per second")]
        public float fps = 30f;

        [Tooltip("The frame index where the screen is 100% obscured (triggers screen content swap)")]
        public int cutFrameIndex = 13;

        [Header("Audio")]
        [Tooltip("Optional sound effect triggered at the start of the stinger")]
        public AudioClip stingerSound;

        [Header("Animation Frames")]
        public Sprite[] frames;

        public int FrameCount => frames != null ? frames.Length : 0;
        public float Duration => fps > 0 ? FrameCount / fps : 0f;

        public Sprite GetFrame(int index)
        {
            if (frames == null || frames.Length == 0) return null;
            index = Mathf.Clamp(index, 0, frames.Length - 1);
            return frames[index];
        }
    }
}
