﻿using SiraUtil.Affinity.Harmony;
using Zenject;

namespace SiraUtil.Affinity
{
    internal class AffinityKernel : IInitializable, ILateDisposable
    {
        private readonly AffinityManager _affinityManager;
        private readonly IAffinityPatcher _affinityPatcher = new HarmonyAffinityPatcher();

        public AffinityKernel([InjectLocal] AffinityManager affinityManager)
        {
            _affinityManager = affinityManager;
        }

        public void Initialize()
        {
            foreach (var affinity in _affinityManager.Affinities)
                _affinityPatcher.Patch(affinity);
        }

        public void LateDispose()
        {
            _affinityPatcher.Dispose();
        }
    }
}