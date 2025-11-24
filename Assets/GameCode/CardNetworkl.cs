using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CardNetwork : NetworkBehaviour
{
    [Header("Base Stats (set on server before spawn)")]
    public int baseAttack = 2;
    public int baseHealth = 3;
    public string cardName;

    [Header("Visuals")]
    public Image artImage;
    public TMP_Text nameText;
    public TMP_Text attackText;
    public TMP_Text healthText;

    public NetworkVariable<int> Attack = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Health = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Attack.Value = baseAttack;
            Health.Value = baseHealth;
        }

        Attack.OnValueChanged += OnAttackChanged;
        Health.OnValueChanged += OnHealthChanged;

        if (nameText != null)
            nameText.text = cardName;

        // Init UI
        OnAttackChanged(0, Attack.Value);
        OnHealthChanged(0, Health.Value);
    }

    void OnDestroy()
    {
        Attack.OnValueChanged -= OnAttackChanged;
        Health.OnValueChanged -= OnHealthChanged;
    }

    void OnAttackChanged(int oldVal, int newVal)
    {
        if (attackText != null)
            attackText.text = newVal.ToString();
    }

    void OnHealthChanged(int oldVal, int newVal)
    {
        if (healthText != null)
            healthText.text = newVal.ToString();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        if (damage <= 0) return;

        Health.Value -= damage;
        if (Health.Value <= 0)
        {
            NetworkObject.Despawn();
        }
    }
}
