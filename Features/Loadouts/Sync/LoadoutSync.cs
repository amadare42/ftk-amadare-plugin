using System.Collections.Generic;
using AmadarePlugin.Common;
using AmadarePlugin.Options;
using GridEditor;
using Newtonsoft.Json;
using Photon;
using UnityEngine;

namespace AmadarePlugin.Features.Loadouts.Sync;

public class LoadoutSync : PunBehaviour
{
    public static LoadoutSync Current;

    private LoadoutRepository loadoutRepository;
    private CharacterShareTracker shareTracker;
    private LoadoutManager loadoutManager;

    public static void Init()
    {
        On.uiStartGame.StartGame += OnGameStart;
    }

    private static void OnGameStart(On.uiStartGame.orig_StartGame orig, uiStartGame self)
    {
        Utils.SafeInvoke(() => orig(self), () => {
            if (self.IsMasterClient)
            {
                Plugin.Log.LogInfo("OnGameStart: Pushing state");
                Current.Push();
            }
        });
    }

    public static GameObject Instantiate(GameObject gameObject, 
        LoadoutRepository loadoutRepository, 
        CharacterShareTracker shareTracker,
        LoadoutManager loadoutManager)
    {
        Current = gameObject.AddComponent<LoadoutSync>();
        Current.loadoutRepository = loadoutRepository;
        Current.shareTracker = shareTracker;
        Current.loadoutManager = loadoutManager;
        Plugin.Log.LogInfo("LoadoutSync crated!");
        return gameObject;
    }

    public void Push(bool buffered = false)
    {
        this.shareTracker.SetAlwaysShare(PhotonNetwork.player.ID, OptionsManager.AlwaysShare);
        this.photonView.RPC(nameof(ReceiveFullState), 
            buffered ? PhotonTargets.OthersBuffered : PhotonTargets.Others,
            this.loadoutRepository.Serialize(),
            this.shareTracker.Serialize(),
            OptionsManager.AlwaysShare
        );
    }

    [PunRPC]
    public void ReceiveFullState(string loadouts, string shares, bool alwaysShare, PhotonMessageInfo info)
    {
        Plugin.Log.LogDebug($"{nameof(ReceiveCharacterLoadouts)}: {loadouts}|\n {shares}|\n {alwaysShare}");
        this.loadoutRepository.Load(loadouts);
        this.shareTracker.SetAlwaysShare(info.sender.ID, alwaysShare);
        this.shareTracker.Load(shares);
        this.loadoutManager.OnLoadoutReceived();
    }

    public void PushShareChange(string name, bool value)
    {
        this.photonView.RPC(nameof(ReceiveShareChange), PhotonTargets.Others,
            name, value
        );
    }

    [PunRPC]
    public void ReceiveShareChange(string name, bool value)
    {
        Plugin.Log.LogDebug($"{nameof(ReceiveShareChange)}: {name} {value}");
        this.shareTracker.Set(name, value);
        this.loadoutManager.OnShareChanged();
    }

    public void PushCharacterLoadouts(string name)
    {
        // for some reason, Photon is failing to serialize Dictionary, so sending already serialized state
        var serializedLoadouts = JsonConvert.SerializeObject(this.loadoutRepository.GetAllSlots(name));
        this.photonView.RPC(nameof(ReceiveCharacterLoadouts), PhotonTargets.Others,
            name, serializedLoadouts
        );
    }

    [PunRPC]
    public void ReceiveCharacterLoadouts(string name, string serializedLoadouts)
    {
        Plugin.Log.LogDebug($"{nameof(ReceiveCharacterLoadouts)}: {name} {serializedLoadouts}");
        var loadouts =
            JsonConvert.DeserializeObject<Dictionary<PlayerInventory.ContainerID, FTK_itembase.ID>[]>(
                serializedLoadouts);
        this.loadoutRepository.SetAllSlots(name, loadouts);
        this.loadoutManager.OnLoadoutReceived();
    }
}