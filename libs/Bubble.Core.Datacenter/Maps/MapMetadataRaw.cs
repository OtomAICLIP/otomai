using System.Text;
using System.Text.Json.Serialization;

namespace Bubble.Core.Datacenter.Maps;

public class MapMetadataRaw
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("topNeighbourId")]
    public long TopNeighbourId { get; set; }
    [JsonPropertyName("bottomNeighbourId")]
    public long BottomNeighbourId { get; set; }
    [JsonPropertyName("leftNeighbourId")]
    public long LeftNeighbourId { get; set; }
    [JsonPropertyName("rightNeighbourId")]
    public long RightNeighbourId { get; set; }
    
    [JsonPropertyName("interactiveElements")]
    public InteractiveElementData[] InteractiveElements { get; set; } = [];

    [JsonPropertyName("cellsData")]
    public CellRawData[] CellsData { get; set; } = [];

    public static MapMetadataRaw ReadFromSimpleText(MemoryStream stream)
    {
        var asString = Encoding.UTF8.GetString(stream.ToArray());
        var lines = asString.Split("\r\n");
        
        var mapName = DatacenterService.GetSimpleTextField(lines, "m_Name");
        var mapId = mapName!.Split('_')[1].Replace("\"", "");
        
        var id = long.Parse(mapId);
        
        var topNeighbourId = DatacenterService.GetSimpleTestFieldAsLong(lines, "topNeighbourId");
        var bottomNeighbourId = DatacenterService.GetSimpleTestFieldAsLong(lines, "bottomNeighbourId");
        var leftNeighbourId = DatacenterService.GetSimpleTestFieldAsLong(lines, "leftNeighbourId");
        var rightNeighbourId = DatacenterService.GetSimpleTestFieldAsLong(lines, "rightNeighbourId");
        
        var interactiveElementIds = DatacenterService.GetSimpleTestFieldAsListOfRids(lines, "interactiveElements");
        var cellsData = GetSimpleTestFieldAsCellDataArray(lines, "cellsData");
        var interactiveElements = new List<InteractiveElementData>();
        
        foreach (var referenceId in interactiveElementIds)
        {
            var refIndex = DatacenterService.GetSimpleTextReferenceIndexField(lines, referenceId);
            
            if (refIndex == -1)
                continue;

            var endIndex = -1;
            for (var i = refIndex + 1; i < lines.Length; i++)
            {
                if (!lines[i].StartsWith("REG_SET"))
                    continue;
                
                endIndex = i;
                break;
            }
            
            if(endIndex == -1) // we are at the end of the file
                endIndex = lines.Length;
            
            var refSet = lines[(refIndex + 1)..endIndex];
            
            var gfxId = DatacenterService.GetSimpleTestFieldAsInt(refSet, "gfxId");
            var cellId = DatacenterService.GetSimpleTestFieldAsInt(refSet, "cellId");
            var interactionId = DatacenterService.GetSimpleTestFieldAsInt(refSet, "m_interactionId");
            
            interactiveElements.Add(new InteractiveElementData
            {
                GfxId = gfxId,
                CellId = cellId,
                InteractionId = interactionId
            });
        }
        
        return new MapMetadataRaw
        {
            Id = id,
            TopNeighbourId = topNeighbourId,
            BottomNeighbourId = bottomNeighbourId,
            LeftNeighbourId = leftNeighbourId,
            RightNeighbourId = rightNeighbourId,
            InteractiveElements = interactiveElements.ToArray(),
            CellsData = cellsData.ToArray()
        };
    }

    private static List<CellRawData> GetSimpleTestFieldAsCellDataArray(string[] source, string fieldName)
    {
        var fieldIndex = DatacenterService.GetSimpleTextIndexField(source, fieldName);
        
        if (fieldIndex == -1)
            return [];
        
        var fieldSet = source[fieldIndex + 1];
        // BEGIN:Array,Array,Size
        var size = int.Parse(fieldSet.Split(',')[2]);
        
        var endArrayIndex = DatacenterService.GetSimpleTextIndexEndArray(source[fieldIndex..]);
        var lines = source[fieldIndex..(fieldIndex + endArrayIndex)];

        var cells = new List<CellRawData>(size);
        CellRawData? cellData = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("BEGIN"))
            {
                continue;
            }
            
            if (line.StartsWith("SET:data="))
            {
                cellData = new CellRawData();
                cells.Add(cellData);
                continue;
            }

            if (line.StartsWith("END"))
            {
                continue;
            }
            
            if(cellData == null)
                continue;

            var subFieldName = line.Split('=')[0][4..];
            var data = line.Split('=')[1];

            switch (subFieldName)
            {
                case "cellNumber":
                    cellData.CellNumber = uint.Parse(data);
                    break;
                case "speed":
                    cellData.Speed = int.Parse(data);
                    break;
                case "mapChangeData":
                    cellData.MapChangeData = int.Parse(data);
                    break;
                case "moveZone":
                    cellData.MoveZone = int.Parse(data);
                    break;
                case "linkedZone":
                    cellData.LinkedZone = int.Parse(data);
                    break;
                case "mov":
                    cellData.Mov = data == "1";
                    break;
                case "los":
                    cellData.Los = data == "1";
                    break;
                case "nonWalkableDuringFight":
                    cellData.NonWalkableDuringFight = data == "1";
                    break;
                case "nonWalkableDuringRP":
                    cellData.NonWalkableDuringRp = data == "1";
                    break;
                case "havenbagCell":
                    cellData.HavenbagCell = data == "1";
                    break;
                case "farmCell":
                    cellData.FarmCell = data == "1";
                    break;
                case "visible":
                    cellData.Visible = data == "1";
                    break;
                case "floor":
                    cellData.Floor = int.Parse(data);
                    break;
                case "blue":
                    cellData.Blue = data == "1";
                    break;
                case "red":
                    cellData.Red = data == "1";
                    break;
                case "arrow":
                    cellData.Arrow = int.Parse(data);
                    break;
            }
        }
        
        return cells;
    }
    
    void WriteVarInt(BinaryWriter writer, int value)
    {
        var uValue = (uint)value;
        while (uValue >= 0x80)
        {
            writer.Write((byte)(uValue | 0x80));
            uValue >>= 7;
        }
        writer.Write((byte)uValue);
    }

    
    public byte[] InteractiveElementsToBin()
    {
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        writer.Write((short)InteractiveElements.Length);
        foreach (var element in InteractiveElements)
        {
            WriteVarInt(writer, element.GfxId);
            writer.Write((short)element.CellId);
            WriteVarInt(writer, element.InteractionId);
        }

        return stream.ToArray();
    }

    public byte[] CellsDataToBin()
    {
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(CellsData.Length);
        foreach (var cell in CellsData)
        {
            WriteVarInt(writer, (short)cell.CellNumber);
            WriteVarInt(writer, cell.Speed);
            WriteVarInt(writer, cell.MapChangeData);
            WriteVarInt(writer, cell.MoveZone);
            WriteVarInt(writer, cell.LinkedZone);
            WriteVarInt(writer, cell.Floor);

            var bitfield = 0;
            bitfield |= (cell.Mov ? 1 : 0) << 0;
            bitfield |= (cell.Los ? 1 : 0) << 1;
            bitfield |= (cell.NonWalkableDuringFight ? 1 : 0) << 2;
            bitfield |= (cell.NonWalkableDuringRp ? 1 : 0) << 3;
            bitfield |= (cell.HavenbagCell ? 1 : 0) << 4;
            bitfield |= (cell.FarmCell ? 1 : 0) << 5;
            bitfield |= (cell.Visible ? 1 : 0) << 6;
            bitfield |= (cell.Red ? 1 : 0) << 7;
            bitfield |= (cell.Blue ? 1 : 0) << 8;
            
            WriteVarInt(writer, (short)bitfield);
            WriteVarInt(writer, cell.Arrow);
        }

        return stream.ToArray();
    }
}


public class InteractiveElementData
{
    [JsonPropertyName("gfxId")]
    public int GfxId { get; set; }
    
    [JsonPropertyName("cellId")]
    public int CellId { get; set; }
    
    [JsonPropertyName("interactionId")]
    public int InteractionId { get; set; }
}


public class CellRawData
{
    [JsonPropertyName("cellNumber")]
    public uint CellNumber { get; set; }
    
    [JsonPropertyName("speed")]
    public int Speed  { get; set; }
    
    [JsonPropertyName("mapChangeData")]
    public int MapChangeData  { get; set; }
    
    [JsonPropertyName("moveZone")]
    public int MoveZone  { get; set; }
    
    [JsonPropertyName("linkedZone")]
    public int LinkedZone  { get; set; }
    
    [JsonPropertyName("mov")]
    public bool Mov  { get; set; }
    
    [JsonPropertyName("los")]
    public bool Los  { get; set; }
    
    [JsonPropertyName("nonWalkableDuringFight")]
    public bool NonWalkableDuringFight  { get; set; }
    
    [JsonPropertyName("nonWalkableDuringRP")]
    public bool NonWalkableDuringRp  { get; set; }
    
    [JsonPropertyName("havenbagCell")]
    public bool HavenbagCell { get; set; }
    
    [JsonPropertyName("farmCell")]
    public bool FarmCell  { get; set; }
    
    [JsonPropertyName("visible")]
    public bool Visible { get; set; }
    
    [JsonPropertyName("floor")]
    public int Floor { get; set; }
    
    [JsonPropertyName("red")]
    public bool Red  { get; set; }
    
    [JsonPropertyName("blue")]
    public bool Blue  { get; set; }
    
    [JsonPropertyName("arrow")]
    public int Arrow  { get; set; }
}
