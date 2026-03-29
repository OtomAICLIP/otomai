using AssetsTools.NET;

namespace Bubble.Core.Datacenter.Datacenter;

public interface IDofusSubObject;

public interface IDofusObject
{
    string Namespace { get; }

    string Class { get; }

    string Assembly { get; }

    int PrimaryKey { get; }

    public void AfterRead(IDictionary<long, IDofusObject> objects);

    IDofusObject Read(AssetsFileReader reader);

    void Write(AssetsFileWriter writer, IDictionary<long, IDofusObject> writeAfter);
}

public interface IDofusRootObject
{
    abstract static string FileName { get; }
}
