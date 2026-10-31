using System;
using System.Runtime.InteropServices;
using MewgenicsModSdk;
using MewgenicsModSdk.Game;

namespace MewMods;

// MewLevelPlus — 10级后继续升级法术/被动（可实现的完整版）。
//
// UI 层（hook 0x3803b0，生成选项的包装函数）：
//   系统刷新(非 reroll)时，param 0x7(属性) 按位置改写：
//     1/3 位 -> param 0xB（新被动替换）
//     2/4 位 -> param 0x8（升级法术，若有未满级法术）或 0x4（新法术替换，若全满级）
//   重掷(reroll) 保持 param 0x7（属性）。
//
// 说明：「升级已有被动(1→2)」原版普通猫无此机制，被动位用「新被动替换」近似。
public sealed class ModImpl : MewgenicsMod
{
    static ModImpl _self;
    static HookSlot _genSlot;
    static bool _inReroll;
    static HookSlot _rerollSlot;
    static nint _lastScreen;
    static int _choicePos;
    static bool _nextIsSkill = true;   // roll 交替状态：true=法术/被动，false=属性

    public ModImpl()
        : base("mew_level_plus", "Mew Level Plus", "10级后升级屏法术/被动布局。")
    {
    }

    protected override unsafe void OnLoad()
    {
        _self = this;
        _genSlot = Hook(0x3803b0, (nint)(delegate* unmanaged<nint, nint, nint, nint>)&OnGenChoice);
        _rerollSlot = Hook(0x383FE0, (nint)(delegate* unmanaged<nint, nint>)&OnReroll);
        Log($"[MewLevelPlus] loaded gen={_genSlot.IsInstalled} reroll={_rerollSlot.IsInstalled}");
    }

    [UnmanagedCallersOnly]
    static nint OnReroll(nint screen)
    {
        _inReroll = true;
        _nextIsSkill = !_nextIsSkill;   // 每次点重掷在「法术/被动」与「属性」间交替
        _choicePos = 0;
        nint r = _rerollSlot.InvokeRet(screen);
        _inReroll = false;
        return r;
    }

    [UnmanagedCallersOnly]
    static nint OnGenChoice(nint screen, nint outBuf, nint type)
    {
        uint p = (uint)((ulong)type & 0xFFFFFFFF);
        if (p == 0x7)
        {
            bool doSkill;
            if (!_inReroll)
            {
                doSkill = true;          // 系统刷新：总是出法术/被动
                _nextIsSkill = true;
                if (screen != _lastScreen) { _lastScreen = screen; _choicePos = 0; }
            }
            else
            {
                doSkill = _nextIsSkill;  // 重掷：按交替状态
            }

            if (doSkill)
            {
                int pos = _choicePos % 4;
                switch (pos)
                {
                    case 0: p = 0xB; break;   // 1 号位：新被动替换
                    case 1: p = HasUnmaxedSpell(screen) ? 0x8u : 0x4u; break;   // 2 号位：升级法术 or 新法术替换
                    case 2: p = 0xD; break;   // 3 号位：新被动替换（第二个）
                    default: p = 0x4; break;  // 4 号位：新法术替换
                }
                _choicePos++;
            }
        }
        return _genSlot.InvokeRet(screen, outBuf, (nint)(ulong)p);
    }

    // 判断当前升级屏的猫是否有未满级法术（法术名不以 "2" 结尾）
    static unsafe bool HasUnmaxedSpell(nint screen)
    {
        ulong catData = 0;
        if (!HeapScan.TryRead<ulong>(screen + 0xa0, out catData) || catData == 0) return true;
        for (int i = 0; i < 4; i++)
        {
            string s = ReadSSO((nint)(catData + 0x810UL + (ulong)(i * 0x20)));
            if (!string.IsNullOrEmpty(s) && s != "None" && !s.EndsWith("2")) return true;
        }
        return false;
    }

    static unsafe string ReadSSO(nint ptr)
    {
        byte* p = (byte*)ptr;
        ulong size = *(ulong*)(p + 16);
        if (size > 15)
        {
            ulong heap = *(ulong*)p;
            if (heap == 0) return "";
            return new string((sbyte*)heap, 0, (int)Math.Min(size, 63), System.Text.Encoding.UTF8);
        }
        return new string((sbyte*)p, 0, (int)size, System.Text.Encoding.UTF8);
    }
}
