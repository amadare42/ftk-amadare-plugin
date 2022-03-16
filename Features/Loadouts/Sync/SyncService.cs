using UnityEngine;

namespace AmadarePlugin.Features.Loadouts.Sync;

public class SyncService
{
    private readonly LoadoutRepository repository;
    private readonly LoadoutManager manager;
    private readonly CharacterShareTracker shareTracker;

    private GameObject syncObject;
    private PhotonView view;
    private LoadoutSync sync;

    public SyncService(LoadoutRepository repository, LoadoutManager manager, CharacterShareTracker shareTracker)
    {
        this.repository = repository;
        this.manager = manager;
        this.shareTracker = shareTracker;
    }

    public bool SyncRequired => (this.syncObject == null || !this.syncObject) && !PhotonNetwork.player.IsMasterClient;

    public void SyncLoadouts()
    {
        AssertObjectCreated();
        this.sync.PushLoadout();
    }

    public void RequestSync()
    {
        AssertObjectCreated();
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

            this.sync = this.syncObject.AddComponent<LoadoutSync>();
            this.sync.LoadoutRepository = this.repository;
            this.sync.Manager = this.manager;
            this.sync.ShareTracker = this.shareTracker;
        }
    }
}