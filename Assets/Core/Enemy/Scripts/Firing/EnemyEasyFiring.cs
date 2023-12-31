using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyEasyFiring : EnemyFiring
{
    // Start is called before the first frame update
    void Start()
    {
        InitFiring();
    }

    private void FixedUpdate()
    {
        currentPhase = (Phase)gameObject.GetComponent<EnemyMovement>().GetPhase();
        if(currentPhase == Phase.Phase1 && isFiringSequencePlaying == false)
        {
            isFiringSequencePlaying = true;
            InitSequence();
            firingSequence.Play();
        }
    }

    protected override void InitSequence()
    {
        firingSequence = DOTween.Sequence();
        firingSequence.AppendInterval(fireDelay);
        firingSequence.AppendCallback(() => Fire());
        firingSequence.AppendInterval(fireRate);
        firingSequence.AppendCallback(() => Fire(true));
    }
}
