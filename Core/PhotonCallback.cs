using AstolfoGorillaTagMenu.Core;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class AstolfoPhotonCallbacks : MonoBehaviourPunCallbacks
{
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        var netPlayer = ConvertToNetPlayer(newMasterClient);
        AstolfoCore.RaiseMasterClientSwitched(netPlayer);
    }

    private NetPlayer? ConvertToNetPlayer(Player player)
    {
        if (player == null)
            return null;

        var ns = NetworkSystem.Instance;
        if (ns == null)
            return null;

        foreach (var p in ns.AllNetPlayers)
        {
            if (p.ActorNumber == player.ActorNumber)
                return p;
        }

        return null;
    }
}