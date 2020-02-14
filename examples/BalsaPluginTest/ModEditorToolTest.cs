using UnityEngine;

namespace BalsaPluginTest
{
	[Construction.EditorTool(name = "Mod Editor Tool", order = 99)]
	public class ModEditorToolTest : Construction.Tool
	{
		/* This example shows how new vehicle editor tools can be created.
		 * Editor tools are automatically given a button on the editor UI (unless the EditorTool attribute specifies they should be hidden).
		 */


		public override void OnEditorHide()
		{

		}

		public override void OnEditorUnhide()
		{

		}

		protected override void OnActivated()
		{
			Debug.Log("[ModEditorToolTest]: Well this is the active tool now!");
		}

		protected override void OnDeactivated()
		{
			Debug.Log("[ModEditorToolTest]: Cheers!");
		}
	}
}
