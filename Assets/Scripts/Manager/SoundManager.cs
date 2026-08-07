using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager
{
    public const string Shoot      = "Shoot";
    public const string Hit        = "Hit";
    public const string PlayerHit  = "PlayerHit";
    public const string EnemyDeath = "EnemyDeath";
    public const string Explosion  = "Explosion";

    // 파일명에 공백이 있어 오타가 나기 쉽다 — 조회 경로는 Sounds/Neon Static Loop
    public const string MainBgm = "BGM";

    private AudioSource[] _audioSources = new AudioSource[(int)Define.Sound.Max];
    private Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, float> _lastEffectTime = new Dictionary<string, float>();

    private GameObject _soundRoot = null;

    public void Init()
    {
		if (_soundRoot == null)
		{
			_soundRoot = GameObject.Find("@SoundRoot");
			if (_soundRoot == null)
			{
				_soundRoot = new GameObject { name = "@SoundRoot" };
				UnityEngine.Object.DontDestroyOnLoad(_soundRoot);

				string[] soundTypeNames = System.Enum.GetNames(typeof(Define.Sound));
				for (int count = 0; count < soundTypeNames.Length - 1; count++)
				{
					GameObject go = new GameObject { name = soundTypeNames[count] };
					_audioSources[count] = go.AddComponent<AudioSource>();
					go.transform.parent = _soundRoot.transform;
				}

				_audioSources[(int)Define.Sound.Bgm].loop = true;
				_audioSources[(int)Define.Sound.SubBgm].loop = true;

				_soundRoot.AddComponent<BgmStarter>();
			}
			else
			{
				// 기존 @SoundRoot 재사용 — _audioSources 재바인딩(누락 시 null 참조 방지)
				string[] soundTypeNames = System.Enum.GetNames(typeof(Define.Sound));
				for (int count = 0; count < soundTypeNames.Length - 1; count++)
				{
					Transform child = _soundRoot.transform.Find(soundTypeNames[count]);
					if (child != null) _audioSources[count] = child.GetComponent<AudioSource>();
				}
			}
		}
	}

	public void ApplyVolume()
	{
		AudioSource bgm = _audioSources[(int)Define.Sound.Bgm];
		if (bgm != null) bgm.volume = Managers.Game.BgmVolume;

		AudioSource subBgm = _audioSources[(int)Define.Sound.SubBgm];
		if (subBgm != null) subBgm.volume = Managers.Game.EffectVolume;

		AudioSource effect = _audioSources[(int)Define.Sound.Effect];
		if (effect != null) effect.volume = Managers.Game.EffectVolume;
	}

	// 볼륨 0은 Stop이 아니라 volume 0 — 슬라이더를 다시 올렸을 때 같은 지점에서 이어지게 하려면 소스가 계속 Play 상태여야 한다
	public void PlayBgm()
	{
		AudioSource audioSource = _audioSources[(int)Define.Sound.Bgm];
		if (audioSource == null) return;
		if (audioSource.isPlaying) return;

		Play(Define.Sound.Bgm, MainBgm);
		ApplyVolume();

		if (audioSource.clip == null)
			Debug.LogError($"[SoundManager] BGM 클립 로드 실패 — Resources/Sounds/{MainBgm}");
	}

    public void Clear()
    {
        foreach (AudioSource audioSource in _audioSources)
            audioSource.Stop();
        _audioClips.Clear();
    }

	public void Play(Define.Sound type)
    {
		AudioSource audioSource = _audioSources[(int)type];
		audioSource.Play();
	}

    public void Play(Define.Sound type, string key, float pitch = 1.0f)
    {
		
        AudioSource audioSource = _audioSources[(int)type];
        if (audioSource == null) return;   // 씬 전환 중 호출 등으로 미바인딩 시 무시

        if (type == Define.Sound.Bgm)
        {
			LoadAudioClip(key, (audioClip) =>
			{
				if (audioSource.isPlaying)
					audioSource.Stop();

				audioSource.clip = audioClip;
				audioSource.volume = Managers.Game.BgmVolume;
				audioSource.Play();
			});
        }
		else if (type == Define.Sound.SubBgm)
        {
            LoadAudioClip(key, (audioClip) =>
            {
                if (audioSource.isPlaying)
                    audioSource.Stop();

                audioSource.clip = audioClip;
				audioSource.volume = Managers.Game.EffectVolume;
				audioSource.Play();
            });
        }
        else
        {
            LoadAudioClip(key, (audioClip) =>
			{
				audioSource.pitch = pitch;
				audioSource.volume = Managers.Game.EffectVolume;
				audioSource.PlayOneShot(audioClip);
			});
        }
	}

	// 대량 적 동시 발생 시 같은 효과음 폭주 방지 (minInterval 내 중복 호출 무시)
	public void PlayEffect(string key, float minInterval = 0.05f)
	{
		float now = Time.unscaledTime;
		if (_lastEffectTime.TryGetValue(key, out float last) && now - last < minInterval)
			return;
		_lastEffectTime[key] = now;
		Play(Define.Sound.Effect, key);
	}

	public void Play(Define.Sound type, AudioClip audioClip, float pitch = 1.0f)
	{
		AudioSource audioSource = _audioSources[(int)type];
		if (audioSource == null) return;   // 씬 전환 중 호출 등으로 미바인딩 시 무시

		if (type == Define.Sound.Bgm)
		{
			if (audioSource.isPlaying)
				audioSource.Stop();

			audioSource.clip = audioClip;
			audioSource.volume = Managers.Game.BgmVolume;
			audioSource.Play();
		}
		else if(type == Define.Sound.SubBgm)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();

            audioSource.clip = audioClip;
			audioSource.volume = Managers.Game.EffectVolume;
			audioSource.Play();
        }
		else
		{
			audioSource.pitch = pitch;
			audioSource.volume = Managers.Game.EffectVolume;
			audioSource.PlayOneShot(audioClip);
		}
	}

	public void Stop(Define.Sound type)
    {
		AudioSource audioSource = _audioSources[(int)type];
		audioSource.Stop();
    }

	private void LoadAudioClip(string key, Action<AudioClip> callback)
    {
        if (_audioClips.TryGetValue(key, out AudioClip cached))
        {
            callback?.Invoke(cached);
            return;
        }

        AudioClip audioClip = Managers.Resource.Load<AudioClip>($"Sounds/{key}");
        if (audioClip != null)
        {
            _audioClips[key] = audioClip;
            callback?.Invoke(audioClip);
        }
    }
}
