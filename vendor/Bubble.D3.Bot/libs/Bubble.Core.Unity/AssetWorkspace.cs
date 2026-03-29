using AssetsTools.NET;
using AssetsTools.NET.Cpp2IL;
using AssetsTools.NET.Extra;
using Serilog;

namespace Bubble.Core.Unity;

public class AssetWorkspace
{
    private bool _setMonoTempGeneratorsYet;
    public Dictionary<AssetPPtr, AssetContainer> LoadedAssets = new();

    public List<AssetsFileInstance> LoadedFiles = [];

    public required AssetsManager AssetsManager { get; init; }

    public AssetContainer? GetAssetContainer(AssetsFileInstance fileInst, AssetTypeValueField pptrField, bool onlyInfo = true)
    {
        var fileId = pptrField["m_FileID"].AsInt;
        var pathId = pptrField["m_PathID"].AsLong;
        return GetAssetContainer(fileInst, fileId, pathId, onlyInfo);
    }

    public AssetContainer? GetAssetContainer(AssetsFileInstance fileInst, int fileId, long pathId, bool onlyInfo = true)
    {
        if (fileId != 0)
            fileInst = fileInst.GetDependency(AssetsManager, fileId - 1);

        if (fileInst != null)
        {
            var assetId = new AssetPPtr(fileInst.path, pathId);

            if (!LoadedAssets.TryGetValue(assetId, out var cont))
                return null;

            if (onlyInfo || cont.HasValueField)
                return cont;

            // only set mono temp generator when we open a MonoBehaviour
            var isMonoBehaviour = cont.ClassId is (int)AssetClassID.MonoBehaviour or < 0;

            if (isMonoBehaviour && !_setMonoTempGeneratorsYet && !fileInst.file.Metadata.TypeTreeEnabled)
            {
                var dataDir = AssetWorkspace.GetAssetsFileDirectory(fileInst);
                var success = SetMonoTempGenerators(dataDir);
                if (!success)
                {
                    //MonoTemplateLoadFailed?.Invoke(dataDir);
                }
            }

            try
            {
                var tempField = GetTemplateField(cont);

                RefTypeManager? refMan = null;
                if (isMonoBehaviour)
                    refMan = AssetsManager.GetRefTypeManager(fileInst);

                var baseField = tempField.MakeValue(cont.FileReader, cont.FilePosition, refMan);
                cont = new AssetContainer(cont, baseField);
            }
            catch
            {
                cont = null;
            }

            return cont;
        }

        return null;
    }

    private static string GetAssetsFileDirectory(AssetsFileInstance fileInst)
    {
        if (fileInst.parentBundle != null)
        {
            var dir = Path.GetDirectoryName(fileInst.parentBundle.path)!;

            // addressables
            var upDir = Path.GetDirectoryName(dir);
            var upDir2 = Path.GetDirectoryName(upDir ?? string.Empty);
            if (upDir == null || upDir2 == null)
                return dir;

            if (Path.GetFileName(upDir) == "aa" && Path.GetFileName(upDir2) == "StreamingAssets")
                dir = Path.GetDirectoryName(upDir2)!;

            return dir;
        }
        else
        {
            var dir = Path.GetDirectoryName(fileInst.path)!;
            if (fileInst.name == "unity default resources" || fileInst.name == "unity_builtin_extra")
                dir = Path.GetDirectoryName(dir)!;
            return dir;
        }
    }

    public AssetTypeValueField? GetBaseField(AssetContainer? cont)
    {
        if (cont == null)
            return null;

        if (cont.HasValueField)
            return cont.BaseValueField;

        cont = GetAssetContainer(cont.FileInstance, 0, cont.PathId, false);
        return cont?.BaseValueField;
    }

    public void GetDisplayNameFast(AssetContainer cont, bool usePrefix, out string assetName, out string typeName)
    {
        assetName = "Unnamed asset";
        typeName = "Unknown type";

        try
        {
            var cldb = AssetsManager.ClassDatabase;
            var file = cont.FileInstance.file;
            var reader = cont.FileReader;
            var filePosition = cont.FilePosition;
            var classId = cont.ClassId;
            var monoId = cont.MonoId;

            if (reader == null)
            {
                assetName = "Unnamed asset";
                return;
            }

            var type = cldb.FindAssetClassByID(classId);

            if (file.Metadata.TypeTreeEnabled)
            {
                var ttType = classId is 0x72 or < 0
                    ? file.Metadata.FindTypeTreeTypeByScriptIndex(monoId)
                    : file.Metadata.FindTypeTreeTypeByID(classId);

                if (ttType != null && ttType.Nodes.Count > 0)
                {
                    typeName = ttType.Nodes[0].GetTypeString(ttType.StringBufferBytes);
                    if (ttType.Nodes.Count > 1 && ttType.Nodes[1].GetNameString(ttType.StringBufferBytes) == "m_Name")
                    {
                        reader.Position = filePosition;
                        assetName = reader.ReadCountStringInt32();

                        if (assetName == "")
                            assetName = "Unnamed asset";

                        return;
                    }

                    switch (typeName)
                    {
                        case "GameObject":
                        {
                            reader.Position = filePosition;
                            var size = reader.ReadInt32();
                            var componentSize = file.Header.Version > 0x10 ? 0x0c : 0x10;
                            reader.Position += size * componentSize;
                            reader.Position += 0x04;
                            assetName = reader.ReadCountStringInt32();
                            if (usePrefix)
                                assetName = $"GameObject {assetName}";
                            return;
                        }
                        case "MonoBehaviour":
                        {
                            reader.Position = filePosition;
                            reader.Position += 0x1c;
                            assetName = reader.ReadCountStringInt32();

                            if (assetName != "")
                                return;

                            assetName = GetMonoBehaviourNameFast(cont);

                            if (assetName == "")
                                assetName = "Unnamed asset";
                            return;
                        }
                        default:
                            assetName = "Unnamed asset";
                            return;
                    }
                }
            }

            if (type == null)
            {
                typeName = $"0x{classId:X8}";
                assetName = "Unnamed asset";
                return;
            }

            typeName = cldb.GetString(type.Name);
            var cldbNodes = type.GetPreferredNode().Children;

            if (cldbNodes.Count == 0)
            {
                assetName = "Unnamed asset";
                return;
            }

            if (cldbNodes.Count > 1 && cldb.GetString(cldbNodes[0].FieldName) == "m_Name")
            {
                reader.Position = filePosition;
                assetName = reader.ReadCountStringInt32();
                if (assetName == "")
                    assetName = "Unnamed asset";
                return;
            }

            switch (typeName)
            {
                case "GameObject":
                {
                    reader.Position = filePosition;
                    var size = reader.ReadInt32();
                    var componentSize = file.Header.Version > 0x10 ? 0x0c : 0x10;
                    reader.Position += size * componentSize;
                    reader.Position += 0x04;
                    assetName = reader.ReadCountStringInt32();
                    if (usePrefix)
                        assetName = $"GameObject {assetName}";
                    return;
                }
                case "MonoBehaviour":
                {
                    reader.Position = filePosition;
                    reader.Position += 0x1c;
                    assetName = reader.ReadCountStringInt32();

                    if (assetName != "")
                        return;

                    assetName = GetMonoBehaviourNameFast(cont);

                    if (assetName == "")
                        assetName = "Unnamed asset";

                    return;
                }
            }

            assetName = "Unnamed asset";
        }
        catch (Exception e)
        {
            // ignored
            Log.Error(e, "Failed to get display name for asset {Asset}", cont.AssetPPtr);
        }
    }

    public string GetMonoBehaviourNameFast(AssetContainer cont)
    {
        try
        {
            if (cont.ClassId != (uint)AssetClassID.MonoBehaviour && cont.ClassId >= 0)
                return string.Empty;

            AssetTypeValueField monoBf;
            if (cont.HasValueField)
                monoBf = cont.BaseValueField!;
            else
            {
                // this is a bad idea. this directly calls am.GetTemplateField
                // which won't look for new MonoScripts from UABEA.
                // hasTypeTree is set to false to ignore type tree (to prevent
                // reading the entire MonoBehaviour if type trees are provided)

                // it might be a better idea to just temporarily remove the extra
                // fields from a single MonoBehaviour so we don't have to read
                // from the cldb (especially so for stripped versions of bundles)

                var wasUsingCache = AssetsManager.UseTemplateFieldCache;
                AssetsManager.UseTemplateFieldCache = false;
                var monoTemp = GetTemplateField(cont, true, true);
                AssetsManager.UseTemplateFieldCache = wasUsingCache;

                if(monoTemp == null)
                    return string.Empty;
                
                monoBf = monoTemp.MakeValue(cont.FileReader, cont.FilePosition);
            }

            var monoScriptCont = GetAssetContainer(cont.FileInstance, monoBf["m_Script"], false);
            if (monoScriptCont == null)
                return string.Empty;

            var scriptBaseField = monoScriptCont.BaseValueField;
            var scriptClassName = scriptBaseField?["m_ClassName"].AsString;

            return scriptClassName ?? string.Empty;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to get MonoBehaviour name for asset {Asset}", cont.AssetPPtr);
            return string.Empty;
        }
    }

    public AssetTypeTemplateField? GetTemplateField(AssetContainer cont, bool forceCldb = false, bool skipMonoBehaviourFields = false)
    {
        var readFlags = AssetReadFlags.None;
        if (forceCldb)
            readFlags |= AssetReadFlags.ForceFromCldb;
        if (skipMonoBehaviourFields)
            readFlags |= AssetReadFlags.SkipMonoBehaviourFields;

        return AssetsManager.GetTemplateBaseField(cont.FileInstance, cont.FileReader, cont.FilePosition, cont.ClassId, cont.MonoId, readFlags);
    }

    public bool SetMonoTempGenerators(string fileDir)
    {
        if (_setMonoTempGeneratorsYet)
            return false;

        _setMonoTempGeneratorsYet = true;

        var il2CppFiles = FindCpp2IlFiles.Find(fileDir);
        if (il2CppFiles.success)
        {
            AssetsManager.MonoTempGenerator = new Cpp2IlTempGenerator(il2CppFiles.metaPath, il2CppFiles.asmPath);
            return true;
        }

        var managedDir = Path.Combine(fileDir, "Managed");

        return Directory.Exists(managedDir);
    }
}