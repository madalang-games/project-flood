using System.Collections;
using UnityEngine;

namespace Game.Core.UI
{
    public class UIStarPop : MonoBehaviour
    {
        private const float PopDuration  = 0.35f;
        private const float StarDelay    = 0.4f;

        public IEnumerator PlayStarSequence(GameObject[] stars, int filledCount)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                if (i < filledCount)
                    yield return PopStar(stars[i]);
                else
                    stars[i].transform.localScale = Vector3.one;

                if (i < stars.Length - 1)
                    yield return new WaitForSeconds(StarDelay);
            }
        }

        private IEnumerator PopStar(GameObject star)
        {
            var rt = star.GetComponent<RectTransform>();
            if (rt == null) yield break;

            float elapsed = 0f;
            while (elapsed < PopDuration)
            {
                elapsed += Time.deltaTime;
                float t = UIEasing.EaseOutBack(Mathf.Clamp01(elapsed / PopDuration));
                rt.localScale = Vector3.one * Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }
    }
}
