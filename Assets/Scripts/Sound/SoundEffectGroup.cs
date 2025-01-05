using Sirenix.OdinInspector;
using UnityEngine;

namespace Sound
{
    //태그는 인스펙터상에서 구분하기 위한 용도의 문자열.
    public class SoundEffectGroup
    {
        [BoxGroup("Sound Group")] 
        [LabelText("그룹 이름(태그)")]
        public string GroupName;

        [BoxGroup("Sound Group")] 
        [LabelText("오디오 클립")]
        public AudioClip[] Clips;
    }
}