using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace CPSSMSToolboxExtension
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(CPSSMSToolboxExtensionCommandPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string)]
    public sealed class CPSSMSToolboxExtensionCommandPackage : Package
    {
        public const string PackageGuidString = "7214E7E3-B33C-4ED6-BC08-7C0B9C35070A";
        public CPSSMSToolboxExtensionCommandPackage()
        {

        }

        #region Package Members

        protected override void Initialize()
        {
            SetSkipLoading();
            System.Threading.Thread.Sleep(2000);

            CPSSMSToolboxExtensionCommand.Initialize(this);
            base.Initialize();
        }

        protected override int QueryClose(out bool canClose)
        {
            SetSkipLoading();
            return base.QueryClose(out canClose);
        }

        void SetSkipLoading()
        {
            try
            {
                var registryKey = UserRegistryRoot.CreateSubKey(string.Format("Packages\\{{{0}}}", PackageGuidString));

                registryKey.SetValue("SkipLoading", 1, RegistryValueKind.DWord);
                registryKey.Close();
            }
            catch
            { }
        }

        #endregion
    }
}
