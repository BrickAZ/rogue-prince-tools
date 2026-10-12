// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;
// Native requests are events; absence before the consumer runs proves nothing.
public sealed class DeferredNativeLease
{
    public bool Owned {get;private set;}
    public bool Releasing {get;private set;}
    public bool Pending {get;private set;}
    private long requestedRevision;
    public void Issue(long revision){if(Releasing)throw new InvalidOperationException("Release pending");Owned=true;Pending=true;requestedRevision=revision;}
    public void Release(long revision){if(!Owned)return;Releasing=true;Pending=true;requestedRevision=revision;}
    public bool CanObserve(long revision)=>Owned && Pending && revision>requestedRevision;
    public bool Observe(long revision,bool present,bool matches)
    {
        if(!CanObserve(revision))return false;
        if(Releasing){if(present)return false;Owned=false;Pending=false;Releasing=false;return true;}
        if(!present || !matches)throw new InvalidOperationException("Native request not confirmed");
        Pending=false;return true;
    }
    public void Retire(){Owned=false;Pending=false;Releasing=false;}
}
