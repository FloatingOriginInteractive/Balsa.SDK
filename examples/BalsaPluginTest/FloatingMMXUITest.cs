
using UI;
using UI.MMX.Data;

using UnityEngine;

namespace BalsaPluginTest
{
	public class FloatingMMXUITest : MonoBehaviour
	{
		public void Start()
		{
			StartCoroutine(CallbackUtil.WaitUntil(() => View.Instance != null, 1f, delegate ()
			{
				BuildMMXUI();
				floatingMMXPanel = FloatingMMXPanel.Create().SetContents("Addon Test UI", content);
			}));
		}

		protected FloatingMMXPanel floatingMMXPanel;
		protected ContentData content = new ContentData();
		bool led = false;
		private void BuildMMXUI()
		{
			content.Clear();

			content.AddMany(
				new Label("This is MMX UI.")
				, new Label("You can drag this window around, btw".RTColor(BalsaColors.OutOfTheWayGray).RTSizeFx(0.6f))
				, new Spacer()
				, new SRText("It is a code-driven modular UI system, allowing you to create UI objects entirely by code")
				{
					animateRevealSpeed = 6f
					, animateRevealTint = XKCDColors.BrightAqua
					, animateRevealSpread = 10
				}
				, new TableLayout(0.3f, 0.6f)

					, new Button()
					{
						caption = "Click me!"
						, OnClicked = (ctrlIndex) =>
						{
							led = !led;
							content.NeedRebuild();
						}
					}
					, new LED("Light me!"
								, () => led ? XKCDColors.ElectricLime : (Color?)null
								, UpdateMoment.Update
								, "MMX LEDs don't need current limiting resistors!"
							)

				, new EndLayout()
				, new Label(() => "Current Game Time: {0}"
												.NotYetLocalized(null, StringUtil.PrintTimespan((float)ClientLogic.UT, @"h\h\,\ mm\m\,\ \ss\s"))
								, 0.1f
								, UpdateMoment.LateUpdate
						)
			);
			content.OnNeedRebuild = RebuildUI;
		}

		private void RebuildUI()
		{
			floatingMMXPanel.ClearContents();
			BuildMMXUI();
			floatingMMXPanel.SetContents("Addon Test UI", content);
		}

		public void Terminate()
		{
			if (floatingMMXPanel != null)
			{
				floatingMMXPanel.Terminate();
			}
		}

	}
}
