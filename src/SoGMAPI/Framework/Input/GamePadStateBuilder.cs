using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SoGModdingAPI.Framework.Input
{
    /// <summary>Manages controller state.</summary>
    internal class GamePadStateBuilder : IInputStateBuilder<GamePadStateBuilder, GamePadState>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The maximum direction to ignore for the left thumbstick.</summary>
        private const float LeftThumbstickDeadZone = 0.2f;

        /// <summary>The maximum direction to ignore for the right thumbstick.</summary>
        private const float RightThumbstickDeadZone = 0.9f;

        /// <summary>The underlying controller state.</summary>
        private GamePadState? State;

        /// <summary>The current button states.</summary>
        private readonly IDictionary<SButton, ButtonState> ButtonStates;

        /// <summary>The left trigger value.</summary>
        private float LeftTrigger;

        /// <summary>The right trigger value.</summary>
        private float RightTrigger;

        /// <summary>The left thumbstick position.</summary>
        private Vector2 LeftStickPos;

        /// <summary>The left thumbstick position.</summary>
        private Vector2 RightStickPos;


        /*********
        ** Accessors
        *********/
        /// <summary>Whether the gamepad is currently connected.</summary>
        public bool IsConnected { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="state">The initial state.</param>
        public GamePadStateBuilder(GamePadState state)
        {
            this.State = state;
            this.IsConnected = state.IsConnected;

            if (!this.IsConnected)
                return;

            GamePadDPad pad = state.DPad;
            GamePadButtons buttons = state.Buttons;
            GamePadTriggers triggers = state.Triggers;
            GamePadThumbSticks sticks = state.ThumbSticks;
            this.ButtonStates = new Dictionary<SButton, ButtonState>
            {
                [SButton.DPadUp] = pad.Up,
                [SButton.DPadDown] = pad.Down,
                [SButton.DPadLeft] = pad.Left,
                [SButton.DPadRight] = pad.Right,

                [SButton.ControllerA] = buttons.A,
                [SButton.ControllerB] = buttons.B,
                [SButton.ControllerX] = buttons.X,
                [SButton.ControllerY] = buttons.Y,
                [SButton.LeftStick] = buttons.LeftStick,
                [SButton.RightStick] = buttons.RightStick,
                [SButton.LeftShoulder] = buttons.LeftShoulder,
                [SButton.RightShoulder] = buttons.RightShoulder,
                [SButton.ControllerBack] = buttons.Back,
                [SButton.ControllerStart] = buttons.Start,
                [SButton.BigButton] = buttons.BigButton
            };
            this.LeftTrigger = triggers.Left;
            this.RightTrigger = triggers.Right;
            this.LeftStickPos = sticks.Left;
            this.RightStickPos = sticks.Right;
        }

        /// <summary>Override the states for a set of buttons.</summary>
        /// <param name="overrides">The button state overrides.</param>
        public GamePadStateBuilder OverrideButtons(IDictionary<SButton, SButtonState> overrides)
        {
            if (!this.IsConnected)
                return this;

            foreach (var pair in overrides)
            {
                bool changed = true;

                bool isDown = pair.Value.IsDown();
                switch (pair.Key)
                {
                    // left thumbstick
                    case SButton.LeftThumbstickUp:
                        this.LeftStickPos.Y = isDown ? 1 : 0;
                        break;
                    case SButton.LeftThumbstickDown:
                        this.LeftStickPos.Y = isDown ? 1 : 0;
                        break;
                    case SButton.LeftThumbstickLeft:
                        this.LeftStickPos.X = isDown ? 1 : 0;
                        break;
                    case SButton.LeftThumbstickRight:
                        this.LeftStickPos.X = isDown ? 1 : 0;
                        break;

                    // right thumbstick
                    case SButton.RightThumbstickUp:
                        this.RightStickPos.Y = isDown ? 1 : 0;
                        break;
                    case SButton.RightThumbstickDown:
                        this.RightStickPos.Y = isDown ? 1 : 0;
                        break;
                    case SButton.RightThumbstickLeft:
                        this.RightStickPos.X = isDown ? 1 : 0;
                        break;
                    case SButton.RightThumbstickRight:
                        this.RightStickPos.X = isDown ? 1 : 0;
                        break;

                    // triggers
                    case SButton.LeftTrigger:
                        this.LeftTrigger = isDown ? 1 : 0;
                        break;
                    case SButton.RightTrigger:
                        this.RightTrigger = isDown ? 1 : 0;
                        break;

                    // buttons
                    default:
                        if (this.ButtonStates.ContainsKey(pair.Key))
                            this.ButtonStates[pair.Key] = isDown ? ButtonState.Pressed : ButtonState.Released;
                        else
                            changed = false;
                        break;
                }

                if (changed)
                    this.State = null;
            }

            return this;
        }

        /// <summary>Get the currently pressed buttons.</summary>
        public IEnumerable<SButton> GetPressedButtons()
        {
            if (!this.IsConnected)
                yield break;

            // buttons
            foreach (var pair in this.ButtonStates)
            {
                if (pair.Value == ButtonState.Pressed && pair.Key.TryGetController(out Buttons button))
                    yield return button.ToSButton();
            }

            // triggers
            if (this.LeftTrigger > 0.2f)
                yield return SButton.LeftTrigger;
            if (this.RightTrigger > 0.2f)
                yield return SButton.RightTrigger;

            // left thumbstick direction
            if (this.LeftStickPos.Y > GamePadStateBuilder.LeftThumbstickDeadZone)
                yield return SButton.LeftThumbstickUp;
            if (this.LeftStickPos.Y < -GamePadStateBuilder.LeftThumbstickDeadZone)
                yield return SButton.LeftThumbstickDown;
            if (this.LeftStickPos.X > GamePadStateBuilder.LeftThumbstickDeadZone)
                yield return SButton.LeftThumbstickRight;
            if (this.LeftStickPos.X < -GamePadStateBuilder.LeftThumbstickDeadZone)
                yield return SButton.LeftThumbstickLeft;

            // right thumbstick direction
            if (this.RightStickPos.Length() > GamePadStateBuilder.RightThumbstickDeadZone)
            {
                if (this.RightStickPos.Y > 0)
                    yield return SButton.RightThumbstickUp;
                if (this.RightStickPos.Y < 0)
                    yield return SButton.RightThumbstickDown;
                if (this.RightStickPos.X > 0)
                    yield return SButton.RightThumbstickRight;
                if (this.RightStickPos.X < 0)
                    yield return SButton.RightThumbstickLeft;
            }
        }

        /// <summary>Get the equivalent state.</summary>
        public GamePadState GetState()
        {
            this.State ??= new GamePadState(
                leftThumbStick: this.LeftStickPos,
                rightThumbStick: this.RightStickPos,
                leftTrigger: this.LeftTrigger,
                rightTrigger: this.RightTrigger,
                buttons: this.GetButtonBitmask() // MonoGame requires one bitmask here; don't specify multiple values
            );

            return this.State.Value;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get a bitmask representing the pressed buttons.</summary>
        private Buttons GetButtonBitmask()
        {
            Buttons flag = 0;
            foreach (var pair in this.ButtonStates)
            {
                if (pair.Value == ButtonState.Pressed && pair.Key.TryGetController(out Buttons button))
                    flag |= button;
            }

            return flag;
        }
    }
}
