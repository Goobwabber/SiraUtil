﻿using System;
using System.Linq;
using Zenject;

namespace SiraUtil.Tools.FPFC
{
    internal class FPFCInstaller : Installer
    {
        public override void InstallBindings()
        {
            if (!Environment.GetCommandLineArgs().Any(a => a.ToLower() == FPFCToggle.Argument))
                return;

            Container.BindInterfacesTo<FPFCToggle>().AsSingle();
        }
    }
}