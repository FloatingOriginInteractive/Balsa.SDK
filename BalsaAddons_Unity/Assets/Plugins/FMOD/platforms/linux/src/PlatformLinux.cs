using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_STANDALONE_LINUX && !UNITY_EDITOR
namespace FMOD
{
    public partial class VERSION
    {
        public const string dll = "fmodstudio" + dllSuffix;
    }
}

namespace FMOD.Studio
{
    public partial class STUDIO_VERSION
    {
        public const string dll = "fmodstudio" + dllSuffix;
    }
}
#endif

namespace FMODUnity
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class PlatformLinux : Platform
    {
        static PlatformLinux()
        {
            Settings.AddPlatformTemplate<PlatformLinux>("b7716510a1f36934c87976f3a81dbf3d");
        }

        public override string DisplayName { get { return "Linux"; } }
        public override void DeclareRuntimePlatforms(Settings settings)
        {
            settings.DeclareRuntimePlatform(RuntimePlatform.LinuxPlayer, this);
        }

#if UNITY_EDITOR
        public override IEnumerable<BuildTarget> GetBuildTargets()
        {
            yield return BuildTarget.StandaloneLinux64;
        }

        public override Legacy.Platform LegacyIdentifier { get { return Legacy.Platform.Linux; } }

        protected override BinaryAssetFolderInfo GetBinaryAssetFolder(BuildTarget buildTarget)
        {
            return new BinaryAssetFolderInfo("linux", "Plugins");
        }

        protected override IEnumerable<FileRecord> GetBinaryFiles(BuildTarget buildTarget, bool allVariants, string suffix)
        {
            foreach (string architecture in GetArchitectures(buildTarget))
            {
                yield return new FileRecord(string.Format("{0}/libfmodstudio{1}.so", architecture, suffix));
            }
        }

        protected override IEnumerable<FileRecord> GetOptionalBinaryFiles(BuildTarget buildTarget, bool allVariants)
        {
            if (allVariants)
            {
                foreach (string architecture in new string[] { "x86", "x86_64" })
                {
                    yield return new FileRecord(string.Format("{0}/libfmod.so", architecture));
                    yield return new FileRecord(string.Format("{0}/libfmodL.so", architecture));
                }

                yield return new FileRecord("x86/libfmodstudio.so");
                yield return new FileRecord("x86/libfmodstudioL.so");

                yield return new FileRecord("x86/libgvraudio.so");
                yield return new FileRecord("x86/libresonanceaudio.so");
            }

            foreach (string architecture in GetArchitectures(buildTarget))
            {
                yield return new FileRecord(string.Format("{0}/libgvraudio.so", architecture));
                yield return new FileRecord(string.Format("{0}/libresonanceaudio.so", architecture));
            }
        }

        private static IEnumerable<string> GetArchitectures(BuildTarget buildTarget)
        {
            bool universal = false;

            if (universal || buildTarget == BuildTarget.StandaloneLinux64)
            {
                yield return "x86_64";
            }
        }
#endif

        public override string GetPluginPath(string pluginName)
        {
            return string.Format("{0}/lib{1}.so", GetPluginBasePath(), pluginName);
        }

#if UNITY_EDITOR
        public override OutputType[] ValidOutputTypes
        {
            get
            {
                return sValidOutputTypes;
            }
        }

        private static OutputType[] sValidOutputTypes = {
           new OutputType() { displayName = "Pulse Audio", outputType = FMOD.OUTPUTTYPE.PULSEAUDIO },
           new OutputType() { displayName = "Advanced Linux Sound Architecture", outputType = FMOD.OUTPUTTYPE.ALSA },
        };
#endif
    }
}
