// iMODValidator is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODValidator.
// 
// iMODValidator is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODValidator is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODValidator. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models.Packages
{
    /// <summary>
    /// Factory class for regisering and creating package objects based on package key
    /// </summary>
    public class PackageManager
    {
        private static PackageManager packageFactory = null;
        private List<Package> packages = null;
        public List<Package> Packages
        {
            get { return packages; }
            set { packages = value; }
        }

        public PackageManager()
        {
            packages = new List<Package>();
            RegisterPackage(new ANIPackage(ANIPackage.DefaultKey));
            RegisterPackage(new BNDPackage(BNDPackage.DefaultKey));
            RegisterPackage(new BOTPackage(BOTPackage.DefaultKey));
            RegisterPackage(new CAPPackage(CAPPackage.DefaultKey));
            RegisterPackage(new CHDPackage(CHDPackage.DefaultKey));
            RegisterPackage(new DRNPackage(DRNPackage.DefaultKey));
            RegisterPackage(new GHBPackage(GHBPackage.DefaultKey));
            RegisterPackage(new FHBPackage(FHBPackage.DefaultKey));
            RegisterPackage(new HFBPackage(HFBPackage.DefaultKey));
            RegisterPackage(new KDWPackage(KDWPackage.DefaultKey));
            RegisterPackage(new KHVPackage(KHVPackage.DefaultKey));
            RegisterPackage(new KVAPackage(KVAPackage.DefaultKey));
            RegisterPackage(new KVVPackage(KVVPackage.DefaultKey));
            RegisterPackage(new OLFPackage(OLFPackage.DefaultKey));
            RegisterPackage(new PSTPackage(PSTPackage.DefaultKey));
            RegisterPackage(new RCHPackage(RCHPackage.DefaultKey));
            RegisterPackage(new RIVPackage(RIVPackage.DefaultKey));
            RegisterPackage(new ISGPackage(ISGPackage.DefaultKey));
            RegisterPackage(new SHDPackage(SHDPackage.DefaultKey));
            RegisterPackage(new TOPPackage(TOPPackage.DefaultKey));
            RegisterPackage(new VCWPackage(VCWPackage.DefaultKey));
            RegisterPackage(new WELPackage(WELPackage.DefaultKey));
            RegisterPackage(new STOPackage(STOPackage.DefaultKey));
            RegisterPackage(new HEADPackage(HEADPackage.DefaultKey));
            RegisterPackage(new BDGFLFPackage(BDGFLFPackage.DefaultKey));
            RegisterPackage(new PCGPackage(PCGPackage.DefaultKey));
        }

        public static PackageManager Instance
        {
            get
            {
                if (packageFactory == null)
                {
                    packageFactory = new PackageManager();
                }
                return packageFactory;
            }
        }

        public void RegisterPackage(Package package)
        {
            packages.Add(package);
        }

        // Retrieves the right package for the given package string
        public Package CreatePackageInstance(string packageKey, Model model)
        {
            return CreatePackageInstance(this.packages, packageKey, model);
        }

        public Package CreatePackageInstance(List<Package> packages, string packageKey, Model model)
        {
            for (int i = 0; i < packages.Count; i++)
            {
                if (packages[i].HasKeyMatch(packageKey))
                {
                    Package package = packages[i].CreateInstance();
                    package.Model = model;
                    return package;
                }
            }
            return null;
        }

        public Package GetPackage(string packageKey)
        {
            return GetPackage(this.packages, packageKey);
        }

        public Package GetPackage(List<Package> packages, string packageKey)
        {
            for (int i = 0; i < packages.Count; i++)
            {
                if (packages[i].HasKeyMatch(packageKey))
                {
                    return packages[i];
                }
            }
            return null;
        }
    }
}
