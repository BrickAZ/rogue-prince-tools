// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;
public static class RecoverySteps
{
    public static void Run(params Action[] steps)
    {
        var failures=new List<Exception>();
        foreach(var step in steps)try{step();}catch(Exception failure){failures.Add(failure);}
        if(failures.Count>0)throw new AggregateException(failures);
    }
}
