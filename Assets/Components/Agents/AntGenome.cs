using UnityEngine;

namespace Antymology.Agents
{
    public class AntGenome
    {
        public float foodSeekingWeight = 1.0f;
        public float acidAvoidanceWeight = 1.5f;
        public float crowdingWeight = 0.3f;
        public float healthUrgencyMultiplier = 2.0f;
        public float queenProximityWeight = 0.5f;
        public float altruismWeight = 1.0f;

        public AntGenome()
        {
        }

        public AntGenome(AntGenome parent1, AntGenome parent2)
        {
            foodSeekingWeight = Random.value < 0.5f ? parent1.foodSeekingWeight : parent2.foodSeekingWeight;
            acidAvoidanceWeight = Random.value < 0.5f ? parent1.acidAvoidanceWeight : parent2.acidAvoidanceWeight;
            crowdingWeight = Random.value < 0.5f ? parent1.crowdingWeight : parent2.crowdingWeight;
            healthUrgencyMultiplier = Random.value < 0.5f ? parent1.healthUrgencyMultiplier : parent2.healthUrgencyMultiplier;
            queenProximityWeight = Random.value < 0.5f ? parent1.queenProximityWeight : parent2.queenProximityWeight;
            altruismWeight = Random.value < 0.5f ? parent1.altruismWeight : parent2.altruismWeight;
            
            Mutate();
        }

        public void Randomize()
        {
            foodSeekingWeight = Random.Range(0.5f, 2.0f);
            acidAvoidanceWeight = Random.Range(1.0f, 3.0f);
            crowdingWeight = Random.Range(0.0f, 1.0f);
            healthUrgencyMultiplier = Random.Range(1.0f, 3.0f);
            queenProximityWeight = Random.Range(0.0f, 1.0f);
            altruismWeight = Random.Range(0.0f, 2.0f);
        }

        public void Mutate()
        {
            if (Random.value < ConfigurationManager.Instance.Mutation_Rate)
                foodSeekingWeight += Random.Range(-0.2f, 0.2f);
            if (Random.value < ConfigurationManager.Instance.Mutation_Rate)
                acidAvoidanceWeight += Random.Range(-0.3f, 0.3f);
            if (Random.value < ConfigurationManager.Instance.Mutation_Rate)
                crowdingWeight += Random.Range(-0.1f, 0.1f);
            if (Random.value < ConfigurationManager.Instance.Mutation_Rate)
                healthUrgencyMultiplier += Random.Range(-0.3f, 0.3f);
            if (Random.value < ConfigurationManager.Instance.Mutation_Rate)
                queenProximityWeight += Random.Range(-0.1f, 0.1f);
            if (Random.value < ConfigurationManager.Instance.Mutation_Rate)
                altruismWeight += Random.Range(-0.2f, 0.2f);

            foodSeekingWeight = Mathf.Max(0.1f, foodSeekingWeight);
            acidAvoidanceWeight = Mathf.Max(0.1f, acidAvoidanceWeight);
            crowdingWeight = Mathf.Max(0.0f, crowdingWeight);
            healthUrgencyMultiplier = Mathf.Max(1.0f, healthUrgencyMultiplier);
            queenProximityWeight = Mathf.Max(0.0f, queenProximityWeight);
            altruismWeight = Mathf.Max(0.0f, altruismWeight);
        }
    }
}
