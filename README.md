# Sea-Power-Crunchatizer
A simple BepInEx cheat mod. Plug-and-play, config explains everything.

# Installation
1) Download BepInEx 5.x (the Mono build matching your OS, e.g. x64) from: https://github.com/BepInEx/BepInEx/releases
2) Extract it into your Sea Power game folder, then launch the game once so BepInEx generates its folders.
3) Place `Sea_Power_Crunchatizer.dll` in the `BepInEx/plugins` folder in your game's root.

# Configuration
Launch the game once with the mod installed so it writes its config, then edit
`BepInEx/config/net.particle.sea_power_crunchatizer.cfg`. Every setting is grouped
into sections (General, Weapons, Aircraft, Sensors, Miscellaneous) and documented inline.

# Known Issues
- The shared launch delay for ships can be altered, but engagement timing seems to still respect it, at least if the delay is one second. I'm not sure how to resolve this; if you have ideas, make an issue or a PR.
- Container auto-refresh refreshes INDIVIDUAL containers instead of all the ship's weapon systems containing ammunition of the desired type. This absolutely eludes me.
- No known way to speed up animations programmatically, so ships get capped by the speed at which their anims take place for things that use anims

If you want any new features, ask me, or better yet, make a PR. If I don't like the idea I won't implement it myself, but if it's in a PR I'll almost certainly do it.
