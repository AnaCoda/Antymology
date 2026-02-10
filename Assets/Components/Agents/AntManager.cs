using Antymology.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace Antymology.Agents
{
    public class AntManager : Singleton<AntManager>
    {
        private List<Ant> ants = new List<Ant>();
        private float timeSinceLastStep = 0f;

        public IReadOnlyList<Ant> Ants => ants.AsReadOnly();

        private void Update()
        {
            timeSinceLastStep += Time.deltaTime;
            
            if (timeSinceLastStep >= ConfigurationManager.Instance.Timestep_Interval)
            {
                timeSinceLastStep = 0f;
                ExecuteTimestep();
            }
        }

        private void ExecuteTimestep()
        {
            foreach (Ant ant in ants)
            {
                ant.PerformTimestep();
            }
        }

        public void RegisterAnt(Ant ant)
        {
            if (!ants.Contains(ant))
            {
                ants.Add(ant);
            }
        }

        public void UnregisterAnt(Ant ant)
        {
            ants.Remove(ant);
        }

        public int GetAntCount()
        {
            return ants.Count;
        }
    }
}
