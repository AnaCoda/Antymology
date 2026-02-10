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
        private Dictionary<AntGenome, float> genomeFitness = new Dictionary<AntGenome, float>();

        private void Update()
        {
            generationTimer += Time.deltaTime;
            
            if (generationTimer >= ConfigurationManager.Instance.Generation_Duration)
            {
                EndGeneration();
            }
        }

        public void EndGenerationNow()
        {
            EndGeneration();
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
            if (!genomeFitness.ContainsKey(genome))
            {
                genomeFitness[genome] = 0f;
            }
        }

        public void RecordAntFitness(Ant ant)
        {
            if (ant.genome == null) return;
            
            // Fitness = health donated to queen * 5 + survival time * 0.1 + mulch consumed * 0.2
            float fitness = (ant.healthDonatedToQueen * 5f) + (ant.survivalTime * 0.1f) + (ant.mulchConsumed * 0.2f);
            
            if (genomeFitness.ContainsKey(ant.genome))
            {
                genomeFitness[ant.genome] = Mathf.Max(genomeFitness[ant.genome], fitness);
            }
        }

        private void EndGeneration()
        {
            // Record fitness for all living ants
            foreach (Ant ant in AntManager.Instance.Ants)
            {
                RecordAntFitness(ant);
            }
            
            Debug.Log($"Generation {currentGeneration} ended. Nests built: {nestsThisGeneration}");
            
            currentGeneration++;
            generationTimer = 0f;
            nestsThisGeneration = 0;
            
            EvolveNextGeneration();
        }

        private void EvolveNextGeneration()
        {
            if (genomeFitness.Count < 2)
            {
                RestartWithRandomGenomes();
                return;
            }

            List<AntGenome> survivors = SelectTopPerformers();
            
            Dictionary<AntGenome, float> newGenomeFitness = new Dictionary<AntGenome, float>();
            
            for (int i = 0; i < ConfigurationManager.Instance.Initial_Ant_Count - 1; i++)
            {
                AntGenome parent1 = survivors[Random.Range(0, survivors.Count)];
                AntGenome parent2 = survivors[Random.Range(0, survivors.Count)];
                AntGenome child = new AntGenome(parent1, parent2);
                newGenomeFitness[child] = 0f;
            }
            
            genomeFitness = newGenomeFitness;
            
            RestartSimulation();
        }

        private List<AntGenome> SelectTopPerformers()
        {
            // Sort genomes by fitness
            var sortedGenomes = genomeFitness.OrderByDescending(kvp => kvp.Value).ToList();
            
            int survivorCount = Mathf.Max(2, sortedGenomes.Count / 5);
            
            List<AntGenome> survivors = new List<AntGenome>();
            for (int i = 0; i < survivorCount && i < sortedGenomes.Count; i++)
            {
                survivors.Add(sortedGenomes[i].Key);
                Debug.Log($"  Survivor {i}: Fitness = {sortedGenomes[i].Value:F1}");
            }
            
            return survivors;
        }

        private void RestartWithRandomGenomes()
        {
            genomeFitness.Clear();
            for (int i = 0; i < ConfigurationManager.Instance.Initial_Ant_Count; i++)
            {
                AntGenome genome = new AntGenome();
                genome.Randomize();
                genomeFitness[genome] = 0f;
            }
            RestartSimulation();
        }

        private void RestartSimulation()
        {
            foreach (Ant ant in AntManager.Instance.Ants.ToList())
            {
                RecordAntFitness(ant);
                Destroy(ant.gameObject);
            }
            
            WorldManager.Instance.RegenerateAnts(new List<AntGenome>(genomeFitness.Keys));
        }

        public AntGenome GetNextGenome()
        {
            if (genomeFitness.Count == 0)
            {
                AntGenome genome = new AntGenome();
                genome.Randomize();
                genomeFitness[genome] = 0f;
                return genome;
            }
            
            AntGenome result = genomeFitness.Keys.First();
            return result;
        }
    }
}
