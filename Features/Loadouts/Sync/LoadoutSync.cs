
namespace AmadarePlugin.Features.Loadouts.Sync;

public class LoadoutSync : Photon.PunBehaviour
{
    public LoadoutRepository LoadoutRepository { get; set; }
    public LoadoutManager Manager { get; set; }
    public CharacterShareTracker ShareTracker { get; set; }

    public void PushLoadout()
    {
        Plugin.Log.LogInfo("PushLoadout: calling RPC");
        this.photonView.RPC(nameof(ReceiveLoadoutsRPC), PhotonTargets.Others,
            this.LoadoutRepository.Serialize(),
            this.ShareTracker.Serialize()
        );
    }

    [PunRPC]
    public void ReceiveLoadoutsRPC(string data, string shareData)
    {
        Plugin.Log.LogInfo("ReceiveLoadoutsRPC: RPC called");
        Plugin.Log.LogInfo(data);
        Plugin.Log.LogInfo(shareData);
        this.LoadoutRepository.Load(data);
        this.ShareTracker.Load(shareData);
        this.Manager.OnLoadoutReceived();
    }

    public void RequestLoadout()
    {
        Plugin.Log.LogInfo("RequestLoadoutRPC: requesting loadout data...");
        this.photonView.RPC(nameof(RequestLoadoutRPC), PhotonTargets.MasterClient);
    }

    [PunRPC]
    public void RequestLoadoutRPC()
    {
        Plugin.Log.LogInfo("RequestLoadoutRPC: loadout data requested");
        PushLoadout();
    }

    public void OnStart()
    {
        Plugin.Log.LogInfo($"{nameof(LoadoutSync)} created!");
    }

    public void OnDestroy()
    {
        Plugin.Log.LogInfo($"{nameof(LoadoutSync)} destroyed!");
    }
}