namespace BubbleBot.Cli.Repository.Maps;

public class MapRecord
{
    public long Id { get; set; }
    public long TopNeighbourId { get; set; }
    public long BottomNeighbourId { get; set; }
    public long LeftNeighbourId { get; set; }
    public long RightNeighbourId { get; set; }
    

    public byte[] InteractiveElements { get; set; }
    public byte[] CellsData { get; set; }

    private List<InteractiveElementData>? _interactiveElements;

    public List<InteractiveElementData> GetInteractiveElements()
    {
        if (_interactiveElements == null)
        {
            DeserializeInteractiveElements();
        }

        return _interactiveElements ?? [];
    }

    private List<Cell>? _cells;

    public List<Cell> GetCells()
    {
        if (_cells == null)
        {
            DeserializeCells();
        }

        return _cells ?? [];
    }

    private void DeserializeCells()
    {
        _cells = [];
        using var reader = new BinaryReader(new MemoryStream(CellsData));
        var count = reader.ReadInt32();

        for (var i = 0; i < count; i++)
        {
            var cellNumber = ReadVarInt(reader);
            var speed = ReadVarInt(reader);
            var mapChangeData = ReadVarInt(reader);
            var moveZone = ReadVarInt(reader);
            var linkedZone = ReadVarInt(reader);
            var floor = ReadVarInt(reader);

            var bitfield = ReadVarInt(reader);
            var arrow = ReadVarInt(reader);
            var cell = new Cell
            {
                Id = (int)cellNumber,
                Speed = speed,
                MapChangeData = mapChangeData,
                MoveZone = moveZone,
                LinkedZone = linkedZone,
                Mov = (bitfield & 1) != 0,
                Los = (bitfield & 2) != 0,
                NonWalkableDuringFight = (bitfield & 4) != 0,
                NonWalkableDuringRp = (bitfield & 8) != 0,
                HavenbagCell = (bitfield & 16) != 0,
                FarmCell = (bitfield & 32) != 0,
                Visible = (bitfield & 64) != 0,
                Red = (bitfield & 128) != 0,
                Blue = (bitfield & 256) != 0,
                Floor = floor,
                Arrow = arrow
            };
            _cells.Add(cell);
        }
    }

    private void DeserializeInteractiveElements()
    {
        _interactiveElements = [];
        using var reader = new BinaryReader(new MemoryStream(InteractiveElements));
        var count = reader.ReadInt16();

        for (var i = 0; i < count; i++)
        {
            var gfxId = ReadVarInt(reader);
            var cellId = reader.ReadInt16();
            var interactionId = ReadVarInt(reader);
            _interactiveElements.Add(new InteractiveElementData
            {
                GfxId = gfxId,
                CellId = cellId,
                InteractionId = interactionId
            });
        }
    }

    private int ReadVarInt(BinaryReader reader)
    {
        var result = 0;
        var shift = 0;
        byte b;
        do
        {
            b = reader.ReadByte();
            result |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);

        return result;
    }
}