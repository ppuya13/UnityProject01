using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioSourcePool : MonoBehaviour
{
    public AudioMixerGroup defaultGroup;
    private Queue<AudioSource> _pool = new Queue<AudioSource>();
    public int initialSize = 10;

    private void Awake()
    {
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewAudioSource();
        }
    }

    private AudioSource CreateNewAudioSource()
    {
        GameObject obj = new GameObject("PooledAudioSource");
        obj.transform.SetParent(this.transform);
        AudioSource source = obj.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = defaultGroup;
        source.playOnAwake = false;
        source.spatialBlend = 1.0f; // 기본을 3D 사운드로 설정
        _pool.Enqueue(source);
        return source;
    }

    public AudioSource GetAudioSource()
    {
        if (_pool.Count == 0)
        {
            CreateNewAudioSource();
        }
        return _pool.Dequeue();
    }

    public void ReturnAudioSource(AudioSource source)
    {
        source.Stop();
        source.transform.position = Vector3.zero;
        _pool.Enqueue(source);
    }
}