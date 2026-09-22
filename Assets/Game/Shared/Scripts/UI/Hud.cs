using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using HyperCasual.Core;
using HyperCasual.Runner;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyperCasual.Gameplay
{
    /// <summary>
    /// This View contains head-up-display functionalities
    /// </summary>
    public class Hud : View
    {
        [SerializeField] private TextMeshProUGUI m_GoldText;
        [SerializeField] private Image[] Keys;
        [SerializeField] private Color gainKeyColor;
        private int m_GoldValue;
        private int m_KeysValue;

        public void AddKey()
        {
            Keys[m_KeysValue].DOFade(1, .5f);
            m_KeysValue++;
        }

        /// <summary>
        /// The amount of gold to display on the hud.
        /// The setter method also sets the hud text.
        /// </summary>
        public int GoldValue
        {
            get => m_GoldValue;
            set
            {
                if (m_GoldValue != value)
                {
                    m_GoldValue = value;
                    m_GoldText.text = GoldValue.ToString();
                }
            }
        }
    }
}