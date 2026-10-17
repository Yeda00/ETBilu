using System.Collections.Generic;
using OneMoreTime.Core.Gadgets;
using OneMoreTime.Gameplay.Combat;
using UnityEngine;

namespace OneMoreTime.Gameplay.Gadgets
{
    /// <summary>
    /// The time mine (GDD 05). Stays where it was dropped in the world, arms after a moment, then blows
    /// up on the first enemy in range — or by itself when the fuse runs out — dealing gadget damage to
    /// every enemy in the blast. Rules live in <see cref="TimeMineModel"/>. It has no collider: range and
    /// blast are physics queries, so it never blocks shots.
    /// </summary>
    public sealed class TimeMine : MonoBehaviour
    {
        [SerializeField, Min(0f)] float armSeconds = 0.4f;
        [SerializeField, Min(0.1f)] float fuseSeconds = 4f;
        [Tooltip("An enemy this close to an armed mine sets it off.")]
        [SerializeField, Min(0.1f)] float triggerRadius = 1.4f;
        [SerializeField, Min(0.1f)] float blastRadius = 2.4f;
        [SerializeField, Min(1)] int damage = 4;
        [Tooltip("Layers that set the mine off and take the blast (Enemy).")]
        [SerializeField] LayerMask targets;
        [Tooltip("Visual of the mine itself, hidden when it goes off.")]
        [SerializeField] Transform visual;
        [Tooltip("Blast sphere, grown and then removed. Optional.")]
        [SerializeField] Transform blast;
        [SerializeField, Min(0.01f)] float blastSeconds = 0.3f;

        static readonly Collider[] Hits = new Collider[32];
        readonly HashSet<Health> struck = new();

        TimeMineModel model;
        Faction faction = Faction.Player;
        ShotSource source = ShotSource.Ship;
        float blastAge = -1f;

        public bool HasExploded => model != null && model.HasExploded;

        /// <summary>Called right after the mine is dropped: who it belongs to, for damage and combos.</summary>
        public void Arm(Faction owner, ShotSource from)
        {
            faction = owner;
            source = from;
        }

        void Awake()
        {
            model = new TimeMineModel(armSeconds, fuseSeconds);
            if (blast != null)
                blast.gameObject.SetActive(false);
        }

        void Update()
        {
            if (blastAge >= 0f)
            {
                GrowBlast();
                return;
            }

            bool inRange = model.IsArmed && Physics.CheckSphere(transform.position, triggerRadius, targets, QueryTriggerInteraction.Collide);
            if (model.Tick(Time.deltaTime, inRange))
                Explode();
            else if (visual != null && model.IsArmed)
                visual.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(model.Age * 18f));   // armed: it throbs
        }

        void Explode()
        {
            struck.Clear();
            int n = Physics.OverlapSphereNonAlloc(transform.position, blastRadius, Hits, targets, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                var target = Hits[i].GetComponentInParent<Health>();
                if (target != null && target.Faction != faction && struck.Add(target))
                    target.TakeDamage(new DamageInfo(damage, DamageKind.Gadget, source));
            }

            if (visual != null)
                visual.gameObject.SetActive(false);
            if (blast == null)
            {
                Destroy(gameObject);
                return;
            }
            blast.gameObject.SetActive(true);
            blastAge = 0f;
            GrowBlast();
        }

        void GrowBlast()
        {
            blastAge += Time.deltaTime;
            float k = Mathf.Clamp01(blastAge / blastSeconds);
            blast.localScale = Vector3.one * (blastRadius * 2f * Mathf.Lerp(0.3f, 1f, k));
            if (k >= 1f)
                Destroy(gameObject);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.79f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, blastRadius);
        }
    }
}
