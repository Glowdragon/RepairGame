﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GGJ2020;
using GGJ2020.Game;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    [SerializeField] private Player myPlayer;

    [SerializeField] private GameObject[] randomSlotPresetPrefabs;
    
    [SerializeField] private GameObject[] itemPrefabs;

    [SerializeField] private UnityEvent onCountdownStart;
    
    public GameObject postGameForm;
    
    private State state;

    private float timer;

    private bool running;

    public UnityEvent onGameStart;
    
    public UnityEvent onGameWon;

    public UnityEvent onGameLost;

    public Player MyPlayer => myPlayer;

    public State State
    {
        get => state;
        set => state = value;
    }

    public float Timer
    {
        get => timer;
        set => timer = value;
    }

    public bool Running
    {
        get => running;
        set => running = value;
    }

    public void PrepareBoard()
    {
        myPlayer.Board.GenerateSlots(GetRandomSlotsPresetPrefab());
    }

    public void StartGame(List<int> itemIds)
    {
        StartCoroutine(CStartGame(itemIds));
    }
    IEnumerator CStartGame(List<int> itemIds)
    {
        onCountdownStart.Invoke();
        yield return new WaitForSeconds(3);
        
        List<Slot> randomSlots = GetRandomSlots(itemIds.Count);
        
        for (int i = 0; i < itemIds.Count; i++)
        {
            yield return new WaitForSeconds(0.25f);
            Slot slot = randomSlots[i];
            int itemId = itemIds[i];
            GameObject itemPrefab = itemPrefabs.First(p => p.GetComponent<Item>().Id == itemId);
            GameObject itemObj = Instantiate(itemPrefab);
            itemObj.transform.position = slot.transform.position;
            slot.Item = itemObj.GetComponent<Item>();
            slot.Item.PlaySpawnAnimation();
        }

        running = true;
        state = State.TakeItem;
        
        onGameStart.Invoke();
    }

    public void PlaceItem(Slot slot, Item item)
    {
        if (slot.IsEmpty())
        {
            slot.Item = item;
            state = State.TakeItem;
        }
    }

    public List<Slot> GetAllSlots()
    {
        /*List<Slot> slots = new List<Slot>();
        foreach (GameObject slotObj in GameObject.FindGameObjectsWithTag("Slot"))
        {
            Slot slot = slotObj.GetComponent<Slot>();
            slots.Add(slot);
        }
        Debug.Log("GetAllSlots() -> " + slots.Count);
        return slots;*/
        return myPlayer.Board.Slots;
    }

    public List<Slot> GetRandomSlots(int count)
    {
        List<Slot> randomSlots = new List<Slot>();
        List<Slot> allSlots = GetAllSlots();
        int loopCount = 0;
        while (randomSlots.Count < count && loopCount < 100)
        {
            loopCount++;
            Slot randomSlot = allSlots[Random.Range(0, allSlots.Count)];
            if (!randomSlots.Contains(randomSlot))
            {
                randomSlots.Add(randomSlot);
            }
        }
        return randomSlots;
    }

    public GameObject GetRandomSlotsPresetPrefab()
    {
        return randomSlotPresetPrefabs[Random.Range(0, randomSlotPresetPrefabs.Length)];
    }

    public List<GameObject> GetRandomItemPrefabs(int count)
    {
        List<GameObject> randomPrefabs = new List<GameObject>();
        while (randomPrefabs.Count < count)
        {
            GameObject randomPrefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
            if (!randomPrefabs.Contains(randomPrefab))
            {
                randomPrefabs.Add(randomPrefab);
            }
        }
        return randomPrefabs;
    }
    
    public List<int> GetRandomItemIds(int count)
    {
        List<int> randomIds = new List<int>();
        while (randomIds.Count < count)
        {
            GameObject randomPrefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];
            int randomId = randomPrefab.GetComponent<Item>().Id;
            if (!randomIds.Contains(randomId))
            {
                randomIds.Add(randomId);
            }
        }
        return randomIds;
    }

    public void EndGame(bool won)
    {
        if (!running)
        {
            Debug.LogWarning("Can't end game again.");
            return;
        }
        
        if (won)
        {
            onGameWon.Invoke();
        }
        else
        {
            onGameLost.Invoke();
        }

        running = false;

        if (Tcp.Type == TcpType.None || Tcp.Type == TcpType.Server)
        {
            StartCoroutine(CEndGame());
        }
    }

    IEnumerator CEndGame()
    {
        /*float extraTimer = 0;
        while (timer + extraTimer < 20)
        {
            yield return new WaitForEndOfFrame();
            extraTimer += Time.deltaTime;
        }*/
        yield return new WaitForSeconds(7);
        if (Tcp.Peer != null)
        {
            Tcp.Peer.SendPacket(new RestartGamePacket());
        }
        postGameForm.SetActive(true);
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}