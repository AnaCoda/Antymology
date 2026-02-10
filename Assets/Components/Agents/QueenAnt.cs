using Antymology.Terrain;
using UnityEngine;

namespace Antymology.Agents
{
    public class QueenAnt : Ant
    {
        public int nestsBuilt = 0;
        private float nestBuildCost;

        private void Awake()
        {
            nestBuildCost = maxHealth / 3f;
        }

        public override void PerformTimestep()
        {
            if (!TryConsumeMulch())
            {
                if (ShouldBuildNest())
                {
                    BuildNest();
                }
                else
                {
                    PerformQueenMicroMovement();
                }
            }
            TakeDamage(ConfigurationManager.Instance.Health_Reduction_Per_Timestep);
            MaybeTakeDamageFromAcid();
        }

        private void PerformQueenMicroMovement()
        {
            MicroInfo[] microOptions = ComputeMicroOptions();
            
            Debug.Log($"[Queen] At {worldPosition}, Health: {health:F1}/{maxHealth:F1}, ConsecutiveStays: {consecutiveStays}");
            
            for (int i = 0; i < microOptions.Length; i++)
            {
                MicroInfo opt = microOptions[i];
                string dirName = opt.direction == Vector3Int.zero ? "STAY" : 
                                opt.direction.x > 0 ? "EAST" :
                                opt.direction.x < 0 ? "WEST" :
                                opt.direction.z > 0 ? "NORTH" : "SOUTH";
                
                if (opt.canMove)
                {
                    float score = opt.ComputeScore(this);
                    Debug.Log($"  [{dirName}] pos={opt.position}, score={score:F3}, food={opt.distanceToFoodSq}, uneatable={opt.hasUneatableFood}, ants={opt.nearbyAnts}, prevPos={opt.position == previousPosition}");
                }
                else
                {
                    Debug.Log($"  [{dirName}] BLOCKED");
                }
            }
            
            MicroInfo best = microOptions[0];
            
            for (int i = 1; i < microOptions.Length; i++)
            {
                if (microOptions[i].IsBetter(best, this, random))
                {
                    best = microOptions[i];
                }
            }
            
            string chosenDir = best.direction == Vector3Int.zero ? "STAY" : 
                              best.direction.x > 0 ? "EAST" :
                              best.direction.x < 0 ? "WEST" :
                              best.direction.z > 0 ? "NORTH" : "SOUTH";
            Debug.Log($"  >> CHOSEN: {chosenDir}, moving to {best.position}, consecutiveStays={consecutiveStays}");
            
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

        private bool ShouldBuildNest()
        {
            return health >= maxHealth * 0.8f;
        }

        private void BuildNest()
        {
            AbstractBlock blockBelow = WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z);
            
            if (!(blockBelow is NestBlock) && !(blockBelow is ContainerBlock) && !(blockBelow is AirBlock))
            {
                WorldManager.Instance.SetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z, new NestBlock());
                TakeDamage(nestBuildCost);
                nestsBuilt++;
                
                if (EvolutionManager.Instance != null)
                {
                    EvolutionManager.Instance.RegisterNestBuilt();
                }
            }
        }
    }
}