using Antymology.Terrain;
using UnityEngine;
using System.Collections.Generic;

namespace Antymology.Agents
{
    public class Ant : MonoBehaviour
    {
        public float health = 100f;
        public float maxHealth = 100f;
        
        public Vector3Int worldPosition;
        public AntGenome genome;
        public int mulchConsumed = 0;
        public float healthDonatedToQueen = 0f;
        public float survivalTime = 0f;
        
        protected System.Random random = new System.Random();
        public Vector3Int previousPosition;
        public int consecutiveStays = 0;

        private void Start()
        {
            AntManager.Instance.RegisterAnt(this);
            
            if (genome == null)
            {
                genome = new AntGenome();
                genome.Randomize();
            }
            
            if (EvolutionManager.Instance != null)
            {
                EvolutionManager.Instance.RegisterAntGenome(genome);
            }
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

        protected virtual void Die()
        {
            Destroy(gameObject);
        }

        public virtual void PerformTimestep()
        {
            survivalTime += 1f;
            
            if (!TryConsumeMulch())
            {
                TryShareHealth();
                PerformMicroMovement();
            }
            TakeDamage(ConfigurationManager.Instance.Health_Reduction_Per_Timestep);
            MaybeTakeDamageFromAcid();
        }

        protected void PerformMicroMovement()
        {
            MicroInfo[] microOptions = ComputeMicroOptions();
            MicroInfo best = microOptions[0];
            
            for (int i = 1; i < microOptions.Length; i++)
            {
                if (microOptions[i].IsBetter(best, this, random))
                {
                    best = microOptions[i];
                }
            }
            
            if (best.canMove && best.position != worldPosition)
            {
                MoveTo(best.position);
                consecutiveStays = 0;
            }
            else
            {
                consecutiveStays++;
            }
        }

        protected MicroInfo[] ComputeMicroOptions()
        {
            Vector3Int[] directions = 
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 0, -1),
                Vector3Int.zero
            };

            MicroInfo[] options = new MicroInfo[5];
            
            for (int i = 0; i < directions.Length; i++)
            {
                options[i] = new MicroInfo();
                options[i].direction = directions[i];
                
                if (directions[i] == Vector3Int.zero)
                {
                    options[i].position = worldPosition;
                    options[i].canMove = true;
                }
                else
                {
                    int targetX = worldPosition.x + directions[i].x;
                    int targetZ = worldPosition.z + directions[i].z;
                    
                    if (TryGetValidPosition(targetX, targetZ, out Vector3Int validPos))
                    {
                        options[i].position = validPos;
                        options[i].canMove = true;
                    }
                    else
                    {
                        options[i].position = worldPosition;
                        options[i].canMove = false;
                    }
                }
                
                ScanEnvironment(options[i]);
                
                // Check if this position has food below that would strand us
                AbstractBlock blockBelow = WorldManager.Instance.GetBlock(options[i].position.x, options[i].position.y - 1, options[i].position.z);
                if (blockBelow is MulchBlock && WouldBeStrandedAtPosition(options[i].position))
                {
                    options[i].hasUneatableFood = true;
                }
            }
            
            return options;
        }

        private void ScanEnvironment(MicroInfo info)
        {
            int visionRange = ConfigurationManager.Instance.Vision_Range;
            
            for (int x = -visionRange; x <= visionRange; x++)
            {
                for (int z = -visionRange; z <= visionRange; z++)
                {
                    int checkX = info.position.x + x;
                    int checkZ = info.position.z + z;
                    
                    for (int y = info.position.y - 2; y <= info.position.y + 2; y++)
                    {
                        AbstractBlock block = WorldManager.Instance.GetBlock(checkX, y, checkZ);
                        int distSq = x * x + z * z;
                        
                        if (block is MulchBlock)
                        {
                            if (distSq < info.distanceToFoodSq)
                                info.distanceToFoodSq = distSq;
                        }
                        else if (block is AcidicBlock)
                        {
                            if (distSq < info.distanceToAcidSq)
                                info.distanceToAcidSq = distSq;
                        }
                    }
                }
            }
            
            info.nearbyAnts = 0;
            foreach (Ant ant in AntManager.Instance.Ants)
            {
                if (ant == this) continue;
                
                int dx = ant.worldPosition.x - info.position.x;
                int dz = ant.worldPosition.z - info.position.z;
                int distSq = dx * dx + dz * dz;
                
                if (distSq <= visionRange * visionRange)
                {
                    info.nearbyAnts++;
                }
                
                // Queens are detectable from much farther away (50 blocks)
                if (ant is QueenAnt && distSq < info.distanceToQueenSq)
                {
                    int queenDetectionRange = 50;
                    if (distSq <= queenDetectionRange * queenDetectionRange)
                    {
                        info.distanceToQueenSq = distSq;
                    }
                }
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

        protected bool TryConsumeMulch()
        {
            AbstractBlock blockBelow = WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z);
            if (blockBelow is MulchBlock && !IsAnotherAntAtPosition(new Vector3Int(worldPosition.x, worldPosition.y - 1, worldPosition.z)))
            {
                if (WouldBeStrandedAfterEating())
                    return false;
                
                TryDig();
                Heal(ConfigurationManager.Instance.Mulch_Healing_Amount);
                mulchConsumed++;
                return true;
            }
            return false;
        }

        private bool WouldBeStrandedAfterEating()
        {
            int newY = worldPosition.y - 1;
            
            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };
            
            for (int i = 0; i < 4; i++)
            {
                int adjX = worldPosition.x + dx[i];
                int adjZ = worldPosition.z + dz[i];
                
                int groundLevel = FindGroundLevel(adjX, adjZ);
                if (groundLevel >= 0)
                {
                    int heightDiff = Mathf.Abs(groundLevel - newY);
                    if (heightDiff == 0)
                        return false;
                }
            }
            
            return true;
        }

        private bool WouldBeStrandedAtPosition(Vector3Int pos)
        {
            int newY = pos.y - 1;
            
            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };
            
            for (int i = 0; i < 4; i++)
            {
                int adjX = pos.x + dx[i];
                int adjZ = pos.z + dz[i];
                
                int groundLevel = FindGroundLevelFrom(adjX, adjZ, newY);
                if (groundLevel >= 0)
                {
                    int heightDiff = Mathf.Abs(groundLevel - newY);
                    if (heightDiff == 0)
                        return false;
                }
            }
            
            return true;
        }

        protected void TryShareHealth()
        {
            if (health < maxHealth * 0.4f)
                return;

            foreach (Ant otherAnt in AntManager.Instance.Ants)
            {
                if (otherAnt == this || otherAnt.worldPosition != worldPosition)
                    continue;

                bool shouldShare = false;
                float shareAmount = 0f;

                // Prioritize feeding queens generously
                if (otherAnt is QueenAnt && otherAnt.health < otherAnt.maxHealth * 0.9f)
                {
                    shouldShare = true;
                    // Give everything we can spare
                    shareAmount = Mathf.Min(health - maxHealth * 0.3f, otherAnt.maxHealth - otherAnt.health);
                    shareAmount = Mathf.Max(0, shareAmount * genome.altruismWeight * 2.0f);
                }
                // Also help other workers in dire need
                else if (!(this is QueenAnt) && otherAnt.health < otherAnt.maxHealth * 0.2f && health > maxHealth * 0.7f)
                {
                    shouldShare = true;
                    shareAmount = Mathf.Min(health - maxHealth * 0.5f, otherAnt.maxHealth * 0.2f);
                    shareAmount = Mathf.Max(0, shareAmount * genome.altruismWeight * 0.3f);
                }

                if (shouldShare && shareAmount > 0)
                {
                    TakeDamage(shareAmount);
                    otherAnt.Heal(shareAmount);
                    
                    // Track health donated to queen for fitness
                    if (otherAnt is QueenAnt)
                    {
                        healthDonatedToQueen += shareAmount;
                    }
                    
                    return;
                }
            }
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

        protected bool MaybeTakeDamageFromAcid()
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
            return FindGroundLevelFrom(x, z, currentGroundY);
        }

        private int FindGroundLevelFrom(int x, int z, int referenceY)
        {
            for (int y = referenceY + 5; y >= referenceY - 2; y--)
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
            
            previousPosition = worldPosition;
            worldPosition = target;
            transform.position = new Vector3(target.x, target.y - 0.5f, target.z);
        }

        private void TryDig()
        {
            AbstractBlock blockBelow = WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z);
            if (blockBelow is not ContainerBlock)
            {
                WorldManager.Instance.SetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z, new AirBlock());
                MoveTo(new Vector3Int(worldPosition.x, worldPosition.y - 1, worldPosition.z));
            }
        }
    }

    public class MicroInfo
    {
        public Vector3Int direction;
        public Vector3Int position;
        public bool canMove;
        public int distanceToFoodSq = int.MaxValue;
        public int distanceToAcidSq = int.MaxValue;
        public int distanceToQueenSq = int.MaxValue;
        public int nearbyAnts = 0;
        public bool hasUneatableFood = false;

        public float ComputeScore(Ant ant)
        {
            if (!canMove) return float.MinValue;

            float healthPercent = ant.health / ant.maxHealth;
            AntGenome genome = ant.genome;
            float score = 0;
            
            // Check if standing on a nest block
            AbstractBlock blockBelow = WorldManager.Instance.GetBlock(position.x, position.y - 1, position.z);
            bool onNest = blockBelow is NestBlock;
            
            // Queens prefer to stay put but not too much, workers prefer to move
            if (ant is QueenAnt)
            {
                if (direction == Vector3Int.zero)
                {
                    if (onNest)
                        score -= 5.0f; // Strong penalty for staying on nest
                    else
                        score += 0.2f; // Queens like staying but can still move
                }
            }
            else
            {
                if (direction != Vector3Int.zero)
                    score += 0.05f; // Workers get small exploration bonus
                else
                    score -= ant.consecutiveStays * 0.3f; // Penalty for staying
            }
                
            if (position == ant.previousPosition)
                score -= 0.5f;
            
            // Heavy penalty for positions with food we can't eat
            if (hasUneatableFood)
                score -= 2.0f;

            float foodUrgency = healthPercent < 0.5f ? genome.healthUrgencyMultiplier : 1.0f;
            
            if (distanceToFoodSq < int.MaxValue)
            {
                score += genome.foodSeekingWeight * foodUrgency / Mathf.Max(1, distanceToFoodSq);
            }

            if (distanceToAcidSq < int.MaxValue)
            {
                score -= genome.acidAvoidanceWeight / Mathf.Max(1, distanceToAcidSq);
            }

            score -= genome.crowdingWeight * nearbyAnts;

            // Workers strongly seek queens when they have health to donate
            if (!(ant is QueenAnt) && distanceToQueenSq < int.MaxValue && healthPercent > 0.5f)
            {
                score += genome.queenProximityWeight * 15.0f / Mathf.Max(1, distanceToQueenSq);
            }

            return score;
        }

        public bool IsBetter(MicroInfo other, Ant ant, System.Random random)
        {
            if (!canMove) return false;
            if (canMove && !other.canMove) return true;

            if (distanceToAcidSq < 4 && other.distanceToAcidSq >= 4)
                return false;
            if (distanceToAcidSq >= 4 && other.distanceToAcidSq < 4)
                return true;

            float myScore = ComputeScore(ant);
            float otherScore = other.ComputeScore(ant);

            if (Mathf.Abs(myScore - otherScore) < 0.01f)
            {
                return random.NextDouble() > 0.5;
            }

            return myScore > otherScore;
        }
    }
}
