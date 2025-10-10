using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Audio Sources")]
    public AudioSource ambientSource;
    public AudioSource musicSource;
    public AudioSource effectsSource;
    public AudioSource voiceSource;

    [Header("Player Sounds")]
    public AudioClip playerWalk;
    public AudioClip playerRun;
    public AudioClip playerAttack;
    public AudioClip playerParry;
    public AudioClip playerDash;
    public AudioClip playerHurt;
    public AudioClip playerDeath;
    public AudioClip playerDialogueLetter;

    [Header("Enemy Sounds")]
    public AudioClip enemyWalk;
    public AudioClip enemyRun;
    public AudioClip enemyAttackMelee;
    public AudioClip enemyAttackRanged;
    public AudioClip enemyHurt;
    public AudioClip enemyDeath;

    [Header("Ambiance")]
    public AudioClip villageAmbience;
    public AudioClip worldAmbience;
    public AudioClip dungeonAmbience;

    [Header("UI/Menu Sounds")]
    public AudioClip uiMove;
    public AudioClip uiClick;
    public AudioClip uiOpen;
    public AudioClip uiClose;

    [Header("Shop Sounds")]
    public AudioClip shopBuy;
    public AudioClip shopRefuse;

    [Header("Item Sounds")]
    public AudioClip itemPickup;

    public AudioClip dialogueSound;

    public float MusicVolume { get; private set; } = 1f;
    public float EffectsVolume { get; private set; } = 1f;
    public float VoiceVolume { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (ambientSource == null)
                ambientSource = gameObject.AddComponent<AudioSource>();
            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();
            if (effectsSource == null)
                effectsSource = gameObject.AddComponent<AudioSource>();
            if (voiceSource == null)
                voiceSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- Player ---
    public void PlayPlayerWalk() => PlayEffect(playerWalk);
    public void PlayPlayerRun() => PlayEffect(playerRun);
    public void PlayPlayerAttack() => PlayEffect(playerAttack);
    public void PlayPlayerParry() => PlayEffect(playerParry);
    public void PlayPlayerDash() => PlayEffect(playerDash);
    public void PlayPlayerHurt() => PlayEffect(playerHurt);
    public void PlayPlayerDeath() => PlayEffect(playerDeath);
    public void PlayPlayerDialogueLetter() => PlayEffect(playerDialogueLetter);

    // --- Enemy ---
    public void PlayEnemyWalk() => PlayEffect(enemyWalk);
    public void PlayEnemyRun() => PlayEffect(enemyRun);
    public void PlayEnemyAttackMelee() => PlayEffect(enemyAttackMelee);
    public void PlayEnemyAttackRanged() => PlayEffect(enemyAttackRanged);
    public void PlayEnemyHurt() => PlayEffect(enemyHurt);
    public void PlayEnemyDeath() => PlayEffect(enemyDeath);

    // --- Ambiance ---
    public void PlayVillageAmbience() => PlayAmbient(villageAmbience);
    public void PlayWorldAmbience() => PlayAmbient(worldAmbience);
    public void PlayDungeonAmbience() => PlayAmbient(dungeonAmbience);

    // --- UI/Menu ---
    public void PlayUIMove() => PlayEffect(uiMove);
    public void PlayUIClick() => PlayEffect(uiClick);
    public void PlayUIOpen() => PlayEffect(uiOpen);
    public void PlayUIClose() => PlayEffect(uiClose);

    // --- Shop ---
    public void PlayShopBuy() => PlayEffect(shopBuy);
    public void PlayShopRefuse() => PlayEffect(shopRefuse);

    // --- Item ---
    public void PlayItemPickup() => PlayEffect(itemPickup);

    // --- Effet générique ---
    public void PlayEffect(AudioClip clip)
    {
        if (clip != null && effectsSource != null)
            effectsSource.PlayOneShot(clip);
    }

    // --- Ambiance générique ---
    public void PlayAmbient(AudioClip clip)
    {
        if (ambientSource == null || clip == null)
            return;

        if (ambientSource.isPlaying && ambientSource.clip == clip)
            return;

        ambientSource.Stop();
        ambientSource.clip = clip;
        ambientSource.loop = true;
        ambientSource.Play();
    }
    public void StopAmbient()
    {
        if (ambientSource != null)
            ambientSource.Stop();
    }

    // --- Musique ---
    public void PlayMusic(AudioClip clip)
    {
        if (clip != null && musicSource != null)
        {
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    // --- Voix ---
    public void PlayVoice(AudioClip clip)
    {
        if (clip != null && voiceSource != null)
            voiceSource.PlayOneShot(clip);
    }

    // --- Réglages volumes (liés aux sliders) ---
    public void SetMusicVolume(float value)
    {
        MusicVolume = value;
        if (musicSource != null) musicSource.volume = value;
    }
    public void SetEffectsVolume(float value)
    {
        EffectsVolume = value;
        if (effectsSource != null) effectsSource.volume = value;
    }
    public void SetVoiceVolume(float value)
    {
        VoiceVolume = value;
        if (voiceSource != null) voiceSource.volume = value;
    }
    public void PlayDialogueSound()
    {
        PlayEffect(dialogueSound);
    }
}
