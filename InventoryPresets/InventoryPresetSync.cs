namespace AmadarePlugin.InventoryPresets;

public class InventoryPresetSync : Photon.PunBehaviour
{
    public LoadoutRepository Repository { get; set; }
    public InventoryPresetManager Manager { get; set; }
    
    public void SyncLoadouts()
    {
        var view = this.gameObject.GetComponent<PhotonView>();
        if (view == null)
        {
            view = this.gameObject.AddComponent<PhotonView>();
            view.viewID = 642;
        }
        
        Plugin.Log.LogInfo("SyncLoadouts: calling RPC");
        view.RPC("SyncLoadoutsRPC", PhotonTargets.Others, Repository.Serialize());
    }

    [PunRPC]
    public void SyncLoadoutsRPC(string data)
    {
        Plugin.Log.LogInfo("SyncLoadouts: RPC called");
        Plugin.Log.LogInfo(data);
        this.Repository.Load(data);
    }
}