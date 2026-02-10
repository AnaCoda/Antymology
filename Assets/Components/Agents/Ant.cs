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
            if (!TryConsumeMulch())
            {
                MoveRandomly();
            }
            TakeDamage(ConfigurationManager.Instance.Health_Reduction_Per_Timestep);
            MaybeTakeDamageFromAcid();
        }

        private void MoveRandomly()
        {
            Vector3Int[] directions = 
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 0, -1)
            };

            Vector3Int direction = directions[random.Next(directions.Length)];
            Vector3Int targetXZ = new Vector3Int(worldPosition.x + direction.x, 0, worldPosition.z + direction.z);

            if (TryGetValidPosition(targetXZ.x, targetXZ.z, out Vector3Int validTarget))
            {
                MoveTo(validTarget);
            }
        }

        private bool TryGetValidPosition(int targetX, int targetZ, out Vector3Int result)
        {
            result = Vector3Int.zero;
            
            int groundY = FindGroundLevel(targetX, targetZ);
            if (groundY < 0)
                return false;

            result = new Vector3Int(targetX, groundY + 1, targetZ);
            return true;
        }

        private bool TryConsumeMulch()
        {
            AbstractBlock blockBelow = WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z);
            if (blockBelow is MulchBlock && !IsAnotherAntAtPosition(new Vector3Int(worldPosition.x, worldPosition.y - 1, worldPosition.z)))
            {
                WorldManager.Instance.SetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z, new AirBlock());
                MoveTo(new Vector3Int(worldPosition.x, worldPosition.y - 1, worldPosition.z));
                Heal(ConfigurationManager.Instance.Mulch_Healing_Amount);
                return true;
            }
            return false;
        }

        private bool IsAnotherAntAtPosition(Vector3Int position)
        {
            foreach (Ant ant in AntManager.Instance.Ants)
            {
                if (ant != this && ant.worldPosition == position)
                    return true;
            }
            return false;
        }

        private bool MaybeTakeDamageFromAcid()
        {
            AbstractBlock blockBelow = WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z);
            if (blockBelow is AcidicBlock)
            {
                TakeDamage(ConfigurationManager.Instance.Health_Reduction_Per_Timestep);
                return true;
            }
            return false;
        }

        private int FindGroundLevel(int x, int z)
        {
            int currentGroundY = worldPosition.y - 1;
            
            for (int y = currentGroundY + 2; y >= currentGroundY - 2; y--)
            {
                AbstractBlock block = WorldManager.Instance.GetBlock(x, y, z);
                AbstractBlock above = WorldManager.Instance.GetBlock(x, y + 1, z);
                
                if (!(block is AirBlock) && above is AirBlock)
                {
                    return y;
                }
            }
            return -1;
        }

        public void MoveTo(Vector3Int target)
        {
            Vector3 direction = new Vector3(target.x - worldPosition.x, 0, target.z - worldPosition.z);
            
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
            worldPosition = target;
            transform.position = new Vector3(target.x, target.y - 0.5f, target.z);
        }
    }
}
