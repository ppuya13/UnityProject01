using System.Collections.Generic;
using Game;
using UnityEngine;

namespace Sound
{
    //사운드들 일일히 할당하기 귀찮으니까 이걸로 할당함
    public class CharacterSounds
    {
        private AudioClip[] swingSounds;
        private Dictionary<SoundType, AudioClip[]> sounds = new();
        

        public void InitializeClips()
        {
            sounds.TryAdd(SoundType.Unknown, null);
            
            swingSounds = Resources.LoadAll<AudioClip>("Sounds/Swing");
            sounds.TryAdd(SoundType.SwordSwing, swingSounds);
        }

        public AudioClip[] GetSounds(SoundType type)
        {
            return sounds.GetValueOrDefault(type);
        }

        public struct SoundStruct
        {
            public SoundType Swing;
            public SoundType Hit;
        }
    }
}