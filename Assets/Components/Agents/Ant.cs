using UnityEngine;

namespace Antymology.Agents
{
    public class Ant : MonoBehaviour
    {
        public float health = 100f;
        public float maxHealth = 100f;
        
        public Vector3Int worldPosition;

        private void Start()
        {
            AntManager.Instance.RegisterAnt(this);
        }

        private void OnDestroy()
        {
            if (AntManager.Instance != null)
            {
                AntManager.Instance.UnregisterAnt(this);
            }
        }

        public void TakeDamage(float amount)
        {
            health -= amount;
            if (health <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            health = Mathf.Min(health + amount, maxHealth);
        }

        private void Die()
        {
            Destroy(gameObject);
        }
    }
}
