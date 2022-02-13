using UnityEngine;

namespace AmadarePlugin.InventoryPresets.Sync;

public class SyncService
{
    private readonly LoadoutRepository repository;
    private readonly LoadoutManager manager;
    
    private GameObject syncObject;
    private PhotonView view;
    private InventoryPresetSync sync;

    public SyncService(LoadoutRepository repository, LoadoutManager manager)
    {
        this.repository = repository;
        this.manager = manager;
    }

    public bool SyncRequired => (this.syncObject == null || !this.syncObject) && !PhotonNetwork.player.IsMasterClient;

    public void SyncLoadouts()
    {
        AssertObjectCreated();
        this.sync.PushLoadout();
    }

    public void RequestSync()
    {
        this.sync.RequestLoadout();
    }

    public void AssertObjectCreated()
    {
        if (this.syncObject == null || !this.syncObject)
        {
            Plugin.Log.LogInfo("Creating SyncService");
            this.syncObject = new GameObject();
            
            this.view = this.syncObject.AddComponent<PhotonView>();
            this.view.viewID = 642;

            this.sync = this.syncObject.AddComponent<InventoryPresetSync>();
            this.sync.Repository = this.repository;
            this.sync.Manager = this.manager;
        }
    }
}