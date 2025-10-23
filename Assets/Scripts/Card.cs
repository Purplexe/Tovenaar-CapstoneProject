using UnityEngine;

public class Card : MonoBehaviour
{
    public string CardName;
    public int Attack;
    public int Health;
    public int Cost = 2;
    public Side Owner;

    public void TakeDamage(int dmg) { Health -= Mathf.Max(0, dmg); }
    public bool IsDead() => Health <= 0;
}
