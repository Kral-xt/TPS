using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace QFramework.Example
{
	public class StartGameViewData : UIPanelData
	{
	}
	public partial class StartGameView : UIPanel
	{
		public event Action StartRequested;
		public event Action SettingRequested;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as StartGameViewData ?? new StartGameViewData();
			StartBtn.onClick.AddListener(OnStartButtonClicked);
			SettingBtn.onClick.AddListener(OnSettingButtonClicked);
		}

		public void SetStartInteractable(bool interactable)
		{
			StartBtn.interactable = interactable;
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
			StartBtn.onClick.RemoveListener(OnStartButtonClicked);
			SettingBtn.onClick.RemoveListener(OnSettingButtonClicked);
			StartRequested = null;
			SettingRequested = null;
		}

		private void OnStartButtonClicked()
		{
			StartRequested?.Invoke();
		}

		private void OnSettingButtonClicked()
		{
			SettingRequested?.Invoke();
		}
	}
}
