using System.Collections;
using UnityEngine;

public class RandomAnimationStatePlayer : MonoBehaviour
{
    public Animator animator;
    public string[] animationStates;
    public float minDelay = 0.5f;
    public float maxDelay = 2f;

    private int lastPlayedIndex = -1;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animationStates.Length > 0)
            StartCoroutine(PlayAnimationsSequentially());
        
    }

    private IEnumerator PlayAnimationsSequentially()
    {
        while (true)
        {
            // Выбираем случайную анимацию, не повторяя последнюю
            int index;
            do
            {
                index = Random.Range(0, animationStates.Length);
            } while (index == lastPlayedIndex && animationStates.Length > 1);

            lastPlayedIndex = index;
            string stateName = animationStates[index];

            animator.CrossFade(stateName, 0.3f);

            // Ждем, пока текущая анимация закончится
            yield return WaitForAnimationToEnd(stateName);

            // Дополнительная задержка между анимациями
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator WaitForAnimationToEnd(string stateName)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Ждём пока анимация не начнётся
        while (!stateInfo.IsName(stateName))
        {
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            yield return null;
        }

        // Ждём пока она проиграется полностью
        while (stateInfo.normalizedTime < 1f)
        {
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            yield return null;
        }
    }
}
