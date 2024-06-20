using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if LUDARE_USE_STEAM_LOGIN
using Steamworks;
#endif

#if LUDARE_USE_EOS_LOGIN
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.Platform;
#endif

public class GameManager : MonoBehaviour
{

#if LUDARE_USE_EOS_LOGIN
    public PlatformInterface PlatformInterface;
#endif

    // Start is called before the first frame update
    void Start()
    {
#if LUDARE_USE_STEAM_LOGIN
        SteamAPI.RestartAppIfNecessary((AppId_t)480);
        SteamAPI.Init();
#endif
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
