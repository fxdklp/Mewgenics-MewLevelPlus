using System.Runtime.InteropServices;
using MewgenicsModSdk;
using MewgenicsModSdk.Api;

namespace MewMods;

// 加载器查找的入口点。照抄即可：它创建 ModImpl 并转发给 SDK。
public static unsafe class Exports
{
    static readonly ModImpl Mod = new();

    [UnmanagedCallersOnly(EntryPoint = "MewMod_GetInfo")]
    public static ModInfo* GetInfo() => ModEntry.GetInfo(Mod);

    [UnmanagedCallersOnly(EntryPoint = "MewMod_Init")]
    public static void Init(MewgenicsApi* api) => ModEntry.Init(Mod, api);

    [UnmanagedCallersOnly(EntryPoint = "MewMod_Enable")]
    public static void Enable() => ModEntry.Enable(Mod);

    [UnmanagedCallersOnly(EntryPoint = "MewMod_Disable")]
    public static void Disable() => ModEntry.Disable(Mod);

    [UnmanagedCallersOnly(EntryPoint = "MewMod_ConfigReload")]
    public static void ConfigReload() => ModEntry.ConfigReload(Mod);

    [UnmanagedCallersOnly(EntryPoint = "MewMod_Action")]
    public static void Action(byte* key) => ModEntry.Action(Mod, Marshal.PtrToStringUTF8((nint)key));
}
