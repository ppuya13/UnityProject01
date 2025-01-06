using System.Collections.Generic;
using UnityEngine;

namespace Sound
{
    //사운드들 일일히 할당하기 귀찮으니까 이걸로 할당함
    public class CharacterSounds
    {
        private readonly AudioClip[] swingSounds = Resources.LoadAll<AudioClip>("Sounds/Swing");
        private Dictionary<SoundType, AudioClip[]> sounds = new();

        public CharacterSounds()
        {
            sounds.TryAdd(SoundType.Unknown, null);
            sounds.TryAdd(SoundType.Swing, swingSounds);
        }

        public AudioClip[] GetSounds(SoundType type)
        {
            return sounds.GetValueOrDefault(type);
        }
        
    }

    public enum SoundType
    {
        Unknown,
        Swing,
        
    }
}