using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Bubble.Core.Datacenter.Datacenter;
using Bubble.Core.Datacenter.Datacenter.WorldGraph;
using Bubble.Core.Datacenter.Maps;
using Bubble.Core.Unity;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Bubble.Core.Datacenter;

public static class DatacenterService
{
    private static string? DofusPath;
    public static LocalizationFile? Localization { get; private set; }

    private static readonly object Lock = new object();

    public static void Initialize(string dofusPath)
    {
        DofusPath = Path.Combine(dofusPath, "Dofus_Data", "StreamingAssets", "Content");
        
        var i18NPath = Path.Combine(DofusPath, "I18n");
        
        Localization = LocalizationReader.ReadFile(Path.Combine(i18NPath, "fr.bin"));

        File.WriteAllText("fr.json", JsonSerializer.Serialize(Localization,
            new JsonSerializerOptions()
            {
                WriteIndented = true
            }));

    }
    
    private static bool DumpWorldGraphData(AssetWorkspace                                 workspace, 
                                           AssetContainer                                 asset,
                                           AssetFileInfo                                  info,
                                           string                                         assetName, 
                                           [NotNullWhen(true)] out WorldGraphEntry? result)
    {
        result = null;
        var baseField = workspace.GetBaseField(asset);

        if (baseField == null)
        {
            Log.Warning("Failed to get base field for asset {Name}.", assetName);
            return false;
        }

        using var memoryStream = new MemoryStream();

        asset.FileInstance.AssetsStream.Position -= asset.Size;
        asset.FileInstance.AssetsStream.CopyTo(memoryStream);
        memoryStream.Position = 0;
        var assetFileReader = new AssetsFileReader(memoryStream);
        
        var monoBehavior = WorldGraphBehaviour.Read(assetFileReader);

        result = monoBehavior.Object;
        return true;
    }
    
    private static bool DumpDataAsset<T>(AssetWorkspace workspace, AssetContainer asset, AssetFileInfo info, string assetName, [NotNullWhen(true)] out Dictionary<long, T>? result)
        where T : IDofusObject
    {
        result = null;
        var baseField = workspace.GetBaseField(asset);

        if (baseField == null)
        {
            Log.Warning("Failed to get base field for asset {Name}.", assetName);
            return false;
        }

        var references = baseField.Children.FirstOrDefault(x => x.FieldName == "references");

        if (references == null)
        {
            Log.Warning("Failed to get references field for asset {Name}.", assetName);
            return false;
        }

        using var memoryStream = new MemoryStream();

        asset.FileInstance.AssetsStream.Position -= asset.Size;
        asset.FileInstance.AssetsStream.CopyTo(memoryStream);
        memoryStream.Position = 0;
        var assetFileReader = new AssetsFileReader(memoryStream);
        
        var monoBehavior = DatacenterBehaviour.Read(assetFileReader);

        result = monoBehavior.Data.Objects.ToDictionary(x => x.Key, x => (T)x.Value);
        return true;
    }

    public static string GetText(uint key)
    {
        if(Localization == null)
            return $"[Unknown text: {key}]";
        
        return Localization.GetText(key);
    }

    public static string GetText(string key)
    {
        if(Localization == null)
            return $"[Unknown text: {key}]";
        
        return Localization.GetText(key);
    }

    public static WorldGraphEntry? LoadWorldGraph()
    {
        if (DofusPath == null)
            throw new InvalidOperationException("Datacenter service not initialized.");
        
        var secondaryDofusPath = Path.Combine(DofusPath, "..", "aa");

        var filePathLinks = Path.Combine(secondaryDofusPath, "catalog.json");
        var fileContent = File.ReadAllText(filePathLinks);
        var catalog = JsonSerializer.Deserialize<CatalogModel>(fileContent) ?? throw new InvalidOperationException("Failed to deserialize catalog.");
        
        var worldGraphPath = catalog.InternalIds.FirstOrDefault(x => x.Contains("worldassets") && !x.Contains("PathFinding"));
        if (worldGraphPath == null)
            throw new InvalidOperationException("Failed to find world-graph path.");
        
        // it look like       "{UnityEngine.AddressableAssets.Addressables.RuntimePath}\\StandaloneWindows64\\worldassets_assets_all_00fb41f1b07b553765149b0a906eb3e2.bundle",

        // we just need the worldassets_assets_all_00fb41f1b07b553765149b0a906eb3e2.bundle part
        var worldGraphBundle = worldGraphPath.Split('\\').Last().Split('.').First();
        var worldGraphBundlePath = Path.Combine(secondaryDofusPath, "StandaloneWindows64", worldGraphBundle + ".bundle");
        
        var bundle = AssetBundleReader.ReadAssetBundle(worldGraphBundlePath);
        var assetManager = UnityLib.GetAssetsManager();
        var workspace = new AssetWorkspace
        {
            AssetsManager = assetManager
        };
        
        for (var index = 0; index < bundle.BlockAndDirInfo.DirectoryInfos.Count; index++)
        {
            var data = BundleHelper.LoadAssetDataFromBundle(bundle, index);

            if (data == null)
            {
                Log.Warning("Failed to load asset {Index} from bundle {File}.", index, worldGraphBundlePath);
                continue;
            }
            
            lock (Lock)
            {
                using var unpackedStream = new MemoryStream(data);
                var fileInst = assetManager.LoadAssetsFile(unpackedStream, worldGraphBundlePath, true);
                workspace.LoadedFiles.Add(fileInst);

                foreach (var info in fileInst.file.AssetInfos)
                {
                    var cont = new AssetContainer(info, fileInst);
                    workspace.LoadedAssets.Add(cont.AssetPPtr, cont);

                    workspace.GetDisplayNameFast(cont, false, out var assetName, out var type);

                    if (type != "MonoBehaviour")
                        continue;

                    if(assetName != "world-graph")
                        continue;
                    
                    Log.Information("Dumping {Name}.", assetName);
                    
                    DumpWorldGraphData(workspace, cont, info, assetName, out var result);
                    
                    return result!;
                }
                    
                assetManager.UnloadAssetsFile(fileInst);
            }

        }

        return null;
    }

    public static Dictionary<long, T> Load<T>() where T : IDofusObject, IDofusRootObject
    {
        if (DofusPath == null)
            throw new InvalidOperationException("Datacenter service not initialized.");

        
        var filePath = T.FileName;

        if (!filePath.StartsWith(DofusPath))
            filePath = Path.Combine(DofusPath, "Data", filePath);

        Log.Information("Loading {Type} from {File}.", typeof (T).Name, filePath);

        var sw = Stopwatch.StartNew();
        try
        {
            var bundle = AssetBundleReader.ReadAssetBundle(filePath);
            var assetManager = UnityLib.GetAssetsManager();
            var workspace = new AssetWorkspace
            {
                AssetsManager = assetManager
            };

            for (var index = 0; index < bundle.BlockAndDirInfo.DirectoryInfos.Count; index++)
            {
                var data = BundleHelper.LoadAssetDataFromBundle(bundle, index);

                if (data == null)
                {
                    Log.Warning("Failed to load asset {Index} from bundle {File}.", index, filePath);
                    continue;
                }

                lock (Lock)
                {
                    using var unpackedStream = new MemoryStream(data);
                    var fileInst = assetManager.LoadAssetsFile(unpackedStream, filePath, true);
                    workspace.LoadedFiles.Add(fileInst);

                    foreach (var info in fileInst.file.AssetInfos)
                    {
                        var cont = new AssetContainer(info, fileInst);
                        workspace.LoadedAssets.Add(cont.AssetPPtr, cont);

                        workspace.GetDisplayNameFast(cont, false, out var assetName, out var type);

                        if (type != "MonoBehaviour")
                            continue;

                        if (DatacenterService.DumpDataAsset<T>(workspace, cont, info, assetName, out var result))
                            return result;
                    }
                    
                    assetManager.UnloadAssetsFile(fileInst);
                }
            }

            return new Dictionary<long, T>();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while loading {Type}.", typeof (T).Name);
            return new Dictionary<long, T>();
        }
        finally
        {
            Log.Information("Loaded {Type} in {Elapsed} ms.", typeof (T).Name, sw.ElapsedMilliseconds);
        }
    }

    public static IEnumerable<MapMetadataRaw> GetMaps(string bundlePath)
    {
        var filePath = bundlePath;
        
        var sw = Stopwatch.StartNew();
        try
        {
            var bundle = AssetBundleReader.ReadAssetBundle(filePath);
            var assetManager = UnityLib.GetAssetsManager();

            var workspace = new AssetWorkspace
            {
                AssetsManager = assetManager
            };

            for (var index = 0; index < bundle.BlockAndDirInfo.DirectoryInfos.Count; index++)
            {
                var data = BundleHelper.LoadAssetDataFromBundle(bundle, index);

                if (data == null)
                {
                    Log.Warning("Failed to load asset {Index} from bundle {File}.", index, filePath);
                    continue;
                }

                using var unpackedStream = new MemoryStream(data);
                var fileInst = assetManager.LoadAssetsFile(unpackedStream, filePath, true);
                workspace.LoadedFiles.Add(fileInst);

                foreach (var info in fileInst.file.AssetInfos)
                {
                    var cont = new AssetContainer(info, fileInst);
                    workspace.LoadedAssets.Add(cont.AssetPPtr, cont);

                    workspace.GetDisplayNameFast(cont, false, out var assetName, out var type);

                    if (type != "MonoBehaviour")
                        continue;

                    var baseField = workspace.GetBaseField(cont);

                    if (baseField == null)
                    {
                        Log.Warning("Failed to get base field for asset {Name}.", assetName);
                        continue;
                    }
                    
                    Log.Information("Dumping {Name}.", assetName);
                    
                    // in memory stream writer
                    using var memoryStream = new MemoryStream();
                    using var x = new StreamWriter(memoryStream);
                    
                    DatacenterService.DumpSimpleTextAsset(x, baseField);
                    
                    memoryStream.Position = 0;
                    var metaData = MapMetadataRaw.ReadFromSimpleText(memoryStream);
                    
                    // write to file
                    //var outPath = Path.Combine(outDir, $"{assetName}.json");
                    //File.WriteAllText(outPath, JsonSerializer.Serialize(metaData));
                    
                    yield return metaData;
                }
            }
        }
        finally
        {
            Log.Information("Loaded in {Elapsed} ms.", sw.ElapsedMilliseconds);
        }
    }


    
    private static void DumpSimpleTextAsset(StreamWriter sw, AssetTypeValueField baseField)
    {
        DatacenterService.RecurseSimpleTextDump(sw, baseField);
    }

    private static void RecurseSimpleTextDump(StreamWriter sw, AssetTypeValueField field)
    {
        var template = field.TemplateField;
        var typeName = template.Type;
        var fieldName = template.Name;
        var isArray = template.IsArray;

        if (isArray)
        {
            if (template.ValueType != AssetValueType.ByteArray)
            {          
                var size = field.AsArray.size;
                sw.WriteLine($"BEGIN:{typeName},{fieldName},{size}");
                foreach (var t in field.Children)
                {
                    DatacenterService.RecurseSimpleTextDump(sw, t);
                    sw.WriteLine("ENDSUB");
                }
                sw.WriteLine($"END:{typeName},{fieldName}");
            }
            else
            {
                var data = field.AsByteArray;
                var size = data.Length;
                
                sw.WriteLine($"BEGIN:{fieldName},{size}");
                foreach (var t in data)
                {
                    sw.WriteLine(t);    
                    sw.WriteLine($"ENDSUB");
                }
            }
        }
        else
        {
            var value = "";
            if (field.Value != null)
            {
                var evt = field.Value.ValueType;
                if (evt == AssetValueType.String)
                {
                    var fixedStr = DatacenterService.TextDumpEscapeString(field.AsString);
                    value = $"\"{fixedStr}\"";
                }
                else if (1 <= (int)evt && (int)evt <= 12)
                {
                    value = $"{field.AsString}";
                }
            }
            sw.WriteLine($"SET:{fieldName}={value}");

            if (field.Value != null && field.Value.ValueType == AssetValueType.ManagedReferencesRegistry)
            {
                var registry = field.Value.AsManagedReferencesRegistry;
                sw.WriteLine($"REGISTRY:{registry.version}");
                
                foreach (var refObj in registry.references)
                {
                    var typeRef = refObj.type;
                    sw.WriteLine($"REG_SET:{refObj.rid}:{typeRef.ClassName},{typeRef.Namespace},{typeRef.AsmName}");
                    
                    foreach (var child in refObj.data)
                    {
                        DatacenterService.RecurseSimpleTextDump(sw, child);
                    }
                }
            }
            else
            {
                foreach (var child in field)
                {
                    DatacenterService.RecurseSimpleTextDump(sw, child);
                }
            }
        }
        
    }
    
    
    internal static long GetSimpleTestFieldAsLong(string[] source, string fieldName)
    {
        var field = DatacenterService.GetSimpleTextField(source, fieldName);
        return field == null ? 0 : long.Parse(field);
    }

    internal static int GetSimpleTestFieldAsInt(string[] source, string fieldName)
    {
        var field = DatacenterService.GetSimpleTextField(source, fieldName);
        return field == null ? 0 : int.Parse(field);
    }

    internal static int GetSimpleTextIndexField(string[] source, string fieldName)
    {
        // We need to get the line number where the field is set
        for(var i = 0; i < source.Length; i++)
        {
            if (source[i].Contains($"SET:{fieldName}="))
                return i;
        }
        
        return -1;
    }    
    
    
    internal static int GetSimpleTextIndexEndArray(string[] source)
    {
        // We need to get the line number where the field is set
        for(var i = 0; i < source.Length; i++)
        {
            if (source[i].Contains($"END:"))
                return i;
        }
        
        return -1;
    }    

    internal static int GetSimpleTextReferenceIndexField(string[] source, long fieldName)
    {
        // We need to get the line number where the field is set
        for(var i = 0; i < source.Length; i++)
        {
            if (source[i].Contains($"REG_SET:{fieldName}:"))
                return i;
        }
        
        return -1;
    }  
    
    internal static List<long> GetSimpleTestFieldAsListOfRids(string[] source, string fieldName)
    {
        var fieldIndex = DatacenterService.GetSimpleTextIndexField(source, fieldName);
        
        if (fieldIndex == -1)
            return [];
        
        var fieldSet = source[fieldIndex + 1];
        // BEGIN:Array,Array,Size
        var size = int.Parse(fieldSet.Split(',')[2]);
        
        var result = new List<long>(size);
        
        for (var i = 0; i < size * 3; i++)
        {
            var field = source[fieldIndex + i];
            
            if (field.StartsWith("SET:rid="))
                result.Add(long.Parse(field.Split('=')[1]));
        }
        
        return result;
    }
    

    
    internal static string? GetSimpleTextField(string[] source, string fieldName)
    {
        // we get the line where it's set
        var fieldSet = source
            .FirstOrDefault(x => x.Contains($"SET:{fieldName}="));

        if (fieldSet == null)
            return null;
        
        return fieldSet.IndexOf('=') switch
        {
            -1 => null,
            var index => fieldSet[(index + 1)..]
        };
    }
    

    
    // only replace \ with \\ but not " with \" lol
    // you just have to find the last "
    private static string TextDumpEscapeString(string str)
    {
        return str
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }


}