#!/usr/bin/env bash
# MonoKickstart Shell Script
# Written by Ethan "flibitijibibo" Lee
# Modified for SoGMAPI by various contributors

# Move to script's directory
cd "$(dirname "$0")" || exit $?

# Get the system architecture
UNAME=$(uname)
ARCH=$(uname -m)

# MonoKickstart picks the right libfolder, so just execute the right binary.
if [ "$UNAME" == "Darwin" ]; then
    # ... Except on OSX.
    export DYLD_LIBRARY_PATH=$DYLD_LIBRARY_PATH:./osx/

    # El Capitan is a total idiot and wipes this variable out, making the
    # Steam overlay disappear. This sidesteps "System Integrity Protection"
    # and resets the variable with Valve's own variable (they provided this
    # fix by the way, thanks Valve!). Note that you will need to update your
    # launch configuration to the script location, NOT just the app location
    # (i.e. Kick.app/Contents/MacOS/Kick, not just Kick.app).
    # -flibit
    if [ "$STEAM_DYLD_INSERT_LIBRARIES" != "" ] && [ "$DYLD_INSERT_LIBRARIES" == "" ]; then
        export DYLD_INSERT_LIBRARIES="$STEAM_DYLD_INSERT_LIBRARIES"
    fi

    # this was here before
    ln -sf mcs.bin.osx mcs

    # fix "DllNotFoundException: libgdiplus.dylib" errors when loading images in SoGMAPI
    if [ -f libgdiplus.dylib ]; then
        rm libgdiplus.dylib
    fi
    if [ -f /Library/Frameworks/Mono.framework/Versions/Current/lib/libgdiplus.dylib ]; then
        ln -s /Library/Frameworks/Mono.framework/Versions/Current/lib/libgdiplus.dylib libgdiplus.dylib
    fi

    # create bin file
    # Note: don't overwrite if it's identical, to avoid resetting permission flags
    if [ ! -x SoGModdingAPI.bin.osx ] || ! cmp SecretsOfGrindea.bin.osx SoGModdingAPI.bin.osx >/dev/null 2>&1; then
        cp -p SecretsOfGrindea.bin.osx SoGModdingAPI.bin.osx
    fi

    # launch SoGMAPI
    open -a Terminal ./SoGModdingAPI.bin.osx "$@"
else
    # choose binary file to launch
    LAUNCH_FILE=""
    if [ "$ARCH" == "x86_64" ]; then
        ln -sf mcs.bin.x86_64 mcs
        cp SecretsOfGrindea.bin.x86_64 SoGModdingAPI.bin.x86_64
        LAUNCH_FILE="./SoGModdingAPI.bin.x86_64"
    else
        ln -sf mcs.bin.x86 mcs
        cp SecretsOfGrindea.bin.x86 SoGModdingAPI.bin.x86
        LAUNCH_FILE="./SoGModdingAPI.bin.x86"
    fi
    export LAUNCH_FILE

    # select terminal (prefer xterm for best compatibility, then known supported terminals)
    for terminal in xterm gnome-terminal kitty terminator xfce4-terminal konsole terminal termite alacritty mate-terminal x-terminal-emulator; do
        if command -v "$terminal" 2>/dev/null; then
            export TERMINAL_NAME=$terminal
            break;
        fi
    done

    # find the true shell behind x-terminal-emulator
    if [ "$TERMINAL_NAME" = "x-terminal-emulator" ]; then
        export TERMINAL_NAME="$(basename "$(readlink -f $(command -v x-terminal-emulator))")"
    fi

    # run in selected terminal and account for quirks
    export TERMINAL_PATH="$(command -v $TERMINAL_NAME)"
    if [ -x $TERMINAL_PATH ]; then
        case $TERMINAL_NAME in
            terminal|termite)
                # consumes only one argument after -e
                # options containing space characters are unsupported
                exec $TERMINAL_NAME -e "env TERM=xterm $LAUNCH_FILE $@"
                ;;

            xterm|konsole|alacritty)
                # consumes all arguments after -e
                exec $TERMINAL_NAME -e env TERM=xterm $LAUNCH_FILE "$@"
                ;;

            terminator|xfce4-terminal|mate-terminal)
                # consumes all arguments after -x
                exec $TERMINAL_NAME -x env TERM=xterm $LAUNCH_FILE "$@"
                ;;

            gnome-terminal)
                # consumes all arguments after --
                exec $TERMINAL_NAME -- env TERM=xterm $LAUNCH_FILE "$@"
                ;;

            kitty)
                # consumes all trailing arguments
                exec $TERMINAL_NAME env TERM=xterm $LAUNCH_FILE "$@"
                ;;

            *)
                # If we don't know the terminal, just try to run it in the current shell.
                # If THAT fails, launch with no output.
                env TERM=xterm $LAUNCH_FILE "$@"
                if [ $? -eq 127 ]; then
                    exec $LAUNCH_FILE --no-terminal "$@"
                fi
        esac

    ## terminal isn't executable; fallback to current shell or no terminal
    else
        echo "The '$TERMINAL_NAME' terminal isn't executable. SoGMAPI might be running in a sandbox or the system might be misconfigured? Falling back to current shell."
        env TERM=xterm $LAUNCH_FILE "$@"
        if [ $? -eq 127 ]; then
            exec $LAUNCH_FILE --no-terminal "$@"
        fi
    fi
fi
