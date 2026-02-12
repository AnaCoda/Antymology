using Antymology.Terrain;
using UnityEngine;

namespace Antymology.Agents
{
    public class QueenAnt : Ant
    {
        public int nestsBuilt = 0;
        private float nestBuildCost;
        private int stepsSinceLastBuild = 999; // Start high so she can build immediately

        private void Awake()
        {
            nestBuildCost = maxHealth / 3f;
        }

        public override void PerformTimestep()
        {
            survivalTime += 1f;
            stepsSinceLastBuild++;
            
            TryShareHealth();

			//// Must wait at least 3 steps between nest builds
			//if (ShouldBuildNest() && stepsSinceLastBuild >= 3)
			//{
			//    BuildNest();
			//}
			//else
			//{
			//    PerformQueenMicroMovement();
			//}

			PerformQueenMicroMovement();
			if (health >= 40) BuildNest();

			TakeDamage(ConfigurationManager.Instance.Health_Reduction_Per_Timestep);
            MaybeTakeDamageFromAcid();
        }

        private void PerformQueenMicroMovement()
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
            
            AbstractBlock blockBelow = WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z);
            if (blockBelow is NestBlock) 
			{
				if (best.position == worldPosition) 
				{
					Debug.Log($"[Queen] WARNING: Choosing to stay on nest! Health: {health:F1}, stepsSinceLastBuild: {stepsSinceLastBuild}");
				}
            } else if (!(blockBelow is NestBlock) && !(blockBelow is ContainerBlock) && !(blockBelow is AirBlock))
			{
				return;
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

        protected override void Die()
        {
            Debug.Log($"[Queen] Died at generation {EvolutionManager.Instance.currentGeneration}. Nests built: {nestsBuilt}");
            
            if (EvolutionManager.Instance != null)
            {
                EvolutionManager.Instance.EndGenerationNow();
            }
            
            base.Die();
        }

        private bool ShouldBuildNest()
        {
            // Build whenever we have enough health (nest costs 33% max health)
            return health >= nestBuildCost + 5f;
        }

        private void BuildNest()
        {
            AbstractBlock blockBelow = WorldManager.Instance.GetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z);
            
            if (!(blockBelow is NestBlock) && !(blockBelow is ContainerBlock) && !(blockBelow is AirBlock))
            {
                WorldManager.Instance.SetBlock(worldPosition.x, worldPosition.y - 1, worldPosition.z, new NestBlock());
                TakeDamage(nestBuildCost);
                nestsBuilt++;
                stepsSinceLastBuild = 0;
                
                Debug.Log($"[Queen] Built nest #{nestsBuilt} at {worldPosition}, health now {health:F1}");
                
                if (EvolutionManager.Instance != null)
                {
                    EvolutionManager.Instance.RegisterNestBuilt();
                }
            }
        }
    }
}