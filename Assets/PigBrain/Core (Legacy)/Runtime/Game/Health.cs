// using System;
// using PigBrain.LegacyCore.Utility;
// using UnityEngine;

// namespace PigBrain.LegacyCore.Game
// {
//     public class Health : MonoBehaviour
//     {
//         [SerializeField][Range(0, 500)] float health;
//         public bool invincible;
//         [SerializeField] Death death = Death.Destroy;
//         public event Action OnDeath;
//         public event Action<float, Vector3> OnEffect;
//         public Material damageMaterial;
//         public Color positiveColor = Color.green;
//         public Color negativeColor = Color.red;
//         public AudioSource hurt;

//         public bool isDead => health == 0;

//         float maxHealth;
//         EffectEmissionFlash flash;

//         public float CurrentHealth => health;
//         public float MaxHealth => maxHealth;

//         void Start()
//         {
//             maxHealth = health;
//             (flash = gameObject.AddComponent<EffectEmissionFlash>()).material = damageMaterial;
//         }

//         public bool Effect(float hitpoints) => Effect(hitpoints, Vector3.zero);
//         public bool Effect(float hitpoints, Vector3 force)
//         {
//             if (health == 0) return false;

//             health = invincible ? health : Mathf.Clamp(health + hitpoints, 0, maxHealth);
//             float unitDamage = Math.Abs(hitpoints) / maxHealth;
//             OnEffect?.Invoke(hitpoints, force * unitDamage);
//             // hurt.Play(new MinMaxFloat(0.8f, 1.2f));

//             if (flash) flash.Add(transform, hitpoints > 0 ? positiveColor : negativeColor, unitDamage);
//             if (health == 0) Dead();
//             return true;
//         }

//         public void Dead()
//         {
//             OnDeath?.Invoke();
//             if (death == Death.Destroy)
//                 Destroy(gameObject);
//         }

//         enum Death
//         {
//             Nothing = 0,
//             Destroy = 1,
//         }
//     }
// }