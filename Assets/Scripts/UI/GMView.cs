using TMPro;
using TPS.Inventory.Presentation;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.Example
{
	public class GMViewData : UIPanelData
	{
	}
	public partial class GMView : UIPanel
	{
		private GMViewController controller;
		private TMP_InputField inputID;
		private TMP_InputField inputNum;
		private Button addButton;
		private bool eventsBound;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GMViewData ?? new GMViewData();
			ResolveReferences();
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
			UnbindEvents();

			GMViewController owner = controller;
			controller = null;
			owner?.NotifyViewClosed(this);
		}

		public void Setup(GMViewController owner)
		{
			controller = owner;
			ResolveReferences();
			BindEvents();
		}

		private void ResolveReferences()
		{
			inputID = inputID != null
				? inputID
				: transform.Find("Bg/AddCell/InputID")?.GetComponent<TMP_InputField>();
			inputNum = inputNum != null
				? inputNum
				: transform.Find("Bg/AddCell/InputNum")?.GetComponent<TMP_InputField>();
			addButton = addButton != null
				? addButton
				: transform.Find("Bg/AddCell/Btn")?.GetComponent<Button>();

			if (inputID == null || inputNum == null || addButton == null)
			{
				Debug.LogError(
					"[GMView] Prefab 结构不完整，需要 Bg/AddCell/InputID、InputNum 和 Btn。",
					this);
				return;
			}

			inputID.contentType = TMP_InputField.ContentType.IntegerNumber;
			inputNum.contentType = TMP_InputField.ContentType.IntegerNumber;
		}

		private void BindEvents()
		{
			if (eventsBound || addButton == null)
			{
				return;
			}

			addButton.onClick.AddListener(OnAddClicked);
			eventsBound = true;
		}

		private void UnbindEvents()
		{
			if (!eventsBound)
			{
				return;
			}

			addButton?.onClick.RemoveListener(OnAddClicked);
			eventsBound = false;
		}

		private void OnAddClicked()
		{
			if (!int.TryParse(inputID?.text, out int itemID) || itemID <= 0
				|| !int.TryParse(inputNum?.text, out int count) || count <= 0)
			{
				Debug.LogWarning("[GMView] 物品 ID 和数量必须是大于 0 的整数。", this);
				return;
			}

			controller?.AddItem(itemID, count);
		}
	}
}
