using System.Collections.Generic;
using Game;
using Monster;
using UnityEngine;


public class SpawnManager: DDSingletonManager<SpawnManager>
{
    public GameObject monsterPrefab;
    public Transform monsterSpawner;
    public GameObject characterPrefab;

    public Dictionary<string, MonsterController> SpawnedMonster;

    public void SpawnMonster(MonsterSpawn msg)
    {
        GameObject go = Instantiate(monsterPrefab, monsterSpawner.position, Quaternion.Euler(0, 180, 0));
        MonsterController mc = go.GetComponent<MonsterController>();
        mc.monsterId = msg.MonsterId;
        mc.SendChangeState(MonsterState.MonsterStatusSpawn);
        SpawnedMonster.Add(mc.monsterId, mc);
    }
}
