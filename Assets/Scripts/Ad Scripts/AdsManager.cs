using GoogleMobileAds.Api;
using UnityEngine;

public class AdsManager : MonoBehaviour
{

    BannerView bannerView;
    string bannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";

    EventBinding<ShowBannerEvent> showBannerBinding;
    EventBinding<HideBannerEvent> hideBannerBinding;

    private void OnEnable()
    {
        showBannerBinding = new EventBinding<ShowBannerEvent>(ShowBanner);
        hideBannerBinding = new EventBinding<HideBannerEvent>(HideBanner);

        EventBus<ShowBannerEvent>.Subscribe(showBannerBinding);
        EventBus<HideBannerEvent>.Subscribe(hideBannerBinding);
    }

    private void OnDisable()
    {
        EventBus<ShowBannerEvent>.Unsubscribe(showBannerBinding);
        EventBus<HideBannerEvent>.Unsubscribe(hideBannerBinding);
    }

    void Start()
    {
        MobileAds.Initialize((InitializationStatus initstatus) =>
        {
            if (initstatus == null)
            {
                Debug.LogError("Google Mobile Ads initialization failed.");
                return;
            }

            Debug.Log("Google Mobile Ads initialization complete.");
        });

        LoadAd();
    }

    void CreateBannerView()
    {
        Debug.Log("Creating banner view");

        if(bannerView != null)
        {
            DestroyAd();
        }

        bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Top);
    }

    void ShowBanner()
    {
        if(bannerView == null)
        {
            CreateBannerView();
        }

        Debug.Log("Showing Banner");
        bannerView.Show();
    }

    void HideBanner()
    {
        if (bannerView == null)
            return;

        bannerView.Hide();
    }

    void DestroyAd()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }
    }

    void LoadAd()
    {
        if(bannerView == null)
        {
             CreateBannerView();
        }

        var adRequest = new AdRequest();

        bannerView.LoadAd(adRequest);
    }

}
