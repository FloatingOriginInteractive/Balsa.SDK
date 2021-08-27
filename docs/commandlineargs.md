# Commandline Arguments
#### aka Launch Options on Steam
--------------

The game can be started with optional commandline arguments to control various functions. These are mostly useful for development, but could be helpful for modding or debugging.


Argument | Description | Parameters
---------|-------------|--------
-nointro | Skip splashscreen
-noMenuMusic | don't play any music in the main menu
-vrscale | vr supersampling factor. leaving at zero falls back to internal slider on VRSetup component | multiplier value 
-emptyWorldInMenu | skip loading a map in the main menu (load a simple fallback scene instead)
-forceLoadMap | force the game to load a specific map at startup | -1: random, 0: emptyWorld, 1:Wirraway Bay, 2: Rovin Harbor, etc
-SkipMenu | bypass main menu and go straight into flight (with default GameLogic settings)
-devtools | show developer tools
-noloadmask | Suppress loading mask UI
-useVRui | Force using the VR version of UI panels (old and likely won't work very well)
-locale | Use the given language in localized content. | language codes eg: `en`, `pt`, `es`, etc

