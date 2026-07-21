using GoogleMobileAds.Api;
using UnityEngine;

public class AdsManager : MonoBehaviour
{   
    bool rewardGiven;
    BannerView bannerView;
    RewardedAd rewardedAd;

    string bannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
    string rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";

    EventBinding<ShowBannerEvent> showBannerBinding;
    EventBinding<HideBannerEvent> hideBannerBinding;
    EventBinding<ShowRewardedAdEvent> showRewardedAdBinding;

    private void OnEnable()
    {
        showBannerBinding = new EventBinding<ShowBannerEvent>(ShowBanner);
        hideBannerBinding = new EventBinding<HideBannerEvent>(HideBanner);
        showRewardedAdBinding = new EventBinding<ShowRewardedAdEvent>(ShowRewardedAd);

        EventBus<ShowBannerEvent>.Subscribe(showBannerBinding);
        EventBus<HideBannerEvent>.Subscribe(hideBannerBinding);
        EventBus<ShowRewardedAdEvent>.Subscribe(showRewardedAdBinding);
    }

    private void OnDisable()
    {
        EventBus<ShowBannerEvent>.Unsubscribe(showBannerBinding);
        EventBus<HideBannerEvent>.Unsubscribe(hideBannerBinding);
        EventBus<ShowRewardedAdEvent>.Unsubscribe(showRewardedAdBinding);
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

        LoadBannerAd();
        LoadRewardedAd();

    }

    #region Banner Ad

    void CreateBannerView()
    {
        Debug.Log("Creating banner view");

        if(bannerView != null)
        {
            DestroyBannerAd();
        }

        bannerView = new BannerView(bannerAdUnitId, AdSize.LargeBanner, AdPosition.Bottom);
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

    void DestroyBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }
    }

    void LoadBannerAd()
    {
        if(bannerView == null)
        {
             CreateBannerView();
        }

        var adRequest = new AdRequest();

        bannerView.LoadAd(adRequest);
    }

    #endregion

    #region Rewarded Ad

    void LoadRewardedAd()
    {
        if(rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        // Create our request used to load the ad.
        var adRequest = new AdRequest();

        // Send the request to load the ad.
        RewardedAd.Load(rewardedAdUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                // The ad failed to load.
                return;
            }

            rewardedAd = ad;
            RegisterReloadHandler(rewardedAd);
        });
    }



    void ShowRewardedAd()
    {
        rewardGiven = false;

        rewardedAd.Show(reward =>
        {
            rewardGiven = true;
        });
    }

    void RegisterReloadHandler(RewardedAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            Time.timeScale = 0;
            AudioListener.pause = true;
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            Time.timeScale = 1f;

            EventBus<RewardEarnedEvent>.Publish(new RewardEarnedEvent
            {
                isRewardGiven = rewardGiven
            });

            AudioListener.pause = false;

            rewardGiven = false;

            LoadRewardedAd();
        };
    }

    #endregion

}
