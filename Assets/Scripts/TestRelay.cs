using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;
using TMPro;



public class NewBehaviourScript : MonoBehaviour
{
    [SerializeField] Button startBtn;
    [SerializeField] Button joinBtn;
    [SerializeField] TMP_InputField relayCodeInput;
    private void Awake()
    {
        startBtn.onClick.AddListener(() =>
        {
            CreateRelay();
        });
        joinBtn.onClick.AddListener(() =>
        {
            string enteredCode = relayCodeInput.text; 
            JoinRelay(enteredCode);
        });
    }
    private async void Start()
    {
      await UnityServices.InitializeAsync();
      AuthenticationService.Instance.SignedIn += () =>
      {
       Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
      };   
      await AuthenticationService.Instance.SignInAnonymouslyAsync();
        CreateRelay();  
    }

    private async void CreateRelay()
    {
        try {
           Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);

           string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
           Debug.Log(joinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
                );
            NetworkManager.Singleton.StartHost();
        } catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining Relay with" + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
                );

            NetworkManager.Singleton.StartClient();
        } catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}
