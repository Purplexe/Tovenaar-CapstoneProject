using UnityEngine;

public class Card : MonoBehaviour
{
    public string CardName;
    public int Attack;
    public int Health;
    public int Cost = 2;
    public PlayerSide Owner = PlayerSide.Player1;

    public void TakeDamage(int dmg) { Health -= dmg; }
    public bool IsDead => Health <= 0;
}
