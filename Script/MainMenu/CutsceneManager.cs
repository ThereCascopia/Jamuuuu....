using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    [Header("Cutscene Settings")]
    public PlayableDirector cutsceneTimeline;
    public GameObject skipButton;
    public string gameSceneName = "GameScene";
    public float fadeOutTime = 1.0f;

    [Header("Optional Components")]
    public Animator fadeAnimator;
    public string fadeOutTrigger = "FadeOut";

    private bool isSkipping = false;

    void Start()
    {
        // Periksa status cutscene
        bool hasWatched = CheckCutsceneStatus();

        if (hasWatched)
        {
            // Jika sudah pernah menonton, langsung skip
            SkipCutscene();
        }
        else
        {
            // Putar cutscene
            PlayCutscene();
        }

        // Setup tombol skip jika ada
        if (skipButton != null)
        {
            skipButton.SetActive(true);
        }
    }

    // Periksa apakah cutscene sudah pernah ditonton
    private bool CheckCutsceneStatus()
    {
        bool hasWatched = false;

        if (GameManager.instance != null)
        {
            hasWatched = GameManager.instance.HasWatchedCutscene();
        }
        else
        {
            // Cara alternatif jika GameManager belum tersedia
            DataGame data = ManagerPP<DataGame>.Get("datagame");
            hasWatched = data.hasWatchedCutscene;
        }

        Debug.Log("Cutscene status check: " + hasWatched);
        return hasWatched;
    }

    // Putar cutscene
    private void PlayCutscene()
    {
        if (cutsceneTimeline != null)
        {
            // Tambahkan listener untuk event selesai
            cutsceneTimeline.stopped += OnCutsceneFinished;
            cutsceneTimeline.Play();
        }
        else
        {
            Debug.LogWarning("Timeline not assigned to CutsceneController!");
            // Jika tidak ada timeline, langsung ke game scene
            FinishCutscene();
        }
    }

    // Event handler untuk cutscene selesai
    private void OnCutsceneFinished(PlayableDirector director)
    {
        // Hapus event listener
        cutsceneTimeline.stopped -= OnCutsceneFinished;

        // Selesaikan cutscene
        FinishCutscene();
    }

    // Tombol untuk melewati cutscene
    public void SkipButtonPressed()
    {
        if (!isSkipping)
        {
            SkipCutscene();
        }
    }

    // Melewati cutscene
    private void SkipCutscene()
    {
        if (isSkipping) return;
        isSkipping = true;

        // Stop cutscene jika sedang diputar
        if (cutsceneTimeline != null)
        {
            cutsceneTimeline.stopped -= OnCutsceneFinished;
            cutsceneTimeline.Stop();
        }

        // Tampilkan fade out jika ada
        if (fadeAnimator != null)
        {
            fadeAnimator.SetTrigger(fadeOutTrigger);
            StartCoroutine(FinishAfterFade());
        }
        else
        {
            // Langsung selesaikan
            FinishCutscene();
        }
    }

    // Menunggu animasi fade selesai
    private IEnumerator FinishAfterFade()
    {
        yield return new WaitForSeconds(fadeOutTime);
        FinishCutscene();
    }

    // Selesaikan cutscene dan pindah ke game scene
    private void FinishCutscene()
    {
        // Tandai cutscene sudah ditonton
        SetCutsceneWatched();

        // Pindah ke game scene
        SceneManager.LoadScene(gameSceneName);
    }

    // Set status cutscene sudah ditonton
    private void SetCutsceneWatched()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.SetCutsceneWatched();
        }
        else
        {
            // Cara alternatif jika GameManager belum tersedia
            DataGame data = ManagerPP<DataGame>.Get("datagame");
            data.hasWatchedCutscene = true;
            ManagerPP<DataGame>.Set("datagame", data);

            Debug.Log("Set cutscene watched: true");
        }
    }
}