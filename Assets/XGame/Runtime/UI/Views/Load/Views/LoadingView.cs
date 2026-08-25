using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;

namespace QFramework.Example
{
	public class LoadingViewData : UIPanelData
	{
	}
	public partial class LoadingView : UIPanel
	{
		private TMP_Text loadingText;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as LoadingViewData ?? new LoadingViewData();
			SliderBar_Loading.minValue = 0f;
			SliderBar_Loading.maxValue = 1f;
			SliderBar_Loading.wholeNumbers = false;
			loadingText = SliderBar_Loading.GetComponentInChildren<TMP_Text>(true);
			UpdateProgress(0f);
		}

		public void SetProgress(float progress)
		{
			UpdateProgress(progress);
		}

		public void UpdateProgress(float progress)
		{
			float normalizedProgress = Mathf.Clamp01(progress);
			SliderBar_Loading.value = normalizedProgress;

			if (loadingText != null)
			{
				int percent = Mathf.FloorToInt(normalizedProgress * 100f);
				loadingText.SetText("{0}%", percent);
			}
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
		}
	}
}
