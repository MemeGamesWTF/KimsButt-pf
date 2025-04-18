using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class GameManager : MonoBehaviour
{
    public Slider fillBar; // Reference to the UI Slider that acts as the fill bar.
    public GameObject hitEffectPrefab; // Prefab for the hit effect image.
    public GameObject centerEffectPrefab; // Prefab for the center effect when hitting.
    public GameObject baseGameObject; // The main game object to be tapped.
    public GameObject cooldownGameObject; // The alternate game object to show during cooldown.
    public GameObject startPanel; // The start panel to display initially.
    public Text scoreText; // Reference to the UI Text that displays the score.
    public Button playButton; // Reference to the play button on the start panel.
    public Button restartButton; // Reference to the restart button.
    public AudioSource primaryHitSound; // Reference to the primary hit sound.
    public AudioSource secondaryHitSound; // Reference to the secondary hit sound.
    public SpriteRenderer playerSpriteRenderer; // Reference to the SpriteRenderer for the player.
    public List<Sprite> playerLevelSprites; // List of sprites for each level.
    public float fillSpeed = 0.1f; // How quickly the bar fills per tap.
    public float drainSpeed = 0.05f; // How quickly the bar drains when idle.
    public int scoreIncrement = 10; // Amount by which the score increases per tap.
    public int scoreDecrement = 1; // Amount by which the score decreases when idle.
    public float cooldownTime = 3f; // Cooldown duration after each hit.

    private float currentFill = 0f; // Current fill amount of the bar (0 to 1).
    private int currentScore = 0; // Current player score.
    private bool isCooldown = false; // Flag to track if cooldown is active.
    private int currentLevel = 0; // Current level based on the slider.

    public GameObject instructionPanel;
    private bool isPrimaryAudioPlaying = false; // Flag to track if primary hit sound is playing.
    private float idleTime = 0f; // Time since the last tap.

    private bool hasFinalScoreProcessed = false;

    [DllImport("__Internal")]
  private static extern void SendScore(int score, int game);

    void Start()
    {
        Time.timeScale = 0f; // Pause the game initially.

        if (fillBar != null)
        {
            fillBar.value = 0f; // Initialize the bar as empty.
        }

        if (scoreText != null)
        {
            scoreText.text = "0"; // Initialize the score display.
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false); // Hide the restart button initially.
            restartButton.onClick.AddListener(RestartGame); // Add listener to the button.
        }

        if (playButton != null && startPanel != null)
        {
            playButton.onClick.AddListener(StartGame); // Add listener to the play button.
        }

        if (baseGameObject != null && cooldownGameObject != null)
        {
            baseGameObject.SetActive(true); // Show the base game object initially.
            cooldownGameObject.SetActive(false); // Hide the cooldown game object initially.
        }

        UpdatePlayerSprite(); // Set the initial player sprite.
    }

    void Update()
    {
        if (isCooldown) return; // Skip updates if in cooldown.

        // Handle slider and score reduction when idle.
        idleTime += Time.deltaTime;
        if (idleTime > 0.1f) // Reduce slider and score every 0.1 seconds when idle.
        {
            ReduceFillBar();
            ReduceScore();
            idleTime = 0f; // Reset idle timer.
        }

        // Check for touch or mouse click.
        if (Input.GetMouseButtonDown(0))
        {
            idleTime = 0f; // Reset idle timer when tapping.

            Vector3 tapPosition = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(tapPosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit.collider != null && hit.collider.gameObject.tag == "point")
            {
                // Play the hit sounds.
                PlayHitSounds();

                // Increment the bar fill.
                IncreaseFillBar();

                // Increment the score.
                IncreaseScore();

                // Spawn the hit effect.
                SpawnHitEffect(tapPosition);

                // Spawn the center effect.
                SpawnCenterEffect();
            }
        }
    }
    public Animator baseAnimator; // Reference to the Animator component on the base game object.

    private void PlayHitSounds()
    {
        if (!isPrimaryAudioPlaying && primaryHitSound != null)
        {
            isPrimaryAudioPlaying = true; // Set flag to true when the audio starts.
            primaryHitSound.PlayOneShot(primaryHitSound.clip); // Play the primary hit sound.
            StartCoroutine(ResetPrimaryAudioFlag(primaryHitSound.clip.length)); // Reset flag after the audio finishes.
            StartCoroutine(EnableCooldownObjectWhilePlayingSound());
        }

        if (secondaryHitSound != null)
        {
            secondaryHitSound.PlayOneShot(secondaryHitSound.clip); // Play the secondary hit sound.
        }

        if (baseAnimator != null)
        {
            baseAnimator.SetTrigger("Click"); // Trigger the "Click" animation.
        }
    }


    private IEnumerator ResetPrimaryAudioFlag(float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for the audio duration.
        isPrimaryAudioPlaying = false; // Reset the flag after the sound finishes.
    }

    private IEnumerator EnableCooldownObjectWhilePlayingSound()
    {
        if (cooldownGameObject != null)
        {
            //cooldownGameObject.SetActive(true); // Show the cooldown game object.
        }

        yield return new WaitForSeconds(3f); // Wait for the duration of the primary hit sound.

        if (cooldownGameObject != null)
        {
            //cooldownGameObject.SetActive(false); // Hide the cooldown game object after the sound finishes.
        }
    }

    private void IncreaseFillBar()
    {
        if (fillBar != null)
        {
            currentFill += fillSpeed;
            currentFill = Mathf.Clamp01(currentFill); // Keep the fill amount between 0 and 1.
            fillBar.value = currentFill;

            UpdatePlayerLevel();

            if (currentFill >= 1f && restartButton != null)
            {
                restartButton.gameObject.SetActive(true); // Show the restart button when the bar is full.

                if(!hasFinalScoreProcessed){
               
                ProcessFinalScore();

                }


               
            }
        }
    }

    private void ProcessFinalScore(){
        Debug.Log(currentScore);
        hasFinalScoreProcessed = true;
        SendScore(currentScore, 11);
         
    }

    private void ReduceFillBar()
    {
        if (currentFill < 1f)
        {
            if (fillBar != null)
            {
                currentFill -= drainSpeed * Time.deltaTime;
                currentFill = Mathf.Clamp01(currentFill); // Ensure the fill stays between 0 and 1.
                fillBar.value = currentFill;
                UpdatePlayerLevel();
            }
        }
    }

    private void IncreaseScore()
    {
        currentScore += scoreIncrement; // Increment the score.

        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString(); // Update the score display.
        }
    }

    private void ReduceScore()
    {
        // Only reduce the score if the fill bar is not full
        if (currentFill < 1f)
        {
            currentScore -= scoreDecrement;
            currentScore = Mathf.Max(0, currentScore); // Ensure the score does not go below zero.

            if (scoreText != null)
            {
                scoreText.text = currentScore.ToString();
            }

        }
        else
        {
            cooldownGameObject.SetActive(true);
        }
                
    }


    private void UpdatePlayerLevel()
    {
        if (playerLevelSprites == null || playerLevelSprites.Count == 0) return;

        int newLevel = Mathf.FloorToInt(currentFill * playerLevelSprites.Count);
        newLevel = Mathf.Clamp(newLevel, 0, playerLevelSprites.Count - 1);

        if (newLevel != currentLevel)
        {
            currentLevel = newLevel;
            UpdatePlayerSprite();
        }
    }

    private void UpdatePlayerSprite()
    {
        if (playerSpriteRenderer != null && playerLevelSprites != null && currentLevel < playerLevelSprites.Count)
        {
            playerSpriteRenderer.sprite = playerLevelSprites[currentLevel];
        }
    }

    private void SpawnHitEffect(Vector3 tapPosition)
    {
        if (hitEffectPrefab != null)
        {
            // Convert screen position to world position.
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(tapPosition);
            worldPosition.z = 0; // Ensure the hit effect is on the correct plane.

            // Create the hit effect instance.
            GameObject hitEffect = Instantiate(hitEffectPrefab, worldPosition, Quaternion.identity);

            // Start coroutine to fade and destroy the effect.
            StartCoroutine(FadeAndDestroyEffect(hitEffect));
        }
    }

    private void SpawnCenterEffect()
    {
        if (centerEffectPrefab != null)
        {
            // Spawn the effect at the center of the screen.
            GameObject centerEffect = Instantiate(centerEffectPrefab, baseGameObject.transform.position, Quaternion.identity);

            // Add a Rigidbody2D to make it fall.
            Rigidbody2D rb = centerEffect.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1.0f; // Adjust gravity as needed.

            // Start coroutine to fade and destroy the effect.
            StartCoroutine(FadeAndDestroyEffect(centerEffect));
        }
    }

    private IEnumerator FadeAndDestroyEffect(GameObject effectObject)
    {
        SpriteRenderer spriteRenderer = effectObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            float fadeDuration = 0.2f; // Duration of the fade effect.
            float fadeStep = Time.deltaTime / fadeDuration;

            while (color.a > 0)
            {
                color.a -= fadeStep;
                spriteRenderer.color = color;
                yield return null;
            }
        }

        Destroy(effectObject); // Destroy the effect after fading.
    }

    private void StartGame()
    {
        if (startPanel != null)
        {
            startPanel.SetActive(false); // Hide the start panel.
        }
        Time.timeScale = 1f; // Resume the game.
    }

    private void RestartGame()
    {
        currentFill = 0f; // Reset the fill amount.
        if (fillBar != null)
        {
            fillBar.value = 0f;
        }

        currentScore = 0; // Reset the score.
        hasFinalScoreProcessed = false;
        if (scoreText != null)
        {
            scoreText.text = "0";
        }

        currentLevel = 0; // Reset the player level.
        UpdatePlayerSprite(); // Reset the player sprite.

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false); // Hide the restart button.
        }

        if (baseGameObject != null && cooldownGameObject != null)
        {
            baseGameObject.SetActive(true); // Ensure the base game object is active.
            cooldownGameObject.SetActive(false); // Ensure the cooldown game object is inactive.
        }

        isCooldown = false; // Reset cooldown state.
    }

    public void instructionActive(bool value)
    {
        instructionPanel.SetActive(value);
    }
}