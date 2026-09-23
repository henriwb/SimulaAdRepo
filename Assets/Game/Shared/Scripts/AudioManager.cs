using System;
using System.Collections;
using System.Collections.Generic;
using HyperCasual.Core;
using UnityEngine;
using AudioSettings = HyperCasual.Core.AudioSettings;

namespace HyperCasual.Runner
{
    /// <summary>
    /// Handles playing sounds and music based on their sound ID
    /// </summary>
    public class AudioManager : AbstractSingleton<AudioManager>
    {
        [Serializable]
        class SoundIDClipPair
        {
            public SoundID m_SoundID;
            public AudioClip m_AudioClip;
        }

        [SerializeField]
        AudioSource m_MusicSource;
        [SerializeField]
        AudioSource m_EffectSource;

        public float MinSoundInterval => m_MinSoundInterval;

        [SerializeField] private float m_MinSoundInterval = 0.1f;

        [SerializeField]
        SoundIDClipPair[] m_Sounds;

        [Tooltip("Master volume at startup (no saved settings: the playable has no SaveManager).")]
        [Range(0f, 1f)]
        [SerializeField] float m_StartVolume = 1f;

        float m_LastSoundPlayTime;
        readonly Dictionary<SoundID, AudioClip> m_Clips = new Dictionary<SoundID, AudioClip>();

        AudioSettings m_AudioSettings = new AudioSettings();

        /// <summary>
        /// Unmute/mute the music
        /// </summary>
        public bool EnableMusic
        {
            set
            {
                m_AudioSettings.EnableMusic = value;
                m_MusicSource.mute = !value;
                // (The playable plays no music; PlayMusic also refuses to start while muted.)
            }
        }
        
        /// <summary>
        /// Unmute/mute all sound effects
        /// </summary>
        public bool EnableSfx
        {
            get => m_AudioSettings.EnableSfx;
            set
            {
                m_AudioSettings.EnableSfx = value;
                m_EffectSource.mute = !value;

                // Second layer for the web build: silence the listener while muted.
                AudioListener.volume = value ? m_AudioSettings.MasterVolume : 0f;
            }
        }

        /// <summary>
        /// The Master volume of the audio listener
        /// </summary>
        public float MasterVolume
        {
            get => m_AudioSettings.MasterVolume;
            set
            {
                m_AudioSettings.MasterVolume = value;
                AudioListener.volume = m_AudioSettings.EnableSfx ? value : 0f;
            }
        }

        protected override void Awake()
        {
            foreach (var sound in m_Sounds)
            {
                m_Clips.Add(sound.m_SoundID, sound.m_AudioClip);
            }
        }

        // No persistence (the tutorial's SaveManager was removed): every session starts with sound on;
        // the mute button (MuteController) toggles EnableSfx/EnableMusic for the current session.
        void OnEnable()
        {
            EnableMusic = m_AudioSettings.EnableMusic;
            EnableSfx = m_AudioSettings.EnableSfx;
            MasterVolume = m_StartVolume;
        }

        void PlayMusic(AudioClip audioClip, bool looping = true)
        {
            if (m_MusicSource.isPlaying || !m_AudioSettings.EnableMusic)
                return;
            
            m_MusicSource.clip = audioClip;
            m_MusicSource.loop = looping;
            m_MusicSource.Play();
        }
        
        /// <summary>
        /// Play a music based on its sound ID
        /// </summary>
        /// <param name="soundID">The ID of the music</param>
        /// <param name="looping">Is music looping?</param>
        public void PlayMusic(SoundID soundID, bool looping = true)
        {
            PlayMusic(m_Clips[soundID], looping);
        }

        /// <summary>
        /// Stop the current music
        /// </summary>
        public void StopMusic()
        {
            m_MusicSource.Stop();
        }

        void PlayEffect(AudioClip audioClip)
        {
            // Muted: don't play at all. AudioSource.mute is not honored by PlayOneShot in the
            // Playworks web runtime, so skipping the call is the reliable mute.
            if (!m_AudioSettings.EnableSfx)
                return;

            if (Time.time - m_LastSoundPlayTime >= MinSoundInterval)
            {
                m_EffectSource.PlayOneShot(audioClip);
                m_LastSoundPlayTime = Time.time;
            }
        }

        /// <summary>
        /// Play a sound effect based on its sound ID
        /// </summary>
        /// <param name="soundID">The ID of the sound effect</param>
        public void PlayEffect(SoundID soundID)
        {
            if (soundID == SoundID.None)
                return;
            
            PlayEffect(m_Clips[soundID]);
        }
    }
}