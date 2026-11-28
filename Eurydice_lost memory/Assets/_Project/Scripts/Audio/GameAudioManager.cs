using UnityEngine;

namespace Eurydice.Gameplay.Audio
{
    [DisallowMultipleComponent]
    public sealed class GameAudioManager : MonoBehaviour
    {
        [Header("Background Music")]
        [SerializeField] private AudioClip startingBgm;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

        [Header("Sound Effects")]
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

        private AudioSource musicSource;
        private AudioSource sfxSource;

        private void Awake()
        {
            musicSource = CreateSource("BGM", musicVolume, true);
            sfxSource = CreateSource("SFX", sfxVolume, false);
        }

        private void Start()
        {
            if (startingBgm != null && musicSource.clip == null)
                PlayBgm(startingBgm);
        }

        public void PlayBgm(AudioClip clip)
        {
            if (clip == null || musicSource == null)
                return;

            if (musicSource.clip == clip && musicSource.isPlaying)
                return;

            musicSource.clip = clip;
            musicSource.Play();
        }

        public void StopBgm()
        {
            if (musicSource == null)
                return;

            musicSource.Stop();
            musicSource.clip = null;
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
                sfxSource.PlayOneShot(clip);
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
                musicSource.volume = musicVolume;
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null)
                sfxSource.volume = sfxVolume;
        }

        private AudioSource CreateSource(string objectName, float volume, bool loop)
        {
            var channel = new GameObject(objectName);
            channel.transform.SetParent(transform, false);

            AudioSource source = channel.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = volume;
            source.loop = loop;
            return source;
        }
    }
}
