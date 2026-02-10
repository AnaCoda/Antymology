using Antymology.Terrain;
using UnityEngine;

namespace Antymology.Agents
{
    public class Ant : MonoBehaviour
    {
        public float health = 100f;
        public float maxHealth = 100f;
        
        public Vector3Int worldPosition;
        private System.Random random = new System.Random();

        private void Start()
        {
            AntManager.Instance.RegisterAnt(this);
            Debug.Log($"Ant registered at position {worldPosition}");
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

        public void PerformTimestep()
        {
            MoveRandomly();
        }

        private void MoveRandomly()
        {
            Vector3Int[] directions = new Vector3Int[]
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 0, -1)
            };

            Vector3Int direction = directions[random.Next(directions.Length)];
            Vector3Int targetPos = worldPosition + direction;

            if (IsValidMove(targetPos))
            {
                worldPosition = targetPos;
                transform.position = new Vector3(targetPos.x, targetPos.y, targetPos.z);
            }
        }

        private bool IsValidMove(Vector3Int target)
        {
            AbstractBlock targetBlock = WorldManager.Instance.GetBlock(target.x, target.y, target.z);
            AbstractBlock belowTarget = WorldManager.Instance.GetBlock(target.x, target.y - 1, target.z);
            AbstractBlock currentBelow = WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z);

            if (!(targetBlock is AirBlock))
                return false;

            if (belowTarget is AirBlock)
                return false;

            int heightDifference = Mathf.Abs(target.y - worldPosition.y);
            if (heightDifference > 2)
                return false;

            return true;
        }
    }
}
