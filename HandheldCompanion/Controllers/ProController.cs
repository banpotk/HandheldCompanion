﻿using HandheldCompanion.Inputs;
using HandheldCompanion.Managers;
using HandheldCompanion.Utils;
using static JSL;

namespace HandheldCompanion.Controllers;

public class ProController : JSController
{
    public ProController() : base()
    { }

    public ProController(JOY_SETTINGS settings, PnPDetails details) : base(settings, details)
    { }

    protected override void InitializeInputOutput()
    {
        // Additional controller specific source buttons
        SourceButtons.Add(ButtonFlags.Special2);
        SourceAxis.Add(AxisLayoutFlags.Gyroscope);
    }

    public override void UpdateInputs(long ticks, float delta)
    {
        // skip if controller isn't connected
        if (!IsConnected())
            return;

        base.UpdateState(delta);

        Inputs.ButtonState[ButtonFlags.Special2] = BitwiseUtils.HasByteSet(sTATE.buttons, ButtonMaskCapture);

        base.UpdateInputs(ticks, delta);
    }

    public override string ToString()
    {
        return "Nintendo Pro Controller";
    }

    public override void Plug()
    {
        TimerManager.Tick += UpdateInputs;
        base.Plug();
    }

    public override void Unplug()
    {
        TimerManager.Tick -= UpdateInputs;
        base.Unplug();
    }

    public override void SetVibration(byte LargeMotor, byte SmallMotor)
    {
        byte LargeMotorF = (byte)(LargeMotor * VibrationStrength);
        byte SmallMotorF = (byte)(SmallMotor * VibrationStrength);

        // Convert Xbox rumble values to Switch HD Rumble values
        float leftAmplitude = RumbleConverter.ConvertAmplitude(LargeMotorF);
        float rightAmplitude = RumbleConverter.ConvertAmplitude(SmallMotorF);

        JslSetHDRumble(UserIndex, RumbleConverter.MinFrequency, leftAmplitude, RumbleConverter.MaxFrequency, rightAmplitude);
    }

    public override string GetGlyph(ButtonFlags button)
    {
        switch (button)
        {
            case ButtonFlags.B1:
                return "\u21D2"; // B
            case ButtonFlags.B2:
                return "\u21D3"; // A
            case ButtonFlags.B3:
                return "\u21D1"; // Y
            case ButtonFlags.B4:
                return "\u21D0"; // X
            case ButtonFlags.L1:
                return "\u219C";
            case ButtonFlags.R1:
                return "\u219D";
            case ButtonFlags.Back:
                return "\u21FD";
            case ButtonFlags.Start:
                return "\u21FE";
            case ButtonFlags.L2Soft:
                return "\u219A";
            case ButtonFlags.L2Full:
                return "\u219A";
            case ButtonFlags.R2Soft:
                return "\u219B";
            case ButtonFlags.R2Full:
                return "\u219B";
            case ButtonFlags.Special:
                return "\u21F9";
            case ButtonFlags.Special2:
                return "\u21FA";
        }

        return base.GetGlyph(button);
    }

    public override string GetGlyph(AxisFlags axis)
    {
        switch (axis)
        {
            case AxisFlags.L2:
                return "\u219A";
            case AxisFlags.R2:
                return "\u219B";
        }

        return base.GetGlyph(axis);
    }

    public override string GetGlyph(AxisLayoutFlags axis)
    {
        switch (axis)
        {
            case AxisLayoutFlags.L2:
                return "\u219A";
            case AxisLayoutFlags.R2:
                return "\u219B";
        }

        return base.GetGlyph(axis);
    }
}