using System.Collections.Generic;
using Game;
using Monster;
using UnityEngine;


public class SpawnManager: DDSingletonManager<SpawnManager>
{
    public GameObject monsterPrefab;
    public Transform monsterSpawner;
    public GameObject characterPrefab;

    public Dictionary<string, MonsterController> SpawnedMonsters = new();
    public Dictionary<string, PlayerController> SpawnedPlayers = new();
    public Dictionary<string, PlayerController> Dummys = new();
    public PlayerController MyCharacter;
    
    private GameObject characters; //캐릭터들을 담고 있는 루트 오브젝트

    protected override void Awake()
    {
        base.Awake();
        characters = GameObject.Find("Characters");
    }

    //연재는 이미 있는 플레이어 캐릭터에 id를 부여하는 기능이지만, 앞으로 플레이어를 스폰하는 기능으로 바뀔 여지가 있음.
    public void SpawnPlayer(string playerId)
    {
        foreach (Transform obj in characters.transform)
        {
            PlayerController playerController = obj.GetComponent<PlayerController>();
            if (playerController)
            {
                if (playerController.type is PlayerController.CharacterType.MyPlayer)
                {
                    playerController.playerId = playerId;
                    SpawnedPlayers.TryAdd(playerId, playerController);
                    MyCharacter = playerController;
                }
            }
        }
    }
        
    public void ScanPlayer()
    {
        foreach (Transform obj in characters.transform)
        {
            PlayerController playerController = obj.GetComponent<PlayerController>();
            if (playerController)
            {
                if (playerController.type == PlayerController.CharacterType.Dummy) 
                    Dummys.TryAdd(playerController.playerId, playerController);
                else if (playerController.type is PlayerController.CharacterType.MyPlayer)
                {
                    SpawnedPlayers.TryAdd(playerController.playerId, playerController);
                    MyCharacter = playerController;
                }
                else if (playerController.type is PlayerController.CharacterType.OtherPlayer)
                {
                    SpawnedPlayers.TryAdd(playerController.playerId, playerController);
                }
            }
        }

        // if (Dummys.Count == 0 && SpawnedPlayers.Count == 0)
        //     Debug.LogError("target을 찾지 못했음.");
        // else
        //     Debug.Log($"더미 개수: {Dummys.Count}, 캐릭터 개수: {SpawnedPlayers.Count}");
    }

    public void SpawnMonster(MonsterSpawn msg)
    {
        GameObject go = Instantiate(monsterPrefab, monsterSpawner.position, Quaternion.Euler(0, 180, 0));
        MonsterController mc = go.GetComponent<MonsterController>();
        mc.monsterId = msg.MonsterId;
        mc.SendChangeState(MonsterState.MonsterStatusSpawn);
        SpawnedMonsters.Add(mc.monsterId, mc);
    }
}
