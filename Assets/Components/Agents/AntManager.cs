using Antymology.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace Antymology.Agents
{
    public class AntManager : Singleton<AntManager>
    {
        private List<Ant> ants = new List<Ant>();

        public IReadOnlyList<Ant> Ants => ants.AsReadOnly();

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
