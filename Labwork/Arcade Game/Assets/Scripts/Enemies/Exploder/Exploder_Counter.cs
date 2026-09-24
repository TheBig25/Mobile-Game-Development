using TMPro;
using UnityEngine;

public class ExploderCounter : Enemy_Exploder
{
    public TextMeshProUGUI counterText;
    
    public GameObject exploderEnemy;

    private Animator exploderAnimator;

    void Start()
    {
        exploderAnimator = exploderEnemy.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {

        enemyLifeTime -= Time.deltaTime;

        if (enemyLifeTime <= 0)
        {
            exploderAnimator.SetTrigger("isDead");
            //Kill itself after lifetime expires
            Destroy(this.gameObject, 0.25f);
        }

        counterText.text = enemyLifeTime.ToString("F0");
    }
}
