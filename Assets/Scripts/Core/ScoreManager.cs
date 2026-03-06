using UnityEngine;

namespace EmersynRunner.Core
{
    /// <summary>
    /// Handles score persistence, streak tracking, and leaderboard data.
    /// Works alongside GameManager for score calculation.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        private const string KEY_HIGH_SCORE = "HighScore";
        private const string KEY_TOTAL_COINS = "TotalCoins";
        private const string KEY_TOTAL_RUNS = "TotalRuns";
        private const string KEY_BEST_DISTANCE = "BestDistance";

        public int HighScore => PlayerPrefs.GetInt(KEY_HIGH_SCORE, 0);
        public int TotalCoins => PlayerPrefs.GetInt(KEY_TOTAL_COINS, 0);
        public int TotalRuns => PlayerPrefs.GetInt(KEY_TOTAL_RUNS, 0);
        public float BestDistance => PlayerPrefs.GetFloat(KEY_BEST_DISTANCE, 0f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Save end-of-run stats. Returns true if new high score.
        /// </summary>
        public bool SaveRunStats(int score, int coins, float distance)
        {
            bool isNewHighScore = score > HighScore;

            if (isNewHighScore)
            {
                PlayerPrefs.SetInt(KEY_HIGH_SCORE, score);
            }

            if (distance > BestDistance)
            {
                PlayerPrefs.SetFloat(KEY_BEST_DISTANCE, distance);
            }

            PlayerPrefs.SetInt(KEY_TOTAL_COINS, TotalCoins + coins);
            PlayerPrefs.SetInt(KEY_TOTAL_RUNS, TotalRuns + 1);
            PlayerPrefs.Save();

            return isNewHighScore;
        }

        /// <summary>
        /// Spend coins (for shop). Returns true if successful.
        /// </summary>
        public bool SpendCoins(int amount)
        {
            if (amount > TotalCoins) return false;
            PlayerPrefs.SetInt(KEY_TOTAL_COINS, TotalCoins - amount);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>
        /// Reset all saved stats.
        /// </summary>
        public void ResetAll()
        {
            PlayerPrefs.DeleteKey(KEY_HIGH_SCORE);
            PlayerPrefs.DeleteKey(KEY_TOTAL_COINS);
            PlayerPrefs.DeleteKey(KEY_TOTAL_RUNS);
            PlayerPrefs.DeleteKey(KEY_BEST_DISTANCE);
            PlayerPrefs.Save();
        }
    }
}
