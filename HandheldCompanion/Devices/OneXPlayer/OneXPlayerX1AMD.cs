﻿namespace HandheldCompanion.Devices;

public class OneXPlayerX1AMD : OneXPlayerX1
{
    public OneXPlayerX1AMD()
    {
        // https://www.amd.com/en/products/apu/amd-ryzen-7-8840u
        nTDP = new double[] { 15, 15, 28 };
        cTDP = new double[] { 15, 30 };
        GfxClock = new double[] { 100, 2700 };
        CpuClock = 5100;
    }

    public override bool IsBatteryProtectionSupported(int majorVersion, int minorVersion)
    {
        return majorVersion >= 1 && minorVersion >= 3;
    }
}
