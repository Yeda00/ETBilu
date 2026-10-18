using System.Collections.Generic;
using MoreMountains.Feedbacks;
using OneMoreTime.Gameplay.Ship;
using UnityEngine;

namespace OneMoreTime.Integration.Feel
{
    /// <summary>
    /// Keeps the gameplay assembly independent from the imported Feel sources while adding a short,
    /// authorable squash-and-stretch accent to every player shot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShipWeaponFeelBridge : MonoBehaviour
    {
        [SerializeField] ShipWeapon weapon;
        [SerializeField] Transform visual;
        [SerializeField] MMF_Player feedbackPlayer;

        void Awake()
        {
            if (weapon == null)
                weapon = GetComponent<ShipWeapon>();
            if (visual == null)
                visual = GetComponent<ShipMotor>()?.Visual;
            if (feedbackPlayer == null)
                feedbackPlayer = GetComponent<MMF_Player>() ?? gameObject.AddComponent<MMF_Player>();

            feedbackPlayer.PerformanceMode = true;
            feedbackPlayer.CanPlayWhileAlreadyPlaying = true;
            feedbackPlayer.FeedbacksList ??= new List<MMF_Feedback>();
            if (visual != null && feedbackPlayer.FeedbacksList.Count == 0)
                feedbackPlayer.FeedbacksList.Add(CreateShotSquash(visual));
        }

        void OnEnable()
        {
            if (weapon != null)
                weapon.Fired += OnFired;
        }

        void OnDisable()
        {
            if (weapon != null)
                weapon.Fired -= OnFired;
        }

        void OnFired(Vector2 _, Vector2 __)
        {
            feedbackPlayer?.PlayFeedbacks(transform.position);
        }

        static MMF_SquashAndStretch CreateShotSquash(Transform target)
        {
            return new MMF_SquashAndStretch
            {
                SquashAndStretchTarget = target,
                Mode = MMF_SquashAndStretch.Modes.Absolute,
                Axis = MMF_SquashAndStretch.PossibleAxis.XtoYZ,
                AnimateScaleDuration = 0.085f,
                RemapCurveZero = 1f,
                RemapCurveOne = 1.085f,
                AllowAdditivePlays = true,
                DetermineScaleOnPlay = false,
                AnimateCurve = new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.16f, 1f),
                    new Keyframe(0.6f, -0.12f),
                    new Keyframe(1f, 0f))
            };
        }
    }
}
