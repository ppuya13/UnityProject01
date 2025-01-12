using System.Collections.Generic;
using Character;
using Game;
using Monster;
using UnityEngine;
using UnityEngine.Serialization;


public class SpawnManager: DDSingletonManager<SpawnManager>
{
    public GameObject monsterPrefab;
    public Transform monsterSpawner;
    public GameObject myPlayerPrefab;
    public GameObject otherPlayerPrefab;
    public Transform characterSpawner;

    public Dictionary<string, MonsterController> SpawnedMonsters = new();
    public Dictionary<string, PlayerController> SpawnedPlayers = new();
    public Dictionary<string, PlayerController> Dummys = new();
    public PlayerController MyCharacter;
    
    private GameObject characters; //캐릭터들을 담고 있는 루트 오브젝트
    public PlayerAttackConfig[] attackConfigs;

    protected override void Awake()
    {
        base.Awake();
        characters = GameObject.Find("Characters");
    }

    //플레이어를 스폰하는 기능
    public void SpawnPlayer(string playerId, string nickname)
    {

        GameObject go;
        PlayerController pc;
        
        if (playerId == SuperManager.Instance.playerId)
        {
            go = Instantiate(myPlayerPrefab, characterSpawner.position, Quaternion.identity);
            pc = go.GetComponent<PlayerController>();
            pc.type = PlayerController.CharacterType.MyPlayer;
            MyCharacter = pc;
            pc.playerId = playerId;
            pc.nickname = nickname;
            UIManager.Instance.myCharacterPanel.ConnectCharacter(pc);
        }
        else
        {
            go = Instantiate(otherPlayerPrefab, characterSpawner.position, Quaternion.identity);
            pc = go.GetComponent<PlayerController>();
            pc.type = PlayerController.CharacterType.OtherPlayer;
            pc.playerId = playerId;
            pc.nickname = nickname;
            UIManager.Instance.CreateCharacterPanel(pc);
        }

        pc.attackConfigs = attackConfigs;
        pc.InitializeAttackConfigs();
        
        go.transform.SetParent(characters.transform);
        SpawnedPlayers.TryAdd(playerId, pc);
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
        UIManager.Instance.monsterPanel.ConnectMonster(mc);
        mc.monsterId = msg.MonsterId;
        mc.SendChangeState(MonsterState.MonsterStatusSpawn);
        SpawnedMonsters.Add(mc.monsterId, mc);
    }
}
