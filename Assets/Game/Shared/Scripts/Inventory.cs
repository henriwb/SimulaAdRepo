using System;
using System.Collections;
using System.Collections.Generic;
using HyperCasual.Core;
using HyperCasual.Gameplay;
using UnityEngine;

namespace HyperCasual.Runner
{
    /// <summary>
    /// A simple inventory class that listens to game events and keeps track of the amount of in-game currencies
    /// collected by the player
    /// </summary>
    public class Inventory : AbstractSingleton<Inventory>
    {
        [SerializeField]
        GenericGameEventListener m_GoldEventListener;
        [SerializeField]
        GenericGameEventListener m_KeyEventListener;
        [SerializeField]
        GenericGameEventListener m_WinEventListener;

        int m_TempGold;
        int m_TotalGold;
        float m_TempXp;
        float m_TotalXp;

        Hud m_Hud;

        void Start()
        {
            m_GoldEventListener.EventHandler = OnGoldPicked;
            m_KeyEventListener.EventHandler = OnKeyPicked;
            m_WinEventListener.EventHandler = OnWin;

            m_TempGold = 0;
            m_TotalGold = SaveManager.Instance.Currency;
            m_TempXp = 0;
            m_TotalXp = SaveManager.Instance.XP;
            m_Hud = UIManager.Instance.GetView<Hud>();
        } 

        void OnEnable()
        {
            m_GoldEventListener.Subscribe();
            m_KeyEventListener.Subscribe();
            m_WinEventListener.Subscribe();
        }

        void OnDisable()
        {
            m_GoldEventListener.Unsubscribe();
            m_KeyEventListener.Unsubscribe();
            m_WinEventListener.Unsubscribe();
        }

        void OnGoldPicked()
        {
            if (m_GoldEventListener.m_Event is ItemPickedEvent goldPickedEvent)
            {
                m_TempGold += goldPickedEvent.Count;
                m_Hud.GoldValue = m_TempGold;
            }
            else
            {
                throw new Exception($"Invalid event type!");
            }
        }

        void OnKeyPicked()
        {
            if (m_KeyEventListener.m_Event is ItemPickedEvent keyPickedEvent)
            {
                m_Hud.AddKey();
            }
            else
            {
                throw new Exception($"Invalid event type!");
            }
        }

        void OnWin()
        {
            m_TotalGold += m_TempGold;
            m_TempGold = 0;
            SaveManager.Instance.Currency = m_TotalGold;

            m_TotalXp += m_TempXp;
            m_TempXp = 0f;
            SaveManager.Instance.XP = m_TotalXp;
        }
    }
}
