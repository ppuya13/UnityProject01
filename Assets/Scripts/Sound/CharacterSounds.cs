﻿using System.Collections.Generic;
using Game;
using UnityEngine;

namespace Sound
{
    //사운드들 일일히 할당하기 귀찮으니까 이걸로 할당함
    public class CharacterSounds
    {
        private Dictionary<SoundType, AudioClip[]> sounds;
        
        private AudioClip[] swordSwingSounds;
        private AudioClip[] swordHitSounds;
        private AudioClip[] kickSounds;
        private AudioClip[] kickHitSounds;
        private AudioClip[] castSound;
        private AudioClip[] guardSound;
        
        public void InitializeClips()
        {
            sounds = new Dictionary<SoundType, AudioClip[]>();
            sounds.TryAdd(SoundType.Unknown, null);
            
            swordSwingSounds = Resources.LoadAll<AudioClip>("Sounds/Swing/Sword");
            if(swordSwingSounds.Length == 0) Debug.LogWarning("CharacterSounds: swordSwingSounds 할당안됨.");
            sounds.TryAdd(SoundType.SwordSwing, swordSwingSounds);
            swordHitSounds = Resources.LoadAll<AudioClip>("Sounds/Hit/Sword");
            if(swordHitSounds.Length == 0) Debug.LogWarning("CharacterSounds: swordHitSounds 할당안됨.");
            sounds.TryAdd(SoundType.SwordHit, swordHitSounds);
            
            kickSounds = Resources.LoadAll<AudioClip>("Sounds/Swing/Kick");
            if(kickSounds.Length == 0) Debug.LogWarning("CharacterSounds: kickSounds 할당안됨.");
            sounds.TryAdd(SoundType.Kick, kickSounds);
            kickHitSounds = Resources.LoadAll<AudioClip>("Sounds/Hit/Kick");
            if(kickHitSounds.Length == 0) Debug.LogWarning("CharacterSounds: kickHitSounds 할당안됨.");
            sounds.TryAdd(SoundType.KickHit, kickHitSounds);
            
            castSound = Resources.LoadAll<AudioClip>("Sounds/Cast");
            if(castSound.Length == 0) Debug.LogWarning("CharacterSounds: castSound 할당안됨.");
            sounds.TryAdd(SoundType.Cast, castSound);
            
            guardSound = Resources.LoadAll<AudioClip>("Sounds/Guard");
            if(guardSound.Length == 0) Debug.LogWarning("CharacterSounds: guardSound 할당안됨.");
            sounds.TryAdd(SoundType.Guard, guardSound);
                
        }

        public AudioClip[] GetSounds(SoundType type)
        {
            return sounds.GetValueOrDefault(type);
        }

        public struct SoundStruct
        {
            public SoundType Swing;
            public SoundType Hit;
            public SoundType Extra;
        }
    }
}