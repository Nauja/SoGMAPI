using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SoG;

#pragma warning disable 809 // obsolete override of non-obsolete method (this is deliberate)
namespace SoGModdingAPI.Framework.Input
{
    public class InputState
    {
        public virtual void Update() { }
        public virtual GamePadState GetGamePadState()
        {
            return new GamePadState();
        }

        /// <summary>Get the keyboard state visible to the game.</summary>
        public virtual KeyboardState GetKeyboardState()
        {
            return new KeyboardState();
        }

        /// <summary>Get the keyboard state visible to the game.</summary>
        public virtual MouseState GetMouseState()
        {
            return new MouseState();
        }

        /// <summary>Manages the game's input state.</summary>
        public sealed class SInputState : InputState
        {
            /*********
            ** Accessors
            *********/
            /// <summary>The cursor position on the screen adjusted for the zoom level.</summary>
            private CursorPosition CursorPositionImpl;

            /// <summary>The player's last known tile position.</summary>
            private Vector2? LastPlayerTile;

            /// <summary>The buttons to press until the game next handles input.</summary>
            private readonly HashSet<SButton> CustomPressedKeys = new HashSet<SButton>();

            /// <summary>The buttons to consider released until the actual button is released.</summary>
            private readonly HashSet<SButton> CustomReleasedKeys = new HashSet<SButton>();

            /// <summary>Whether there are new overrides in <see cref="CustomPressedKeys"/> or <see cref="CustomReleasedKeys"/> that haven't been applied to the previous state.</summary>
            private bool HasNewOverrides;


            /*********
            ** Accessors
            *********/
            /// <summary>The controller state as of the last update, with overrides applied.</summary>
            public GamePadState ControllerState { get; private set; }

            /// <summary>The keyboard state as of the last update, with overrides applied.</summary>
            public KeyboardState KeyboardState { get; private set; }

            /// <summary>The mouse state as of the last update, with overrides applied.</summary>
            public MouseState MouseState { get; private set; }

            /// <summary>The buttons which were pressed, held, or released as of the last update.</summary>
            public IDictionary<SButton, SButtonState> ButtonStates { get; private set; } = new Dictionary<SButton, SButtonState>();

            /// <summary>The cursor position on the screen adjusted for the zoom level.</summary>
            public ICursorPosition CursorPosition => this.CursorPositionImpl;


            /*********
            ** Public methods
            *********/
            /// <summary>This method is called by the game, and does nothing since SMAPI will already have updated by that point.</summary>
            [Obsolete("This method should only be called by the game itself.")]
            public override void Update() { }


            /// <summary>Get the gamepad state visible to the game.</summary>
            public override GamePadState GetGamePadState()
            {
                return this.ControllerState;
            }

            /// <summary>Get the keyboard state visible to the game.</summary>
            public override KeyboardState GetKeyboardState()
            {
                return this.KeyboardState;
            }

            /// <summary>Get the keyboard state visible to the game.</summary>
            public override MouseState GetMouseState()
            {
                return this.MouseState;
            }

            /// <summary>Override the state for a button.</summary>
            /// <param name="button">The button to override.</param>
            /// <param name="setDown">Whether to mark it pressed; else mark it released.</param>
            public void OverrideButton(SButton button, bool setDown)
            {
                bool changed = setDown
                    ? this.CustomPressedKeys.Add(button) | this.CustomReleasedKeys.Remove(button)
                    : this.CustomPressedKeys.Remove(button) | this.CustomReleasedKeys.Add(button);

                if (changed)
                    this.HasNewOverrides = true;
            }

            /// <summary>Get whether a mod has indicated the key was already handled, so the game shouldn't handle it.</summary>
            /// <param name="button">The button to check.</param>
            public bool IsSuppressed(SButton button)
            {
                return this.CustomReleasedKeys.Contains(button);
            }

            /// <summary>Apply input overrides to the current state.</summary>
            public void ApplyOverrides()
            {
            }

            /// <summary>Get whether a given button was pressed or held.</summary>
            /// <param name="button">The button to check.</param>
            public bool IsDown(SButton button)
            {
                return false;
            }

            /// <summary>Get whether any of the given buttons were pressed or held.</summary>
            /// <param name="buttons">The buttons to check.</param>
            public bool IsAnyDown(LocalInputHelper.KeyOrMouse[] buttons)
            {
                return buttons.Any(button => this.IsDown(button.ToSButton()));
            }

            /// <summary>Get the state of a button.</summary>
            /// <param name="button">The button to check.</param>
            public SButtonState GetState(SButton button)
            {
                return new SButtonState();
            }
        }
    }
}
