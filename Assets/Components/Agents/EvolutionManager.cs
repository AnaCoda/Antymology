using Antymology.Helpers;
using Antymology.Terrain;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Antymology.Agents
{
    public class EvolutionManager : Singleton<EvolutionManager>
    {
        public int currentGeneration = 0;
        public int nestsThisGeneration = 0;
        public int bestNestCount = 0;
        
        private float generationTimer = 0f;
        private List<AntGenome> genePool = new List<AntGenome>();

        private void Update()
        {
            generationTimer += Time.deltaTime;
            
            if (generationTimer >= ConfigurationManager.Instance.Generation_Duration)
            {
                EndGeneration();
            }
        }

        public void RegisterNestBuilt()
        {
            nestsThisGeneration++;
            if (nestsThisGeneration > bestNestCount)
            {
                bestNestCount = nestsThisGeneration;
            }
        }

        public void RegisterAntGenome(AntGenome genome)
        {
            if (!genePool.Contains(genome))
            {
                genePool.Add(genome);
            }
        }

        private void EndGeneration()
        {
            Debug.Log($"Generation {currentGeneration} ended. Nests built: {nestsThisGeneration}");
            
            currentGeneration++;
            generationTimer = 0f;
            nestsThisGeneration = 0;
            
            EvolveNextGeneration();
        }

        private void EvolveNextGeneration()
        {
            if (genePool.Count < 2)
            {
                RestartWithRandomGenomes();
                return;
            }

            List<AntGenome> survivors = SelectTopPerformers();
            
            List<AntGenome> newGeneration = new List<AntGenome>();
            
            for (int i = 0; i < ConfigurationManager.Instance.Initial_Ant_Count - 1; i++)
            {
                AntGenome parent1 = survivors[Random.Range(0, survivors.Count)];
                AntGenome parent2 = survivors[Random.Range(0, survivors.Count)];
                newGeneration.Add(new AntGenome(parent1, parent2));
            }
            
            genePool = newGeneration;
            
            RestartSimulation();
        }

        private List<AntGenome> SelectTopPerformers()
        {
            int survivorCount = Mathf.Max(2, genePool.Count / 5);
            return genePool.Take(survivorCount).ToList();
        }

        private void RestartWithRandomGenomes()
        {
            genePool.Clear();
            for (int i = 0; i < ConfigurationManager.Instance.Initial_Ant_Count; i++)
            {
                AntGenome genome = new AntGenome();
                genome.Randomize();
                genePool.Add(genome);
            }
            RestartSimulation();
        }

        private void RestartSimulation()
        {
            foreach (Ant ant in AntManager.Instance.Ants.ToList())
            {
                Destroy(ant.gameObject);
            }
            
            WorldManager.Instance.RegenerateAnts(genePool);
        }

        public AntGenome GetNextGenome()
        {
            if (genePool.Count == 0)
            {
                AntGenome genome = new AntGenome();
                genome.Randomize();
                return genome;
            }
            
            AntGenome result = genePool[0];
            genePool.RemoveAt(0);
            return result;
        }
    }
}
