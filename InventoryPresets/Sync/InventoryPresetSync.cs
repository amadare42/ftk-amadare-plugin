
namespace AmadarePlugin.InventoryPresets.Sync;

public class InventoryPresetSync : Photon.PunBehaviour
{
    public LoadoutRepository Repository { get; set; }
    public LoadoutManager Manager { get; set; }
    
    public void PushLoadout()
    {
        Plugin.Log.LogInfo("PushLoadout: calling RPC");
        this.photonView.RPC(nameof(ReceiveLoadoutsRPC), PhotonTargets.Others, this.Repository.Serialize());
    }

    [PunRPC]
    public void ReceiveLoadoutsRPC(string data)
    {
        Plugin.Log.LogInfo("ReceiveLoadoutsRPC: RPC called");
        Plugin.Log.LogInfo(data);
        this.Repository.Load(data);
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
        Plugin.Log.LogInfo($"{nameof(InventoryPresetSync)} created!");
    }

    public void OnDestroy()
    {
        Plugin.Log.LogInfo($"{nameof(InventoryPresetSync)} destroyed!");
    }
}