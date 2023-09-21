using Microsoft.MixedReality.Toolkit.Input;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace HoloAuopsy
{
    [RequireComponent(typeof(PhotonView), typeof(PhotonTransformView))]
    public class OwnershipTransferHandler : MonoBehaviourPun, IPunOwnershipCallbacks, IMixedRealityInputHandler
    {
        public void OnInputDown(InputEventData eventData)
        {
            photonView.RequestOwnership();
        }

        public void OnInputUp(InputEventData eventData)
        {
        }

        public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
        {
            targetView.TransferOwnership(requestingPlayer);
        }

        public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
        {
        }

        public void OnOwnershipTransferFailed(PhotonView targetView, Player previousOwner)
        {
        }

        private void TransferControl(Player idPlayer)
        {
            if (photonView.IsMine) photonView.TransferOwnership(idPlayer);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (photonView != null) photonView.RequestOwnership();
        }

        private void OnTriggerExit(Collider other)
        {
        }

        public void RequestOwnership()
        {
            photonView.RequestOwnership();
        }
    }
}
