     ___           ___           ___           ___        ___     
    /  /\         /__/\         /  /\         /  /\      /  /\    
   /  /:/_       |  |::\       /  /::\       /  /::\    /  /:/    
  /  /:/ /\      |  |:|:\     /  /:/\:\     /  /:/\:\  /  /:/     
 /  /:/ /::\   __|__|:|\:\   /  /:/~/::\   /  /:/~/:/ /  /::\ ___ 
/__/:/ /:/\:\ /__/::::| \:\ /__/:/ /:/\:\ /__/:/ /:/ /__/:/\:\  /\
\  \:\/:/~/:/ \  \:\~~\__\/ \  \:\/:/__\/ \  \:\/:/  \__\/  \:\/:/
 \  \::/ /:/   \  \:\        \  \::/       \  \::/        \__\::/ 
  \__\/ /:/     \  \:\        \  \:\        \  \:\        /  /:/  
    /__/:/       \  \:\        \  \:\        \  \:\      /__/:/   
    \__\/         \__\/         \__\/         \__\/      \__\/    


SoGMAPI lets you run Stardew Valley with mods. Don't forget to download mods separately.


Automated install
--------------------------------
See https://stardewvalleywiki.com/Modding:Player_Guide for help installing SoGMAPI, adding mods, etc.


Manual install
--------------------------------
THIS IS NOT RECOMMENDED FOR MOST PLAYERS. See the instructions above instead.
If you really want to install SoGMAPI manually, here's how.

1. Unzip "internal/windows/install.dat" (on Windows) or "internal/unix/install.dat" (on Linux or
   macOS). You can change '.dat' to '.zip', it's just a normal zip file renamed to prevent
   confusion.

2. Copy the files from the folder you just unzipped into your game folder. The
   `SoGModdingAPI.exe` file should be right next to the game's executable.

3. Copy `Stardew Valley.deps.json` in the game folder, and rename the copy to
   `SoGModdingAPI.deps.json`.

4.
  - Windows only: if you use Steam, see the install guide above to enable achievements and
    overlay. Otherwise, just run SoGModdingAPI.exe in your game folder to play with mods.

  - Linux/macOS only: rename the "StardewValley" file (no extension) to "StardewValley-original", and
    "SoGModdingAPI" (no extension) to "StardewValley". Now just launch the game as usual to
    play with mods.

When installing on Linux or macOS:
- To configure the color scheme, edit the `sogmapi-internal/config.json` file and see instructions
  there for the 'ColorScheme' setting.
