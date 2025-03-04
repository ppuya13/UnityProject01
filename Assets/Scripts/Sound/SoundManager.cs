using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game;
using UnityEngine;
using UnityEngine.Audio;

namespace Sound
{
    /// <summary>
    /// <para>사운드 재생을 담당. 메소드에서 볼륨을 비워놓을 경우 기본값으로 설정되고, 태그를 비워놓을 경우 "Default"태그로 지정되며, SoundGroup을 비워놓을 경우 SFX로 설정된다.</para>
    /// <para>또한, 위치를 입력할 경우 해당 위치에서 3d sound로 재생되며, 위치를 비워놓을 경우 2d사운드로 재생됨.
    /// <para>PlaySound(AudioClip, SoundGroup, 볼륨, 위치) : 사운드를 1회 재생 (볼륨은 비워놓을 경우 기본값, position을 비워놓으면 2d 사운드로 재생됨.)
    /// <para>PlaySound(SoundType, 위치, 강도, SoundGroup, 볼륨): 사운드를 재생하고, 소리의 위치와 강도를 이벤트로 발행함.</para>
    /// <para>PlayRandomSound(AudioClip[], SoundGroup, 볼륨, 위치) : 배열 내의 랜덤한 사운드를 1회 재생</para>
    /// <para>PlaySoundsSeq(AudioClip, 태그, SoundGroup, 볼륨) : 태그별로 순차적으로 사운드를 재생함. 같은 태그의 사운드가 재생중이면 대기열에 추가됨.</para>
    /// <para>PlaySoundOverride(AudioClip, 태그, SoundGroup, 볼륨) : 같은 태그의 현재 재생중인 사운드를 중단하고 재생 대기열도 모두 취소한 뒤 새 사운드를 재생함.</para>
    /// <para>StopSound() : PlaySound 메소드로 실행된 모든 사운드를 재생 중지한다.</para>
    /// <para>StopSoundWithTag(태그) : 해당 태그의 현재 재생중인 사운드를 중단하고, 대기열을 지운다.</para>
    /// <para>StopAllSounds() : PlaySound 메소드로 실행된 모든 사운드와 태그가 있는 모든 사운드의 재생을 중단하고, 대기열을 지운다. (BGM 제외)</para>
    /// <para>PlayBGM(AudioClip, 볼륨) : 실행 시 해당 사운드를 루프하여 재생한다. 재차 실행 시 FadeOut된 후 새로운 사운드가 FadeIn 된다. 또한, Resources/Sound/BGM 폴더에 있는 AudioClip들은 모두 SoundManager.Instance.bgmClips[]에 담기며, Start()에서 bgmClips[0]을 게임 시작 시 자동으로 재생한다.</para>
    /// <para>FadeOutBGM(시간) : 입력한 시간에 걸쳐 BGM을 FadeOut 시킨다. 기본값은 0.2초.</para>
    /// <para>FadeInBGM(시간, 볼륨) : 입력한 시간에 걸쳐 현재 볼륨에서 입력한 볼륨까지 BGM의 볼륨을 변화시킨다. 기본값은 0.2초.</para>
    /// </summary>
    public class SoundManager : DDSingletonManager<SoundManager>
    {
        public event Action<Vector3, float> OnSoundEmitted;
        public AudioMixer mixer; //볼륨 조절과 사운드 효과를 위한 믹서
        private AudioMixerGroup _masterGroup;
        private AudioMixerGroup _musicGroup;
        private AudioMixerGroup _sfxGroup;
        private AudioMixerGroup _uiGroup;
        private AudioSourcePool _audioSourcePool;
    
        
        // 서버 관련 코드는 SoundMapping, SoundType 관련 메소드와 변수들을 주석 해제할 것
        // [Serializable]
        // public struct SoundMapping
        // {
        //     public Game.SoundType soundType; // Protobuf에서 정의한 SoundType
        //     public AudioClip[] clips;
        // }

        // public SoundMapping[] soundMappings;
        // private Dictionary<SoundType, AudioClip[]> _soundDict;
    

        private AudioSource _sfxSource;
        private AudioSource _bgmSource; // BGM 전용 AudioSource
        private AudioSource _uiSource; // UI 사운드 전용 오디오소스
        [HideInInspector] public AudioClip[] bgmClips;

        private bool _isFading = false; // 페이드 상태를 추적하는 플래그


        private Dictionary<string, Coroutine> _activeOverrideCoroutines = new();
        private Dictionary<string, Queue<AudioClip>> _taggedSequentialQueues = new();
        private Dictionary<string, bool> _isPlayingSequential = new();
        private Dictionary<string, float> _sequentialVolumes = new();
        private Dictionary<string, AudioSource> _taggedAudioSources = new();


        protected override void Awake()
        {
            base.Awake();
            // AudioSourcePool 초기화
            if (!_audioSourcePool)
            {
                GameObject poolObj = new GameObject("AudioSourcePool");
                poolObj.transform.SetParent(transform);
                _audioSourcePool = poolObj.AddComponent<AudioSourcePool>();
                _audioSourcePool.defaultGroup = _sfxGroup; // 기본 그룹 설정
            }
        
            if (mixer)
            {
                _masterGroup = mixer.FindMatchingGroups("Master").FirstOrDefault();
                _musicGroup = mixer.FindMatchingGroups("Music").FirstOrDefault();
                _sfxGroup = mixer.FindMatchingGroups("SFX").FirstOrDefault();
                _uiGroup = mixer.FindMatchingGroups("UI").FirstOrDefault();
            }
            // SFX 전용 AudioSource 초기화
            _sfxSource = GetComponent<AudioSource>();
            if (!_sfxSource)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
            }
            _sfxSource.outputAudioMixerGroup = _sfxGroup;

            // UI 전용 AudioSource 초기화
            _uiSource = gameObject.AddComponent<AudioSource>();
            _uiSource.outputAudioMixerGroup = _uiGroup;

            // BGM 전용 AudioSource 초기화
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.outputAudioMixerGroup = _musicGroup;
            bgmClips = Resources.LoadAll<AudioClip>("Sounds/BGM");

            // // SoundType과 AudioClip 매핑 초기화
            // _soundDict = new Dictionary<SoundType, AudioClip[]>();
            // foreach (var mapping in soundMappings)
            // {
            //     if (!_soundDict.ContainsKey(mapping.soundType))
            //         _soundDict.Add(mapping.soundType, mapping.clips);
            //     else
            //         Debug.LogWarning($"Duplicate SoundType detected: {mapping.soundType}");
            // }
        }

        private void Start()
        {
            if (bgmClips.Length > 0)
            {
                // PlayBGM(bgmClips[0], UIManager.Instance.bgmVolume);
                // PlayBGM(bgmClips[0], 0.1f); //게임을 켜면 bgm을 재생
            }
        }

        /// <summary>
        /// PlaySound(AudioClip, SoundGroup, 볼륨, 위치) : 사운드를 1회 재생 (볼륨은 비워놓을 경우 기본값, position을 비워놓으면 2d 사운드로 재생됨.)
        /// </summary>
        public void PlaySound(AudioClip clip, SoundGroup soundGroup = SoundGroup.Sfx, float volume = -1.0f, Vector3? position = null)
        {
            // 입력된 볼륨이 0보다 작으면(볼륨을 입력하지 않으면) globalVolume으로 소리를 재생하고
            // 볼륨을 따로 입력했으면 볼륨에 globalVolume에 volume를 곱해서 재생한다.
            if (!clip)
            {
                Debug.Log("PlaySound: clip이 null임");
                return;
            }

            float finalVolume = CalculateVolume(volume);
        
            if (position.HasValue)
            {
                // 풀에서 AudioSource 가져오기
                AudioSource source = _audioSourcePool.GetAudioSource();
                source.outputAudioMixerGroup = GetMixerGroup(soundGroup);
                source.transform.position = position.Value;
                source.spatialBlend = 1.0f;
                source.volume = finalVolume;
                source.clip = clip;
                source.Play();

                // 사운드 재생이 끝난 후 AudioSource를 풀에 반환
                StartCoroutine(ReturnToPoolAfterPlaying(source, clip.length));
            }
            else
            {
                AudioSource source = GetAudioSource(soundGroup);
                source.PlayOneShot(clip, finalVolume);
            }
        }
    
        private IEnumerator ReturnToPoolAfterPlaying(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            _audioSourcePool.ReturnAudioSource(source);
        }

        // /// <summary>
        // /// PlaySound(SoundType, 위치, 강도, SoundGroup, 볼륨): 사운드를 재생하고, 소리의 위치와 강도를 이벤트로 발행함.
        // /// </summary>
        // public void PlaySound(Game.SoundType soundType, Vector3 position, float intensity,
        //     SoundGroup soundGroup = SoundGroup.Sfx, float volume = -1.0f)
        // {
        //     if (_soundDict.TryGetValue(soundType, out AudioClip[] clips))
        //     {
        //         if (clips == null || clips.Length == 0)
        //         {
        //             Debug.LogWarning($"PlaySound: '{soundType}' 매칭된 클립이 없음.");
        //             return;
        //         }
        //         else if (clips.Length == 1)
        //         {
        //             PlaySound(clips[0], soundGroup, volume, position);
        //         }
        //         else
        //         {
        //             PlayRandomSound(clips, soundGroup, volume, position);
        //         }
        //     }
        //     else
        //     {
        //         Debug.LogWarning($"PlaySound: '{soundType}' 매칭된 클립이 없음.");
        //     }
        //
        //     // Debug.Log("인보크");
        //     OnSoundEmitted?.Invoke(position, intensity); // 소리 발생 이벤트 발행
        // }
        //
        // /// <summary>
        // /// SendPlaySound(사운드 타입, 위치, 강도, SoundGroup, 볼륨): 서버에 사운드 재생을 요청함
        // /// </summary>
        // public void SendPlaySound(SoundType soundType, Vector3 position, float intensity,
        //     SoundGroup soundGroup = SoundGroup.Sfx, float volume = -1.0f)
        // {
        //     TcpProtobufClient.Instance.SendPlaySoundMessage(soundType, position, intensity, volume, soundGroup);
        //     PlaySound(soundType, position, intensity, soundGroup, volume);
        //     // Debug.Log($"플레이 사운드 {soundType}");
        // }

        /// <summary>
        /// PlayRandomSound(AudioClip[], SoundGroup, 볼륨, 위치) : 배열 내의 랜덤한 사운드를 1회 재생
        /// </summary>
        public void PlayRandomSound(AudioClip[] clips, SoundGroup soundGroup = SoundGroup.Sfx, float volume = -1.0f, Vector3? position = null)
        {
            if (clips == null || clips.Length == 0)
            {
                Debug.LogWarning("PlayRandomSound: 클립 배열이 비어있음.");
                return;
            }

            int randomIdx = UnityEngine.Random.Range(0, clips.Length);
            PlaySound(clips[randomIdx], soundGroup, volume, position);
        }

        /// <summary>
        /// PlaySoundsSeq(AudioClip, 태그, SoundGroup, 볼륨) : 태그별로 순차적으로 사운드를 재생함. 같은 태그의 사운드가 재생중이면 대기열에 추가됨.
        /// </summary>
        public void PlaySoundsSeq(AudioClip clip, string tagString = "Default", SoundGroup soundGroup = SoundGroup.Sfx, float volume = -1.0f)
        {
            if (!clip)
            {
                Debug.Log($"PlaySoundsSeq: clip이 null임 (태그: {tagString})");
                return;
            }

            if (!_taggedSequentialQueues.ContainsKey(tagString))
            {
                _taggedSequentialQueues[tagString] = new Queue<AudioClip>();
                _isPlayingSequential[tagString] = false;
                _sequentialVolumes[tagString] = -1.0f;
            }

            _taggedSequentialQueues[tagString].Enqueue(clip);
            _sequentialVolumes[tagString] = volume;

            if (!_isPlayingSequential[tagString] && !_activeOverrideCoroutines.ContainsKey(tagString))
            {
                StartCoroutine(PlaySequentialSounds(tagString, soundGroup));
            }
        }

        private IEnumerator PlaySequentialSounds(string tagString, SoundGroup soundGroup)
        {
            _isPlayingSequential[tagString] = true;

            while (_taggedSequentialQueues[tagString].Count > 0)
            {
                // Override 사운드가 재생 중이면 대기
                while (_activeOverrideCoroutines.ContainsKey(tagString))
                {
                    yield return null;
                }

                AudioClip nextClip = _taggedSequentialQueues[tagString].Dequeue();
                PlaySound(nextClip, soundGroup, _sequentialVolumes[tagString]);

                yield return new WaitForSeconds(nextClip.length);

                if (!_isPlayingSequential[tagString])
                {
                    Debug.Log($"순차 재생 중단됨 (태그: {tagString})");
                    yield break;
                }
            }

            _isPlayingSequential[tagString] = false;
        }

        /// <summary>
        /// PlaySoundOverride(AudioClip, 태그, SoundGroup, 볼륨) : 같은 태그의 현재 재생중인 사운드를 중단하고 재생 대기열도 모두 취소한 뒤 새 사운드를 재생함.
        /// </summary>
        public void PlaySoundOverride(AudioClip clip, string tagString = "Default", SoundGroup soundGroup = SoundGroup.Sfx, float volume = -1.0f)
        {
            if (!clip)
            {
                Debug.Log($"PlaySoundOverride: clip이 null임 (태그: {tagString})");
                return;
            }

            // 현재 실행 중인 Override 코루틴이 있다면 중지
            if (_activeOverrideCoroutines.ContainsKey(tagString))
            {
                StopCoroutine(_activeOverrideCoroutines[tagString]);
                _activeOverrideCoroutines.Remove(tagString);
            }

            // 새 Override 코루틴 시작
            _activeOverrideCoroutines[tagString] = StartCoroutine(PlayOverrideSound(clip, volume, tagString, soundGroup));
        }

        private IEnumerator PlayOverrideSound(AudioClip clip, float volume, string tagString, SoundGroup soundGroup)
        {
            // 태그에 해당하는 AudioSource가 없으면 새로 생성
            if (!_taggedAudioSources.ContainsKey(tagString))
            {
                GameObject audioSourceObj = new GameObject($"AudioSource_{tagString}");
                audioSourceObj.transform.SetParent(this.transform);
                AudioSource newSource = audioSourceObj.AddComponent<AudioSource>();
                newSource.outputAudioMixerGroup = GetMixerGroup(soundGroup);
                _taggedAudioSources[tagString] = newSource;
            }

            AudioSource source = _taggedAudioSources[tagString];

            // 현재 재생 중인 사운드 중지
            source.Stop();

            // 새로운 사운드 재생
            float finalVolume = CalculateVolume(volume);
            source.clip = clip;
            source.volume = finalVolume;
            source.Play();

            Debug.Log($"사운드 오버라이드 실행됨 (태그: {tagString})");

            // 사운드 재생이 끝날 때까지 대기
            yield return new WaitForSeconds(clip.length);

            // Override 코루틴 완료
            _activeOverrideCoroutines.Remove(tagString);
            Debug.Log($"사운드 오버라이드 완료 (태그: {tagString})");
        }

        /// <summary>
        /// StopSound() : PlaySound 메소드로 실행된 모든 사운드를 재생 중지한다.
        /// </summary>
        public void StopSound()
        {
            _sfxSource.Stop();
        }

        /// <summary>
        /// StopSoundWithTag(태그) : 해당 태그의 현재 재생중인 사운드를 중단하고, 대기열을 지운다.
        /// </summary>
        public void StopSoundWithTag(string tagString = "Default")
        {
            if (_taggedAudioSources.ContainsKey(tagString))
            {
                _taggedAudioSources[tagString].Stop();
            }

            // 순차 재생 큐 초기화
            if (_taggedSequentialQueues.ContainsKey(tagString))
            {
                _taggedSequentialQueues[tagString].Clear();
                _isPlayingSequential[tagString] = false;
            }

            // Override 코루틴 중지
            if (_activeOverrideCoroutines.ContainsKey(tagString))
            {
                StopCoroutine(_activeOverrideCoroutines[tagString]);
                _activeOverrideCoroutines.Remove(tagString);
            }

            Debug.Log($"태그 '{tagString}'의 모든 사운드가 중지되었습니다.");
        }

        /// <summary>
        /// StopAllSounds() : PlaySound 메소드로 실행된 모든 사운드와 태그가 있는 모든 사운드의 재생을 중단하고, 대기열을 지운다. (BGM 제외)
        /// </summary>
        public void StopAllSounds()
        {
            // 태그 없는 일반 사운드 중지
            StopSound();

            // 태그가 지정된 모든 사운드 중지
            foreach (var tagString in _taggedAudioSources.Keys.ToList())
            {
                StopSoundWithTag(tagString);
            }

            Debug.Log("모든 사운드가 중지되었습니다. (BGM 제외)");
        }

        /// <summary>
        /// PlayBGM(사운드, 볼륨) : 실행 시 해당 사운드를 루프하여 재생한다. 재차 실행 시 FadeOut된 후 새로운 사운드가 FadeIn 된다. 또한, Resources/Sound/BGM 폴더에 있는 AudioClip들은 모두 SoundManager.Instance.bgmClip[]에 담기며, Start()에서 bgmClip[0]을 게임 시작 시 자동으로 재생한다.
        /// </summary>
        public void PlayBGM(AudioClip bgmClip = null, float volume = -1.0f)
        {
            if (!bgmClip) bgmClip = bgmClips[0]; //클립을 비워놓을 경우 bgmClips의 첫 번째 곡을 재생
            
            if (_bgmSource.clip == bgmClip) return; // 같은 BGM이면 다시 재생하지 않음

            float finalVolume = CalculateVolume(volume);

            if (_bgmSource.isPlaying) // BGM이 재생 중이면
            {
                // 페이드아웃 시작
                StartCoroutine(FadeOutBGMCoroutine(_bgmSource, 0.2f, () =>
                {
                    _bgmSource.clip = bgmClip; // 새로운 BGM 클립 설정
                    _bgmSource.volume = finalVolume; // 볼륨 초기화
                    _bgmSource.Play(); // 새로운 BGM 재생
                    // Debug.Log($"볼륨: {finalVolume}");
                }));
            }
            else
            {
                // BGM이 재생 중이 아니면 바로 재생
                _bgmSource.clip = bgmClip; // 새로운 BGM 클립 설정
                _bgmSource.volume = finalVolume; // 볼륨 초기화
                _bgmSource.Play(); // 새로운 BGM 재생
                // Debug.Log($"볼륨: {finalVolume}");
            }
        }

        // BGM 전환시 페이드아웃, PlayBGM 호출 시 불림.
        private IEnumerator FadeOutBGMCoroutine(AudioSource source, float duration, Action onFadeComplete)
        {
            if (!source || _isFading) yield break;

            _isFading = true;

            float startVolume = source.volume;
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0, elapsedTime / duration);
                yield return null;
            }

            source.volume = 0;
            source.Stop();
            onFadeComplete?.Invoke();

            _isFading = false;
        }

        // StartCoroutine(FadeOutBGM(bgmSource, 0.2f, () => { ... }));는 FadeOutBGM 코루틴을 시작합니다.
        // FadeOutBGM 코루틴은 지정된 duration 동안 AudioSource의 볼륨을 서서히 줄입니다.
        // FadeOutBGM 코루틴이 완료되면 onFadeComplete 델리게이트가 호출됩니다. 이 델리게이트에는 람다 표현식 () => { ... }가 할당되어 있습니다.
        // 람다 표현식이 실행되어 새로운 BGM 클립을 설정하고, 볼륨을 초기화한 후 새로운 BGM을 재생합니다.
    

        /// <summary>
        /// FadeOutBGM(시간) : 입력한 시간에 걸쳐 BGM을 FadeOut 시킨다. 기본값은 0.2초.
        /// </summary>
        public void FadeOutBGM(float duration = 0.2f) //BGM을 종료시키는 메소드
        {
            StartCoroutine(FadeOutCoroutine(_bgmSource, duration));
        }

        private IEnumerator FadeOutCoroutine(AudioSource source, float duration)
        {
            if (!source || _isFading) yield break;

            _isFading = true;

            float startVolume = source.volume;
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0, elapsedTime / duration);
                yield return null;
            }

            source.volume = 0;

            _isFading = false;
        }

        /// <summary>
        /// FadeInBGM(시간, 볼륨) : 입력한 시간에 걸쳐 현재 볼륨에서 입력한 볼륨까지 BGM의 볼륨을 변화시킨다. 기본값은 0.2초.
        /// </summary>
        public void FadeInBGM(float duration = 0.2f, float targetVolume = -1.0f)
        {
            float finalTargetVolume = CalculateVolume(targetVolume);
            StartCoroutine(FadeInCoroutine(_bgmSource, finalTargetVolume, duration));
        }

        private IEnumerator FadeInCoroutine(AudioSource source, float targetVolume, float duration)
        {
            if (!source || _isFading) yield break;

            _isFading = true;

            float startVolume = source.volume;
            float finalTargetVolume = CalculateVolume(targetVolume);
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, finalTargetVolume, elapsedTime / duration);
                yield return null;
            }

            source.volume = finalTargetVolume;

            _isFading = false;
        }

        // 글로벌 볼륨을 적용한 최종 볼륨 계산 메서드
        private float CalculateVolume(float volume)
        {
            if (Mathf.Approximately(volume, -1f)) return 1.0f;
            return Mathf.Clamp01(volume);
        }
    
        /// AudioMixerGroup에 따른 AudioSource 반환 메소드
        private AudioSource GetAudioSource(SoundGroup soundGroup)
        {
            switch (soundGroup)
            {
                case SoundGroup.Music:
                    return _bgmSource;
                case SoundGroup.Sfx:
                    return _sfxSource;
                case SoundGroup.Ui:
                    return _uiSource;
                default:
                    return _sfxSource; // 기본 Sfx 그룹
            }
        }
    
        private AudioMixerGroup GetMixerGroup(SoundGroup soundGroup)
        {
            switch (soundGroup)
            {
                case SoundGroup.Music:
                    return _musicGroup;
                case SoundGroup.Sfx:
                    return _sfxGroup;
                case SoundGroup.Ui:
                    return _uiGroup;
                default:
                    return _masterGroup;
            }
        }
        
        public enum SoundGroup
        {
            Sfx,
            Music,
            Ui,
        }
    }
}