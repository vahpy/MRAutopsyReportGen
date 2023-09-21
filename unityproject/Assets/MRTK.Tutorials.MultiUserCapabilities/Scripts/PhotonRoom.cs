using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace MRTK.Tutorials.MultiUserCapabilities
{
    public class PhotonRoom : MonoBehaviourPunCallbacks, IInRoomCallbacks
    {
        public static PhotonRoom Room;

        [SerializeField] private GameObject userPrefab = default;
        [SerializeField] private GameObject userEyeGazeCursorPrefab = default;
        [SerializeField,Range(0,1)] private float scale = 1;


        // private PhotonView pv;
        private Player[] photonPlayers;
        private int playersInRoom;
        private int myNumberInRoom;
        private GameObject myPlayer;

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);
            photonPlayers = PhotonNetwork.PlayerList;
            playersInRoom++;
            
        }

        private void Awake()
        {
            if (Room == null)
            {
                Room = this;
            }
            else
            {
                if (Room != this)
                {
                    Destroy(Room.gameObject);
                    Room = this;
                }
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            PhotonNetwork.AddCallbackTarget(this);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        private void Start()
        {
            // pv = GetComponent<PhotonView>();
            if (!this.photonView.IsOwnerActive) return;

            // Allow prefabs not in a Resources folder
            if (PhotonNetwork.PrefabPool is DefaultPool pool)
            {
                if (userPrefab != null) pool.ResourceCache.Add(userPrefab.name, userPrefab);

                if (userEyeGazeCursorPrefab != null) pool.ResourceCache.Add(userEyeGazeCursorPrefab.name, userEyeGazeCursorPrefab);
            }
        }

        private void Update()
        {
            if (myPlayer != null)
            {
                if (scale != myPlayer.transform.localScale.x)
                {
                    myPlayer.transform.localScale = new Vector3(scale,scale,scale);
                }
            }
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();

            photonPlayers = PhotonNetwork.PlayerList;
            playersInRoom = photonPlayers.Length;
            myNumberInRoom = playersInRoom;
            PhotonNetwork.NickName = myNumberInRoom.ToString();

            StartGame();
        }

        private void StartGame()
        {
            CreatPlayer();

            if (!PhotonNetwork.IsMasterClient) return;

            //if (TableAnchor.Instance != null) CreateInteractableObjects();
        }

        private void CreatPlayer()
        {
            var player = PhotonNetwork.Instantiate(userPrefab.name, Vector3.zero, Quaternion.identity);
            myPlayer = player;
            if (userEyeGazeCursorPrefab != null)
            {
                var playerEyeGazeCursor = PhotonNetwork.Instantiate(userEyeGazeCursorPrefab.name, Vector3.zero, Quaternion.identity);
            }
        }

        private void CreateInteractableObjects()
        {
            //var position = roverExplorerLocation.position;
            //var positionOnTopOfSurface = new Vector3(position.x, position.y + roverExplorerLocation.localScale.y / 2,
            //    position.z);

            //var go = PhotonNetwork.Instantiate(roverExplorerPrefab.name, positionOnTopOfSurface,
            //    roverExplorerLocation.rotation);
        }

        // private void CreateMainLunarModule()
        // {
        //     module = PhotonNetwork.Instantiate(roverExplorerPrefab.name, Vector3.zero, Quaternion.identity);
        //     pv.RPC("Rpc_SetModuleParent", RpcTarget.AllBuffered);
        // }
        //
        // [PunRPC]
        // private void Rpc_SetModuleParent()
        // {
        //     Debug.Log("Rpc_SetModuleParent- RPC Called");
        //     module.transform.parent = TableAnchor.Instance.transform;
        //     module.transform.localPosition = moduleLocation;
        // }
    }
}
