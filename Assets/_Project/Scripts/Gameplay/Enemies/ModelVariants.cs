using System;
using UnityEngine;

namespace OneMoreTime.Gameplay.Enemies
{
    /// <summary>
    /// The designer's model variants of one enemy type (GDD 04): one is picked at random when the enemy
    /// spawns and the others stay off. Looks only: path, gun, health and score belong to the type. Each
    /// model brings its own hitbox and nose, so shots and the gun match what is drawn.
    /// </summary>
    public sealed class ModelVariants : MonoBehaviour
    {
        [Serializable]
        public struct Variant
        {
            [Tooltip("A child of the Visual holding this variant's model.")]
            public GameObject model;
            [Tooltip("Hitbox size on the play plane for this model.")]
            public Vector2 hitbox;
            [Tooltip("Local x of this model's nose, where the gun fires from.")]
            public float muzzleX;
        }

        [SerializeField] Variant[] variants = Array.Empty<Variant>();
        [Tooltip("Indexes the random pick may choose from; empty means all of them. The interceptor shares the small models but keeps off the kamikaze's.")]
        [SerializeField] int[] randomFrom = Array.Empty<int>();
        [SerializeField] BoxCollider hitbox;
        [Tooltip("The EnemyGun's muzzle, moved to the picked model's nose.")]
        [SerializeField] Transform muzzle;

        public int Count => variants.Length;
        /// <summary>Index of the model showing, or -1 before one is picked.</summary>
        public int Current { get; private set; } = -1;

        void Awake()
        {
            if (variants.Length == 0)
                return;
            Pick(randomFrom.Length > 0
                ? randomFrom[UnityEngine.Random.Range(0, randomFrom.Length)]
                : UnityEngine.Random.Range(0, variants.Length));
        }

        public void Pick(int index)
        {
            if (index < 0 || index >= variants.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            Current = index;
            for (int i = 0; i < variants.Length; i++)
                if (variants[i].model != null)
                    variants[i].model.SetActive(i == index);
            var picked = variants[index];
            if (hitbox != null)
                hitbox.size = new Vector3(picked.hitbox.x, picked.hitbox.y, hitbox.size.z);
            if (muzzle != null)
                muzzle.localPosition = new Vector3(picked.muzzleX, muzzle.localPosition.y, muzzle.localPosition.z);
        }
    }
}
