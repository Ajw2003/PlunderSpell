using System;
using UnityEngine;

namespace Plunderspell.Core
{
    /// <summary>Drives the extraction countdown. Plain C# so it is testable without a scene.</summary>
    public class ExtractionController
    {
        public float DurationSeconds { get; }
        public bool IsExtracting { get; private set; }
        public float RemainingSeconds { get; private set; }

        public event Action ExtractionStarted;
        public event Action ExtractionCancelled;
        public event Action ExtractionCompleted;

        /// <summary>Normalized 0..1 progress, raised each Tick while extracting.</summary>
        public event Action<float> ExtractionProgress;

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
            ExtractionStarted?.Invoke();
        }

        public void CancelExtraction()
        {
            if (!IsExtracting)
            {
                return;
            }

            IsExtracting = false;
            ExtractionCancelled?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            if (!IsExtracting)
            {
                return;
            }

            RemainingSeconds -= deltaTime;
            float progress = 1f - Mathf.Clamp01(RemainingSeconds / DurationSeconds);
            ExtractionProgress?.Invoke(progress);

            if (RemainingSeconds <= 0f)
            {
                IsExtracting = false;
                ExtractionCompleted?.Invoke();
            }
        }
    }
}
