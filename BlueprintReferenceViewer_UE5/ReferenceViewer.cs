using BlueprintReferenceViewer_UE5.Exceptions;
using BlueprintReferenceViewer_UE5.Extensions;
using BlueprintReferenceViewer_UE5.Extensions.Components;
using CUE4Parse.Compression;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using System.Reflection;

namespace BlueprintReferenceViewer_UE5
{
    public class ReferenceViewer
    {
        private readonly ViewerSettings _viewerSettings;
        private readonly IFileProvider _provider;

        // pairs AssetName - PackagePath (i.e. SM_Mesh - /Game/Meshes/SM_Mesh)
        public Dictionary<string, string> AlreadyExistingAssets = [];

        private UBlueprintGeneratedClass? _currentBlueprint;

        private List<HashSet<string>> ReferencedBlueprintsLevels = [];

        public ReferenceViewer(ViewerSettings Settings)
        {
            _viewerSettings = Settings;

            string OodleBinaryFilepath = GetDll("oo2core_9_win64.dll");
            OodleHelper.Initialize(OodleBinaryFilepath);
            string ZlibBinaryFilepath = GetDll("zlib-ng2.dll");
            ZlibHelper.Initialize(ZlibBinaryFilepath);

            _provider = InitializeProvider();

            try
            {
                if (Settings.bScanProjectForReferencedAssets)
                {
                    GetProjectAssets();
                }
            }
            catch (ProjectNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"Warning: {ex.Message}. Continuing without project assets.");
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            // Clean up all existing "Level <num>" files first
            string[] ExistingFiles = Directory.GetFiles(_viewerSettings.DumpFolder);
            foreach (string ExistingFile in ExistingFiles)
            {
                if (ExistingFile.ToLower().Contains("level"))
                {
                    File.Delete(ExistingFile);
                }
            }
        }

        public bool Execute()
        {
            string[] _blueprintsToList = File.ReadAllLines(_viewerSettings.DumpList);

            foreach (var BlueprintPackagePath in _blueprintsToList)
            {
                bool PackageFound = _provider.TryLoadPackage(BlueprintPackagePath, out IPackage? Package);
                if (!PackageFound || Package is null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed to load package {BlueprintPackagePath} in game files.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    continue;
                }
                var PackageExports = Package.GetExports();

                var BlueprintGeneratedClass = PackageExports.First(x => x.ExportType == "BlueprintGeneratedClass");
                _currentBlueprint = new(BlueprintGeneratedClass);
                ListReferencedBlueprints();
            }

            for (int i = 0; i < ReferencedBlueprintsLevels.Count; i++)
            {
                File.WriteAllLines($"{_viewerSettings.DumpFolder}\\Level_{i}.txt",
                    ReferencedBlueprintsLevels[i].Order());
            }

            return true;
        }

        private void ListReferencedBlueprints(int NestingLevel = 0)
        {
            HashSet<string> BlueprintPackagesToDump = [];

            if (_currentBlueprint is null)
            {
                throw new Exception("_currentBlueprint was null");
            }
            if (NestingLevel != 0 && !_currentBlueprint.HasAnyComponents())
            {
                return;
            }

            for (int i = 0; i < NestingLevel; i++)
            {
                Console.Write('\t');
            }

            string AssetName = _currentBlueprint.GetPackage().SubstringAfterLast('/');
            if (AlreadyExistingAssets.ContainsKey(AssetName))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;

                if (NestingLevel == 0)
                {
                    if (ReferencedBlueprintsLevels.Count == 0)
                    {
                        ReferencedBlueprintsLevels.Add([]);
                    }
                    ReferencedBlueprintsLevels[NestingLevel].Add(_currentBlueprint.GetPackage());
                }
            }
            else
            {
                while (ReferencedBlueprintsLevels.Count - 1 < NestingLevel)
                {
                    ReferencedBlueprintsLevels.Add([]);
                }
                ReferencedBlueprintsLevels[NestingLevel].Add(_currentBlueprint.GetPackage());
            }

            Console.WriteLine(AssetName);
            Console.ForegroundColor = ConsoleColor.Gray;

            HashSet<string> BlueprintPackages = new HashSet<string>();

            // Child class (can be blueprint)
            string SuperClass = _currentBlueprint.GetSuperClass();
            if (!SuperClass.StartsWith("/Script"))
            {
                BlueprintPackages.Add(SuperClass);
            }

            if (_viewerSettings.bSearchForActorSpawners)
            {
                List<UActorSpawner> ActorSpawners = _currentBlueprint.GetComponentsOfClass<UActorSpawner>();
                foreach (UActorSpawner ActorSpawner in ActorSpawners)
                {
                    if (ActorSpawner.Visualization is not null && !_viewerSettings.bIgnoreEditorOnlyVisualization)
                    {
                        BlueprintPackages.Add(ActorSpawner.Visualization);
                    }
                    foreach (string SceneElement in ActorSpawner.ActivatedSceneElements.Concat(ActorSpawner.DectivatedSceneElement))
                    {
                        BlueprintPackages.Add(SceneElement);
                    }
                }
            }
            else
            {
                List<UChildActorComponent> ChildActorComponents = _currentBlueprint.GetComponentsOfClass<UChildActorComponent>();
                foreach (UChildActorComponent ChildActorComponent in ChildActorComponents)
                {
                    if (ChildActorComponent.ChildActorClass is not null)
                    {
                        BlueprintPackages.Add(ChildActorComponent.ChildActorClass);
                    }
                }
            }

            foreach (string BlueprintPackagePath in BlueprintPackages)
            {
                bool PackageFound = _provider.TryLoadPackage(BlueprintPackagePath, out IPackage? Package);
                if (!PackageFound || Package is null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed to load package {BlueprintPackagePath} in game files.");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    continue;
                }
                var PackageExports = Package.GetExports();

                var BlueprintGeneratedClass = PackageExports.First(x => x.ExportType == "BlueprintGeneratedClass");
                _currentBlueprint = new(BlueprintGeneratedClass);
                ListReferencedBlueprints(NestingLevel + 1);
            }

            if (NestingLevel == 0)
            {
                Console.WriteLine();
            }
        }

        private static string GetDll(string DllFilename)
        {
            string TempDllPath = Path.Combine(Path.GetTempPath(), DllFilename);
            if (!File.Exists(TempDllPath))
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"BlueprintDumper_UE5.Resources.{DllFilename}");
                if (stream == null)
                {
                    throw new Exception($"Couldn't find {DllFilename} in Embedded Resources");
                }
                var ba = new byte[(int)stream.Length];
                _ = stream.Read(ba, 0, (int)stream.Length);
                File.WriteAllBytes(TempDllPath, ba);
            }
            return TempDllPath;
        }

        private IFileProvider InitializeProvider()
        {
            var ParseResult = Enum.TryParse(_viewerSettings.EngineVersion, out EGame CUE4Parse_GameVersion);
            if (!ParseResult)
            {
                throw new Exception($"Failed to parse UE game version {_viewerSettings.EngineVersion}");
            }

            var VersionContainer = new VersionContainer(CUE4Parse_GameVersion);
            var Provider = new DefaultFileProvider(_viewerSettings.PaksDirectory, SearchOption.TopDirectoryOnly,
                VersionContainer, StringComparer.Ordinal);
            Provider.MappingsContainer = new FileUsmapTypeMappingsProvider(_viewerSettings.MappingsFilepath);
            Provider.Initialize();
            Provider.SubmitKey(new FGuid(), new FAesKey(_viewerSettings.AESKey));
            Provider.PostMount();

            return Provider;
        }

        private void GetProjectAssets()
        {
            if (!Directory.Exists(_viewerSettings.ProjectDirectory))
            {
                throw new ProjectNotFoundException();
            }
            string[] ProjectAssets = Directory.GetFiles($"{_viewerSettings.ProjectDirectory}\\Content", "*.uasset", SearchOption.AllDirectories);
            foreach (string projectAssetPath in ProjectAssets)
            {
                string AssetPath = "/Game" + projectAssetPath.SubstringAfter("Content").SubstringBeforeLast('.').Replace('\\', '/');
                if (AssetPath.StartsWith(_viewerSettings.AssetsPackagePathToScanAt))
                {
                    if (AlreadyExistingAssets.ContainsKey(GetAssetName(projectAssetPath)))
                    {
                        throw new Exception($"Two assets with the same name: {AlreadyExistingAssets[GetAssetName(projectAssetPath)]} and {AssetPath}");
                    }
                    AlreadyExistingAssets.Add(GetAssetName(projectAssetPath), AssetPath);
                }
            }
        }

        private string GetAssetName(string AssetPath)
        {
            // if AssetPath in the following format: /Game/Meshes/SM_MyMesh
            if (AssetPath.Contains('/'))
            {
                return AssetPath.SubstringAfterLast('/');
            }
            // if AssetPath in the following format: ...\Content\Meshes\SM_MyMesh
            else if (AssetPath.Contains('\\'))
            {
                return AssetPath.SubstringAfterLast('\\').SubstringBeforeLast('.');
            }
            return string.Empty;
        }
    }
}
