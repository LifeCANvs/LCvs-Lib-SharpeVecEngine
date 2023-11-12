using System.Text;
using ShapeEngine.Lib;

namespace ShapeEngine.Input;

public class InputTypeGamepadButtonAxis : IInputType
{
    private readonly ShapeGamepadButton neg;
    private readonly ShapeGamepadButton pos;
    private float deadzone;
    private readonly ShapeGamepadButton modifier;
    private readonly bool reverseModifier;
    public InputTypeGamepadButtonAxis(ShapeGamepadButton neg, ShapeGamepadButton pos, float deadzone = 0.1f, ShapeGamepadButton modifier = ShapeGamepadButton.NONE, bool reverseModifier = false)
    {
        this.neg = neg;
        this.pos = pos;
        this.deadzone = deadzone;
        this.modifier = modifier;
        this.reverseModifier = reverseModifier;
    }

    public virtual string GetName(bool shorthand = true)
    {
        string mod = (modifier != ShapeGamepadButton.NONE && !reverseModifier)
            ? InputTypeGamepadButton.GetGamepadButtonName(modifier, shorthand)
            : "";
        string negName = InputTypeGamepadButton.GetGamepadButtonName(neg, shorthand);
        string posName = InputTypeGamepadButton.GetGamepadButtonName(pos, shorthand);
        StringBuilder b = new(negName.Length + posName.Length + mod.Length + 2);
        if (mod.Length > 0)
        {
            b.Append(mod);
            b.Append('+');
        }
        b.Append(negName);
        b.Append('|');
        b.Append(posName);
        return b.ToString();
    }

    public float GetDeadzone() => deadzone;

    public void SetDeadzone(float value)
    {
        deadzone = ShapeMath.Clamp(value, 0f, 1f);
    }
    public InputState GetState(int gamepad = -1)
    {
        return GetState(neg, pos, gamepad, deadzone, modifier, reverseModifier);
    }

    public InputState GetState(InputState prev, int gamepad = -1)
    {
        return GetState(neg, pos, prev, gamepad, deadzone, modifier, reverseModifier);
    }

    public InputDevice GetInputDevice() => InputDevice.Gamepad;

    public IInputType Copy() => new InputTypeGamepadButtonAxis(neg, pos, deadzone);

    private static float GetAxis(ShapeGamepadButton neg, ShapeGamepadButton pos, int gamepad, float deadzone = 0.1f, ShapeGamepadButton modifier = ShapeGamepadButton.NONE, bool reverseModifier = false)
    {
        if (modifier != ShapeGamepadButton.NONE)
        {
            if (InputTypeGamepadButton.IsDown(modifier, gamepad, deadzone) == reverseModifier) return 0f;
        }
        float vNegative = GetValue(neg, gamepad, deadzone);
        float vPositive = GetValue(pos, gamepad, deadzone);
        return vPositive - vNegative;
    }
    private static float GetValue(ShapeGamepadButton button, int gamepad, float deadzone = 0.1f)
    {
        if (gamepad < 0) return 0f;

        int id = (int)button;
        if (id >= 30 && id <= 33)
        {
            id -= 30;
            float value = GetGamepadAxisMovement(gamepad, id);
            if (MathF.Abs(value) < deadzone) return 0f;
            if (value > 0f) return value;
            
            return 0f;
        }
        
        if (id >= 40 && id <= 43)
        {
            id -= 40;
            float value = GetGamepadAxisMovement(gamepad, id);
            if (MathF.Abs(value) < deadzone) return 0f;
            if (value < 0) return MathF.Abs(value);
            
            return 0f;
        }
        
        return IsGamepadButtonDown(gamepad, id) ? 1f : 0f;
    }
    public static InputState GetState(ShapeGamepadButton neg, ShapeGamepadButton pos, int gamepad, float deadzone = 0.1f, ShapeGamepadButton modifier = ShapeGamepadButton.NONE, bool reverseModifier = false)
    {
        float axis = GetAxis(neg, pos, gamepad, deadzone, modifier, reverseModifier);
        bool down = axis != 0f;
        return new(down, !down, axis, gamepad, InputDevice.Gamepad);
    }
    public static InputState GetState(ShapeGamepadButton neg, ShapeGamepadButton pos,
        InputState previousState, int gamepad, float deadzone = 0.2f, ShapeGamepadButton modifier = ShapeGamepadButton.NONE, bool reverseModifier = false)
    {
        return new(previousState, GetState(neg, pos, gamepad, deadzone, modifier, reverseModifier));
    }
    
}