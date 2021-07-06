using System;
using System.Linq;

using UnityEngine;
using UnityEngine.Networking;

namespace BalsaPluginTest
{

	public class AddonPartModule : Modules.PartModule
	{
		/* Plugins can contain new Part Modules. These don't require any entry points, as they get attached to parts that 
		 * use this module (defined in their .cfg files)
		 */

		/* PartModules in Balsa inherit from NetworkedBehaviour, so SyncVar fields here will work.
		 */
		[SyncVar]
		public int someValue;

		/* Part Modules have their values defined in .cfg and/or in .craft files
		 * ou can use the CfgField attributes to automate saving and loading these values so they save and load automatically.
		 * 
		 * You can also control in which context values are saved and loaded using the CfgContext paramater of the attribute:
		 * 
		 * CfgContext.Config will only save/load the value from the .cfg file definition. Use these for tuning parameters and things that don't change based on vehicle design.
		 * CfgContext.Persistent will save/load to/from .craft files. Use these to store data that does change during vehicle construction.
		 * CfgContext.All, as the name implies, does both.
		 * 
		 * (if omitted, the value defaults to Config)
		 */



		[CfgField(CfgFields.CfgContext.Config)]
		protected float aCfgDefinedValue;

		public override void OnModuleSpawn()
		{
			Debug.Log("[AddonTestPartModule]: w00t!");
		}


		// remember that PartModules are MonoBehaviours, so your filename here needs to match the class name.
	}
}
