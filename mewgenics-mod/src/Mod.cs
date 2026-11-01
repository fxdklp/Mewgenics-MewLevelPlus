using System;
using System.Runtime.InteropServices;
using MewgenicsModSdk;
using MewgenicsModSdk.Game;

namespace MewMods;

// MewLevelPlus — 10级后继续升级法术/被动（可实现的完整版）。
//
// UI 层（hook 0x3803b0，生成选项的包装函数）：
//   系统刷新(非 reroll)时，param 0x7(属性) 按位置改写：
//     1 号位 -> param 0xB（新被动替换）
//     2 号位 -> 优先「升级法术(param 0x8)」，若无可升级法术则「新法术替换(param 0x4)」
//     3 号位 -> param 0xD（新被动替换）
//     4 号位 -> param 0x4（新法术替换）
//   重掷(reroll) 在「法术/被动」与「属性」间交替。
//
// 2 号位的「优先升级、耗尽后替换」实现：先以 param 0x8 调生成函数，
// 读回 outBuf 的 type 字段（+0x000 低32位），type==2 表示成功生成升级；
// 游戏在「无可升级法术」时会 fallback 成属性(type==1)，此时改用 param 0x4 重新生成。
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
        bool tryUpgrade = false;   // 2 号位：优先升级法术，耗尽后替换

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
                    case 0: p = 0xB; break;            // 1 号位：新被动替换
                    case 1: tryUpgrade = true; break;  // 2 号位：升级法术 or 新法术替换
                    case 2: p = 0xD; break;            // 3 号位：新被动替换（第二个）
                    default: p = 0x4; break;           // 4 号位：新法术替换
                }
                _choicePos++;
            }
        }

        if (tryUpgrade)
        {
            // 先尝试「升级法术」；游戏在无可升级法术时会 fallback 成属性(type=1)
            nint r = _genSlot.InvokeRet(screen, outBuf, (nint)0x8);
            if (ReadChoiceType(outBuf) == 2) return r;   // 成功生成「升级法术」选项
            // 没有可升级法术了 -> 改为「新法术替换」
            _self.Log("[MewLevelPlus] 2号位无可升级法术，改为新法术替换");
            return _genSlot.InvokeRet(screen, outBuf, (nint)0x4);
        }

        return _genSlot.InvokeRet(screen, outBuf, (nint)(ulong)p);
    }

    // 读选项缓冲区的类型字段（+0x000 低32位）：2=升级法术，1=属性，4=新法术，5=新被动
    static unsafe uint ReadChoiceType(nint outBuf)
    {
        return *(uint*)outBuf;
    }
}
