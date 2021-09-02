using System;
using System.Collections;
using System.Linq;

using UnityEngine;

namespace BalsaPluginTest
{

	[BalsaCore.BalsaAddon]
	public class AddonTest
	{


		/* Classes with the BalsaAddon attribute are the most basic type of plugin you can make, and therefore also the most flexible. 
* 
* They receive calls to initialize and finalize on static methods marked with the BalsaAddonInit and BalsaAddonFinalize attributes, 
* giving your plugin an entry point to do anything it needs to do, without any additional management from the game's side.
* 
* There are a few different options for when to receive initialize/finalize calls. See AddonInvokeTime to learn more.
* You can use any combination of init and finalize methods, as needed.
* 
* Use finalize methods if you need to clean up after your mod. However, keep in mind that AddonInvokeTime.OnLoaded finalize is invoked from OnApplicationQuit,
* So chances are any instances you spawned are probably getting destroyed already, so expect lots of nulls.
*/



		[BalsaCore.BalsaAddonInit(invokeTime = AddonInvokeTime.OnLoaded)]
		public static void InitOnLoad()
		{
			Debug.Log("[AddonTest]: Hello!");
		}



		[BalsaCore.BalsaAddonInit(invokeTime = AddonInvokeTime.MainMenu)]
		public static void OnMenuSceneStart()
		{
			Debug.Log("[AddonTest]: Indeed, Hello!");
		}
		[BalsaCore.BalsaAddonFinalize(invokeTime = AddonInvokeTime.MainMenu)]
		public static void OnMenuSceneEnd()
		{
			Debug.Log("[AddonTest]: We are now leaving the menu. So long!");
		}


		private static FloatingMMXUITest mmxUITest;

		[BalsaCore.BalsaAddonInit(invokeTime = AddonInvokeTime.Flight)]
		public static void OnFlightStart()
		{
			Debug.Log("[AddonTest]: Good day to you, sir!");

			// uncomment the line below to spawn a UI test at flight start
			//mmxUITest = (new GameObject("mmxUItest")).AddComponent<FloatingMMXUITest>();
		}


		[BalsaCore.BalsaAddonFinalize(invokeTime = AddonInvokeTime.Flight)]
		public static void OnFlightEnd()
		{
			Debug.Log("[AddonTest]: Now leaving flight.");

			if (mmxUITest != null) mmxUITest.Terminate();
		}
	}
}
