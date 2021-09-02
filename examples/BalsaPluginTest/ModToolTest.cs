namespace BalsaPlugin
{
	[UI.PlayerToolkit.PlayerTool("Mod Tool Test", icon = IconSprite.Browser, tooltip = "Testing mod-defined tools here")]
	public class ModToolTest : UI.PlayerToolkit.PlayerTool
	{
		/* You can create new Player Toolkit items like in this example.
		 * Player Toolkit items are buttons that, when clicked, invoke the OnToolSelect method.
		 * 		 
		 */

		public override bool GetToolAvailable()
		{
			return true;
		}

		public override void OnToolSelect()
		{
			Chat.GameChat.PostMessage("Mods can also use the chat output!");
			Chat.GameChat.PostServerMessage("Including sending messages as the server.");
		}
	}
}
