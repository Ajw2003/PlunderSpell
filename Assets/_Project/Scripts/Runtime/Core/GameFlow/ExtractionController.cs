using Code.Scripts.EventSystems;
using UnityEngine;

namespace Plunderspell.Core
{
    /// <summary>Drives the extraction countdown. Plain C# so it is testable without a scene.</summary>
    public class ExtractionController
    {
        public float DurationSeconds { get; }
        public bool IsExtracting { get; private set; }
        public float RemainingSeconds { get; private set; }

        public ExtractionController(float extractionDurationSeconds)
        {
            DurationSeconds = extractionDurationSeconds;
        }

        public void StartExtraction()
        {
            if (IsExtracting)
            {
                return;
            }

            IsExtracting = true;
            RemainingSeconds = DurationSeconds;
            EventManager.Instance?.Publish(new ExtractionStarted());
        }

        public void CancelExtraction()
        {
            if (!IsExtracting)
            {
                return;
            }

            IsExtracting = false;
            EventManager.Instance?.Publish(new ExtractionCancelled());
        }

        public void Tick(float deltaTime)
        {
            if (!IsExtracting)
            {
                return;
            }

            RemainingSeconds -= deltaTime;
            float progress = 1f - Mathf.Clamp01(RemainingSeconds / DurationSeconds);
            EventManager.Instance?.Publish(new ExtractionProgress(progress));

            if (RemainingSeconds <= 0f)
            {
                IsExtracting = false;
                EventManager.Instance?.Publish(new ExtractionCompleted());
            }
        }
    }
}
