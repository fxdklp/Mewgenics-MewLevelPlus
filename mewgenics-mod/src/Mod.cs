using System;
using System.Runtime.InteropServices;
using MewgenicsModSdk;
using MewgenicsModSdk.Game;

namespace MewMods;

// MewLevelPlus — 10级后继续升级法术/被动（可实现的完整版）。
//
// UI 层（hook 0x3803b0，即「奖励生成函数」的包装，第三参数 = rewardKind）：
//   系统刷新(非 reroll)时，param 0x7(属性) 按位置改写：
//     1 号位 -> 优先「升级已有被动(kind 9)」，耗尽后「新被动替换(kind 13=0xD)」
//     2 号位 -> 优先「升级法术(kind 8)」，耗尽后「新法术替换(kind 0x4)」
//     3 号位 -> 新被动替换(kind 13=0xD)
//     4 号位 -> 新法术替换(kind 0x4)
//   重掷(reroll) 在「法术/被动」与「属性」间交替。
//
// rewardKind 枚举（十进制，参考开源 mod "Skills & Passives First" 及实测）：
//   1 = 新主动技能、7 = 属性、8 = 升级法术、9 = 被动升级、10 = 混合升级、13 = 新被动。
// 关键：kind 9 = 升级已有被动（1→2级），此前一直误以为原版普通猫无此机制。
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
        bool tryUpgradeSpell = false;    // 2 号位
        bool tryUpgradePassive = false;  // 1 号位

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
                    case 0: tryUpgradePassive = true; break;  // 1 号位：被动升级 or 新被动替换
                    case 1: tryUpgradeSpell = true; break;    // 2 号位：法术升级 or 新法术替换
                    case 2: p = 0xD; break;                   // 3 号位：新被动替换
                    default: p = 0x4; break;                  // 4 号位：新法术替换
                }
                _choicePos++;
            }
        }

        if (tryUpgradePassive)
        {
            // 优先「升级已有被动」(kind 9)；耗尽后游戏 fallback 成属性(type=1)，改用「新被动替换」(kind 13=0xD)
            nint r = _genSlot.InvokeRet(screen, outBuf, (nint)0x9);
            uint t = ReadChoiceType(outBuf);
            if (t != 1) return r;   // 非属性(type!=1)，视为成功生成「升级被动」选项
            _self.Log($"[MewLevelPlus] 1号位无可升级被动(kind9->type={t})，改为新被动替换");
            return _genSlot.InvokeRet(screen, outBuf, (nint)0xD);
        }

        if (tryUpgradeSpell)
        {
            // 优先「升级法术」(kind 8)；耗尽后 fallback 成属性(type=1)，改用「新法术替换」
            nint r = _genSlot.InvokeRet(screen, outBuf, (nint)0x8);
            if (ReadChoiceType(outBuf) == 2) return r;   // 成功生成「升级法术」选项
            _self.Log("[MewLevelPlus] 2号位无可升级法术，改为新法术替换");
            return _genSlot.InvokeRet(screen, outBuf, (nint)0x4);
        }

        return _genSlot.InvokeRet(screen, outBuf, (nint)(ulong)p);
    }

    // 读选项缓冲区的类型字段（+0x000 低32位）：1=属性，2=升级法术，3=升级被动，4=新法术，5=新被动
    static unsafe uint ReadChoiceType(nint outBuf)
    {
        return *(uint*)outBuf;
    }
}
