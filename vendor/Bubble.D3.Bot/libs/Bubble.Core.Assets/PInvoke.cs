using System.Runtime.InteropServices;

namespace Bubble.Core.Assets;

public class PInvoke
{
    [DllImport("textoolwrap")]
    public extern static uint DecodeByCrunchUnity(IntPtr data, IntPtr buf, int mode, uint width, uint height, uint byteSize);

    [DllImport("textoolwrap")]
    public extern static uint DecodeByPVRTexLib(IntPtr data, IntPtr buf, int mode, uint width, uint height);

    [DllImport("textoolwrap")]
    public extern static uint EncodeByCrunchUnity(IntPtr data, ref int checkoutId, int mode, int level, uint width, uint height, uint ver, int mips);

    [DllImport("textoolwrap")]
    public extern static bool PickUpAndFree(IntPtr outBuf, uint size, int id);

    [DllImport("textoolwrap")]
    public extern static uint EncodeByPVRTexLib(IntPtr data, IntPtr buf, int mode, int level, uint width, uint height);

    [DllImport("textoolwrap")]
    public extern static uint EncodeByISPC(IntPtr data, IntPtr buf, int mode, int level, uint width, uint height);
}